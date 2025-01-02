using System.Collections.Generic;
using System.Linq;

namespace _Project.Scripts.Vo
{
    public class TeamVo
    {
        public string Id { get; }
        public string Name { get; }
        public List<PvpUserVo> Members { get; }
        public int Score => Members.Sum(m => m.Score);
        public int RoundScore => Members.Sum(m => m.RoundScore);
        
        public TeamVo(string id, string name)
        {
            Id = id;
            Name = name;
            Members = new List<PvpUserVo>();
        }
        
        public void AddMember(PvpUserVo member)
        {
            if (!Members.Contains(member))
            {
                Members.Add(member);
                member.TeamId = Id;
            }
        }
        
        public void RemoveMember(PvpUserVo member)
        {
            if (Members.Contains(member))
            {
                Members.Remove(member);
                member.TeamId = null;
            }
        }
        
        public bool IsTeamEliminated => Members.All(m => !m.IsAlive);
    }
}
