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
        [SerializeField] private GameObject _shieldContainer;

        [Header("Combat Stats UI")]
        [SerializeField] private TextMeshProUGUI _killsText;
        [SerializeField] private TextMeshProUGUI _deathsText;
        [SerializeField] private TextMeshProUGUI _assistsText;
        [SerializeField] private TextMeshProUGUI _damageDealtText;
        [SerializeField] private TextMeshProUGUI _damageTakenText;

        [SyncVar(hook = nameof(OnCombatDataChanged))]
        private CombatUserVo _combatData;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[PlayerHUD] Server başlatıldı");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("[PlayerHUD] Client başlatıldı");

            if (_combatData != null)
            {
                InitializeClientHUD(_combatData);
            }
        }

        [Server]
        public void Init(CombatUserVo combatData)
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("[PlayerHUD] Init sadece server'da çağrılabilir!");
                return;
            }

            _combatData = combatData;
            Debug.Log($"[PlayerHUD] Server: HUD başlatıldı - Oyuncu: {_combatData.UserData.DisplayName}");
        }

        private void OnCombatDataChanged(CombatUserVo oldValue, CombatUserVo newValue)
        {
            if (!isClient) return;

            Debug.Log($"[PlayerHUD] Combat data değişti: {(newValue != null ? newValue.UserData.DisplayName : "null")}");
            
            if (newValue != null)
            {
                InitializeClientHUD(newValue);
            }
        }

        private void InitializeClientHUD(CombatUserVo combatData)
        {
            try
            {
                UnsubscribeFromEvents();
                _combatData = combatData;

                if (_combatData == null)
                {
                    Debug.LogError("[PlayerHUD] Combat data null!");
                    return;
                }

                InitializeUI();
                SubscribeToEvents();
                UpdateAllUI();

                Debug.Log($"[PlayerHUD] Client: HUD başlatıldı - Oyuncu: {_combatData.UserData.DisplayName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerHUD] HUD başlatılırken hata: {e.Message}\n{e.StackTrace}");
            }
        }

        private void InitializeUI()
        {
            if (_healthSlider != null)
            {
                _healthSlider.maxValue = _combatData.MaxHealth;
                _healthSlider.value = _combatData.CurrentHealth;
            }

            if (_shieldSlider != null)
            {
                _shieldSlider.maxValue = _combatData.MaxShield;
                _shieldSlider.value = _combatData.ShieldAmount;
            }

            // Combat stats UI'ı sadece local player için göster
            bool showCombatStats = isLocalPlayer;
            if (_killsText != null) _killsText.gameObject.SetActive(showCombatStats);
            if (_deathsText != null) _deathsText.gameObject.SetActive(showCombatStats);
            if (_assistsText != null) _assistsText.gameObject.SetActive(showCombatStats);
            if (_damageDealtText != null) _damageDealtText.gameObject.SetActive(showCombatStats);
            if (_damageTakenText != null) _damageTakenText.gameObject.SetActive(showCombatStats);

            UpdateShieldVisibility(_combatData.CharacterController.GetCurrentCharacterType());
        }

        private void SubscribeToEvents()
        {
            if (_combatData == null) return;

            _combatData.OnHealthChanged += UpdateHealthUI;
            _combatData.OnShieldChanged += UpdateShieldUI;
            _combatData.OnDeath += OnPlayerDeath;
            _combatData.OnCharacterChanged += OnCharacterChanged;

            Debug.Log("[PlayerHUD] Event'lere abone olundu");
        }

        private void UnsubscribeFromEvents()
        {
            if (_combatData == null) return;

            _combatData.OnHealthChanged -= UpdateHealthUI;
            _combatData.OnShieldChanged -= UpdateShieldUI;
            _combatData.OnDeath -= OnPlayerDeath;
            _combatData.OnCharacterChanged -= OnCharacterChanged;

            Debug.Log("[PlayerHUD] Event abonelikleri kaldırıldı");
        }

        private void UpdateAllUI()
        {
            if (_combatData == null) return;

            UpdateDisplayName();
            UpdateHealthUI();
            UpdateShieldUI();
            
            // Combat stats sadece local player için güncelle
            if (isLocalPlayer)
            {
                UpdateCombatStats();
            }
        }

        private void UpdateDisplayName()
        {
            if (_displayNameText != null && !string.IsNullOrEmpty(_combatData.UserData.DisplayName))
            {
                _displayNameText.text = _combatData.UserData.DisplayName;
            }
        }

        private void UpdateHealthUI()
        {
            if (_combatData == null) return;

            if (_healthSlider != null)
            {
                _healthSlider.maxValue = _combatData.MaxHealth;
                _healthSlider.value = _combatData.CurrentHealth;
            }

            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.CeilToInt(_combatData.CurrentHealth)}/{_combatData.MaxHealth}";
            }

            Debug.Log($"[PlayerHUD] Can güncellendi: {_combatData.CurrentHealth}/{_combatData.MaxHealth}");
        }

        private void UpdateShieldUI()
        {
            if (_combatData == null) return;

            if (_shieldSlider != null)
            {
                _shieldSlider.maxValue = _combatData.MaxShield;
                _shieldSlider.value = _combatData.ShieldAmount;
            }

            if (_shieldText != null)
            {
                _shieldText.text = $"{Mathf.CeilToInt(_combatData.ShieldAmount)}/{_combatData.MaxShield}";
            }

            Debug.Log($"[PlayerHUD] Kalkan güncellendi: {_combatData.ShieldAmount}/{_combatData.MaxShield}");
        }

        private void UpdateCombatStats()
        {
            if (!isLocalPlayer || _combatData == null) return;

            if (_killsText != null) _killsText.text = $"Öldürme: {_combatData.Kills}";
            if (_deathsText != null) _deathsText.text = $"Ölüm: {_combatData.Deaths}";
            if (_assistsText != null) _assistsText.text = $"Asist: {_combatData.Assists}";
            if (_damageDealtText != null) _damageDealtText.text = $"Verilen Hasar: {_combatData.TotalDamageDealt}";
            if (_damageTakenText != null) _damageTakenText.text = $"Alınan Hasar: {_combatData.TotalDamageTaken}";
        }

        private void UpdateShieldVisibility(CharacterType characterType)
        {
            if (_shieldContainer != null)
            {
                bool isTank = characterType == CharacterType.Tank;
                _shieldContainer.SetActive(isTank);
            }
        }

        private void OnCharacterChanged(CharacterType newType)
        {
            UpdateShieldVisibility(newType);
        }

        private void OnPlayerDeath()
        {
            UpdateAllUI();
        }

        private void Update()
        {
            // Combat stats'i sadece local player için güncelle
            if (isLocalPlayer && _combatData != null)
            {
                UpdateCombatStats();
            }
        }

        private void OnEnable()
        {
            if (_combatData != null)
            {
                SubscribeToEvents();
                UpdateAllUI();
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