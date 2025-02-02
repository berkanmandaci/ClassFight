using UnityEngine;
using Mirror;
using ProjectV3.Shared.Data;
using System.Collections.Generic;
using ProjectV3.Server.Player;

namespace ProjectV3.Server.Combat
{
    /// <summary>
    /// Sunucu tarafında savaş sistemini yöneten sınıf
    /// </summary>
    public class ServerCombatSystem : NetworkBehaviour
    {
        #region Properties
        [Header("Combat Settings")]
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _criticalHitChance = 0.1f;
        [SerializeField] private float _criticalHitMultiplier = 2f;
        [SerializeField] private LayerMask _targetLayers;

        private readonly Dictionary<int, float> _lastAttackTime = new();
        private readonly Dictionary<AttackType, float> _attackCooldowns = new()
        {
            { AttackType.LightAttack, 0.5f },
            { AttackType.HeavyAttack, 1.5f },
            { AttackType.SpecialAttack, 3f }
        };
        #endregion

        #region Server Methods
        /// <summary>
        /// Saldırının geçerli olup olmadığını kontrol eder
        /// </summary>
        public bool ValidateAttack(int attackerId, int targetId, AttackType attackType)
        {
            if (!_lastAttackTime.TryGetValue(attackerId, out float lastAttack))
            {
                _lastAttackTime[attackerId] = 0f;
                return true;
            }

            float cooldown = _attackCooldowns[attackType];
            if (Time.time - lastAttack < cooldown)
            {
                return false;
            }

            _lastAttackTime[attackerId] = Time.time;
            return true;
        }

        /// <summary>
        /// Saldırı hasarını hesaplar
        /// </summary>
        public float CalculateDamage(AttackData attackData)
        {
            float baseDamage = GetBaseDamage(attackData.Type);
            bool isCritical = Random.value < _criticalHitChance;
            float damageMultiplier = isCritical ? _criticalHitMultiplier : 1f;

            return baseDamage * damageMultiplier;
        }

        /// <summary>
        /// Hedefin menzilde olup olmadığını kontrol eder
        /// </summary>
        public bool IsTargetInRange(Vector3 attackerPosition, Vector3 targetPosition)
        {
            return Vector3.Distance(attackerPosition, targetPosition) <= _attackRange;
        }

        /// <summary>
        /// Çarpışma tespiti yapar
        /// </summary>
        public bool PerformHitDetection(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
        {
            return Physics.Raycast(origin, direction, out hitInfo, _attackRange, _targetLayers);
        }
        #endregion

        #region Private Methods
        private float GetBaseDamage(AttackType attackType)
        {
            return attackType switch
            {
                AttackType.LightAttack => 10f,
                AttackType.HeavyAttack => 25f,
                AttackType.SpecialAttack => 40f,
                _ => 0f
            };
        }
        #endregion

        #region Network Messages
        [ClientRpc]
        private void RpcOnAttackBlocked(AttackData attackData)
        {
            // İstemci tarafında saldırı bloklama efektlerini tetikle
        }

        [ClientRpc]
        private void RpcOnAttackMissed(AttackData attackData)
        {
            // İstemci tarafında ıskalama efektlerini tetikle
        }

        [ClientRpc]
        private void RpcOnCriticalHit(AttackData attackData)
        {
            // İstemci tarafında kritik vuruş efektlerini tetikle
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Saldırı işlemini gerçekleştirir
        /// </summary>
        public void ProcessAttack(NetworkIdentity attacker, NetworkIdentity target, AttackType attackType)
        {
            if (!isServer) return;

            if (!ValidateAttack((int)attacker.netId, (int)target.netId, attackType))
            {
                return;
            }

            if (!IsTargetInRange(attacker.transform.position, target.transform.position))
            {
                RpcOnAttackMissed(new AttackData { 
                    AttackerId = (int)attacker.netId, 
                    TargetId = (int)target.netId 
                });
                return;
            }

            var attackData = new AttackData
            {
                AttackerId = (int)attacker.netId,
                TargetId = (int)target.netId,
                Type = attackType,
                DamageType = DamageType.Physical,
                Damage = CalculateDamage(new AttackData { Type = attackType }),
                ImpactPoint = target.transform.position,
                ImpactNormal = (attacker.transform.position - target.transform.position).normalized
            };

            if (target.TryGetComponent<ServerPlayerController>(out var targetController))
            {
                targetController.CmdTakeDamage(attackData);
            }
        }
        #endregion
    }
} 