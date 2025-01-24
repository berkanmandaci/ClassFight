using Nakama;
namespace _Project.Scripts.Vo
{
    public class UserVo
    {
        public string Id => User.Id;

        public string DisplayName => User.DisplayName;

        public int Level { get; set; }

        public float Experience { get; set; }

        public float ExperienceToNextLevel { get; set; }

        public int Elo { get; set; }
        
        private IApiUser User { get; set; }

        public UserVo(IApiUser apiUser)
        {
            User = apiUser;
        }
    }
}
