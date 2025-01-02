using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Scripts.Vo
{
    public class PvpArenaVo
    {
        private readonly Dictionary<string, PvpUserVo> _users;
        private readonly Dictionary<string, TeamVo> _teams;
        
        public string MatchId { get; }
        public IReadOnlyList<PvpUserVo> Users => _users.Values.ToList();
        public IReadOnlyDictionary<string, TeamVo> Teams => _teams;
        
        // Round ve zaman yönetimi
        public int CurrentRound { get; private set; }
        public int MaxRounds { get; private set; }
        public float RoundStartTime { get; private set; }
        public float RoundEndTime { get; private set; }
        public float RoundDuration { get; private set; }
        
        // Match tipi
        public bool IsTeamMatch { get; private set; }
        
        public PvpArenaVo(List<TeamVo> teams, List<PvpUserVo> users, string matchId, bool isTeamMatch = false, int maxRounds = 3, float roundDuration = 300f)
        {
            MatchId = matchId;
            _users = users.ToDictionary(u => u.Id);
            _teams = teams.ToDictionary(t => t.Id);
            
            IsTeamMatch = isTeamMatch;
            MaxRounds = maxRounds;
            RoundDuration = roundDuration;
            CurrentRound = 0;
        }

        public PvpUserVo GetUser(string userId) => _users[userId];

        public TeamVo GetTeam(string teamId) => _teams[teamId];

        public void StartNewRound()
        {
            CurrentRound++;
            RoundStartTime = Time.time;
            RoundEndTime = RoundStartTime + RoundDuration;
            
            // Oyuncuları round başlangıç pozisyonlarına yerleştir
            foreach (var user in Users)
            {
                user.ResetRoundStats();
            }
        }

        public bool IsRoundTimeUp()
        {
            return Time.time >= RoundEndTime;
        }

        public float GetRemainingRoundTime()
        {
            return Mathf.Max(0, RoundEndTime - Time.time);
        }

        public bool IsMatchComplete()
        {
            return CurrentRound >= MaxRounds;
        }

        public TeamVo GetWinningTeam()
        {
            if (!IsTeamMatch) return null;
            
            return _teams.Values.OrderByDescending(t => t.Score).First();
        }

        public PvpUserVo GetTopPlayer()
        {
            return Users.OrderByDescending(u => u.Score).First();
        }
    }
}
