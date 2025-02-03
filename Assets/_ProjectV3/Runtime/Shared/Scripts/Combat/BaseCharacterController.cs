using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using ProjectV3.Shared.Data;
using Unity.Cinemachine;
using System.Collections;

namespace ProjectV3.Shared.Combat
{
    /// <summary>
    /// Tüm karakter kontrollerinin temel sınıfı
    /// </summary>
    public abstract class BaseCharacterController : NetworkBehaviour
    {
        #region Properties
        [Header("Hareket Ayarları")]
        [SerializeField] protected float _moveSpeed = 5f;
        [SerializeField] protected float _rotationSpeed = 15f;

        [Header("Dash Ayarları")]
        [SerializeField] protected float _dashCooldown = 2f;
        [SerializeField] protected float _dashDistance = 5f;
        [SerializeField] protected float _dashDuration = 0.2f;
        [SerializeField] protected float _dashSpeed = 20f;
        [SerializeField] protected int _maxDashStacks = 2;
        [SerializeField] protected AnimationCurve _dashSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Dodge Ayarları")]
        [SerializeField] protected float _dodgeCooldown = 1f;
        [SerializeField] protected float _dodgeDistance = 3f;
        [SerializeField] protected float _dodgeDuration = 0.5f;
        [SerializeField] protected float _dodgeSpeed = 15f;
        [SerializeField] protected float _dodgeImmunityDuration = 0.3f;
        [SerializeField] protected AnimationCurve _dodgeSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Referanslar")]
        [SerializeField] protected PlayerInput _playerInput;
        [SerializeField] protected Animator _animator;
        [SerializeField] protected Transform _weaponPivot;

        // SyncVar'lar
        [SyncVar]
        protected bool _isDashing;
        [SyncVar]
        protected bool _isDodging;
        [SyncVar]
        protected bool _isImmune;
        [SyncVar]
        protected int _currentDashStacks;

        // Input değişkenleri
        protected Vector2 _moveInput;
        protected Vector2 _aimInput;
        protected Camera _mainCamera;
        protected Plane _groundPlane;

        // State değişkenleri
        protected bool _isAttacking;
        protected float _lastDashTime;
        protected float _lastDodgeTime;
        protected float _dashTimeLeft;
        protected float _dodgeTimeLeft;
        protected float _immunityTimeLeft;
        protected Vector3 _dashDirection;
        protected Vector3 _dodgeDirection;
        protected Rigidbody _rigidbody;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            InitializeComponents();
            _groundPlane = new Plane(Vector3.up, Vector3.zero);
            _currentDashStacks = _maxDashStacks;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isLocalPlayer)
            {
                SetupCamera();
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            EnablePlayerInput();
            _mainCamera = Camera.main;
        }

        protected virtual void Update()
        {
            if (!isLocalPlayer) return;
            
            HandleMovement();
            HandleAiming();
            UpdateDashStacks();
        }

        protected virtual void FixedUpdate()
        {
            if (!isServer) return;

            if (_isDashing)
            {
                UpdateDash();
            }
            
            if (_isDodging)
            {
                UpdateDodge();
            }

            if (_isImmune)
            {
                UpdateImmunity();
            }
        }
        #endregion

        #region Initialization
        protected virtual void InitializeComponents()
        {
            if (_playerInput == null)
                _playerInput = GetComponent<PlayerInput>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
                _rigidbody.freezeRotation = true;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }

        protected virtual void EnablePlayerInput()
        {
            if (_playerInput == null) return;

            _playerInput.enabled = true;
            _playerInput.actions["Move"].performed += OnMoveInput;
            _playerInput.actions["Move"].canceled += OnMoveInput;
            _playerInput.actions["Aim"].performed += OnAimInput;
            _playerInput.actions["Aim"].canceled += OnAimInput;
            _playerInput.actions["Fire"].performed += OnFireInput;
            _playerInput.actions["Dash"].performed += OnDashInput;
            _playerInput.actions["Dodge"].performed += OnDodgeInput;
        }

        protected virtual void SetupCamera()
        {
            var vcams = FindObjectsOfType<CinemachineCamera>();
            foreach (var vcam in vcams)
            {
                vcam.Follow = transform;
            }
        }
        #endregion

        #region Input Handling
        protected virtual void OnMoveInput(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        protected virtual void OnAimInput(InputAction.CallbackContext context)
        {
            _aimInput = context.ReadValue<Vector2>();
        }

        protected virtual void OnDashInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[Dash] Input received - Stacks: {_currentDashStacks}, IsDashing: {_isDashing}, IsDodging: {_isDodging}");
            
            if (_currentDashStacks > 0 && !_isDashing && !_isDodging)
            {
                Debug.Log("[Dash] Conditions met, requesting dash from server");
                CmdRequestDash();
            }
            else
            {
                if (_currentDashStacks <= 0)
                    Debug.LogWarning("[Dash] Failed - No dash stacks available");
                if (_isDashing)
                    Debug.LogWarning("[Dash] Failed - Already dashing");
                if (_isDodging)
                    Debug.LogWarning("[Dash] Failed - Currently dodging");
            }
        }

        protected virtual void OnDodgeInput(InputAction.CallbackContext context)
        {
            if (!_isDashing && !_isDodging && Time.time - _lastDodgeTime >= _dodgeCooldown)
            {
                CmdRequestDodge();
            }
        }

        protected abstract void OnFireInput(InputAction.CallbackContext context);
        #endregion

        #region Movement & Combat
        protected virtual void HandleMovement()
        {
            if (_isDodging) return;

            Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 movement = moveDirection * _moveSpeed * Time.deltaTime;
                transform.position += movement;
                UpdateAnimator(1f);
            }
            else
            {
                UpdateAnimator(0f);
            }
        }

        protected virtual void HandleAiming()
        {
            if (_mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (_groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldAimPoint = ray.GetPoint(distance);
                Vector3 aimDirection = (worldAimPoint - transform.position).normalized;
                aimDirection.y = 0f;

                if (aimDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, 
                        Quaternion.LookRotation(aimDirection), 
                        Time.deltaTime * _rotationSpeed);

                    if (_weaponPivot != null)
                    {
                        _weaponPivot.rotation = transform.rotation;
                    }
                }
            }
        }

        protected virtual void UpdateAnimator(float moveSpeed)
        {
            if (_animator != null)
            {
                _animator.SetFloat("MoveSpeed", moveSpeed);
                _animator.SetBool("IsMoving", moveSpeed > 0.1f);
            }
        }

        protected virtual void UpdateDashStacks()
        {
            if (_currentDashStacks < _maxDashStacks && Time.time - _lastDashTime >= _dashCooldown)
            {
                _currentDashStacks++;
                _lastDashTime = Time.time;
                Debug.Log($"[Dash] Stack replenished - Current Stacks: {_currentDashStacks}");
            }
        }

        protected virtual void UpdateDash()
        {
            if (!_isDashing || !isServer) return;

            float normalizedTime = 1 - (_dashTimeLeft / _dashDuration);
            Vector3 dashMovement = _dashDirection * _dashSpeed;
            
            // Server'da pozisyonu güncelle
            Vector3 newPosition = transform.position + (dashMovement * Time.fixedDeltaTime);
            transform.position = newPosition;
            
            Debug.Log($"[Dash][Physics] Position Delta: {dashMovement * Time.fixedDeltaTime}, Current Position: {transform.position}");

            _dashTimeLeft -= Time.fixedDeltaTime;

            if (_dashTimeLeft <= 0)
            {
                Debug.Log("[Dash] Completed naturally");
                CompleteDash();
            }
        }

        protected virtual void UpdateDodge()
        {
            if (!_isDodging) return;

            float normalizedTime = 1 - (_dodgeTimeLeft / _dodgeDuration);
            float speedMultiplier = _dodgeSpeedCurve.Evaluate(normalizedTime);
            Vector3 dodgeVelocity = _dodgeDirection * (_dodgeSpeed * speedMultiplier);

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = dodgeVelocity;
            }
            else
            {
                transform.position += dodgeVelocity * Time.fixedDeltaTime;
            }

            _dodgeTimeLeft -= Time.fixedDeltaTime;

            if (_dodgeTimeLeft <= 0)
            {
                CompleteDodge();
            }
        }

        protected virtual void UpdateImmunity()
        {
            if (!_isImmune) return;

            _immunityTimeLeft -= Time.fixedDeltaTime;
            if (_immunityTimeLeft <= 0)
            {
                _isImmune = false;
                RpcOnImmunityEnd();
            }
        }

        protected virtual void CompleteDash()
        {
            if (!isServer) return;

            Debug.Log($"[Dash] Completing - Final Position: {transform.position}");
            _isDashing = false;
            
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false; // Fizik etkileşimlerini tekrar etkinleştir
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            
            RpcOnDashComplete();
        }

        protected virtual void CompleteDodge()
        {
            _isDodging = false;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
            }
            RpcOnDodgeComplete();
        }
        #endregion

        #region Network Commands
        [Command]
        protected virtual void CmdRequestDash()
        {
            Debug.Log($"[Dash][Server] Request received - Stacks: {_currentDashStacks}, IsDashing: {_isDashing}");
            
            if (_currentDashStacks <= 0 || _isDashing || _isDodging)
            {
                Debug.LogWarning("[Dash][Server] Request rejected - Invalid state");
                return;
            }

            _isDashing = true;
            _currentDashStacks--;
            _dashTimeLeft = _dashDuration;
            
            // Hareket yönünü kullan, eğer hareket yoksa karakterin baktığı yönü kullan
            Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y);
            _dashDirection = moveDirection != Vector3.zero ? moveDirection.normalized : transform.forward;
            
            // Dash başlangıcında Rigidbody'yi sıfırla
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true; // Dash sırasında fizik etkileşimlerini devre dışı bırak
            }
            
            Debug.Log($"[Dash][Server] Started - Direction: {_dashDirection}, Duration: {_dashDuration}s");
            RpcOnDashStarted(_dashDirection);
        }

        [Command]
        protected virtual void CmdRequestDodge()
        {
            if (_isDashing || _isDodging || Time.time - _lastDodgeTime < _dodgeCooldown) return;

            _isDodging = true;
            _isImmune = true;
            _dodgeTimeLeft = _dodgeDuration;
            _immunityTimeLeft = _dodgeImmunityDuration;
            _dodgeDirection = _moveInput.normalized;
            _lastDodgeTime = Time.time;

            // Dodge başlangıç efektini tetikle
            RpcOnDodgeStarted(_dodgeDirection);
        }

        protected abstract void CmdRequestAttack(AttackType attackType);
        #endregion

        #region ClientRpc Callbacks
        [ClientRpc]
        protected virtual void RpcOnDashStarted(Vector3 direction)
        {
            Debug.Log($"[Dash][Client] Started - Direction: {direction}");
            if (_animator != null)
            {
                _animator.SetTrigger("Dash");
                _animator.SetFloat("DashDirectionX", direction.x);
                _animator.SetFloat("DashDirectionZ", direction.z);
                Debug.Log("[Dash][Client] Animation triggered");
            }

            // Client tarafında da Rigidbody'yi devre dışı bırak
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
        }

        [ClientRpc]
        protected virtual void RpcOnDodgeStarted(Vector3 direction)
        {
            // Visual efektleri ve animasyonları tetikle
            if (_animator != null)
            {
                _animator.SetTrigger("Dodge");
                _animator.SetFloat("DodgeDirectionX", direction.x);
                _animator.SetFloat("DodgeDirectionZ", direction.z);
            }
        }

        [ClientRpc]
        protected virtual void RpcOnDashComplete()
        {
            Debug.Log("[Dash][Client] Completed");
            if (_animator != null)
            {
                _animator.ResetTrigger("Dash");
            }

            // Client tarafında Rigidbody'yi tekrar etkinleştir
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
            }
        }

        [ClientRpc]
        protected virtual void RpcOnDodgeComplete()
        {
            if (_animator != null)
            {
                _animator.ResetTrigger("Dodge");
            }
        }

        [ClientRpc]
        protected virtual void RpcOnImmunityEnd()
        {
            // İmmunity görsel efektlerini kapat
        }
        #endregion

        #region Cleanup
        protected virtual void OnDisable()
        {
            if (_playerInput != null)
            {
                _playerInput.actions["Move"].performed -= OnMoveInput;
                _playerInput.actions["Move"].canceled -= OnMoveInput;
                _playerInput.actions["Aim"].performed -= OnAimInput;
                _playerInput.actions["Aim"].canceled -= OnAimInput;
                _playerInput.actions["Fire"].performed -= OnFireInput;
                _playerInput.actions["Dash"].performed -= OnDashInput;
                _playerInput.actions["Dodge"].performed -= OnDodgeInput;
            }
        }
        #endregion

        #region Public Methods
        public virtual bool IsImmune()
        {
            return _isImmune;
        }

        public virtual bool CanPerformAction()
        {
            return !_isDashing && !_isDodging;
        }
        #endregion
    }
} 