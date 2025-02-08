using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Cysharp.Threading.Tasks;
using System;
using ProjectV3.Shared.Enums;
using ProjectV3.Shared.Vo;
using ProjectV3.Shared.UI;

namespace ProjectV3.Shared.Combat
{
    public class BaseCharacterController : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("Hareket Ayarları")]
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _dashDistance = 5f;
        [SerializeField] private float _dodgeSpeed = 15f;
        
        [Header("Referanslar")]
        [SerializeField] private GameObject _archerGameObject;
        [SerializeField] private GameObject _warriorGameObject;
        [SerializeField] private GameObject _tankGameObject;

        [Header("UI References")]
        [SerializeField] private PlayerHudController _hudController;
        #endregion

        #region Private Fields
        private Vector2 _moveInput;
        private Vector2 _aimInput;
        private Vector3 _moveDirection;
        private bool _isInitialized;
        
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

        // Combat verilerini tutan referans
        private CombatUserVo _combatData;
        #endregion


        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        public override void OnStartLocalPlayer()
        {
            if (!InitializeLocalPlayer())
                return;

            SetupInputActions();
            SubscribeToInputEvents();
            SetupCamera();
            ActivateCharacter(_currentCharacterType);
        }

        private void OnDisable()
        {
            if (!isLocalPlayer) return;
            UnsubscribeFromInputEvents();
        }

        private void Update()
        {
            if (!isLocalPlayer || !_isInitialized) return;

            HandleRotation();

            if (!_isDashing && !_isDodging)
            {
                Move();
            }

            // Combat verilerini güncelle
            if (_combatData != null)
            {
                _combatData.UpdatePlayTime(Time.deltaTime);
            }
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            try
            {
                _characterController = GetComponent<CharacterController>();
                if (_characterController == null)
                    throw new Exception("CharacterController bulunamadı!");

                _archerController = _archerGameObject?.GetComponent<ICharacterController>();
                _warriorController = _warriorGameObject?.GetComponent<ICharacterController>();
                _tankController = _tankGameObject?.GetComponent<ICharacterController>();

                ValidateControllers();
                _isInitialized = true;
                
                Debug.Log($"[{gameObject.name}] Bileşenler başarıyla başlatıldı.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Başlatma hatası: {e.Message}");
                _isInitialized = false;
            }
        }

        private bool InitializeLocalPlayer()
        {
            try
            {
                enabled = true;
                _playerInput = GetComponent<PlayerInput>();
                
                if (_playerInput == null)
                    throw new Exception("PlayerInput bulunamadı!");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Local player başlatma hatası: {e.Message}");
                return false;
            }
        }

        private void ValidateControllers()
        {
            if (_archerController == null)
                Debug.LogWarning($"[{gameObject.name}] Archer controller bulunamadı!");
            if (_warriorController == null)
                Debug.LogWarning($"[{gameObject.name}] Warrior controller bulunamadı!");
            if (_tankController == null)
                Debug.LogWarning($"[{gameObject.name}] Tank controller bulunamadı!");
        }
        #endregion

        #region Input Setup
        private void SetupInputActions()
        {
            _moveAction = _playerInput.actions["Move"];
            _aimAction = _playerInput.actions["Aim"];
            _attackAction = _playerInput.actions["Attack"];
            _dashAction = _playerInput.actions["Dash"];
            _dodgeAction = _playerInput.actions["Dodge"];
            _previousCharacterAction = _playerInput.actions["PreviousCharacter"];
            _nextCharacterAction = _playerInput.actions["NextCharacter"];
        }

        private void SubscribeToInputEvents()
        {
            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
            _aimAction.performed += OnAim;
            _aimAction.canceled += OnAim;
            _attackAction.performed += OnAttack;
            _dashAction.performed += OnDash;
            _dodgeAction.performed += OnDodge;
            _previousCharacterAction.performed += OnPreviousCharacter;
            _nextCharacterAction.performed += OnNextCharacter;
            
            _playerInput.enabled = true;
        }

        private void UnsubscribeFromInputEvents()
        {
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
        #endregion

        #region Input Handlers
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

        protected virtual void OnAttack(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed) return;

            try
            {
                CmdAttack();
                _activeCharacter?.OnAttack();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Saldırı hatası: {e.Message}");
            }
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed || _isDashing) return;

            try
            {
                _isDashing = true;
                CmdDash(_moveDirection == Vector3.zero ? transform.forward : _moveDirection);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Dash hatası: {e.Message}");
                _isDashing = false;
            }
        }

        public void OnDodge(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer || !context.performed || _isDodging) return;

            try
            {
                Vector3 dodgeDirection = -_moveDirection;
                if (dodgeDirection == Vector3.zero)
                {
                    dodgeDirection = -transform.forward;
                }
                CmdDodge(dodgeDirection);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Dodge hatası: {e.Message}");
                _isDodging = false;
            }
        }
        #endregion

        #region Network Commands
        [Command]
        private void CmdAttack()
        {
            RpcAttack();
        }

        [Command]
        private void CmdDash(Vector3 dashDirection)
        {
            _isDashing = true;
            RpcDash(dashDirection);
        }

        [Command]
        private void CmdDodge(Vector3 dodgeDirection)
        {
            _isDodging = true;
            RpcDodge(dodgeDirection);
        }
        #endregion

        #region Network RPCs
        [ClientRpc]
        private void RpcAttack()
        {
            Debug.Log("Attack RPC çağrıldı!");
        }

        [ClientRpc]
        private void RpcDash(Vector3 dashDirection)
        {
            try
            {
                if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[{gameObject.name}] RpcDash çağrıldı ama obje aktif değil!");
                    return;
                }

                if (_characterController == null)
                {
                    _characterController = GetComponent<CharacterController>();
                    if (_characterController == null)
                    {
                        throw new Exception("CharacterController bulunamadı!");
                    }
                }

                _characterController.Move(dashDirection * _dashDistance);
                ResetDashStateAsync().Forget();

                Debug.Log($"[{(isServer ? "Server" : "Client")}] Dash gerçekleştirildi. Yön: {dashDirection}, Mesafe: {_dashDistance}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] RpcDash hatası: {e.Message}");
                _isDashing = false;
            }
        }

        [ClientRpc]
        private void RpcDodge(Vector3 dodgeDirection)
        {
            try
            {
                if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[{gameObject.name}] RpcDodge çağrıldı ama obje aktif değil!");
                    return;
                }

                if (_characterController == null)
                {
                    _characterController = GetComponent<CharacterController>();
                    if (_characterController == null)
                    {
                        throw new Exception("CharacterController bulunamadı!");
                    }
                }

                _characterController.Move(dodgeDirection * _dodgeSpeed * Time.deltaTime);
                ResetDodgeStateAsync().Forget();

                Debug.Log($"[{(isServer ? "Server" : "Client")}] Dodge gerçekleştirildi. Yön: {dodgeDirection}, Hız: {_dodgeSpeed}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] RpcDodge hatası: {e.Message}");
                _isDodging = false;
            }
        }
        #endregion

        #region Character Management
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

            try
            {
                int currentIndex = (int)_currentCharacterType;
                int totalCharacters = Enum.GetValues(typeof(CharacterType)).Length;
                int newIndex = (currentIndex + direction + totalCharacters) % totalCharacters;
                
                CmdChangeCharacter((CharacterType)newIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Karakter değiştirme hatası: {e.Message}");
            }
        }

        [Command]
        private void CmdChangeCharacter(CharacterType newType)
        {
            CharacterType oldType = _currentCharacterType;
            _currentCharacterType = newType;
            
            OnCharacterTypeChanged(oldType, newType);
            RpcChangeCharacter(oldType, newType);
        }

        [ClientRpc]
        private void RpcChangeCharacter(CharacterType oldType, CharacterType newType)
        {
            if (isServer) return;
            
            OnCharacterTypeChanged(oldType, newType);
            _currentCharacterType = newType;
        }

        private void OnCharacterTypeChanged(CharacterType oldType, CharacterType newType)
        {
            if (oldType == newType) return;

            try
            {
                DeactivateCharacter(oldType);
                ActivateCharacter(newType);
                
                // Combat verilerini güncelle
                if (_combatData != null)
                {
                    _combatData.OnCharacterTypeChanged(newType);
                }
                
                Debug.Log($"[{(isServer ? "Server" : "Client")}] Karakter değiştirildi: {oldType} -> {newType}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Karakter değiştirme hatası: {e.Message}");
            }
        }

        private void DeactivateCharacter(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.Archer:
                    if (_archerGameObject != null) _archerGameObject.SetActive(false);
                    break;
                case CharacterType.Warrior:
                    if (_warriorGameObject != null) _warriorGameObject.SetActive(false);
                    break;
                case CharacterType.Tank:
                    if (_tankGameObject != null) _tankGameObject.SetActive(false);
                    break;
            }
        }

        private void ActivateCharacter(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.Archer:
                    if (_archerGameObject != null)
                    {
                        _archerGameObject.SetActive(true);
                        _activeCharacter = _archerController;
                    }
                    break;
                case CharacterType.Warrior:
                    if (_warriorGameObject != null)
                    {
                        _warriorGameObject.SetActive(true);
                        _activeCharacter = _warriorController;
                    }
                    break;
                case CharacterType.Tank:
                    if (_tankGameObject != null)
                    {
                        _tankGameObject.SetActive(true);
                        _activeCharacter = _tankController;
                    }
                    break;
            }
        }
        #endregion

        #region Movement and Camera
        private void HandleRotation()
        {
            if (!isLocalPlayer) return;

            try
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 direction = (mousePosition - screenCenter).normalized;
                Vector3 targetDirection = new Vector3(direction.x, 0, direction.y);

                if (targetDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(targetDirection);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Rotasyon hatası: {e.Message}");
            }
        }

        private void Move()
        {
            if (!isLocalPlayer || _characterController == null) return;

            try
            {
                if (_moveDirection != Vector3.zero)
                {
                    _characterController.Move(_moveDirection * _moveSpeed * Time.deltaTime);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Hareket hatası: {e.Message}");
            }
        }

        private void SetupCamera()
        {
            try
            {
                CombatArenaModel.Instance.GetCamera().Follow = transform;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Kamera ayarlama hatası: {e.Message}");
            }
        }
        #endregion

        #region Async State Reset
        private async UniTaskVoid ResetDashStateAsync()
        {
            try
            {
                await UniTask.Delay(50); // 0.05 saniye
                _isDashing = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Dash durumu sıfırlama hatası: {e.Message}");
                _isDashing = false;
            }
        }

        private async UniTaskVoid ResetDodgeStateAsync()
        {
            try
            {
                await UniTask.Delay(200); // 0.2 saniye
                _isDodging = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Dodge durumu sıfırlama hatası: {e.Message}");
                _isDodging = false;
            }
        }
        #endregion

        public CharacterType GetCurrentCharacterType() => _currentCharacterType;
        
        public CombatUserVo GetCombatData() => _combatData;

        public void SetCombatData(CombatUserVo combatData)
        {
            _combatData = combatData;
            
            // HUD'u başlat
            if (_hudController != null)
            {
                _hudController.Init(combatData);
                Debug.Log($"[{gameObject.name}] HUD başlatıldı");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] HUD Controller bulunamadı!");
            }
            
            Debug.Log($"[{gameObject.name}] Combat verileri ayarlandı: {_combatData.UserData.DisplayName}");
        }
    }
}
