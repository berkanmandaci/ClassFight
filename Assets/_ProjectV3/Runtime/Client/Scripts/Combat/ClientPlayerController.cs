using UnityEngine;
using Mirror;
using ProjectV3.Client.Views;
using ProjectV3.Shared.Data;
using ProjectV3.Shared.Combat;
using UnityEngine.InputSystem;

namespace ProjectV3.Client.Combat
{
    /// <summary>
    /// İstemci tarafında oyuncu kontrollerini yöneten sınıf
    /// </summary>
    public class ClientPlayerController : BaseCharacterController
    {
        [Header("Client References")]
        [SerializeField] private PlayerVisualController _visualController;

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            Debug.Log("[Setup] ClientPlayerController initialized");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // Visual Controller'ı aktifleştir
            if (_visualController != null)
            {
                _visualController.gameObject.SetActive(true);
            }
        }
        #endregion

        #region Initialization
        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            
            if (_visualController == null)
                _visualController = GetComponentInChildren<PlayerVisualController>();
        }
        #endregion

        #region Input Handling
        protected override void OnFireInput(InputAction.CallbackContext context)
        {
            if (_isAttacking) return;
            _isAttacking = true;

            CmdRequestAttack(AttackType.LightAttack);
            StartCoroutine(ResetAttackFlag());
        }
        #endregion

        #region Network Commands
        [Command]
        protected override void CmdRequestAttack(AttackType attackType)
        {
            // Sunucuya saldırı isteğini gönder
            _isAttacking = false;
            RpcOnAttackPerformed(new AttackData { Type = attackType });
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

        #region Utility
        private System.Collections.IEnumerator ResetAttackFlag()
        {
            yield return new WaitForSeconds(0.2f); // Ateş hızını ayarlamak için
            _isAttacking = false;
        }
        #endregion
    }
} 