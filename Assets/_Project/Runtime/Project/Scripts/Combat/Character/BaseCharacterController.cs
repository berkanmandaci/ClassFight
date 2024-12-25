using UnityEngine;
using Fusion;
using UniRx;
using System;

public abstract class BaseCharacterController : NetworkBehaviour
{
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float dashForce = 10f;
    [SerializeField] protected float dashCooldown = 2f;
    [SerializeField] protected int maxDashStacks = 2;
    [SerializeField] protected float rotationSpeed = 15f;
    
    protected NetworkCharacterController characterController;
    protected Animator animator;
    protected Camera mainCamera;
    protected int currentDashStacks;
    protected float lastDashTime;
    protected bool canMove = true;
    
    [Networked] public float Health { get; set; } = 100f;
    [Networked] public bool IsDead { get; set; }
    [Networked] public PlayerRef Owner { get; set; }

    // Animator hash IDs
    protected readonly int MovementXHash = Animator.StringToHash("MovementX");
    protected readonly int MovementZHash = Animator.StringToHash("MovementZ");
    protected readonly int AttackHash = Animator.StringToHash("Attack");
    protected readonly int DodgeHash = Animator.StringToHash("Dodge");

    protected virtual void Awake()
    {
        characterController = GetComponent<NetworkCharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        currentDashStacks = maxDashStacks;
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead || !canMove) return;
        
        if (GetInput(out NetworkInputData input))
        {
            // Mouse'a göre dönüş - Her zaman mouse'u takip et
            UpdateRotation(input.RotationInput);

            // Hareket - Karakterin baktığı yöne göre relative
            UpdateMovement(input.MovementInput);

            // Dash
            if (input.DashPressed && currentDashStacks > 0 && Time.time >= lastDashTime + dashCooldown)
            {
                Vector3 moveDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y);
                Dash(moveDirection != Vector3.zero ? moveDirection : transform.forward);
            }

            // Attack
            if (input.AttackPressed)
            {
                Attack();
            }

            // Dodge
            if (input.DodgePressed)
            {
                Dodge();
            }
        }
    }

    protected void UpdateRotation(Vector2 mousePosition)
    {
        Vector3 mouseWorldPosition = GetMouseWorldPosition(mousePosition);
        Vector3 lookDirection = (mouseWorldPosition - transform.position).normalized;
        lookDirection.y = 0;

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime);
        }
    }

    protected void UpdateMovement(Vector2 input)
    {
        // Dünya koordinatlarında hareket yönünü hesapla
        Vector3 moveDirection = new Vector3(input.x, 0, input.y);
        
        if (moveDirection != Vector3.zero)
        {
            // Dünya koordinatlarında hareketi uygula
            characterController.Move(moveDirection * moveSpeed * Runner.DeltaTime);

            // Hareket yönünü karakterin local space'ine çevir (animasyon için)
            Vector3 localMoveDirection = transform.InverseTransformDirection(moveDirection);

            // Animator parametrelerini güncelle
            if (animator != null)
            {
                animator.SetFloat(MovementXHash, localMoveDirection.x);
                animator.SetFloat(MovementZHash, localMoveDirection.z);
            }
        }
        else
        {
            // Hareket yoksa animator parametrelerini sıfırla
            if (animator != null)
            {
                animator.SetFloat(MovementXHash, 0);
                animator.SetFloat(MovementZHash, 0);
            }
        }
    }

    protected Vector3 GetMouseWorldPosition(Vector2 mousePosition)
    {
        if (mainCamera == null) return transform.position + transform.forward;

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return transform.position + transform.forward;
    }

    protected virtual void Dash(Vector3 direction)
    {
        if (!HasStateAuthority) return;

        characterController.Move(direction * dashForce);
        currentDashStacks--;
        lastDashTime = Time.time;

        // Reset dash stack after cooldown
        Observable.Timer(TimeSpan.FromSeconds(dashCooldown))
            .Subscribe(_ => 
            {
                if (currentDashStacks < maxDashStacks)
                    currentDashStacks++;
            })
            .AddTo(this);
    }

    protected abstract void Attack();
    protected abstract void Dodge();

    public virtual void TakeDamage(float damage)
    {
        if (!HasStateAuthority) return;
        
        Health -= damage;
        if (Health <= 0)
        {
            IsDead = true;
            Die();
        }
    }

    protected virtual void Die()
    {
        gameObject.SetActive(false);
    }
}
