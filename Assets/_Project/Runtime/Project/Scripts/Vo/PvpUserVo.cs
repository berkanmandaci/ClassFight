using Nakama;
namespace _Project.Scripts.Vo
{
    public class PvpUserVo : UserVo
    {
        public string TeamId;
        public PvpUserVo(IApiUser apiUser) : base(apiUser)
        {
        }
    }
}
