using UnityEngine;
using Mirror;
using ProjectV3.Shared.Vo;

namespace ProjectV3.Shared.Combat
{
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Rigidbody))]
    public class ArrowProjectile : NetworkBehaviour
    {
        [Header("Ok Ayarları")]
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _criticalMultiplier = 1.5f;
        [SerializeField] private float _criticalChance = 0.2f;

        private CombatUserVo _owner;
        private bool _hasHit;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            Debug.Log($"[Arrow] Ok oluşturuldu - Position: {transform.position}");

            // Ok için fizik ayarları
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = true;
                _rigidbody.isKinematic = false;
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                Debug.Log("[Arrow] Rigidbody ayarları yapıldı");
            }
            else
            {
                Debug.LogError("[Arrow] Rigidbody bulunamadı!");
            }
        }

        public void Initialize(CombatUserVo owner)
        {
            _owner = owner;
            Debug.Log($"[Arrow] Ok başlatıldı - Sahibi: {_owner?.UserData?.DisplayName ?? "Bilinmeyen"}, NetworkIdentity: {GetComponent<NetworkIdentity>()?.netId ?? 0}");
        }

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Arrow] Çarpışma tespit edildi - Hedef: {other.name}");

            if (_hasHit)
            {
                Debug.Log("[Arrow] Ok zaten bir hedefe çarpmış, işlem iptal ediliyor.");
                return;
            }

            if (!isServer)
            {
                Debug.Log("[Arrow] Bu ok server'da değil, işlem iptal ediliyor.");
                return;
            }

            if (_owner == null)
            {
                Debug.LogError("[Arrow] Ok sahibi (owner) bulunamadı!");
                return;
            }

            var targetIdentity = other.GetComponent<NetworkIdentity>();
            if (targetIdentity == null)
            {
                Debug.Log($"[Arrow] Hedefte NetworkIdentity yok: {other.name}");
                return;
            }

            var targetController = targetIdentity.GetComponent<BaseCharacterController>();
            if (targetController == null)
            {
                Debug.Log($"[Arrow] Hedefte BaseCharacterController yok: {other.name}");
                return;
            }

            var targetCombatData = targetController.GetCombatData();
            if (targetCombatData == null)
            {
                Debug.Log($"[Arrow] Hedefte CombatUserVo yok: {other.name}");
                return;
            }

            if (targetCombatData == _owner)
            {
                Debug.Log("[Arrow] Ok sahibine çarptı, hasar verilmiyor.");
                return;
            }

            // Takım arkadaşına hasar verme
            if (CombatArenaModel.Instance.AreTeammates(_owner, targetCombatData))
            {
                Debug.Log($"[Arrow] {_owner.UserData.DisplayName}'in oku takım arkadaşına çarptı: {targetCombatData.UserData.DisplayName}");
                return;
            }

            _hasHit = true;

            // Kritik vuruş hesapla
            float finalDamage = _damage;
            bool isCritical = Random.value < _criticalChance;
            if (isCritical)
            {
                finalDamage *= _criticalMultiplier;
                Debug.Log($"[Arrow] KRİTİK VURUŞ! Hasar: {finalDamage}");
            }

            // Hasarı uygula
            Debug.Log($"[Arrow] Hasar uygulanıyor - Hedef: {targetCombatData.UserData.DisplayName}, Hasar: {finalDamage}");
            targetCombatData.TakeDamage(finalDamage, _owner);

            // Ok'u durdur
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
                Debug.Log("[Arrow] Ok durduruldu");
            }

            // Vuruş efektini oynat
            RpcOnHit(isCritical, transform.position);

            // Ok'u yok et (gecikmeli)
            StartCoroutine(DestroyAfterDelay());
        }

        private System.Collections.IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(0.1f); // Efektlerin oynatılması için kısa bir gecikme
            if (isServer)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        [ClientRpc]
        private void RpcOnHit(bool isCritical, Vector3 hitPosition)
        {
            string hitType = isCritical ? "KRİTİK VURUŞ!" : "Vuruş";
            float finalDamage = _damage * (isCritical ? _criticalMultiplier : 1);
            Debug.Log($"[Arrow] {hitType} - Hasar: {finalDamage} - Pozisyon: {hitPosition}");
            
            // TODO: Vuruş efektlerini burada oynat
            // Örnek: ParticleSystem.Play();
        }
    }
} 