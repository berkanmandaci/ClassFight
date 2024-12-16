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
    
    protected NetworkCharacterController characterController;
    protected int currentDashStacks;
    protected float lastDashTime;
    protected bool canMove = true;
    
    [Networked] public float Health { get; set; } = 100f;
    [Networked] public bool IsDead { get; set; }
    [Networked] public PlayerRef Owner { get; set; }

    protected virtual void Awake()
    {
        characterController = GetComponent<NetworkCharacterController>();
        currentDashStacks = maxDashStacks;
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead || !canMove) return;
        
        if (GetInput(out NetworkInputData input))
        {
            // Movement
            Vector3 moveDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y);
            if (moveDirection != Vector3.zero)
            {
                characterController.Move(moveDirection * moveSpeed * Runner.DeltaTime);
            }

            // Rotation
            if (input.RotationInput != Vector2.zero)
            {
                Vector3 lookDirection = new Vector3(input.RotationInput.x, 0, input.RotationInput.y);
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            // Dash
            if (input.DashPressed && currentDashStacks > 0 && Time.time >= lastDashTime + dashCooldown)
            {
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
        // Implementation will be added later
    }
}
