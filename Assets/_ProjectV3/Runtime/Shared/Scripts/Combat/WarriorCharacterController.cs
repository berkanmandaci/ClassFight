using ProjectV3.Shared.Vo;
using UnityEngine;
using Mirror;

namespace ProjectV3.Shared.Combat
{
    public class WarriorCharacterController : BaseCharacter, ICharacterController
    {
        [Header("Saldırı Ayarları")]
        [SerializeField] private float _baseDamage = 15f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private LayerMask _attackLayer;

        private int _comboCount = 0;
        private float _lastAttackTime;
        private readonly float _comboResetTime = 1.5f;

        public void OnMove(Vector2 moveInput)
        {
            // Base controller handles movement
        }

        public void OnAim(Vector2 aimInput)
        {
            // Base controller handles aiming
        }

        [Command]
        private void CmdAttackPerform(Vector3 position, Vector3 direction)
        {
            // Çarpışma kontrolü yap
            RaycastHit[] hits = Physics.SphereCastAll(position, _attackRange, direction, _attackRange, _attackLayer);
            
            foreach (var hit in hits)
            {
                var targetController = hit.collider.GetComponent<BaseCharacterController>();
                if (targetController != null && targetController != _characterController)
                {
                    var targetCombatData = targetController.GetCombatData();
                    if (targetCombatData != null && !CombatArenaModel.Instance.AreTeammates(_combatData, targetCombatData))
                    {
                        float damage = _baseDamage * (1 + (_comboCount * 0.5f)); // Her combo ile %50 artan hasar
                        targetCombatData.TakeDamage(damage, _combatData);
                        Debug.Log($"[Warrior] Hedefe vuruldu: {targetCombatData.UserData.DisplayName}, Hasar: {damage}, Combo: {_comboCount + 1}");
                    }
                }
            }

            // Vuruş efektini oynat
            RpcPlayAttackEffect(_comboCount);
        }

        [ClientRpc]
        private void RpcPlayAttackEffect(int comboCount)
        {
            // TODO: Combo sayısına göre farklı efektler oynat
            Debug.Log($"[Warrior] Saldırı efekti oynatılıyor - Combo: {comboCount + 1}");
        }

        public void OnAttack()
        {
            if (!isLocalPlayer) return;

            float currentTime = Time.time;
            
            // Combo sistemini kontrol et
            if (currentTime - _lastAttackTime > _comboResetTime)
            {
                _comboCount = 0;
            }

            // Maximum 3 combo
            if (_comboCount >= 3)
            {
                _comboCount = 0;
            }

            _lastAttackTime = currentTime;
            
            // Server'da saldırıyı gerçekleştir
            CmdAttackPerform(transform.position, transform.forward);
            
            _comboCount++;
            
            Debug.Log($"[Warrior] Saldırı başlatıldı - Combo: {_comboCount}");
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
