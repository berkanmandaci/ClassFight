using System;

namespace ProjectV3.Shared.Vo
{
    [Serializable]
    public class UserVo
    {
        public string Id { get; private set; }
        public string Username { get; private set; }
        public string DisplayName { get; private set; }
        public int Level { get; set; }
        public float Experience { get; set; }
        public float ExperienceToNextLevel { get; set; }
        public int Elo { get; set; }

        public UserVo(string id, string username, string displayName = null)
        {
            Id = id;
            Username = username;
            DisplayName = displayName ?? username;
            Level = 1;
            Experience = 0;
            ExperienceToNextLevel = 100;
            Elo = 1000;
        }
    }
}
