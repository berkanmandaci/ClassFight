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
    
    protected Animator animator;
    protected Camera mainCamera;
    protected int currentDashStacks;
    protected float lastDashTime;
    protected bool canMove = true;

    [Networked] protected NetworkButtons ButtonsPrevious { get; set; }
    [Networked] public float Health { get; set; } = 100f;
    [Networked] public bool IsDead { get; set; }
    [Networked] public PlayerRef Owner { get; set; }
    [Networked] protected Vector3 NetworkedPosition { get; set; }
    [Networked] protected Quaternion NetworkedRotation { get; set; }

    // Animator hash IDs
    protected readonly int MovementXHash = Animator.StringToHash("MovementX");
    protected readonly int MovementZHash = Animator.StringToHash("MovementZ");
    protected readonly int AttackHash = Animator.StringToHash("Attack");
    protected readonly int DodgeHash = Animator.StringToHash("Dodge");

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        currentDashStacks = maxDashStacks;
    }

    public override void Spawned()
    {
        NetworkedPosition = transform.position;
        NetworkedRotation = transform.rotation;

        if (Object.HasInputAuthority)
        {
            ArenaManager.Instance.SetupLocalPlayerCamera(Object);
        }
    }

    public override void FixedUpdateNetwork()
    {
      
        if (IsDead || !canMove) return;
        
     
        if (GetInput(out NetworkInputData input))
        {
            // Mouse'a bakış - Her zaman fareyi takip et
            
            if (HasStateAuthority)
            {
                UpdateRotation(input.RotationInput);
            }

            // Hareket - Dünya koordinatlarında sabit yönler
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

        // Pozisyon ve rotasyonu networked değerlerle senkronize et
        // if (HasStateAuthority)
        // {
        //     NetworkedPosition = transform.position;
        //     NetworkedRotation = transform.rotation;
        // }
        // else
        // {
        //     transform.position = NetworkedPosition;
        //     transform.rotation = NetworkedRotation;
        // }
    }

    protected void UpdateRotation(Vector2 mousePosition)
    {
        if (mainCamera == null) return;

        // Mouse pozisyonunu world space'e çevir
        Vector3 mouseWorldPosition = GetMouseWorldPosition(mousePosition);
        
        // Karakterden mouse'a doğru yön vektörü
        Vector3 lookDirection = (mouseWorldPosition - transform.position).normalized;
        lookDirection.y = 0;

        if (lookDirection != Vector3.zero)
        {
            // Karakteri direkt olarak mouse'a döndür
            NetworkedRotation = Quaternion.Lerp(
                NetworkedRotation,
                Quaternion.LookRotation(lookDirection),
                rotationSpeed * Runner.DeltaTime
            );
            transform.rotation = NetworkedRotation;
        }
    }

    protected void UpdateMovement(Vector2 input)
    {
        // Dünya koordinatlarında hareket yönü (W = Z+, S = Z-, A = X-, D = X+)
        Vector3 worldMoveDirection = new Vector3(input.x, 0, input.y);

        if (worldMoveDirection != Vector3.zero && HasStateAuthority)
        {
            // Dünya koordinatlarında hareket et
            Vector3 movement = worldMoveDirection * moveSpeed * Runner.DeltaTime;
            NetworkedPosition += movement;
            transform.position = NetworkedPosition;

            // Hareket yönünü karakterin local space'ine çevir (sadece animasyon için)
            Vector3 localMoveDirection = transform.InverseTransformDirection(worldMoveDirection);

            // Animator parametrelerini local space'e göre güncelle
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

        NetworkedPosition += direction * dashForce;
        transform.position = NetworkedPosition;
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
