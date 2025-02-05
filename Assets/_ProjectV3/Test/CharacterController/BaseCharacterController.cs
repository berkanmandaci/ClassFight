using UnityEngine;
using UnityEngine.InputSystem;
namespace ProjectV3.Test
{
    public class BaseCharacterController : MonoBehaviour
    {
        [Header("Hareket Ayarları")]
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _dashDistance = 5f;
        [SerializeField] private float _dodgeSpeed = 15f;

        [Header("Referanslar")]
        [SerializeField] private Transform _rotationTransform;

        private Vector2 _moveInput;
        private Vector2 _aimInput;
        private Vector3 _moveDirection;
        private bool _isDashing;
        private bool _isDodging;
        private CharacterController _characterController;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _aimAction;
        private InputAction _attackAction;
        private InputAction _dashAction;
        private InputAction _dodgeAction;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();
            
            if (_rotationTransform == null)
            {
                Debug.LogError("Rotation Transform atanmamış!");
                _rotationTransform = transform;
            }
            
            // Input actions'ları al
            _moveAction = _playerInput.actions["Move"];
            _aimAction = _playerInput.actions["Aim"];
            _attackAction = _playerInput.actions["Attack"];
            _dashAction = _playerInput.actions["Dash"];
            _dodgeAction = _playerInput.actions["Dodge"];
        }

        private void OnEnable()
        {
            // Event'lere abone ol
            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
            
            _aimAction.performed += OnAim;
            _aimAction.canceled += OnAim;
            
            _attackAction.performed += OnAttack;
            
            _dashAction.performed += OnDash;
            
            _dodgeAction.performed += OnDodge;
        }

        private void OnDisable()
        {
            // Event'lerden çık
            _moveAction.performed -= OnMove;
            _moveAction.canceled -= OnMove;
            
            _aimAction.performed -= OnAim;
            _aimAction.canceled -= OnAim;
            
            _attackAction.performed -= OnAttack;
            
            _dashAction.performed -= OnDash;
            
            _dodgeAction.performed -= OnDodge;
        }

        private void Update()
        {
            HandleRotation();

            if (!_isDashing && !_isDodging)
            {
                Move();
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
            _moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
            Debug.Log($"Move Input: {_moveInput}");
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            _aimInput = context.ReadValue<Vector2>();
            Debug.Log($"Aim Input: {_aimInput}");
        }

        private void HandleRotation()
        {
            // Mouse pozisyonunu al
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            // Ekran merkezini al
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            
            // Mouse'un ekran merkezine göre yönünü hesapla
            Vector2 direction = (mousePosition - screenCenter).normalized;
            
            // Yönü 3D uzaya çevir
            Vector3 targetDirection = new Vector3(direction.x, 0, direction.y);
            
            if (targetDirection != Vector3.zero)
            {
                // Karakteri döndür
                _rotationTransform.rotation = Quaternion.LookRotation(targetDirection);
                
                // Debug için görsel yardımcı
                Debug.DrawRay(transform.position, targetDirection * 5f, Color.red);
            }
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Attack();
            }
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.performed && !_isDashing)
            {
                Dash();
            }
        }

        public void OnDodge(InputAction.CallbackContext context)
        {
            if (context.performed && !_isDodging)
            {
                Dodge();
            }
        }

        private void Move()
        {
            if (_moveDirection != Vector3.zero)
            {
                _characterController.Move(_moveDirection * _moveSpeed * Time.deltaTime);
            }
        }

        private async void Dash()
        {
            _isDashing = true;
            Vector3 dashDirection = _moveDirection;
            if (dashDirection == Vector3.zero)
            {
                dashDirection = _rotationTransform.forward;
            }

            // Anlık dash hareketi
            _characterController.Move(dashDirection * _dashDistance);
            await System.Threading.Tasks.Task.Delay(50); // Çok kısa bir delay, sadece animasyon için
            _isDashing = false;
        }

        private async void Dodge()
        {
            _isDodging = true;
            Vector3 dodgeDirection = -_moveDirection;
            if (dodgeDirection == Vector3.zero)
            {
                dodgeDirection = -_rotationTransform.forward;
            }

            _characterController.Move(dodgeDirection * _dodgeSpeed * Time.deltaTime);
            await System.Threading.Tasks.Task.Delay(200);
            _isDodging = false;
        }

        private void Attack()
        {
            Debug.Log("Attack!");
            // Temel saldırı mantığı buraya eklenecek
        }
    }
}
