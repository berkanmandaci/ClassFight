using UnityEngine;
using ProjectV3.Shared.Data;
using System.Collections.Generic;
using System.Collections;

namespace ProjectV3.Client.Core.Managers
{
    /// <summary>
    /// Savaş efektlerini yöneten sınıf
    /// </summary>
    public class CombatEffectsManager : MonoBehaviour
    {
        #region Properties
        [Header("Effect Prefabs")]
        [SerializeField] private ParticleSystem _lightAttackEffectPrefab;
        [SerializeField] private ParticleSystem _heavyAttackEffectPrefab;
        [SerializeField] private ParticleSystem _specialAttackEffectPrefab;
        [SerializeField] private ParticleSystem _hitEffectPrefab;
        [SerializeField] private ParticleSystem _criticalHitEffectPrefab;
        [SerializeField] private ParticleSystem _blockEffectPrefab;

        [Header("Effect Settings")]
        [SerializeField] private int _poolSize = 10;
        [SerializeField] private float _effectDuration = 2f;
        [SerializeField] private float _trailDuration = 0.5f;

        private readonly Dictionary<AttackType, Queue<ParticleSystem>> _attackEffectPools = new();
        private Queue<ParticleSystem> _hitEffectPool;
        private Queue<ParticleSystem> _criticalHitEffectPool;
        private Queue<ParticleSystem> _blockEffectPool;

        private Transform _effectContainer;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeEffectContainer();
            InitializeEffectPools();
        }
        #endregion

        #region Initialization
        private void InitializeEffectContainer()
        {
            _effectContainer = new GameObject("EffectContainer").transform;
            _effectContainer.SetParent(transform);
            _effectContainer.localPosition = Vector3.zero;
        }

        private void InitializeEffectPools()
        {
            // Saldırı efektleri havuzlarını oluştur
            _attackEffectPools[AttackType.LightAttack] = CreateEffectPool(_lightAttackEffectPrefab);
            _attackEffectPools[AttackType.HeavyAttack] = CreateEffectPool(_heavyAttackEffectPrefab);
            _attackEffectPools[AttackType.SpecialAttack] = CreateEffectPool(_specialAttackEffectPrefab);

            // Diğer efekt havuzlarını oluştur
            _hitEffectPool = CreateEffectPool(_hitEffectPrefab);
            _criticalHitEffectPool = CreateEffectPool(_criticalHitEffectPrefab);
            _blockEffectPool = CreateEffectPool(_blockEffectPrefab);
        }

        private Queue<ParticleSystem> CreateEffectPool(ParticleSystem prefab)
        {
            if (prefab == null) return new Queue<ParticleSystem>();

            var pool = new Queue<ParticleSystem>();
            for (int i = 0; i < _poolSize; i++)
            {
                var effect = Instantiate(prefab, _effectContainer);
                effect.gameObject.SetActive(false);
                pool.Enqueue(effect);
            }
            return pool;
        }
        #endregion

        #region Effect Spawning
        /// <summary>
        /// Saldırı efektini oynatır
        /// </summary>
        public void PlayAttackEffect(AttackType attackType, Vector3 position, Vector3 direction)
        {
            if (!_attackEffectPools.TryGetValue(attackType, out var pool)) return;

            var effect = GetEffectFromPool(pool);
            if (effect == null) return;

            PlayEffect(effect, position, direction);
        }

        /// <summary>
        /// Hasar alma efektini oynatır
        /// </summary>
        public void PlayHitEffect(Vector3 position, Vector3 normal, bool isCritical = false)
        {
            var pool = isCritical ? _criticalHitEffectPool : _hitEffectPool;
            var effect = GetEffectFromPool(pool);
            if (effect == null) return;

            PlayEffect(effect, position, normal);
        }

        /// <summary>
        /// Bloklama efektini oynatır
        /// </summary>
        public void PlayBlockEffect(Vector3 position, Vector3 normal)
        {
            var effect = GetEffectFromPool(_blockEffectPool);
            if (effect == null) return;

            PlayEffect(effect, position, normal);
        }
        #endregion

        #region Effect Management
        private ParticleSystem GetEffectFromPool(Queue<ParticleSystem> pool)
        {
            if (pool == null || pool.Count == 0) return null;

            var effect = pool.Dequeue();
            pool.Enqueue(effect);
            return effect;
        }

        private void PlayEffect(ParticleSystem effect, Vector3 position, Vector3 direction)
        {
            effect.gameObject.SetActive(true);
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.LookRotation(direction);
            
            effect.Play();
            StartCoroutine(DisableEffectAfterDuration(effect));
        }

        private IEnumerator DisableEffectAfterDuration(ParticleSystem effect)
        {
            yield return new WaitForSeconds(_effectDuration);
            effect.gameObject.SetActive(false);
        }
        #endregion

        #region Trail Effects
        /// <summary>
        /// Silah izi efektini başlatır
        /// </summary>
        public void StartWeaponTrail(TrailRenderer trail)
        {
            if (trail == null) return;
            trail.emitting = true;
            StartCoroutine(StopTrailAfterDuration(trail));
        }

        private IEnumerator StopTrailAfterDuration(TrailRenderer trail)
        {
            yield return new WaitForSeconds(_trailDuration);
            trail.emitting = false;
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            if (_effectContainer != null)
            {
                Destroy(_effectContainer.gameObject);
            }
        }
        #endregion
    }
} 