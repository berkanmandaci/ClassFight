using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using ProjectV3.Client;
using Cysharp.Threading.Tasks;

namespace ProjectV3.Test
{
    public class BaseCharacterController : NetworkBehaviour
    {
        [Header("Hareket Ayarları")]
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _dashDistance = 5f;
        [SerializeField] private float _dodgeSpeed = 15f;

        
        [Header("Referanslar")]
        [SerializeField] private GameObject _archerGameObject;
        [SerializeField] private GameObject _warriorGameObject;
        [SerializeField] private GameObject _tankGameObject;

        private Vector2 _moveInput;
        private Vector2 _aimInput;
        private Vector3 _moveDirection;
        [SyncVar] private bool _isDashing;
        [SyncVar] private bool _isDodging;
        [SyncVar(hook = nameof(OnCharacterTypeChanged))] 
        private CharacterType _currentCharacterType = CharacterType.Archer;
        private CharacterController _characterController;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _aimAction;
        private InputAction _attackAction;
        private InputAction _dashAction;
        private InputAction _dodgeAction;
        private InputAction _previousCharacterAction;
        private InputAction _nextCharacterAction;

        private ICharacterController _activeCharacter;
        private ICharacterController _archerController;
        private ICharacterController _warriorController;
        private ICharacterController _tankController;

        private enum CharacterType
        {
            Archer,
            Warrior,
            Tank
        }

        private void Awake()
        {
            // Başlangıçta tüm kontrolcü referanslarını al
            _archerController = _archerGameObject.GetComponent<ICharacterController>();
            _warriorController = _warriorGameObject.GetComponent<ICharacterController>();
            _tankController = _tankGameObject.GetComponent<ICharacterController>();
        }

        public override void OnStartLocalPlayer()
        {
            enabled = true;


            _characterController = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();

            // Input actions'ları al
            _moveAction = _playerInput.actions["Move"];
            _aimAction = _playerInput.actions["Aim"];
            _attackAction = _playerInput.actions["Attack"];
            _dashAction = _playerInput.actions["Dash"];
            _dodgeAction = _playerInput.actions["Dodge"];
            _previousCharacterAction = _playerInput.actions["PreviousCharacter"];
            _nextCharacterAction = _playerInput.actions["NextCharacter"];

            // Event'lere abone ol
            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;

            _aimAction.performed += OnAim;
            _aimAction.canceled += OnAim;

            _attackAction.performed += OnAttack;

            _dashAction.performed += OnDash;

            _dodgeAction.performed += OnDodge;
            
            _previousCharacterAction.performed += OnPreviousCharacter;
            _nextCharacterAction.performed += OnNextCharacter;

            CombatArenaModel.Instance.GetCamera().Follow = transform;
            _playerInput.enabled = true;

            // İlk karakteri aktif et
            ActivateCharacter(_currentCharacterType);
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
            
            if (_previousCharacterAction != null)
                _previousCharacterAction.performed -= OnPreviousCharacter;
            
            if (_nextCharacterAction != null)
                _nextCharacterAction.performed -= OnNextCharacter;
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
                transform.rotation = Quaternion.LookRotation(targetDirection);
            }
        }

        protected virtual void OnAttack(InputAction.CallbackContext context)
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
            CmdDash(_moveDirection == Vector3.zero ? transform.forward : _moveDirection);
        }

        [Command]
        private void CmdDash(Vector3 dashDirection)
        {
            _isDashing = true;
            RpcDash(dashDirection);
        }

        [ClientRpc]
        private void RpcDash(Vector3 dashDirection)
        {
            _characterController.Move(dashDirection * _dashDistance);
            ResetDashStateAsync().Forget();
        }

        public void OnDodge(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed || _isDodging) return;

            Vector3 dodgeDirection = -_moveDirection;
            if (dodgeDirection == Vector3.zero)
            {
                dodgeDirection = -transform.forward;
            }
            CmdDodge(dodgeDirection);
        }

        [Command]
        private void CmdDodge(Vector3 dodgeDirection)
        {
            _isDodging = true;
            RpcDodge(dodgeDirection);
        }

        [ClientRpc]
        private void RpcDodge(Vector3 dodgeDirection)
        {
            _characterController.Move(dodgeDirection * _dodgeSpeed * Time.deltaTime);
            ResetDodgeStateAsync().Forget();
        }

        private void OnPreviousCharacter(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed) return;

            ChangeCharacter(-1);
        }
        
        private void OnNextCharacter(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed) return;

            ChangeCharacter(1);
        }
        
        private void ChangeCharacter(int direction)
        {
            if (!isLocalPlayer) return;

            int currentIndex = (int)_currentCharacterType;
            int totalCharacters = System.Enum.GetValues(typeof(CharacterType)).Length;
            
            // Yeni karakter indeksini hesapla (döngüsel)
            int newIndex = (currentIndex + direction + totalCharacters) % totalCharacters;
            
            // Server'a karakter değişimini bildir
            CmdChangeCharacter((CharacterType)newIndex);
        }

        [Command]
        private void CmdChangeCharacter(CharacterType newType)
        {
            // Server'da karakter değişimini yap
            CharacterType oldType = _currentCharacterType;
            _currentCharacterType = newType;
            
            // Server'da karakteri değiştir
            OnCharacterTypeChanged(oldType, newType);
            
            // Tüm clientlara bildir
            RpcChangeCharacter(oldType, newType);
        }

        [ClientRpc]
        private void RpcChangeCharacter(CharacterType oldType, CharacterType newType)
        {
            if (isServer) return; // Server zaten değişikliği yapmış durumda
            
            // Client'larda karakter değişimini yap
            OnCharacterTypeChanged(oldType, newType);
            _currentCharacterType = newType;
        }

        private void OnCharacterTypeChanged(CharacterType oldType, CharacterType newType)
        {
            if (oldType == newType) return;

            // Önceki karakteri deaktif et
            DeactivateCharacter(oldType);
            // Yeni karakteri aktif et
            ActivateCharacter(newType);

            Debug.Log($"[{(isServer ? "Server" : "Client")}] Karakter değiştirildi: {oldType} -> {newType}");
        }

        private void DeactivateCharacter(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.Archer:
                    _archerGameObject.SetActive(false);
                    break;
                case CharacterType.Warrior:
                    _warriorGameObject.SetActive(false);
                    break;
                case CharacterType.Tank:
                    _tankGameObject.SetActive(false);
                    break;
            }
        }

        private void ActivateCharacter(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.Archer:
                    _archerGameObject.SetActive(true);
                    _activeCharacter = _archerController;
                    break;
                case CharacterType.Warrior:
                    _warriorGameObject.SetActive(true);
                    _activeCharacter = _warriorController;
                    break;
                case CharacterType.Tank:
                    _tankGameObject.SetActive(true);
                    _activeCharacter = _tankController;
                    break;
            }
        }

        private void Move()
        {
            if (!isLocalPlayer) return;

            if (_moveDirection != Vector3.zero)
            {
                _characterController.Move(_moveDirection * _moveSpeed * Time.deltaTime);
            }
        }

        private async UniTaskVoid ResetDashStateAsync()
        {
            await UniTask.Delay(50); // 0.05 saniye
            _isDashing = false;
        }

        private async UniTaskVoid ResetDodgeStateAsync()
        {
            await UniTask.Delay(200); // 0.2 saniye
            _isDodging = false;
        }
    }
}
