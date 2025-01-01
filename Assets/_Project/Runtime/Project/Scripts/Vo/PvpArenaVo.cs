using System.Collections.Generic;
using System.Linq;

namespace _Project.Scripts.Vo
{
    public class PvpArenaVo
    {
        private readonly Dictionary<string, PvpUserVo> _users;
        private readonly Dictionary<string, TeamVo> _teams;
        
        public string MatchId { get; }
        public IReadOnlyList<PvpUserVo> Users => _users.Values.ToList();
        public IReadOnlyDictionary<string, TeamVo> Teams => _teams;
        
        public PvpArenaVo(List<TeamVo> teams, List<PvpUserVo> users, string matchId)
        {
            MatchId = matchId;
            _users = users.ToDictionary(u => u.Id);
            _teams = teams.ToDictionary(t => t.Id);
        }

        public PvpUserVo GetUser(string userId) => 
            _users.TryGetValue(userId, out var user) ? user : null;

        public TeamVo GetTeam(string teamId) => 
            _teams.TryGetValue(teamId, out var team) ? team : null;
    }
}
