using UnityEngine;
using Fusion;
using UniRx;
using System;

public class ArcherController : BaseCharacterController
{
    [SerializeField] private NetworkPrefabRef arrowPrefab;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float dodgeDuration = 0.5f;
    [SerializeField] private float dodgeCooldown = 1f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float minArrowSpeed = 15f;
    [SerializeField] private float maxArrowSpeed = 30f;

    private float lastAttackTime;
    private float lastDodgeTime;
    [Networked] private float chargeStartTime { get; set; }
    [Networked] private NetworkBool isCharging { get; set; }
    private readonly int aimingHash = Animator.StringToHash("IsAiming");

    public int CurrentDashStacks => currentDashStacks;

    protected override void Attack()
    {
        if (!Object.HasInputAuthority) return;

        if (Runner.SimulationTime >= lastAttackTime + attackCooldown)
        {
            if (!isCharging)
            {
                StartCharging();
            }
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeStartTime = (float)Runner.SimulationTime;
        if (animator != null)
        {
            animator.SetBool(aimingHash, true);
        }
    }

    private void ReleaseArrow()
    {
        if (!isCharging) return;

        float chargeTime = Mathf.Min((float)(Runner.SimulationTime - chargeStartTime), maxChargeTime);
        float chargePercent = chargeTime / maxChargeTime;
        float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, chargePercent);

        // Input Authority olan RPC çağırır
        if (Object.HasInputAuthority)
        {
            Rpc_RequestSpawnArrow(transform.position + transform.forward + Vector3.up, transform.rotation, transform.forward, arrowSpeed);
        }

        isCharging = false;
        if (animator != null)
        {
            animator.SetBool(aimingHash, false);
            animator.SetTrigger(AttackHash);
        }

        lastAttackTime = (float)Runner.SimulationTime;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestSpawnArrow(Vector3 spawnPosition, Quaternion rotation, Vector3 direction, float speed)
    {
        if (Object.HasStateAuthority)
        {
            // Host tarafında ok spawn et
            NetworkObject arrowObj = Runner.Spawn(arrowPrefab, spawnPosition, rotation);
            if (arrowObj.TryGetComponent<Arrow>(out var arrow))
            {
                arrow.Initialize(direction, Object.InputAuthority, speed);
                
                // Ok spawn olduğunda tüm clientlara bildir
                Rpc_OnArrowSpawned(arrowObj, direction, speed);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_OnArrowSpawned(NetworkObject arrowObj, Vector3 direction, float speed)
    {
        if (arrowObj.TryGetComponent<Arrow>(out var arrow))
        {
            // Tüm clientlarda ok yönünü ve hızını ayarla
            arrow.Initialize(direction, Object.InputAuthority, speed);
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (GetInput(out NetworkInputData input))
        {
            if (Object.HasInputAuthority)
            {
                if (!input.AttackPressed && isCharging)
                {
                    ReleaseArrow();
                }
            }
        }

        // Animasyon senkronizasyonu için charging durumunu güncelle
        if (animator != null)
        {
            animator.SetBool(aimingHash, isCharging);
        }
    }

    protected override void Dodge()
    {
        if (!Object.HasInputAuthority) return;

        if (Runner.SimulationTime >= lastDodgeTime + dodgeCooldown)
        {
            if (isCharging)
            {
                isCharging = false;
                if (animator != null)
                {
                    animator.SetBool(aimingHash, false);
                }
            }

            if (animator != null)
            {
                animator.SetTrigger(DodgeHash);
            }

            canMove = false;
            lastDodgeTime = (float)Runner.SimulationTime;

            Observable.Timer(TimeSpan.FromSeconds(dodgeDuration))
                .Subscribe(_ => canMove = true)
                .AddTo(this);
        }
    }

    public void OnAttackPoint()
    {
        Debug.Log("Attack point reached in animation");
    }

    // Cooldown getters for UI
    public float GetDashCooldownPercent()
    {
        if (currentDashStacks >= maxDashStacks) return 0f;
        float timeSinceLastDash = Time.time - lastDashTime;
        return 1f - Mathf.Clamp01(timeSinceLastDash / dashCooldown);
    }

    public float GetDodgeCooldownPercent()
    {
        float timeSinceLastDodge = Time.time - lastDodgeTime;
        return 1f - Mathf.Clamp01(timeSinceLastDodge / dodgeCooldown);
    }

    public float GetAttackCooldownPercent()
    {
        float timeSinceLastAttack = Time.time - lastAttackTime;
        return 1f - Mathf.Clamp01(timeSinceLastAttack / attackCooldown);
    }
}
