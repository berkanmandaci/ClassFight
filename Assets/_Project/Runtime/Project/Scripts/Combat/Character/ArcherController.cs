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
    private float chargeStartTime;
    private bool isCharging;
    private readonly int aimingHash = Animator.StringToHash("IsAiming");

    public int CurrentDashStacks => currentDashStacks;

    protected override void Attack()
    {
        if (!HasStateAuthority) return;

        if (Time.time >= lastAttackTime + attackCooldown)
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
        chargeStartTime = Time.time;
        if (animator != null)
        {
            animator.SetBool(aimingHash, true);
        }
    }

    private void ReleaseArrow()
    {
        if (!isCharging) return;

        float chargeTime = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
        float chargePercent = chargeTime / maxChargeTime;
        float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, chargePercent);

        Vector3 spawnPosition = transform.position + transform.forward + Vector3.up;
        NetworkObject arrowObj = Runner.Spawn(arrowPrefab, spawnPosition, transform.rotation, Object.InputAuthority);

        if (arrowObj.TryGetComponent<Arrow>(out var arrow))
        {
            arrow.Initialize(transform.forward, Object.InputAuthority, arrowSpeed);
        }

        isCharging = false;
        if (animator != null)
        {
            animator.SetBool(aimingHash, false);
            animator.SetTrigger(AttackHash);
        }

        lastAttackTime = Time.time;
    }


    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (GetInput(out NetworkInputData input))
        {
            if (!input.AttackPressed && isCharging)
            {
                ReleaseArrow();
            }
        }
    }

    protected override void Dodge()
    {
        if (!HasStateAuthority) return;

        if (Time.time >= lastDodgeTime + dodgeCooldown)
        {
            if (isCharging)
            {
                isCharging = false;
                if (animator != null)
                {
                    animator.SetBool(aimingHash, false);
                }
            }

            if (animator != null) animator.SetTrigger(DodgeHash);

            canMove = false;

            Observable.Timer(TimeSpan.FromSeconds(dodgeDuration))
                .Subscribe(_ =>
                {
                    canMove = true;
                })
                .AddTo(this);

            lastDodgeTime = Time.time;
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
