using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using ProjectV3.Client;

namespace ProjectV3.Test
{
    public class BaseCharacterController : NetworkBehaviour
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
        [SyncVar] private bool _isDashing;
        [SyncVar] private bool _isDodging;
        private CharacterController _characterController;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _aimAction;
        private InputAction _attackAction;
        private InputAction _dashAction;
        private InputAction _dodgeAction;

        public override void OnStartLocalPlayer()
        {
            enabled = true;

            if (_rotationTransform == null)
            {
                Debug.LogError("Rotation Transform atanmamış!");
                _rotationTransform = transform;
            }

            _characterController = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();

            // Input actions'ları al
            _moveAction = _playerInput.actions["Move"];
            _aimAction = _playerInput.actions["Aim"];
            _attackAction = _playerInput.actions["Attack"];
            _dashAction = _playerInput.actions["Dash"];
            _dodgeAction = _playerInput.actions["Dodge"];

            // Event'lere abone ol
            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;

            _aimAction.performed += OnAim;
            _aimAction.canceled += OnAim;

            _attackAction.performed += OnAttack;

            _dashAction.performed += OnDash;

            _dodgeAction.performed += OnDodge;

            CombatArenaModel.Instance.GetCamera().Follow = transform;
            _playerInput.enabled = true;
        }


        private void OnDisable()
        {
            if (!isLocalPlayer) return;

            // Event'lerden çık
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMove;
            }

            if (_aimAction != null)
            {
                _aimAction.performed -= OnAim;
                _aimAction.canceled -= OnAim;
            }

            if (_attackAction != null)
                _attackAction.performed -= OnAttack;

            if (_dashAction != null)
                _dashAction.performed -= OnDash;

            if (_dodgeAction != null)
                _dodgeAction.performed -= OnDodge;
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            HandleRotation();

            if (!_isDashing && !_isDodging)
            {
                Move();
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer) return;

            _moveInput = context.ReadValue<Vector2>();
            _moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer) return;

            _aimInput = context.ReadValue<Vector2>();
        }

        private void HandleRotation()
        {
            if (!isLocalPlayer) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 direction = (mousePosition - screenCenter).normalized;
            Vector3 targetDirection = new Vector3(direction.x, 0, direction.y);

            if (targetDirection != Vector3.zero)
            {
                _rotationTransform.rotation = Quaternion.LookRotation(targetDirection);
            }
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed) return;

            CmdAttack();
        }

        [Command]
        private void CmdAttack()
        {
            RpcAttack();
        }

        [ClientRpc]
        private void RpcAttack()
        {
            Debug.Log("Attack!");
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed || _isDashing) return;
            _isDashing = true;
            CmdDash(_moveDirection == Vector3.zero ? _rotationTransform.forward : _moveDirection);
        }

        private void CmdDash(Vector3 dashDirection)
        {
            RpcDash(dashDirection);
        }

        private void RpcDash(Vector3 dashDirection)
        {
            _characterController.Move(dashDirection * _dashDistance);
            StartCoroutine(ResetDashState());
        }

        private System.Collections.IEnumerator ResetDashState()
        {
            yield return new WaitForSeconds(0.05f);
            _isDashing = false;
        }

        public void OnDodge(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed || _isDodging) return;

            Vector3 dodgeDirection = -_moveDirection;
            if (dodgeDirection == Vector3.zero)
            {
                dodgeDirection = -_rotationTransform.forward;
            }
            CmdDodge(dodgeDirection);
        }

        private void CmdDodge(Vector3 dodgeDirection)
        {
            _isDodging = true;
            RpcDodge(dodgeDirection);
        }

        private void RpcDodge(Vector3 dodgeDirection)
        {
            _characterController.Move(dodgeDirection * _dodgeSpeed * Time.deltaTime);
            StartCoroutine(ResetDodgeState());
        }

        private System.Collections.IEnumerator ResetDodgeState()
        {
            yield return new WaitForSeconds(0.2f);
            _isDodging = false;
        }

        private void Move()
        {
            if (!isLocalPlayer) return;

            if (_moveDirection != Vector3.zero)
            {
                _characterController.Move(_moveDirection * _moveSpeed * Time.deltaTime);
            }
        }


    }
}
