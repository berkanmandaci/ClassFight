using UnityEngine;
using Fusion;
using ProjectV2.Shared;

namespace ProjectV2.Player
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;

        [Networked] private Vector3 NetworkedPosition { get; set; }
        [Networked] private Quaternion NetworkedRotation { get; set; }
        [Networked] public PlayerState State { get; set; }
        [Networked] public float Health { get; set; } = 100f;
        [Networked] public NetworkString<_16> PlayerName { get; set; }

        private CharacterController characterController;
        private Camera mainCamera;
        private NetworkRunner runner;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public override void Spawned()
        {
            runner = Object.Runner;
            
            if (Object.HasInputAuthority)
            {
                mainCamera = Camera.main;
                Debug.Log("Local player spawned");
            }

            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
            State = PlayerState.Playing;
        }

        public override void FixedUpdateNetwork()
        {
            if (State != PlayerState.Playing) return;

            // Server ve input sahibi için hareket işlemleri
            if (Object.HasStateAuthority)
            {
                if (GetInput(out NetworkInputData input))
                {
                    ProcessMovement(input);
                }

                NetworkedPosition = transform.position;
                NetworkedRotation = transform.rotation;
            }
            // Diğer clientlar için interpolasyon
            else
            {
                transform.position = Vector3.Lerp(transform.position, NetworkedPosition, Runner.DeltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, NetworkedRotation, Runner.DeltaTime * 10f);
            }
        }

        private void ProcessMovement(NetworkInputData input)
        {
            // Hareket yönünü hesapla
            Vector3 moveDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y);
            
            if (moveDirection != Vector3.zero)
            {
                // Karakteri hareket ettir
                if (characterController != null)
                {
                    characterController.Move(moveDirection * moveSpeed * Runner.DeltaTime);
                }
                else
                {
                    transform.position += moveDirection * moveSpeed * Runner.DeltaTime;
                }

                // Karakteri hareket yönüne döndür
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Runner.DeltaTime
                );
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_TakeDamage(float damage)
        {
            if (!Object.HasStateAuthority) return;

            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                State = PlayerState.Dead;
                RPC_OnPlayerDied();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_OnPlayerDied()
        {
            Debug.Log($"Player {PlayerName} died!");
            // Ölüm animasyonu veya efektleri burada tetiklenebilir
        }

        public void Respawn(Vector3 position)
        {
            if (!Object.HasStateAuthority) return;

            transform.position = position;
            NetworkedPosition = position;
            Health = 100f;
            State = PlayerState.Playing;
        }
    }

    public struct NetworkInputData : INetworkInput
    {
        public Vector2 MovementInput;
        public NetworkButtons Buttons;
        public Vector3 AimDirection;

        public bool IsAttackPressed => Buttons.IsSet(InputButtons.Attack);
        public bool IsDashPressed => Buttons.IsSet(InputButtons.Dash);
        public bool IsJumpPressed => Buttons.IsSet(InputButtons.Jump);
    }

    public static class InputButtons
    {
        public const int Attack = 1;
        public const int Dash = 2;
        public const int Jump = 3;
    }
} 