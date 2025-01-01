using _Project.Runtime.Core.Extensions.Singleton;
using _Project.Scripts.Vo;
namespace _Project.Runtime.Project.Service.Scripts.Model
{
    public class PvpArenaModel : Singleton<PvpArenaModel>
    {
        public PvpArenaVo PvpArenaVo { get; private set; }
        
        
        public void Init(PvpArenaVo pvpArenaVo)
        {
            PvpArenaVo = pvpArenaVo;
        }
    }
}
