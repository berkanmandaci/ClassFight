using Nakama;
namespace _Project.Scripts.Vo
{
    public class PvpUserVo : UserVo
    {
        public string TeamId;
        
        // Maç istatistikleri
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Assists { get; private set; }
        public int Score { get; private set; }
        public float DamageDealt { get; private set; }
        public float DamageTaken { get; private set; }

        public PvpUserVo(IApiUser apiUser) : base(apiUser)
        {
            ResetStats();
        }

        public void AddKill()
        {
            Kills++;
            Score += 100;
        }

        public void AddDeath()
        {
            Deaths++;
        }

        public void AddAssist()
        {
            Assists++;
            Score += 50;
        }

        public void AddDamageDealt(float damage)
        {
            DamageDealt += damage;
            Score += (int)(damage * 0.1f);
        }

        public void AddDamageTaken(float damage)
        {
            DamageTaken += damage;
        }

        public void ResetStats()
        {
            Kills = 0;
            Deaths = 0;
            Assists = 0;
            Score = 0;
            DamageDealt = 0;
            DamageTaken = 0;
        }
    }
}
