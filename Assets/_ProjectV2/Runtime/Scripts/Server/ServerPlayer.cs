using UnityEngine;
using Fusion;
using ProjectV2.Shared;

namespace ProjectV2.Server
{
    public class ServerPlayer : NetworkBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnTime = 3f;

        [Header("Movement Validation")]
        [SerializeField] private float maxMoveSpeed = 10f;
        [SerializeField] private float maxJumpForce = 8f;
        [SerializeField] private float maxPositionError = 5f;

        [Networked] private Vector3 NetworkedPosition { get; set; }
        [Networked] private Quaternion NetworkedRotation { get; set; }
        [Networked] public PlayerState State { get; set; }
        [Networked] public float Health { get; set; }
        [Networked] public NetworkString<_16> PlayerName { get; set; }

        private CharacterController characterController;
        private TickTimer respawnTimer;
        private Vector3 lastValidPosition;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public override void Spawned()
        {
            if (!Object.HasStateAuthority) return;

            // İlk spawn ayarları
            Health = maxHealth;
            State = PlayerState.Playing;
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
            lastValidPosition = transform.position;

            Debug.Log($"Server player spawned: {Object.InputAuthority}");
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            // Ölü oyuncunun yeniden doğma kontrolü
            if (State == PlayerState.Dead)
            {
                if (respawnTimer.Expired(Runner))
                {
                    Respawn();
                }
                return;
            }

            // Input işleme ve validasyon
            if (GetInput(out NetworkInputData input))
            {
                ValidateAndProcessMovement(input);
            }
        }

        private void ValidateAndProcessMovement(NetworkInputData input)
        {
            Vector3 moveDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y);
            
            // Hareket hızı validasyonu
            if (moveDirection.magnitude > 1)
            {
                moveDirection.Normalize();
            }

            Vector3 movement = moveDirection * maxMoveSpeed * Runner.DeltaTime;

            // Zıplama validasyonu
            if (input.IsJumpPressed && characterController.isGrounded)
            {
                movement.y = Mathf.Min(maxJumpForce, movement.y + maxJumpForce);
            }

            // Pozisyon değişimi validasyonu
            Vector3 newPosition = transform.position + movement;
            float positionDelta = Vector3.Distance(newPosition, lastValidPosition);

            if (positionDelta > maxPositionError)
            {
                Debug.LogWarning($"Invalid movement detected for player {Object.InputAuthority}. Delta: {positionDelta}");
                transform.position = lastValidPosition;
                return;
            }

            // Hareketi uygula
            if (characterController != null)
            {
                characterController.Move(movement);
            }
            else
            {
                transform.position += movement;
            }

            // Rotasyonu güncelle
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            // Networked değerleri güncelle
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
            lastValidPosition = transform.position;
        }

        public void TakeDamage(float damage, PlayerRef attacker)
        {
            if (!Object.HasStateAuthority) return;

            Health = Mathf.Max(0, Health - damage);
            
            if (Health <= 0)
            {
                Die(attacker);
            }
        }

        private void Die(PlayerRef killer)
        {
            if (!Object.HasStateAuthority) return;

            State = PlayerState.Dead;
            respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);

            // Ölüm olayını RPC ile tüm clientlara bildir
            RPC_OnPlayerDied(killer);
        }

        private void Respawn()
        {
            if (!Object.HasStateAuthority) return;

            // Spawn noktası al
            Vector3 spawnPoint = GetRespawnPoint();

            // Oyuncuyu yeniden canlandır
            transform.position = spawnPoint;
            NetworkedPosition = spawnPoint;
            Health = maxHealth;
            State = PlayerState.Playing;
            lastValidPosition = spawnPoint;

            // Yeniden doğma olayını RPC ile bildir
            RPC_OnPlayerRespawned();
        }

        private Vector3 GetRespawnPoint()
        {
            // Spawn noktası mantığı
            return Vector3.zero;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_OnPlayerDied(PlayerRef killer)
        {
            Debug.Log($"Player {Object.InputAuthority} was killed by {killer}");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_OnPlayerRespawned()
        {
            Debug.Log($"Player {Object.InputAuthority} respawned");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Debug.Log($"Server player despawned: {Object.InputAuthority}");
        }
    }
} 