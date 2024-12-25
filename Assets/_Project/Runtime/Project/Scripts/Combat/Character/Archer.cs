using UnityEngine;
using Fusion;

public class Archer : NetworkBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private NetworkPrefabRef _arrowPrefab;
    [SerializeField] private Transform _arrowSpawnPoint;
    [SerializeField] private float _attackCooldown = 0.5f;
    [SerializeField] private float _arrowSpeed = 15f;

    private float _lastAttackTime;
    private NetworkCharacterController _characterController;
    private Camera _mainCamera;

    private void Awake()
    {
        _characterController = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        _mainCamera = Camera.main;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            if (input.AttackPressed && Time.time >= _lastAttackTime + _attackCooldown)
            {
                ShootArrow();
                _lastAttackTime = Time.time;
            }
        }
    }

    private void ShootArrow()
    {
        if (!HasStateAuthority) return;

        // Get shooting direction based on where the character is facing
        Vector3 shootDirection = transform.forward;

        // Spawn the arrow
        var arrow = Runner.Spawn(_arrowPrefab, _arrowSpawnPoint.position, Quaternion.LookRotation(shootDirection));
        
        // Initialize the arrow
        if (arrow.TryGetComponent<Arrow>(out var arrowComponent))
        {
            arrowComponent.Initialize(shootDirection, Object.InputAuthority, _arrowSpeed);
        }
    }
}
