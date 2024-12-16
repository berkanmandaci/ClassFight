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

    private float lastAttackTime;
    private float lastDodgeTime;

    protected override void Attack()
    {
        if (!HasStateAuthority) return;
        
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Spawn arrow and set its direction
            Vector3 spawnPosition = transform.position + transform.forward + Vector3.up;
            NetworkObject arrowObj = Runner.Spawn(arrowPrefab, spawnPosition, transform.rotation, Object.InputAuthority);
            
            if (arrowObj.TryGetComponent<Arrow>(out var arrow))
            {
                arrow.Initialize(transform.forward, Object.InputAuthority);
            }

            lastAttackTime = Time.time;
        }
    }

    protected override void Dodge()
    {
        if (!HasStateAuthority) return;
        
        if (Time.time >= lastDodgeTime + dodgeCooldown)
        {
            // Make character invulnerable and unable to move
            canMove = false;
            
            // Re-enable movement after dodge duration
            Observable.Timer(TimeSpan.FromSeconds(dodgeDuration))
                .Subscribe(_ => 
                {
                    canMove = true;
                })
                .AddTo(this);

            lastDodgeTime = Time.time;
        }
    }
}
