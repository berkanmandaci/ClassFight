using Nakama;
namespace _Project.Scripts.Vo
{
    public class PvpUserVo : UserVo
    {
        public string TeamId;
        
        // Toplam maç istatistikleri
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Assists { get; private set; }
        public int Score { get; private set; }
        public float DamageDealt { get; private set; }
        public float DamageTaken { get; private set; }
        
        // Mevcut round istatistikleri
        public int RoundKills { get; private set; }
        public int RoundDeaths { get; private set; }
        public int RoundAssists { get; private set; }
        public int RoundScore { get; private set; }
        public float RoundDamageDealt { get; private set; }
        public float RoundDamageTaken { get; private set; }
        
        // Oyuncu durumu
        public bool IsReady { get; private set; }
        public bool IsAlive { get; private set; }

        public PvpUserVo(IApiUser apiUser) : base(apiUser)
        {
            ResetStats();
            IsAlive = true;
        }

        public void AddKill()
        {
            Kills++;
            RoundKills++;
            AddScore(100);
        }

        public void AddDeath()
        {
            Deaths++;
            RoundDeaths++;
            IsAlive = false;
        }

        public void AddAssist()
        {
            Assists++;
            RoundAssists++;
            AddScore(50);
        }

        public void AddDamageDealt(float damage)
        {
            DamageDealt += damage;
            RoundDamageDealt += damage;
            AddScore((int)(damage * 0.1f));
        }

        public void AddDamageTaken(float damage)
        {
            DamageTaken += damage;
            RoundDamageTaken += damage;
        }

        private void AddScore(int points)
        {
            Score += points;
            RoundScore += points;
        }

        public void ResetStats()
        {
            Kills = 0;
            Deaths = 0;
            Assists = 0;
            Score = 0;
            DamageDealt = 0;
            DamageTaken = 0;
            ResetRoundStats();
        }

        public void ResetRoundStats()
        {
            RoundKills = 0;
            RoundDeaths = 0;
            RoundAssists = 0;
            RoundScore = 0;
            RoundDamageDealt = 0;
            RoundDamageTaken = 0;
            IsAlive = true;
        }

        public void SetReady(bool ready)
        {
            IsReady = ready;
        }

        public void Respawn()
        {
            IsAlive = true;
        }
    }
}
