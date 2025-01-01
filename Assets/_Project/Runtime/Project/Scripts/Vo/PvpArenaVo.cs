using System.Collections.Generic;
namespace _Project.Scripts.Vo
{
    public class PvpArenaVo
    {
        public string MatchId;

        public Dictionary<string, PvpUserVo> ConnectedUsers { get; private set; } = new Dictionary<string, PvpUserVo>();

        public Dictionary<string, TeamVo> Teams { get; private set; } = new Dictionary<string, TeamVo>();


        public PvpArenaVo(List<TeamVo> teamVos, List<PvpUserVo> pvpUserVos, string matchId)
        {
            MatchId = matchId;
            
            foreach (var teamVo in teamVos)
            {
                Teams[teamVo.Id] = teamVo;
            }

            foreach (var pvpUserVo in pvpUserVos)
            {
                ConnectedUsers[pvpUserVo.Id] = pvpUserVo;
            }
        }
        
        public PvpUserVo GetUser(string userId)
        {
            return ConnectedUsers[userId];
        }

    }

}
