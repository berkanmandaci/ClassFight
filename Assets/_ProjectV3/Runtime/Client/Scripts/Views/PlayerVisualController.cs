using UnityEngine;
using UnityEngine.UI;
using ProjectV3.Shared.Data;
using System.Collections;

namespace ProjectV3.Client.Views
{
    /// <summary>
    /// Oyuncu görsel efektlerini ve animasyonlarını yöneten sınıf
    /// </summary>
    public class PlayerVisualController : MonoBehaviour
    {
        #region Properties
        [Header("Visual References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private ParticleSystem _hitEffect;
        [SerializeField] private ParticleSystem _attackEffect;
        [SerializeField] private TrailRenderer _weaponTrail;

        [Header("UI References")]
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Image _healthFillImage;
        [SerializeField] private float _healthBarUpdateSpeed = 5f;

        [Header("Visual Settings")]
        [SerializeField] private float _hitFlashDuration = 0.1f;
        [SerializeField] private Color _hitFlashColor = Color.red;
        [SerializeField] private Material _characterMaterial;

        private static readonly int _hitColorProperty = Shader.PropertyToID("_HitColor");
        private static readonly int _hitIntensityProperty = Shader.PropertyToID("_HitIntensity");

        private float _targetHealth;
        private float _currentHealth;
        private Coroutine _healthUpdateCoroutine;
        private Coroutine _hitFlashCoroutine;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            UpdateHealthBarVisual();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_characterMaterial == null && TryGetComponent<Renderer>(out var renderer))
                _characterMaterial = renderer.material;

            _currentHealth = 100f;
            _targetHealth = 100f;

            if (_healthBar != null)
            {
                _healthBar.maxValue = 100f;
                _healthBar.value = 100f;
            }
        }
        #endregion

        #region Animation Control
        /// <summary>
        /// Saldırı animasyonunu oynatır
        /// </summary>
        public void PlayAttackAnimation(AttackType attackType)
        {
            if (_animator == null) return;

            string triggerName = attackType switch
            {
                AttackType.LightAttack => "TriggerLightAttack",
                AttackType.HeavyAttack => "TriggerHeavyAttack",
                AttackType.SpecialAttack => "TriggerSpecialAttack",
                _ => "TriggerLightAttack"
            };

            _animator.SetTrigger(triggerName);
            PlayAttackEffects(attackType);
        }

        /// <summary>
        /// Hasar alma animasyonunu oynatır
        /// </summary>
        public void PlayHitAnimation()
        {
            if (_animator == null) return;

            _animator.SetTrigger("TriggerHit");
            PlayHitEffects();
        }

        /// <summary>
        /// Ölüm animasyonunu oynatır
        /// </summary>
        public void PlayDeathAnimation()
        {
            if (_animator == null) return;

            _animator.SetTrigger("TriggerDeath");
            StopAllEffects();
        }
        #endregion

        #region Visual Effects
        private void PlayAttackEffects(AttackType attackType)
        {
            if (_attackEffect != null)
            {
                var main = _attackEffect.main;
                main.duration = attackType switch
                {
                    AttackType.LightAttack => 0.5f,
                    AttackType.HeavyAttack => 1f,
                    AttackType.SpecialAttack => 1.5f,
                    _ => 0.5f
                };
                _attackEffect.Play();
            }

            if (_weaponTrail != null)
            {
                StartCoroutine(ShowWeaponTrail(0.5f));
            }
        }

        private void PlayHitEffects()
        {
            if (_hitEffect != null)
            {
                _hitEffect.Play();
            }

            if (_hitFlashCoroutine != null)
            {
                StopCoroutine(_hitFlashCoroutine);
            }
            _hitFlashCoroutine = StartCoroutine(PlayHitFlash());
        }

        private void StopAllEffects()
        {
            if (_hitEffect != null) _hitEffect.Stop();
            if (_attackEffect != null) _attackEffect.Stop();
            if (_weaponTrail != null) _weaponTrail.emitting = false;
        }
        #endregion

        #region Health Bar
        /// <summary>
        /// Can barını günceller
        /// </summary>
        public void UpdateHealthBar(float newHealth)
        {
            _targetHealth = newHealth;

            if (_healthUpdateCoroutine != null)
            {
                StopCoroutine(_healthUpdateCoroutine);
            }
            _healthUpdateCoroutine = StartCoroutine(UpdateHealthBarSmooth());
        }

        private void UpdateHealthBarVisual()
        {
            if (_healthBar == null || _healthFillImage == null) return;

            _healthFillImage.color = Color.Lerp(Color.red, Color.green, _currentHealth / 100f);
        }
        #endregion

        #region Coroutines
        private IEnumerator ShowWeaponTrail(float duration)
        {
            _weaponTrail.emitting = true;
            yield return new WaitForSeconds(duration);
            _weaponTrail.emitting = false;
        }

        private IEnumerator PlayHitFlash()
        {
            if (_characterMaterial == null) yield break;

            _characterMaterial.SetColor(_hitColorProperty, _hitFlashColor);
            _characterMaterial.SetFloat(_hitIntensityProperty, 1f);

            float elapsed = 0f;
            while (elapsed < _hitFlashDuration)
            {
                float intensity = Mathf.Lerp(1f, 0f, elapsed / _hitFlashDuration);
                _characterMaterial.SetFloat(_hitIntensityProperty, intensity);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _characterMaterial.SetFloat(_hitIntensityProperty, 0f);
        }

        private IEnumerator UpdateHealthBarSmooth()
        {
            while (Mathf.Abs(_currentHealth - _targetHealth) > 0.01f)
            {
                _currentHealth = Mathf.Lerp(_currentHealth, _targetHealth, Time.deltaTime * _healthBarUpdateSpeed);
                if (_healthBar != null)
                {
                    _healthBar.value = _currentHealth;
                }
                yield return null;
            }

            _currentHealth = _targetHealth;
            if (_healthBar != null)
            {
                _healthBar.value = _currentHealth;
            }
        }
        #endregion
    }
} 