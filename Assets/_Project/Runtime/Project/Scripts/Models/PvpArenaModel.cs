using _Project.Runtime.Core.Extensions.Singleton;
using _Project.Scripts.Vo;
using System;
using System.Linq;
using UnityEngine;

namespace _Project.Runtime.Project.Service.Scripts.Model
{
    public enum MatchState
    {
        WaitingForPlayers,
        Starting,
        RoundStarting,
        RoundInProgress,
        RoundEnd,
        MatchEnd
    }

    public class PvpArenaModel : Singleton<PvpArenaModel>
    {
        public PvpArenaVo PvpArenaVo { get; private set; }
        public MatchState CurrentState { get; private set; } = MatchState.WaitingForPlayers;
        
        // Events
        public event Action<MatchState> OnMatchStateChanged;
        public event Action<int> OnRoundStarted;
        public event Action<int> OnRoundEnded;
        public event Action<PvpUserVo> OnPlayerDied;
        public event Action<TeamVo> OnTeamWonRound;
        public event Action<PvpUserVo> OnPlayerWonRound;
        
        private float _stateTransitionTime;
        private const float ROUND_START_DELAY = 5f;
        private const float ROUND_END_DELAY = 3f;

        public void Init(PvpArenaVo pvpArenaVo)
        {
            PvpArenaVo = pvpArenaVo;
            CurrentState = MatchState.WaitingForPlayers;
        }

        public void Update()
        {
            if (PvpArenaVo == null) return;

            switch (CurrentState)
            {
                case MatchState.Starting when AreAllPlayersReady():
                    StartNextRound();
                    break;
                
                case MatchState.RoundStarting when Time.time >= _stateTransitionTime:
                    StartRound();
                    break;
                
                case MatchState.RoundInProgress:
                    CheckRoundEnd();
                    break;
                
                case MatchState.RoundEnd when Time.time >= _stateTransitionTime:
                    if (PvpArenaVo.IsMatchComplete())
                        EndMatch();
                    else
                        StartNextRound();
                    break;
            }
        }

        private bool AreAllPlayersReady()
        {
            return PvpArenaVo.Users.All(user => user.IsReady);
        }

        private void StartNextRound()
        {
            UpdateMatchState(MatchState.RoundStarting);
            _stateTransitionTime = Time.time + ROUND_START_DELAY;
        }

        private void StartRound()
        {
            PvpArenaVo.StartNewRound();
            UpdateMatchState(MatchState.RoundInProgress);
            OnRoundStarted?.Invoke(PvpArenaVo.CurrentRound);
        }

        private void CheckRoundEnd()
        {
            if (PvpArenaVo.IsRoundTimeUp() || IsRoundComplete())
            {
                EndRound();
            }
        }

        private bool IsRoundComplete()
        {
            if (PvpArenaVo.IsTeamMatch)
            {
                // Takım bazlı mod için kontrol
                return PvpArenaVo.Teams.Values.Any(team => 
                    team.Members.All(member => !member.IsAlive));
            }
            else
            {
                // FFA mod için kontrol
                return PvpArenaVo.Users.Count(u => u.IsAlive) <= 1;
            }
        }

        private void EndRound()
        {
            UpdateMatchState(MatchState.RoundEnd);
            _stateTransitionTime = Time.time + ROUND_END_DELAY;
            
            OnRoundEnded?.Invoke(PvpArenaVo.CurrentRound);

            // Round kazananını belirle
            if (PvpArenaVo.IsTeamMatch)
            {
                var winningTeam = PvpArenaVo.GetWinningTeam();
                if (winningTeam != null)
                {
                    OnTeamWonRound?.Invoke(winningTeam);
                }
            }
            else
            {
                var topPlayer = PvpArenaVo.GetTopPlayer();
                if (topPlayer != null)
                {
                    OnPlayerWonRound?.Invoke(topPlayer);
                }
            }
        }

        private void EndMatch()
        {
            UpdateMatchState(MatchState.MatchEnd);
        }

        public void UpdateMatchState(MatchState newState)
        {
            if (CurrentState == newState) return;
            
            CurrentState = newState;
            OnMatchStateChanged?.Invoke(newState);
        }

        public void OnPlayerDeath(PvpUserVo player)
        {
            OnPlayerDied?.Invoke(player);
            CheckRoundEnd();
        }

        public bool IsMatchActive => CurrentState == MatchState.RoundInProgress;
    }
}
