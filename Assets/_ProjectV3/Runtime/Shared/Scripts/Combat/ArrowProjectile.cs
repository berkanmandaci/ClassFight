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
        [SerializeField] private float _maxLifetime = 5f;
        [SerializeField] private float _destroyDelay = 0.1f;

        [SyncVar]
        private CombatUserVo _owner;
        
        [SyncVar]
        private bool _hasHit;
        
        private Rigidbody _rigidbody;
        private float _spawnTime;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            SetupRigidbody();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _spawnTime = Time.time;
        }

        private void SetupRigidbody()
        {
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
            Debug.Log($"[Arrow] Ok başlatıldı - Sahibi: {_owner.UserData.DisplayName}, NetworkId: {netId}");
        }

        private void Update()
        {
            if (isServer && Time.time - _spawnTime > _maxLifetime)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit || !isServer)
                return;

            if (_owner == null)
            {
                Debug.LogError("[Arrow] Ok sahibi (owner) bulunamadı!");
                return;
            }

            var targetController = other.GetComponent<BaseCharacterController>();
            if (targetController == null)
                return;

            var targetCombatData = targetController.GetCombatData();
            if (targetCombatData == null || targetCombatData == _owner)
                return;

            // Takım arkadaşına hasar verme
            if (CombatArenaModel.Instance.AreTeammates(_owner, targetCombatData))
            {
                Debug.Log($"[Arrow] {_owner.UserData.DisplayName}'in oku takım arkadaşına çarptı: {targetCombatData.UserData.DisplayName}");
                return;
            }

            _hasHit = true;

            // Kritik vuruş hesapla
            bool isCritical = Random.value < _criticalChance;
            float finalDamage = isCritical ? _damage * _criticalMultiplier : _damage;

            // Hasarı uygula
            targetCombatData.TakeDamage(finalDamage, _owner);
            Debug.Log($"[Arrow] Hedefe vuruldu: {targetCombatData.UserData.DisplayName}, Hasar: {finalDamage}, Kritik: {isCritical}");

            // Ok'u durdur ve efektleri oynat
            StopArrow();
            RpcOnHit(isCritical, transform.position);

            // Ok'u yok et
            Destroy(gameObject, _destroyDelay);
        }

        private void StopArrow()
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
                Debug.Log("[Arrow] Ok durduruldu");
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