using UnityEngine;
using ProjectV3.Shared.Data;

namespace ProjectV3.Client.Views
{
    /// <summary>
    /// Oyuncu animasyonlarını yöneten sınıf
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        #region Properties
        [Header("Animation Settings")]
        [SerializeField] private Animator _animator;
        [SerializeField] private float _movementBlendDampTime = 0.1f;
        [SerializeField] private float _attackBlendDampTime = 0.05f;

        [Header("Animation Parameters")]
        [SerializeField] private string _moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string _isMovingParameter = "IsMoving";
        [SerializeField] private string _attackTriggerParameter = "TriggerAttack";
        [SerializeField] private string _attackTypeParameter = "AttackType";
        [SerializeField] private string _hitTriggerParameter = "TriggerHit";
        [SerializeField] private string _deathTriggerParameter = "TriggerDeath";
        [SerializeField] private string _isDeadParameter = "IsDead";

        private int _moveSpeedHash;
        private int _isMovingHash;
        private int _attackTriggerHash;
        private int _attackTypeHash;
        private int _hitTriggerHash;
        private int _deathTriggerHash;
        private int _isDeadHash;

        private bool _isDead;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
            CacheAnimationParameters();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
        }

        private void CacheAnimationParameters()
        {
            _moveSpeedHash = Animator.StringToHash(_moveSpeedParameter);
            _isMovingHash = Animator.StringToHash(_isMovingParameter);
            _attackTriggerHash = Animator.StringToHash(_attackTriggerParameter);
            _attackTypeHash = Animator.StringToHash(_attackTypeParameter);
            _hitTriggerHash = Animator.StringToHash(_hitTriggerParameter);
            _deathTriggerHash = Animator.StringToHash(_deathTriggerParameter);
            _isDeadHash = Animator.StringToHash(_isDeadParameter);
        }
        #endregion

        #region Animation Control
        /// <summary>
        /// Hareket animasyonunu günceller
        /// </summary>
        public void UpdateMovementAnimation(float moveSpeed, bool isMoving)
        {
            if (_isDead || _animator == null) return;

            _animator.SetFloat(_moveSpeedHash, moveSpeed, _movementBlendDampTime, Time.deltaTime);
            _animator.SetBool(_isMovingHash, isMoving);
        }

        /// <summary>
        /// Saldırı animasyonunu tetikler
        /// </summary>
        public void TriggerAttackAnimation(AttackType attackType)
        {
            if (_isDead || _animator == null) return;

            _animator.SetInteger(_attackTypeHash, (int)attackType);
            _animator.SetTrigger(_attackTriggerHash);
        }

        /// <summary>
        /// Hasar alma animasyonunu tetikler
        /// </summary>
        public void TriggerHitAnimation()
        {
            if (_isDead || _animator == null) return;

            _animator.SetTrigger(_hitTriggerHash);
        }

        /// <summary>
        /// Ölüm animasyonunu tetikler
        /// </summary>
        public void TriggerDeathAnimation()
        {
            if (_isDead || _animator == null) return;

            _isDead = true;
            _animator.SetBool(_isDeadHash, true);
            _animator.SetTrigger(_deathTriggerHash);
        }

        /// <summary>
        /// Oyuncuyu canlandırır
        /// </summary>
        public void Revive()
        {
            if (!_isDead || _animator == null) return;

            _isDead = false;
            _animator.SetBool(_isDeadHash, false);
        }
        #endregion

        #region Animation Events
        /// <summary>
        /// Saldırı animasyonu bittiğinde çağrılır
        /// </summary>
        public void OnAttackAnimationEnd()
        {
            // Saldırı sonrası işlemler burada yapılabilir
        }

        /// <summary>
        /// Hasar alma animasyonu bittiğinde çağrılır
        /// </summary>
        public void OnHitAnimationEnd()
        {
            // Hasar alma sonrası işlemler burada yapılabilir
        }

        /// <summary>
        /// Ölüm animasyonu bittiğinde çağrılır
        /// </summary>
        public void OnDeathAnimationEnd()
        {
            // Ölüm sonrası işlemler burada yapılabilir
        }
        #endregion

        #region Animation State Info
        /// <summary>
        /// Belirli bir animasyon katmanının normalleştirilmiş zamanını döndürür
        /// </summary>
        public float GetLayerNormalizedTime(int layerIndex)
        {
            if (_animator == null) return 0f;

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(layerIndex);
            return stateInfo.normalizedTime;
        }

        /// <summary>
        /// Belirli bir animasyon durumunun aktif olup olmadığını kontrol eder
        /// </summary>
        public bool IsInAnimationState(int layerIndex, string stateName)
        {
            if (_animator == null) return false;

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(layerIndex);
            return stateInfo.IsName(stateName);
        }
        #endregion
    }
} 