using UnityEngine;
using Fusion;
using UniRx;
using System;

public class ArcherController : BaseCharacterController
{
    [Header("Arrow Settings")]
    [SerializeField] private NetworkPrefabRef arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;
    [SerializeField] private float arrowDamage = 20f;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private float attackCooldown = 1f;

    [Networked] private TickTimer attackCooldownTimer { get; set; }

    protected override void Attack()
    {
        if (!Object.HasInputAuthority) return;
        if (!attackCooldownTimer.ExpiredOrNotRunning(Runner)) return;

        // Ok spawn pozisyonunu ve rotasyonunu ayarla
        Vector3 spawnPosition = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + transform.forward + Vector3.up;
        Quaternion spawnRotation = transform.rotation;

        // Ok prefabını spawn et
        NetworkObject arrowObject = Runner.Spawn(arrowPrefab, spawnPosition, spawnRotation, Object.InputAuthority);
        
        if (arrowObject.TryGetComponent<Arrow>(out var arrow))
        {
            // Ok özelliklerini ayarla
            arrow.Initialize(transform.forward * arrowSpeed, Owner, arrowSpeed, TeamId);
            Debug.Log($"Spawned arrow - Owner: {Owner}, Speed: {arrowSpeed}, TeamId: {TeamId}");
        }

        // Cooldown'u başlat
        attackCooldownTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);

        // Animasyonu tetikle
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }
    }

    protected override void Dodge()
    {
        // Dodge mekanizması
    }

    // UI için cooldown progress
    public float GetAttackCooldownProgress()
    {
        if (attackCooldownTimer.ExpiredOrNotRunning(Runner)) return 1f;
        return 1f - attackCooldownTimer.RemainingTime(Runner).Value / attackCooldown;
    }
}
