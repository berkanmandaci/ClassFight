using UnityEngine;
using Mirror;
using ProjectV3.Shared.Data;
using System.Collections.Generic;

namespace ProjectV3.Server.Player
{
    /// <summary>
    /// Sunucu tarafında oyuncu mantığını yöneten sınıf
    /// </summary>
    public class ServerPlayerController : NetworkBehaviour
    {
        #region Properties
        [Header("Player Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField] private float _pushForce = 5f;

        private float _currentHealth;
        private PlayerState _currentState = PlayerState.Idle;
        private readonly HashSet<int> _activeTargets = new();
        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;
        #endregion

        #region SyncVars
        [SyncVar(hook = nameof(OnHealthChanged))]
        private float _syncedHealth;

        [SyncVar(hook = nameof(OnStateChanged))]
        private PlayerState _syncedState;

        [SyncVar]
        private Vector3 _syncedPosition;

        [SyncVar]
        private Quaternion _syncedRotation;
        #endregion

        #region Unity Lifecycle
        public override void OnStartServer()
        {
            base.OnStartServer();
            InitializeComponents();
            _currentHealth = _maxHealth;
            _syncedHealth = _currentHealth;
            UpdatePlayerState(PlayerState.Idle);
        }

        private void InitializeComponents()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            }
        }

        private void FixedUpdate()
        {
            if (!isServer) return;
            UpdateSyncedTransform();
        }
        #endregion

        #region Network Callbacks
        [Command]
        public void CmdMove(Vector3 movement, float rotation)
        {
            if (_currentState == PlayerState.Dead) return;

            // Çarpışma kontrolü
            Vector3 targetPosition = transform.position + movement;
            if (!Physics.CapsuleCast(transform.position, transform.position + Vector3.up * _collider.height, 
                _collider.radius, movement.normalized, out RaycastHit hit, movement.magnitude, _collisionMask))
            {
                // Hareket ve rotasyonu uygula
                transform.position = targetPosition;
                transform.rotation = Quaternion.Euler(0, rotation, 0);

                // Hareket durumunu güncelle
                UpdatePlayerState(movement.magnitude > 0.1f ? PlayerState.Moving : PlayerState.Idle);
            }
            else
            {
                // Çarpışma durumunda geri tepme uygula
                if (hit.rigidbody != null)
                {
                    hit.rigidbody.AddForce(-movement.normalized * _pushForce, ForceMode.Impulse);
                }
            }
        }

        [Command]
        public void CmdAttack(int targetId, AttackType attackType)
        {
            if (_currentState == PlayerState.Dead || _currentState == PlayerState.Attacking) return;

            UpdatePlayerState(PlayerState.Attacking);
            
            // Saldırı verilerini hazırla
            var attackData = new AttackData
            {
                AttackerId = (int)netId,
                TargetId = targetId,
                Type = attackType,
                DamageType = DamageType.Physical,
                Damage = CalculateDamage(attackType),
                ImpactPoint = transform.position + transform.forward * 2f,
                ImpactNormal = -transform.forward
            };

            // Saldırıyı işle ve sonucu yayınla
            RpcOnAttackPerformed(attackData);
        }

        [Command]
        public void CmdTakeDamage(AttackData attackData)
        {
            if (_currentState == PlayerState.Dead) return;

            float finalDamage = CalculateFinalDamage(attackData);
            _currentHealth -= finalDamage;
            _syncedHealth = _currentHealth;

            // Hasar yönünde geri tepme uygula
            if (_rigidbody != null)
            {
                Vector3 knockbackDirection = (transform.position - attackData.ImpactPoint).normalized;
                _rigidbody.AddForce(knockbackDirection * _pushForce, ForceMode.Impulse);
            }

            var damageResult = new DamageResult
            {
                TargetId = (int)netId,
                DamageDealt = finalDamage,
                RemainingHealth = _currentHealth,
                IsCritical = false,
                IsKillingBlow = _currentHealth <= 0
            };

            if (damageResult.IsKillingBlow)
            {
                Die();
            }

            RpcOnDamageReceived(damageResult);
        }
        #endregion

        #region Private Methods
        private void UpdatePlayerState(PlayerState newState)
        {
            _currentState = newState;
            _syncedState = newState;
        }

        private void UpdateSyncedTransform()
        {
            _syncedPosition = transform.position;
            _syncedRotation = transform.rotation;
        }

        private float CalculateDamage(AttackType attackType)
        {
            return attackType switch
            {
                AttackType.LightAttack => 10f,
                AttackType.HeavyAttack => 20f,
                AttackType.SpecialAttack => 30f,
                _ => 0f
            };
        }

        private float CalculateFinalDamage(AttackData attackData)
        {
            // Basit hasar hesaplama - geliştirilebilir
            return attackData.Damage;
        }

        private void Die()
        {
            UpdatePlayerState(PlayerState.Dead);
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
            // Ölüm işlemleri burada yapılabilir
        }
        #endregion

        #region SyncVar Hooks
        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            _currentHealth = newHealth;
        }

        private void OnStateChanged(PlayerState oldState, PlayerState newState)
        {
            _currentState = newState;
        }
        #endregion

        #region ClientRpc Methods
        [ClientRpc]
        private void RpcOnAttackPerformed(AttackData attackData)
        {
            // İstemci tarafında saldırı efektlerini tetikle
        }

        [ClientRpc]
        private void RpcOnDamageReceived(DamageResult damageResult)
        {
            // İstemci tarafında hasar efektlerini tetikle
        }
        #endregion
    }
} 