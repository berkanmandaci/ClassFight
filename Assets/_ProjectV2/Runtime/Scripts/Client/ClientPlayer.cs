using UnityEngine;
using Fusion;
using ProjectV2.Shared;
using ProjectV2.Core;

namespace ProjectV2.Client
{
    public class ClientPlayer : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float jumpForce = 5f;

        [Header("Components")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;

        [Networked] private Vector3 NetworkedPosition { get; set; }
        [Networked] private Quaternion NetworkedRotation { get; set; }
        [Networked] public PlayerState State { get; set; }
        [Networked] public float Health { get; set; } = 100f;
        [Networked] public NetworkString<_16> PlayerName { get; set; }

        private bool isGrounded;
        private Vector3 verticalVelocity;
        private const float GRAVITY = -9.81f;

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                Debug.Log("Local player spawned");
                SetupLocalPlayer();
            }

            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
            State = PlayerState.Playing;

            // Player Manager'a kaydol
            ClientPlayerManager.Instance?.RegisterSpawnedPlayer(Object.InputAuthority, Object);
        }

        private void SetupLocalPlayer()
        {
            // Kamera ayarları
            if (Camera.main != null)
            {
                var cameraController = Camera.main.gameObject.AddComponent<CameraFollow>();
                cameraController.SetTarget(transform);
            }

            // Input system kurulumu yapılabilir
            // Animator parametreleri ayarlanabilir
            // UI elementleri aktif edilebilir
        }

        public override void FixedUpdateNetwork()
        {
            if (State != PlayerState.Playing) return;

            // Local player için input ve hareket işlemleri
            if (Object.HasInputAuthority)
            {
                if (GetInput(out NetworkInputData input))
                {
                    ProcessMovement(input);
                    ProcessActions(input);
                }
            }
            // Diğer oyuncular için interpolasyon
            else
            {
                transform.position = Vector3.Lerp(transform.position, NetworkedPosition, Runner.DeltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, NetworkedRotation, Runner.DeltaTime * 10f);
            }
        }

        private void ProcessMovement(NetworkInputData input)
        {
            // Yatay hareket
            Vector3 moveDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y);
            
            // Kamera yönüne göre hareket
            if (Camera.main != null)
            {
                var cameraTransform = Camera.main.transform;
                var forward = cameraTransform.forward;
                var right = cameraTransform.right;

                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                moveDirection = forward * input.MovementInput.y + right * input.MovementInput.x;
            }

            // Yerçekimi ve zıplama
            isGrounded = characterController.isGrounded;
            if (isGrounded)
            {
                verticalVelocity.y = -0.5f; // Sürekli yere yapışık tutmak için küçük bir değer
                if (input.IsJumpPressed)
                {
                    verticalVelocity.y = jumpForce;
                }
            }
            else
            {
                verticalVelocity.y += GRAVITY * Runner.DeltaTime;
            }

            // Hareketi uygula
            Vector3 movement = moveDirection * moveSpeed + verticalVelocity;
            characterController.Move(movement * Runner.DeltaTime);

            // Karakteri hareket yönüne döndür
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime);
            }

            // Networked değerleri güncelle
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;

            // Animator güncelleme
            if (animator != null)
            {
                animator.SetFloat("Speed", moveDirection.magnitude);
                animator.SetBool("IsGrounded", isGrounded);
            }
        }

        private void ProcessActions(NetworkInputData input)
        {
            if (input.IsAttackPressed)
            {
                Attack();
            }

            if (input.IsDashPressed)
            {
                Dash();
            }
        }

        private void Attack()
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            // RPC ile server'a saldırı bilgisi gönderilebilir
        }

        private void Dash()
        {
            // Dash mantığı
        }

        public void OnHealthChanged(float newHealth)
        {
            Health = newHealth;
            if (Health <= 0)
            {
                State = PlayerState.Dead;
                OnDeath();
            }
        }

        private void OnDeath()
        {
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
            // Ölüm efektleri, UI güncellemeleri vs.
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ClientPlayerManager.Instance?.UnregisterPlayer(Object.InputAuthority);
        }
    }
} 