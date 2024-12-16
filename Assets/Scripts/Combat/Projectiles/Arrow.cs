using UnityEngine;
using Fusion;

public class Arrow : NetworkBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float lifetime = 5f;

    private Vector3 direction;
    [Networked] private PlayerRef ownerRef { get; set; }
    private float spawnTime;

    public void Initialize(Vector3 direction, PlayerRef owner)
    {
        this.direction = direction;
        this.ownerRef = owner;
        spawnTime = Time.time;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // Move arrow
            transform.position += direction * speed * Runner.DeltaTime;

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
                character.TakeDamage(damage);
                Runner.Despawn(Object);
            }
        }
    }
}
