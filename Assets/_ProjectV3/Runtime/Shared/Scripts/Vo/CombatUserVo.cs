using System;
using Mirror;
using ProjectV3.Shared.Combat;
using ProjectV3.Shared.Enums;
using UnityEngine;

namespace ProjectV3.Shared.Vo
{
    [Serializable]
    public class CombatUserVo
    {
        #region Character Stats
        [Header("Temel Stats")]
        public float MaxHealth { get; private set; } = 100f;
        public float CurrentHealth { get; private set; }
        public float ShieldAmount { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        
        [Header("Combat Stats")]
        public int TotalDamageDealt { get; private set; }
        public int TotalDamageTaken { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Assists { get; private set; }
        
        [Header("Team Info")]
        public int TeamId { get; private set; }
        public bool IsAlly(CombatUserVo other) => TeamId == other.TeamId;
        
        [Header("Class Specific Stats")]
        public float ArcherTotalDamage { get; private set; }
        public float WarriorTotalDamage { get; private set; }
        public float TankTotalDamage { get; private set; }
        public float ArcherPlayTime { get; private set; }
        public float WarriorPlayTime { get; private set; }
        public float TankPlayTime { get; private set; }
        #endregion

        #region References
        public UserVo UserData { get; private set; }
        public BaseCharacterController CharacterController { get; private set; }
        public NetworkIdentity NetworkIdentity { get; private set; }
        public bool IsLocalPlayer => NetworkIdentity != null && NetworkIdentity.isLocalPlayer;
        #endregion

        #region Initialization
        public void Initialize(UserVo userData, BaseCharacterController controller, NetworkIdentity identity, int teamId)
        {
            UserData = userData;
            CharacterController = controller;
            NetworkIdentity = identity;
            TeamId = teamId;
            CurrentHealth = MaxHealth;
            
            Debug.Log($"[CombatUserVo] {UserData.DisplayName} için combat verileri başlatıldı. Team ID: {TeamId}, Can: {CurrentHealth}/{MaxHealth}");
        }
        #endregion

        #region Combat Methods
        public void TakeDamage(float damage, CombatUserVo attacker)
        {
            if (IsDead)
            {
                Debug.Log($"[CombatUserVo] {UserData.DisplayName} zaten ölü!");
                return;
            }

            float finalDamage = damage;
            if (ShieldAmount > 0)
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

            TotalDamageTaken += Mathf.RoundToInt(finalDamage);
            attacker.AddDamageDealt(finalDamage);

            Debug.Log($"[CombatUserVo] {UserData.DisplayName} {finalDamage} hasar aldı. Kalan can: {CurrentHealth}, Kalkan: {ShieldAmount}");

            if (IsDead)
            {
                Die(attacker);
            }
        }

        private float ProcessShieldDamage(float damage)
        {
            float remainingDamage = 0;
            if (damage > ShieldAmount)
            {
                remainingDamage = damage - ShieldAmount;
                ShieldAmount = 0;
                Debug.Log($"[CombatUserVo] {UserData.DisplayName}'in kalkanı kırıldı!");
            }
            else
            {
                ShieldAmount -= damage;
                Debug.Log($"[CombatUserVo] {UserData.DisplayName}'in kalkanı {damage} hasar aldı. Kalan kalkan: {ShieldAmount}");
            }
            return remainingDamage;
        }

        private void ProcessHealthDamage(float damage)
        {
            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            Debug.Log($"[CombatUserVo] {UserData.DisplayName}'in canı {previousHealth}'den {CurrentHealth}'e düştü");
        }

        public void AddShield(float amount)
        {
            float previousShield = ShieldAmount;
            ShieldAmount = Mathf.Min(ShieldAmount + amount, 100f);
            Debug.Log($"[CombatUserVo] {UserData.DisplayName} kalkan kazandı: {previousShield} -> {ShieldAmount}");
        }

        public void Heal(float amount)
        {
            if (IsDead)
            {
                Debug.Log($"[CombatUserVo] {UserData.DisplayName} ölü olduğu için iyileştirilemez!");
                return;
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
            Debug.Log($"[CombatUserVo] {UserData.DisplayName} iyileştirildi: {previousHealth} -> {CurrentHealth}");
        }

        private void Die(CombatUserVo killer)
        {
            Deaths++;
            killer.AddKill();
            
            Debug.Log($"[CombatUserVo] {UserData.DisplayName} öldü! Öldüren: {killer.UserData.DisplayName}");
            Debug.Log($"[CombatUserVo] İstatistikler - Ölümler: {Deaths}, Öldürmeler: {Kills}, Asistler: {Assists}");

            // TODO: Ölüm animasyonunu oynat
            // TODO: Yeniden doğma sistemini başlat
        }

        public void AddKill()
        {
            Kills++;
            Debug.Log($"[CombatUserVo] {UserData.DisplayName} bir kill aldı! Toplam kill: {Kills}");
        }

        public void AddAssist()
        {
            Assists++;
            Debug.Log($"[CombatUserVo] {UserData.DisplayName} bir asist aldı! Toplam asist: {Assists}");
        }

        private void AddDamageDealt(float damage)
        {
            TotalDamageDealt += Mathf.RoundToInt(damage);
            
            // Aktif karaktere göre hasar istatistiğini güncelle
            switch (CharacterController.GetCurrentCharacterType())
            {
                case CharacterType.Archer:
                    ArcherTotalDamage += damage;
                    break;
                case CharacterType.Warrior:
                    WarriorTotalDamage += damage;
                    break;
                case CharacterType.Tank:
                    TankTotalDamage += damage;
                    break;
            }

            Debug.Log($"[CombatUserVo] {UserData.DisplayName} toplam {TotalDamageDealt} hasar verdi");
        }
        #endregion

        #region Time Tracking
        public void UpdatePlayTime(float deltaTime)
        {
            switch (CharacterController.GetCurrentCharacterType())
            {
                case CharacterType.Archer:
                    ArcherPlayTime += deltaTime;
                    break;
                case CharacterType.Warrior:
                    WarriorPlayTime += deltaTime;
                    break;
                case CharacterType.Tank:
                    TankPlayTime += deltaTime;
                    break;
            }
        }
        #endregion

        #region Stats
        public float GetTotalPlayTime()
        {
            return ArcherPlayTime + WarriorPlayTime + TankPlayTime;
        }

        public float GetAverageDamagePerMinute()
        {
            float totalMinutes = GetTotalPlayTime() / 60f;
            return totalMinutes > 0 ? TotalDamageDealt / totalMinutes : 0;
        }

        public float GetKDARatio()
        {
            return Deaths > 0 ? (Kills + (Assists * 0.5f)) / Deaths : Kills + (Assists * 0.5f);
        }
        #endregion
    }
} 