using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using ProjectV3.Shared.Vo;
using ProjectV3.Shared.Enums;

namespace ProjectV3.Shared.UI
{
    public class PlayerHudController : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _displayNameText;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private TMP_Text _shieldText;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Slider _shieldSlider;
        [SerializeField] private GameObject _shieldContainer; // Shield UI'ın parent objesi

        private CombatUserVo _combatData;
        private float _maxHealth = 100f;
        private float _maxShield = 100f;

        private void Awake()
        {
            // Slider'ların başlangıç değerlerini ayarla
            if (_healthSlider != null)
            {
                _healthSlider.maxValue = _maxHealth;
                _healthSlider.value = _maxHealth;
            }

            if (_shieldSlider != null)
            {
                _shieldSlider.maxValue = _maxShield;
                _shieldSlider.value = 0;
            }

            // Shield UI'ı başlangıçta gizle
            if (_shieldContainer != null)
            {
                _shieldContainer.SetActive(false);
            }
        }

        public void Init(CombatUserVo combatData)
        {
            UnsubscribeFromEvents();
            
            _combatData = combatData;
            _maxHealth = combatData.MaxHealth;

            if (_healthSlider != null)
            {
                _healthSlider.maxValue = _maxHealth;
                _healthSlider.value = _maxHealth;
            }

            UpdateDisplayName();
            UpdateHealthUI();
            UpdateShieldUI();
            UpdateShieldVisibility(_combatData.CharacterController.GetCurrentCharacterType());

            SubscribeToEvents();

            Debug.Log($"[PlayerHUD] HUD başlatıldı - Oyuncu: {_combatData.UserData.DisplayName}");
        }

        private void UpdateShieldVisibility(CharacterType characterType)
        {
            if (_shieldContainer != null)
            {
                bool isTank = characterType == CharacterType.Tank;
                _shieldContainer.SetActive(isTank);
                Debug.Log($"[PlayerHUD] Shield UI {(isTank ? "gösteriliyor" : "gizleniyor")} - Karakter: {characterType}");
            }
        }

        private void SubscribeToEvents()
        {
            if (_combatData == null) return;

            _combatData.OnHealthChanged += UpdateHealthUI;
            _combatData.OnShieldChanged += UpdateShieldUI;
            _combatData.OnDeath += OnPlayerDeath;
            _combatData.OnCharacterChanged += OnCharacterChanged;

            Debug.Log("[PlayerHUD] Event dinleyicileri eklendi");
        }

        private void UnsubscribeFromEvents()
        {
            if (_combatData == null) return;

            _combatData.OnHealthChanged -= UpdateHealthUI;
            _combatData.OnShieldChanged -= UpdateShieldUI;
            _combatData.OnDeath -= OnPlayerDeath;
            _combatData.OnCharacterChanged -= OnCharacterChanged;

            Debug.Log("[PlayerHUD] Event dinleyicileri kaldırıldı");
        }

        private void OnCharacterChanged(CharacterType newType)
        {
            UpdateShieldVisibility(newType);
            
            // Tank değilse shield'ı sıfırla
            if (newType != CharacterType.Tank)
            {
                if (_shieldSlider != null)
                {
                    _shieldSlider.value = 0;
                }
                if (_shieldText != null)
                {
                    _shieldText.text = "0/0";
                }
            }
        }

        private void OnPlayerDeath()
        {
            // Ölüm durumunda HUD'u güncelle
            UpdateHealthUI();
            UpdateShieldUI();
            
            // TODO: Ölüm efektlerini göster
            Debug.Log($"[PlayerHUD] Oyuncu öldü: {_combatData.UserData.DisplayName}");
        }

        private void UpdateDisplayName()
        {
            if (_displayNameText != null && _combatData != null)
            {
                _displayNameText.text = _combatData.UserData.DisplayName;
                Debug.Log($"[PlayerHUD] Display name güncellendi: {_combatData.UserData.DisplayName}");
            }
        }

        public void UpdateHealthUI()
        {
            if (_combatData == null) return;

            if (_healthSlider != null)
            {
                _healthSlider.value = _combatData.CurrentHealth;
            }

            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.CeilToInt(_combatData.CurrentHealth)}/{_maxHealth}";
            }

            Debug.Log($"[PlayerHUD] Can güncellendi - Değer: {_combatData.CurrentHealth}/{_maxHealth}");
        }

        public void UpdateShieldUI()
        {
            if (_combatData == null) return;

            if (_shieldSlider != null)
            {
                _shieldSlider.value = _combatData.ShieldAmount;
            }

            if (_shieldText != null)
            {
                _shieldText.text = $"{Mathf.CeilToInt(_combatData.ShieldAmount)}/{_maxShield}";
            }

            Debug.Log($"[PlayerHUD] Kalkan güncellendi - Değer: {_combatData.ShieldAmount}/{_maxShield}");
        }

        private void OnEnable()
        {
            if (_combatData != null)
            {
                SubscribeToEvents();
                UpdateHealthUI();
                UpdateShieldUI();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
} 