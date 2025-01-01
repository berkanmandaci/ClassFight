using _Project.Runtime.Core.Extensions.Singleton;
using _Project.Scripts.Vo;
namespace _Project.Runtime.Project.Service.Scripts.Model
{
    public enum MatchState
    {
        WaitingForPlayers,
        Starting,
        InProgress,
        Finished
    }

    public class PvpArenaModel : Singleton<PvpArenaModel>
    {
        public PvpArenaVo PvpArenaVo { get; private set; }
        public MatchState CurrentState { get; private set; } = MatchState.WaitingForPlayers;
        
        public void Init(PvpArenaVo pvpArenaVo)
        {
            PvpArenaVo = pvpArenaVo;
            CurrentState = MatchState.WaitingForPlayers;
        }

        public void UpdateMatchState(MatchState newState)
        {
            CurrentState = newState;
            OnMatchStateChanged?.Invoke(newState);
        }

        public bool IsMatchActive => CurrentState == MatchState.InProgress;
        
        // Match durumu değiştiğinde tetiklenecek event
        public delegate void MatchStateChangedHandler(MatchState newState);
        public event MatchStateChangedHandler OnMatchStateChanged;
    }
}
