using ProjectV3.Shared.Vo;
using UnityEngine;
using Mirror;

namespace ProjectV3.Shared.Combat
{
    public class TankCharacterController : BaseCharacter, ICharacterController
    {
        [Header("Kalkan Ayarları")]
        [SerializeField] private float _shieldDamage = 10f;
        [SerializeField] private float _shieldRange = 2f;
        [SerializeField] private float _shieldCooldown = 1f;
        [SerializeField] private LayerMask _shieldLayer;

        private float _lastShieldTime;
        private bool _isShieldActive;

        public void OnMove(Vector2 moveInput)
        {
            // Base controller handles movement
        }

        public void OnAim(Vector2 aimInput)
        {
            // Base controller handles aiming
        }

        [Command]
        private void CmdShieldAttack(Vector3 position, Vector3 direction)
        {
            // Çarpışma kontrolü yap
            RaycastHit[] hits = Physics.SphereCastAll(position, _shieldRange, direction, _shieldRange, _shieldLayer);
            
            foreach (var hit in hits)
            {
                var targetController = hit.collider.GetComponent<BaseCharacterController>();
                if (targetController != null && targetController != _characterController)
                {
                    var targetCombatData = targetController.GetCombatData();
                    if (targetCombatData != null && !CombatArenaModel.Instance.AreTeammates(_combatData, targetCombatData))
                    {
                        targetCombatData.TakeDamage(_shieldDamage, _combatData);
                        Debug.Log($"[Tank] Kalkan ile vuruldu: {targetCombatData.UserData.DisplayName}, Hasar: {_shieldDamage}");
                    }
                }
            }

            // Kalkan efektini oynat
            RpcPlayShieldEffect();
        }

        [ClientRpc]
        private void RpcPlayShieldEffect()
        {
            // TODO: Kalkan efektlerini oynat
            Debug.Log("[Tank] Kalkan efekti oynatılıyor");
        }

        public void OnAttack()
        {
            if (!isLocalPlayer) return;

            float currentTime = Time.time;
            if (currentTime - _lastShieldTime < _shieldCooldown)
            {
                Debug.Log($"[Tank] Kalkan bekleme süresinde! Kalan süre: {_shieldCooldown - (currentTime - _lastShieldTime):F1}");
                return;
            }

            _lastShieldTime = currentTime;
            _isShieldActive = true;

            // Server'da kalkan saldırısını gerçekleştir
            CmdShieldAttack(transform.position, transform.forward);

            Debug.Log("[Tank] Kalkan saldırısı başlatıldı");
        }

        public void OnDash()
        {
            // Base controller handles dash
        }

        public void OnDodge()
        {
            // Base controller handles dodge
        }
    }
}
