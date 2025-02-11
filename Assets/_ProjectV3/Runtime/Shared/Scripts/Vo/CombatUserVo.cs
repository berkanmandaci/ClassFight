using System;
using Mirror;
using ProjectV3.Shared.Combat;
using ProjectV3.Shared.Enums;
using UnityEngine;

namespace ProjectV3.Shared.Vo
{
    [Serializable]
    public class CombatUserVo : NetworkBehaviour
    {
        #region Character Stats
        [Header("Temel Stats")]
        [SyncVar(hook = nameof(OnMaxHealthChanged))]
        private float _maxHealth = 100f;
        public float MaxHealth => _maxHealth;

        [SyncVar(hook = nameof(OnMaxShieldChanged))]
        private float _maxShield = 100f;
        public float MaxShield => _maxShield;

        [SyncVar(hook = nameof(OnCurrentHealthChanged))]
        private float _currentHealth;
        public float CurrentHealth => _currentHealth;

        [SyncVar(hook = nameof(OnShieldAmountChanged))]
        private float _shieldAmount;
        public float ShieldAmount => _shieldAmount;

        [SyncVar]
        private bool _isDead;
        public bool IsDead => _isDead;
        
        [Header("Combat Stats")]
        [SyncVar(hook = nameof(OnTotalDamageDealtChanged))]
        private int _totalDamageDealt;
        public int TotalDamageDealt => _totalDamageDealt;

        [SyncVar(hook = nameof(OnTotalDamageTakenChanged))]
        private int _totalDamageTaken;
        public int TotalDamageTaken => _totalDamageTaken;

        [SyncVar(hook = nameof(OnKillsChanged))]
        private int _kills;
        public int Kills => _kills;

        [SyncVar(hook = nameof(OnDeathsChanged))]
        private int _deaths;
        public int Deaths => _deaths;

        [SyncVar(hook = nameof(OnAssistsChanged))]
        private int _assists;
        public int Assists => _assists;
        
        [Header("Team Info")]
        [SyncVar(hook = nameof(OnTeamIdChanged))]
        private int _teamId;
        public int TeamId => _teamId;
        public bool IsAlly(CombatUserVo other) => TeamId == other.TeamId;
        
        [Header("Class Specific Stats")]
        [SyncVar]
        private float _archerTotalDamage;
        public float ArcherTotalDamage => _archerTotalDamage;

        [SyncVar]
        private float _warriorTotalDamage;
        public float WarriorTotalDamage => _warriorTotalDamage;

        [SyncVar]
        private float _tankTotalDamage;
        public float TankTotalDamage => _tankTotalDamage;

        [SyncVar]
        private float _archerPlayTime;
        public float ArcherPlayTime => _archerPlayTime;

        [SyncVar]
        private float _warriorPlayTime;
        public float WarriorPlayTime => _warriorPlayTime;

        [SyncVar]
        private float _tankPlayTime;
        public float TankPlayTime => _tankPlayTime;
        #endregion

        #region References
        [SyncVar(hook = nameof(OnUserDataChanged))]
        private UserVo _userData;
        public UserVo UserData => _userData;
        [SerializeField] private BaseCharacterController _characterController;
        public BaseCharacterController CharacterController => _characterController;

        #endregion

        #region Initialization
        [Server]
        public void Initialize(UserVo userData, int teamId)
        {
            _userData = userData;
            _teamId = teamId;
            _currentHealth = _maxHealth;
            _isDead = false;
            
            Debug.Log($"[CombatUserVo] {_userData.DisplayName} için combat verileri başlatıldı. Team ID: {_teamId}, Can: {_currentHealth}/{_maxHealth}");
            RpcInitializeClient(userData);
        }

        [ClientRpc]
        private void RpcInitializeClient(UserVo userData)
        {
            if (!isServer) // Sadece client tarafında çalışsın
            {
                _userData = userData;
                Debug.Log($"[CombatUserVo] Client: {_userData.DisplayName} için combat verileri senkronize edildi.");
            }
        }

        private void OnUserDataChanged(UserVo oldValue, UserVo newValue)
        {
            // if (newValue != null)
            // {
                Debug.Log($"[CombatUserVo] UserData güncellendi: {newValue.DisplayName}");
            // }
        }
        #endregion

        #region Combat Methods
        [Server]
        public void TakeDamage(float damage, CombatUserVo attacker)
        {
            if (_isDead)
            {
                Debug.Log($"[CombatUserVo] {_userData.DisplayName} zaten ölü!");
                return;
            }

            float finalDamage = damage;
            if (_shieldAmount > 0)
            {
                float remainingDamage = ProcessShieldDamage(damage);
                if (remainingDamage > 0)
                {
                    ProcessHealthDamage(remainingDamage);
                }
            }
            else
            {
                ProcessHealthDamage(damage);
            }

            _totalDamageTaken += Mathf.RoundToInt(finalDamage);
            attacker.RpcAddDamageDealt(finalDamage);

            Debug.Log($"[CombatUserVo] {_userData.DisplayName} {finalDamage} hasar aldı. Kalan can: {_currentHealth}, Kalkan: {_shieldAmount}");

            if (_isDead)
            {
                Die(attacker);
            }
        }

        [Server]
        private float ProcessShieldDamage(float damage)
        {
            float remainingDamage = 0;
            if (damage > _shieldAmount)
            {
                remainingDamage = damage - _shieldAmount;
                _shieldAmount = 0;
                Debug.Log($"[CombatUserVo] {_userData.DisplayName}'in kalkanı kırıldı!");
            }
            else
            {
                _shieldAmount -= damage;
                Debug.Log($"[CombatUserVo] {_userData.DisplayName}'in kalkanı {damage} hasar aldı. Kalan kalkan: {_shieldAmount}");
            }
            return remainingDamage;
        }

        [Server]
        private void ProcessHealthDamage(float damage)
        {
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            _isDead = _currentHealth <= 0;
            Debug.Log($"[CombatUserVo] {_userData.DisplayName}'in canı {previousHealth}'den {_currentHealth}'e düştü");
        }

        [Server]
        public void AddShield(float amount)
        {
            float previousShield = _shieldAmount;
            _shieldAmount = Mathf.Min(_shieldAmount + amount, _maxShield);
            Debug.Log($"[CombatUserVo] {_userData.DisplayName} kalkan kazandı: {previousShield} -> {_shieldAmount}");
        }

        [Server]
        public void Heal(float amount)
        {
            if (_isDead)
            {
                Debug.Log($"[CombatUserVo] {_userData.DisplayName} ölü olduğu için iyileştirilemez!");
                return;
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            Debug.Log($"[CombatUserVo] {_userData.DisplayName} iyileştirildi: {previousHealth} -> {_currentHealth}");
        }

        [Server]
        private void Die(CombatUserVo killer)
        {
            _deaths++;
            _isDead = true;
            killer.RpcAddKill();
            
            Debug.Log($"[CombatUserVo] {_userData.DisplayName} öldü! Öldüren: {killer.UserData.DisplayName}");
            Debug.Log($"[CombatUserVo] İstatistikler - Ölümler: {_deaths}, Öldürmeler: {_kills}, Asistler: {_assists}");

            // Combat Arena'ya ölüm bilgisini gönder
            if (CombatArenaModel.Instance != null)
            {
                CombatArenaModel.Instance.OnPlayerDeath(this);
            }
            else
            {
                Debug.LogError("[CombatUserVo] CombatArenaModel bulunamadı!");
            }

            RpcOnDeath();
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            Debug.Log($"[CombatUserVo] Client: {_userData.DisplayName} öldü!");
            _isDead = true;
            OnDeath?.Invoke();
        }

        [ClientRpc]
        private void RpcAddKill()
        {
            _kills++;
            Debug.Log($"[CombatUserVo] {_userData.DisplayName} bir kill aldı! Toplam kill: {_kills}");
        }

        [ClientRpc]
        private void RpcAddAssist()
        {
            _assists++;
            Debug.Log($"[CombatUserVo] {_userData.DisplayName} bir asist aldı! Toplam asist: {_assists}");
        }

        [ClientRpc]
        private void RpcAddDamageDealt(float damage)
        {
            _totalDamageDealt += Mathf.RoundToInt(damage);
            
            switch (_characterController.GetCurrentCharacterType())
            {
                case CharacterType.Archer:
                    _archerTotalDamage += damage;
                    break;
                case CharacterType.Warrior:
                    _warriorTotalDamage += damage;
                    break;
                case CharacterType.Tank:
                    _tankTotalDamage += damage;
                    break;
            }

            Debug.Log($"[CombatUserVo] {_userData.DisplayName} toplam {_totalDamageDealt} hasar verdi");
        }
        #endregion

        #region Time Tracking
        [Server]
        public void UpdatePlayTime(float deltaTime)
        {
            switch (_characterController.GetCurrentCharacterType())
            {
                case CharacterType.Archer:
                    _archerPlayTime += deltaTime;
                    break;
                case CharacterType.Warrior:
                    _warriorPlayTime += deltaTime;
                    break;
                case CharacterType.Tank:
                    _tankPlayTime += deltaTime;
                    break;
            }
        }
        #endregion

        #region Stats
        public float GetTotalPlayTime()
        {
            return _archerPlayTime + _warriorPlayTime + _tankPlayTime;
        }

        public float GetAverageDamagePerMinute()
        {
            float totalMinutes = GetTotalPlayTime() / 60f;
            return totalMinutes > 0 ? _totalDamageDealt / totalMinutes : 0;
        }

        public float GetKDARatio()
        {
            return _deaths > 0 ? (_kills + (_assists * 0.5f)) / _deaths : _kills + (_assists * 0.5f);
        }
        #endregion

        #region SyncVar Hooks
        private void OnMaxHealthChanged(float oldValue, float newValue)
        {
            Debug.Log($"[CombatUserVo] MaxHealth değişti: {oldValue} -> {newValue}");
            OnHealthChanged?.Invoke();
        }

        private void OnMaxShieldChanged(float oldValue, float newValue)
        {
            Debug.Log($"[CombatUserVo] MaxShield değişti: {oldValue} -> {newValue}");
            OnShieldChanged?.Invoke();
        }

        private void OnCurrentHealthChanged(float oldValue, float newValue)
        {
            Debug.Log($"[CombatUserVo] CurrentHealth değişti: {oldValue} -> {newValue}");
            OnHealthChanged?.Invoke();
        }

        private void OnShieldAmountChanged(float oldValue, float newValue)
        {
            Debug.Log($"[CombatUserVo] ShieldAmount değişti: {oldValue} -> {newValue}");
            OnShieldChanged?.Invoke();
        }

        private void OnTotalDamageDealtChanged(int oldValue, int newValue)
        {
            Debug.Log($"[CombatUserVo] TotalDamageDealt değişti: {oldValue} -> {newValue}");
        }

        private void OnTotalDamageTakenChanged(int oldValue, int newValue)
        {
            Debug.Log($"[CombatUserVo] TotalDamageTaken değişti: {oldValue} -> {newValue}");
        }

        private void OnKillsChanged(int oldValue, int newValue)
        {
            Debug.Log($"[CombatUserVo] Kills değişti: {oldValue} -> {newValue}");
        }

        private void OnDeathsChanged(int oldValue, int newValue)
        {
            Debug.Log($"[CombatUserVo] Deaths değişti: {oldValue} -> {newValue}");
        }

        private void OnAssistsChanged(int oldValue, int newValue)
        {
            Debug.Log($"[CombatUserVo] Assists değişti: {oldValue} -> {newValue}");
        }

        private void OnTeamIdChanged(int oldValue, int newValue)
        {
            Debug.Log($"[CombatUserVo] TeamId değişti: {oldValue} -> {newValue}");
        }
        #endregion

        public event Action OnHealthChanged;
        public event Action OnShieldChanged;
        public event Action OnDeath;
        public event Action<CharacterType> OnCharacterChanged;

        [Server]
        public void Respawn(Vector3 position)
        {
            // Can ve kalkanı yenile
            _currentHealth = _maxHealth;
            _shieldAmount = 0;
            _isDead = false;

            // Pozisyonu ayarla
            if (_characterController != null && _characterController.gameObject != null)
            {
                _characterController.gameObject.transform.position = position;
                Debug.Log($"[CombatUserVo] {_userData.DisplayName} yeniden doğdu - Can: {_currentHealth}/{_maxHealth}, Pozisyon: {position}");
            }
            else
            {
                Debug.LogError($"[CombatUserVo] {_userData.DisplayName} için CharacterController bulunamadı!");
            }

            RpcOnRespawn(position);
        }

        [ClientRpc]
        private void RpcOnRespawn(Vector3 position)
        {
            Debug.Log($"[CombatUserVo] Client: {_userData.DisplayName} yeniden doğdu");
            _isDead = false;
            _currentHealth = _maxHealth;
            _shieldAmount = 0;

            if (_characterController != null && _characterController.gameObject != null)
            {
                _characterController.gameObject.transform.position = position;
            }
        }
    }
} 