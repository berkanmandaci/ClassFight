using Nakama;
using UnityEngine;

namespace _Project.Scripts.Vo
{
    public class UserVo
    {
        public string Id => User.Id;
        public string DisplayName => User.DisplayName;
        public string AvatarUrl => User.AvatarUrl;
        public bool IsOnline => User.Online;
        public string Metadata => User.Metadata;

        public int Level { get; set; }
        public float Experience { get; set; }
        public float ExperienceToNextLevel { get; set; }
        public int Elo { get; set; }

        // Arena için ek özellikler
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public string CurrentWeapon { get; set; }
        public string CharacterType { get; set; }
        
        private IApiUser User { get; set; }

        public UserVo(IApiUser apiUser)
        {
            User = apiUser;
            InitializeDefaultValues();
        }

        private void InitializeDefaultValues()
        {
            Level = 1;
            Experience = 0;
            ExperienceToNextLevel = 100;
            Elo = 1000;
            Health = 100;
            MaxHealth = 100;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            CurrentWeapon = "Default";
            CharacterType = "Default";

            // Metadata'dan değerleri yükle
            if (!string.IsNullOrEmpty(Metadata))
            {
                try
                {
                    var data = JsonUtility.FromJson<UserMetadata>(Metadata);
                    if (data != null)
                    {
                        Level = data.level;
                        Experience = data.experience;
                        Elo = data.elo;
                        CharacterType = data.characterType;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing user metadata: {e.Message}");
                }
            }
        }

        [System.Serializable]
        private class UserMetadata
        {
            public int level;
            public float experience;
            public int elo;
            public string characterType;
        }
    }
}
