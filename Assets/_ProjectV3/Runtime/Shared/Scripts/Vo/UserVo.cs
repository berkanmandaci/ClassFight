using System;
using Mirror;

namespace ProjectV3.Shared.Vo
{
    [Serializable]
    public struct UserVo
    {
        private string _id;
        private string _username;
        private string _displayName;
        private int _level;
        private float _experience;
        private float _experienceToNextLevel;
        private int _elo;

        public string Id => _id;
        public string Username => _username;
        public string DisplayName => _displayName;
        public int Level => _level;
        public float Experience => _experience;
        public float ExperienceToNextLevel => _experienceToNextLevel;
        public int Elo => _elo;

        public UserVo(string id, string username, string displayName = null)
        {
            _id = id;
            _username = username;
            _displayName = displayName ?? username;
            _level = 1;
            _experience = 0;
            _experienceToNextLevel = 100;
            _elo = 1000;
        }

        public void SetLevel(int level) => _level = level;
        public void SetExperience(float experience) => _experience = experience;
        public void SetExperienceToNextLevel(float experienceToNextLevel) => _experienceToNextLevel = experienceToNextLevel;
        public void SetElo(int elo) => _elo = elo;
    }
}
