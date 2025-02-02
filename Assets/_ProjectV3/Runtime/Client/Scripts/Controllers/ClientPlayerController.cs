using UnityEngine;
using Mirror;
using ProjectV3.Client.Views;
using ProjectV3.Shared.Data;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

namespace ProjectV3.Client.Controllers
{
    /// <summary>
    /// İstemci tarafında oyuncu kontrollerini yöneten sınıf
    /// </summary>
    public class ClientPlayerController : NetworkBehaviour
    {
        #region Properties
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 15f;

        [Header("References")]
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private PlayerVisualController _visualController;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _weaponPivot;

        private Vector2 _moveInput;
        private Vector2 _aimInput;
        private Camera _mainCamera;
        private Plane _groundPlane;
        private bool _isAttacking;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
            _groundPlane = new Plane(Vector3.up, Vector3.zero);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            EnablePlayerInput();
            _mainCamera = Camera.main;
            
            // Tüm Cinemachine kameralarını bul ve hedefi ayarla
            var vcams = FindObjectsOfType<CinemachineCamera>();
            foreach (var vcam in vcams)
            {
                vcam.Follow = transform;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;
            
            HandleMovement();
            HandleAiming();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            if (_playerInput == null)
                _playerInput = GetComponent<PlayerInput>();

            if (_visualController == null)
                _visualController = GetComponentInChildren<PlayerVisualController>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }
        #endregion

        #region Input Handling
        private void EnablePlayerInput()
        {
            _playerInput.enabled = true;
            _playerInput.actions["Move"].performed += OnMoveInput;
            _playerInput.actions["Move"].canceled += OnMoveInput;
            _playerInput.actions["Aim"].performed += OnAimInput;
            _playerInput.actions["Aim"].canceled += OnAimInput;
            _playerInput.actions["Fire"].performed += OnFireInput;
        }

        private void OnMoveInput(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
            Debug.Log($"[Movement] Input received: {_moveInput}");
        }

        private void OnAimInput(InputAction.CallbackContext context)
        {
            _aimInput = context.ReadValue<Vector2>();
        }

        private void OnFireInput(InputAction.CallbackContext context)
        {
            if (_isAttacking) return;
            _isAttacking = true;

            CmdRequestAttack(AttackType.LightAttack);
            StartCoroutine(ResetAttackFlag());
        }
        #endregion

        #region Movement & Aiming
        private void HandleMovement()
        {
            // Hareket yönünü hesapla
            Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
            Debug.Log($"[Movement] Direction: {moveDirection}, Magnitude: {moveDirection.magnitude}");

            // Hareketi uygula
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 movement = moveDirection * _moveSpeed * Time.deltaTime;
                Debug.Log($"[Movement] Applying movement: {movement}, Speed: {_moveSpeed}");
                CmdMove(movement, transform.rotation.eulerAngles.y);
            }
        }

        private void HandleAiming()
        {
            if (_mainCamera == null) return;

            // Fare pozisyonundan ışın gönder
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Yerle kesişim noktasını bul
            if (_groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldAimPoint = ray.GetPoint(distance);
                Vector3 aimDirection = (worldAimPoint - transform.position).normalized;
                aimDirection.y = 0f;

                // Karakteri hedef noktaya döndür
                if (aimDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(aimDirection);

                    // Silah pivot'ını döndür
                    if (_weaponPivot != null)
                    {
                        _weaponPivot.rotation = transform.rotation;
                    }
                }
            }
        }

        private void UpdateAnimator(float moveSpeed)
        {
            if (_animator != null)
            {
                _animator.SetFloat("MoveSpeed", moveSpeed);
                _animator.SetBool("IsMoving", moveSpeed > 0.1f);
            }
        }
        #endregion

        #region Network Commands
        [Command]
        private void CmdMove(Vector3 movement, float rotation)
        {
            Debug.Log($"[Server] CmdMove received - Movement: {movement}, Rotation: {rotation}");
            
            // Anti-cheat kontrolleri
            if (movement.magnitude > _moveSpeed * Time.deltaTime * 1.5f)
            {
                Debug.LogWarning($"[Server] Movement rejected - Too fast: {movement.magnitude}");
                return;
            }

            // Karakterin pozisyonunu güncelle
            transform.position += movement;
            transform.rotation = Quaternion.Euler(0, rotation, 0);
            Debug.Log($"[Server] Position updated - New Position: {transform.position}");

            // Tüm istemcilere hareket bilgisini gönder
            RpcUpdateMovement(transform.position, rotation);
        }

        [ClientRpc]
        private void RpcUpdateMovement(Vector3 position, float rotation)
        {
            if (!isLocalPlayer)
            {
                Debug.Log($"[Client] Updating other player - Position: {position}, Rotation: {rotation}");
                // Diğer oyuncuların pozisyonlarını güncelle
                transform.position = position;
                transform.rotation = Quaternion.Euler(0, rotation, 0);

                // Animasyonları güncelle
                if (_animator != null)
                {
                    _animator.SetBool("IsMoving", true);
                    _animator.SetFloat("MoveSpeed", 1f);
                }
            }
        }

        [Command]
        private void CmdRequestAttack(AttackType attackType)
        {
            // Sunucuya saldırı isteğini gönder
            _isAttacking = false;
        }
        #endregion

        #region Network Callbacks
        [ClientRpc]
        private void RpcOnAttackPerformed(AttackData attackData)
        {
            if (_visualController != null)
            {
                _visualController.PlayAttackAnimation(attackData.Type);
            }
        }

        [ClientRpc]
        private void RpcOnDamageReceived(DamageResult damageResult)
        {
            if (_visualController != null)
            {
                _visualController.PlayHitAnimation();
                _visualController.UpdateHealthBar(damageResult.RemainingHealth);
            }
        }
        #endregion

        #region Cleanup
        private void OnDisable()
        {
            if (_playerInput != null)
            {
                _playerInput.actions["Move"].performed -= OnMoveInput;
                _playerInput.actions["Move"].canceled -= OnMoveInput;
                _playerInput.actions["Aim"].performed -= OnAimInput;
                _playerInput.actions["Aim"].canceled -= OnAimInput;
                _playerInput.actions["Fire"].performed -= OnFireInput;
            }
        }

        private System.Collections.IEnumerator ResetAttackFlag()
        {
            yield return new WaitForSeconds(0.2f); // Ateş hızını ayarlamak için
            _isAttacking = false;
        }
        #endregion
    }
} 