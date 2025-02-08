using UnityEngine;
using Mirror;
using System.Collections;
using ProjectV3.Shared.Vo;

namespace ProjectV3.Shared.Combat
{
    public class ArcherCharacterController : NetworkBehaviour, ICharacterController
    {
        [Header("Ok Ayarları")]
        [SerializeField] private GameObject _arrowPrefab;
        [SerializeField] private Transform _arrowSpawnPoint;
        [SerializeField] private float _arrowSpeed = 20f;
        [SerializeField] private float _arrowLifetime = 3f;

        private float _lastAttackTime;
        private readonly float _attackCooldown = 1f;
        private CombatUserVo _combatData;

        public void Init(CombatUserVo combatUserVo)
        {
            _combatData = combatUserVo;
            Debug.Log($"[Archer] Başlatıldı - Oyuncu: {_combatData?.UserData?.DisplayName ?? "Bilinmeyen"}");
        }

        public void OnMove(Vector2 moveInput)
        {
            // Base controller handles movement
        }

        public void OnAim(Vector2 aimInput)
        {
            // Base controller handles aiming
        }

        public void OnAttack()
        {
            if (Time.time - _lastAttackTime < _attackCooldown)
            {
                Debug.Log("[Archer] Saldırı bekleme süresinde!");
                return;
            }

            _lastAttackTime = Time.time;
            CmdShootArrow(transform.position, transform.rotation);
            Debug.Log("[Archer] Ok atışı başlatıldı!");
        }

        [Command]
        private void CmdShootArrow(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (_combatData == null)
            {
                Debug.LogError("[Archer] Combat verisi bulunamadı!");
                return;
            }

            if (_arrowPrefab == null)
            {
                Debug.LogError("[Archer] Arrow prefab atanmamış!");
                return;
            }

            if (_arrowSpawnPoint == null)
            {
                Debug.LogError("[Archer] Arrow spawn point atanmamış!");
                return;
            }

            Debug.Log($"[Archer] {_combatData.UserData.DisplayName} ok atıyor! - Pozisyon: {_arrowSpawnPoint.position}, Rotasyon: {_arrowSpawnPoint.rotation}");

            // Server'da ok oluştur
            GameObject arrowObj = Instantiate(_arrowPrefab, _arrowSpawnPoint.position, spawnRotation);
            if (arrowObj == null)
            {
                Debug.LogError("[Archer] Ok instantiate edilemedi!");
                return;
            }

            // Ok bileşenini ayarla
            var arrow = arrowObj.GetComponent<ArrowProjectile>();
            if (arrow == null)
            {
                Debug.LogError("[Archer] ArrowProjectile bileşeni bulunamadı!");
                Destroy(arrowObj);
                return;
            }

            arrow.Initialize(_combatData);
            Debug.Log($"[Archer] Ok başlatıldı - NetworkId: {arrow.netId}");

            // Ok'a hız ver
            Rigidbody arrowRb = arrowObj.GetComponent<Rigidbody>();
            if (arrowRb != null)
            {
                arrowRb.linearVelocity = spawnRotation * Vector3.forward * _arrowSpeed;
                Debug.Log($"[Archer] Ok'a hız verildi - Hız: {arrowRb.linearVelocity.magnitude}");
            }
            else
            {
                Debug.LogError("[Archer] Ok'un Rigidbody bileşeni bulunamadı!");
            }

            // Network üzerinde spawn et
            NetworkServer.Spawn(arrowObj);
            Debug.Log($"[Archer] Ok ağa spawn edildi - NetworkId: {arrow.netId}");

            // Belirli süre sonra ok'u yok et
            StartCoroutine(DestroyArrowAfterDelay(arrowObj));
        }

        private System.Collections.IEnumerator DestroyArrowAfterDelay(GameObject arrow)
        {
            Debug.Log("[Archer] Ok yok etme sayacı başladı");
            yield return new WaitForSeconds(_arrowLifetime);
            
            if (arrow != null && NetworkServer.active)
            {
                Debug.Log("[Archer] Ok süre dolduğu için yok ediliyor");
                NetworkServer.Destroy(arrow);
            }
            else
            {
                Debug.Log("[Archer] Ok zaten yok edilmiş veya server aktif değil");
            }
        }

        [ClientRpc]
        private void RpcOnArrowShot()
        {
            Debug.Log("[Archer] Ok atış efekti oynatıldı!");
            // TODO: Ok atış efektlerini burada oynat
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
