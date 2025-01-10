using UnityEngine;
using Fusion;
using _Project.Shared.Scripts.Data;

namespace _Project.Server.Scripts.Player
{
    public class ServerPlayer : NetworkBehaviour
    {
        [Networked] public NetworkString<_16> PlayerId { get; set; }
        [Networked] public float Health { get; set; } = 100f;
        [Networked] public NetworkBool IsAlive { get; set; }
        [Networked] public NetworkButtons ButtonsPrevious { get; set; }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float dashForce = 3f;
        [SerializeField] private float maxMoveDistance = 10f;

        private Vector3 _lastValidPosition;
        private float _lastMoveTime;
        private PlayerRef _owner;
        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void Initialize(PlayerRef owner)
        {
            if (Object.HasStateAuthority)
            {
                _owner = owner;
                Health = 100f;
                IsAlive = true;
                _lastValidPosition = transform.position;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            if (GetInput(out NetworkInputData input))
            {
                ProcessInput(input);
            }
        }

        private void ProcessInput(NetworkInputData input)
        {
            if (!IsAlive) return;

            // Movement
            Vector3 movement = new Vector3(input.movementInput.x, 0, input.movementInput.y);
            if (ValidateMovement(movement))
            {
                if (_controller != null)
                {
                    _controller.Move(movement * Runner.DeltaTime * moveSpeed);
                }
                else
                {
                    transform.position += movement * Runner.DeltaTime * moveSpeed;
                }
                _lastValidPosition = transform.position;
                _lastMoveTime = Time.time;
            }

            // Actions
            NetworkButtons buttons = input.buttons;
            NetworkButtons pressed = buttons.GetPressed(ButtonsPrevious);

            if (pressed.IsSet(NetworkInputData.ATTACK))
            {
                ProcessAttack();
            }

            if (pressed.IsSet(NetworkInputData.DASH))
            {
                ProcessDash(movement);
            }

            ButtonsPrevious = buttons;
        }

        private bool ValidateMovement(Vector3 movement)
        {
            // Hareket mesafesi kontrolü
            Vector3 newPosition = transform.position + movement * Runner.DeltaTime * moveSpeed;
            float distanceFromLast = Vector3.Distance(_lastValidPosition, newPosition);
            
            if (distanceFromLast > maxMoveDistance)
            {
                Debug.LogWarning($"Invalid movement detected for player {_owner}. Distance: {distanceFromLast}");
                return false;
            }

            // Zemin kontrolü
            if (!IsGrounded(newPosition))
            {
                return false;
            }

            return true;
        }

        private bool IsGrounded(Vector3 position)
        {
            // Basit bir zemin kontrolü
            return Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, 0.2f);
        }

        private void ProcessAttack()
        {
            if (!IsAlive) return;

            // Saldırı işleme
            Debug.Log($"Player {_owner} attacked");
        }

        private void ProcessDash(Vector3 direction)
        {
            if (!IsAlive) return;

            // Dash işleme
            if (direction != Vector3.zero)
            {
                Vector3 dashMovement = direction.normalized * dashForce;
                if (ValidateMovement(dashMovement))
                {
                    if (_controller != null)
                    {
                        _controller.Move(dashMovement);
                    }
                    else
                    {
                        transform.position += dashMovement;
                    }
                    _lastValidPosition = transform.position;
                }
            }
        }

        public void TakeDamage(float damage)
        {
            if (!Object.HasStateAuthority) return;

            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;
                Die();
            }
        }

        private void Die()
        {
            // Ölüm işlemleri
            Debug.Log($"Player {_owner} died");
            gameObject.SetActive(false);
        }

        public void TeleportTo(Vector3 position)
        {
            if (!Object.HasStateAuthority) return;
            
            transform.position = position;
            _lastValidPosition = position;
        }

        public void Heal(float amount)
        {
            if (!Object.HasStateAuthority) return;

            Health = Mathf.Min(Health + amount, 100f);
        }
    }
} 