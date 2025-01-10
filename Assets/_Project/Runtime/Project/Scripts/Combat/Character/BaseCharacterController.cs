using UnityEngine;
using Fusion;
using _Project.Runtime.Project.Service.Scripts.Model;
using System;

public abstract class BaseCharacterController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float rotationSpeed = 15f;

    [Header("Dash Settings")]
    [SerializeField] protected float dashForce = 10f;
    [SerializeField] protected float dashCooldown = 2f;
    [SerializeField] protected int maxDashStacks = 2;
    [SerializeField] private CharacterUI characterUI;

    protected Animator animator;
    protected Camera mainCamera;
    protected bool canMove = true;

    // Cooldown değişkenleri
    [Networked] protected TickTimer dashCooldownTimer { get; set; }
    [Networked] protected int currentDashStacks { get; set; }

    // Networked değişkenler
    [Networked] protected NetworkButtons ButtonsPrevious { get; set; }
    [Networked] public float Health { get; set; } = 100f;
    [Networked] public bool IsDead { get; set; }
    [Networked] public PlayerRef Owner { get; set; }
    [Networked] protected Vector3 NetworkedPosition { get; set; }
    [Networked] protected Quaternion NetworkedRotation { get; set; }

    private string _userId;
    public string UserId
    {
        get => _userId;
        private set => _userId = value;
    }

    [Networked] public string TeamId { get; set; }

    // Animator hash IDs
    protected readonly int MovementXHash = Animator.StringToHash("MovementX");
    protected readonly int MovementZHash = Animator.StringToHash("MovementZ");
    protected readonly int AttackHash = Animator.StringToHash("Attack");
    protected readonly int DodgeHash = Animator.StringToHash("Dodge");

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
    }

    public override void Spawned()
    {
        NetworkedPosition = transform.position;
        NetworkedRotation = transform.rotation;
        currentDashStacks = maxDashStacks;

        if (Object.HasInputAuthority)
        {
            ArenaManager.Instance.SetupLocalPlayerCamera(Object);

            // Kendi bilgilerini tüm clientlara gönder
            var user = PvpArenaModel.Instance.PvpArenaVo.GetUser(ServiceModel.Instance.Session.UserId);
            RPC_SetUserId(user.Id);
        }
    }
    public void Init(string userId)
    {
        UserId = userId;
        characterUI.Init(UserId);
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead || !canMove) return;

        if (GetInput(out NetworkInputData input))
        {
            if (Object.HasInputAuthority)
            {
                // Hareket ve rotasyon
                UpdateMovement(input.movementInput);
                UpdateRotation(input.rotationInput);

                // Dash
                if (input.IsDashPressed && CanDash())
                {
                    Vector3 moveDirection = new Vector3(input.movementInput.x, 0, input.movementInput.y);
                    Dash(moveDirection != Vector3.zero ? moveDirection : transform.forward);
                }

                // Attack
                if (input.IsAttackPressed)
                {
                    Attack();
                }

                // Dodge
                if (input.IsDodgePressed)
                {
                    Dodge();
                }
            }
        }

        // Pozisyon ve rotasyon senkronizasyonu
        if (Object.HasStateAuthority)
        {
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, NetworkedPosition, Runner.DeltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, NetworkedRotation, Runner.DeltaTime * 10f);
        }

        // Dash stack yenileme kontrolü
        if (Object.HasStateAuthority && currentDashStacks < maxDashStacks && dashCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            currentDashStacks++;
            if (currentDashStacks < maxDashStacks)
            {
                dashCooldownTimer = TickTimer.CreateFromSeconds(Runner, dashCooldown);
            }
        }
    }

    protected bool CanDash()
    {
        return currentDashStacks > 0;
    }

    protected virtual void Dash(Vector3 direction)
    {
        NetworkedPosition += direction * dashForce;
        transform.position = NetworkedPosition;
        currentDashStacks--;

        if (currentDashStacks < maxDashStacks)
        {
            dashCooldownTimer = TickTimer.CreateFromSeconds(Runner, dashCooldown);
        }
    }

    // Cooldown durumlarını UI için public metodlar
    public float GetDashCooldownProgress()
    {
        if (dashCooldownTimer.ExpiredOrNotRunning(Runner)) return 1f;
        return 1f - dashCooldownTimer.RemainingTime(Runner).Value / dashCooldown;
    }

    public int GetCurrentDashStacks()
    {
        return currentDashStacks;
    }

    public int GetMaxDashStacks()
    {
        return maxDashStacks;
    }

    protected void UpdateRotation(Vector2 mousePosition)
    {
        if (mainCamera == null) return;

        Vector3 mouseWorldPosition = GetMouseWorldPosition(mousePosition);
        Vector3 lookDirection = (mouseWorldPosition - transform.position).normalized;
        lookDirection.y = 0;

        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(lookDirection),
                rotationSpeed * Runner.DeltaTime
            );
        }
    }

    protected void UpdateMovement(Vector2 input)
    {
        Vector3 worldMoveDirection = new Vector3(input.x, 0, input.y);

        if (worldMoveDirection != Vector3.zero)
        {
            Vector3 movement = worldMoveDirection * moveSpeed * Runner.DeltaTime;
            transform.position += movement;

            Vector3 localMoveDirection = transform.InverseTransformDirection(worldMoveDirection);

            if (animator != null)
            {
                animator.SetFloat(MovementXHash, localMoveDirection.x);
                animator.SetFloat(MovementZHash, localMoveDirection.z);
            }
        }
        else
        {
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

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RequestUserInfo()
    {
        if (Object.HasInputAuthority)
        {
            var user = PvpArenaModel.Instance.PvpArenaVo.GetUser(ServiceModel.Instance.Session.UserId);
            RPC_SetUserId(user.Id);
            Debug.Log($"Responding to user info request - UserId: {user.Id}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Invalid userId received in RPC_SetUserId: {userId}");
            return;
        }

        try
        {
            _userId = userId;
            
            // UI bileşenini bul ve kontrol et
            var characterUI = GetComponent<CharacterUI>();
            if (characterUI != null)
            {
                // PvpArenaModel'in hazır olduğundan emin ol
                if (PvpArenaModel.Instance != null && PvpArenaModel.Instance.PvpArenaVo != null)
                {
                    characterUI.Init(userId);
                }
                else
                {
                    Debug.LogWarning("PvpArenaModel or PvpArenaVo is not ready yet. UI initialization delayed.");
                    // Gerekirse burada bir event sistemi veya coroutine ile tekrar deneme yapılabilir
                }
            }
            else
            {
                Debug.LogWarning("CharacterUI component not found on character.");
            }

            Debug.Log($"UserId set successfully: {userId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in RPC_SetUserId: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
}
