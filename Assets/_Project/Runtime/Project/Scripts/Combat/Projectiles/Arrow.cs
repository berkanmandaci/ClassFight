using UnityEngine;
using Fusion;

public class Arrow : NetworkBehaviour
{
    [SerializeField] private float baseDamage = 15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float minDamageMultiplier = 1f;
    [SerializeField] private float maxDamageMultiplier = 2.5f;

    private Vector3 direction;
    [Networked] private PlayerRef ownerRef { get; set; }
    [Networked] private float currentSpeed { get; set; }
    private float spawnTime;

    public void Initialize(Vector3 direction, PlayerRef owner, float speed)
    {
        this.direction = direction;
        this.ownerRef = owner;
        this.currentSpeed = speed;
        spawnTime = Time.time;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // Move arrow
            transform.position += direction * currentSpeed * Runner.DeltaTime;

            // Destroy arrow after lifetime
            if (Time.time >= spawnTime + lifetime)
            {
                Runner.Despawn(Object);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;

        if (other.TryGetComponent<BaseCharacterController>(out var character))
        {
            // Don't damage owner
            if (character.Owner != ownerRef)
            {
                // Calculate damage based on speed
                float speedRatio = (currentSpeed - 15f) / (30f - 15f); // 15 = min speed, 30 = max speed
                float damageMultiplier = Mathf.Lerp(minDamageMultiplier, maxDamageMultiplier, speedRatio);
                float finalDamage = baseDamage * damageMultiplier;

                character.TakeDamage(finalDamage);
                Runner.Despawn(Object);
            }
        }
        else
        {
            // Hit something else (wall, ground, etc.)
            Runner.Despawn(Object);
        }
    }
}
