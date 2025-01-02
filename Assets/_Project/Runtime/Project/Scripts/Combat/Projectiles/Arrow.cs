using UnityEngine;
using Fusion;

public class Arrow : NetworkBehaviour
{
    [SerializeField] private float baseDamage = 20f;
    [SerializeField] private float minDamageMultiplier = 1f;
    [SerializeField] private float maxDamageMultiplier = 2f;
    [SerializeField] private float minSpeed = 15f;
    [SerializeField] private float maxSpeed = 30f;

    private Vector3 _velocity;
    private PlayerRef _owner;
    private string _teamId;
    private float _currentSpeed;
    private bool _isInitialized;
    private float _lifeTime = 5f;
    private TickTimer _despawnTimer;

    public void Initialize(Vector3 velocity, PlayerRef owner, float speed, string teamId)
    {
        _velocity = velocity;
        _owner = owner;
        _currentSpeed = speed;
        _teamId = teamId;
        _isInitialized = true;
        _despawnTimer = TickTimer.CreateFromSeconds(Runner, _lifeTime);

        Debug.Log($"Arrow initialized - Owner: {_owner}, Speed: {_currentSpeed}, TeamId: {_teamId}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!_isInitialized) return;

        // Ok hareketi
        transform.position += _velocity * Runner.DeltaTime;
        transform.rotation = Quaternion.LookRotation(_velocity);

        // Belirli bir süre sonra oku yok et
        if (_despawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Arrow hit: {other.name}");
        if (!HasStateAuthority) return;

        if (other.TryGetComponent<BaseCharacterController>(out var character))
        {
            Debug.Log($"Hit character - TeamId: {character.TeamId}, Arrow TeamId: {_teamId}, Owner: {character.Owner}, Arrow Owner: {_owner}");
            
            // Takım arkadaşına veya kendine hasar verme
            if (character.TeamId == _teamId || character.Owner == _owner)
            {
                Debug.Log("Same team or owner, no damage");
                return;
            }

            // Calculate damage based on speed
            float speedRatio = (_currentSpeed - minSpeed) / (maxSpeed - minSpeed);
            float damageMultiplier = Mathf.Lerp(minDamageMultiplier, maxDamageMultiplier, speedRatio);
            float finalDamage = baseDamage * damageMultiplier;

            Debug.Log($"Dealing {finalDamage} damage to enemy. Speed: {_currentSpeed}, Multiplier: {damageMultiplier}");
            character.TakeDamage(finalDamage);
            Runner.Despawn(Object);
        }
        else
        {
            Debug.Log($"Hit non-character object: {other.name}");
            Runner.Despawn(Object);
        }
    }
}
