using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

namespace ProjectV3.Shared.Game
{
    public class TeamDeathmatchMode : BaseGameMode
    {
        [Header("Team Settings")]
        [SerializeField] private int teamsCount = 2;
        [SerializeField] private int playersPerTeam = 3;
        [SerializeField] private int scoreToWin = 10;

        [SyncVar]
        private int[] teamScores;

        private Dictionary<int, int> playerTeams = new Dictionary<int, int>(); // playerId -> teamId

        public override void OnStartServer()
        {
            base.OnStartServer();
            maxPlayers = teamsCount * playersPerTeam;
            teamScores = new int[teamsCount];
            Debug.Log($"[TeamDeathmatch] Started - Teams: {teamsCount}, Players per team: {playersPerTeam}");
        }

        [Server]
        public override void OnPlayerJoined(NetworkConnection conn)
        {
            if (!isServer) return;

            int playerId = conn.connectionId;
            if (!playerStats.ContainsKey(playerId))
            {
                // Oyuncuyu en az oyuncusu olan takıma ekle
                int teamId = GetTeamWithLeastPlayers();
                var stats = new PlayerStats { TeamId = teamId };
                playerStats.Add(playerId, stats);
                playerTeams.Add(playerId, teamId);

                Debug.Log($"[TeamDeathmatch] Player {playerId} joined Team {teamId}");
                RpcUpdatePlayerTeam(playerId, teamId);

                // Yeterli oyuncu varsa ısınmayı başlat
                if (playerStats.Count >= maxPlayers && currentState == GameState.WaitingForPlayers)
                {
                    StartWarmup();
                }
            }
        }

        [Server]
        public override void OnPlayerLeft(NetworkConnection conn)
        {
            if (!isServer) return;

            int playerId = conn.connectionId;
            if (playerTeams.ContainsKey(playerId))
            {
                playerTeams.Remove(playerId);
            }
            base.OnPlayerLeft(conn);
        }

        [Server]
        private int GetTeamWithLeastPlayers()
        {
            var teamCounts = new int[teamsCount];
            foreach (var team in playerTeams.Values)
            {
                teamCounts[team]++;
            }

            int minPlayers = int.MaxValue;
            int selectedTeam = 0;

            for (int i = 0; i < teamCounts.Length; i++)
            {
                if (teamCounts[i] < minPlayers)
                {
                    minPlayers = teamCounts[i];
                    selectedTeam = i;
                }
            }

            return selectedTeam;
        }

        [Server]
        public override void AddKill(int killerId)
        {
            if (!isServer) return;

            base.AddKill(killerId);

            if (playerTeams.TryGetValue(killerId, out int teamId))
            {
                teamScores[teamId]++;
                RpcUpdateTeamScore(teamId, teamScores[teamId]);

                // Kazanan takımı kontrol et
                if (teamScores[teamId] >= scoreToWin)
                {
                    EndGame();
                    RpcAnnounceWinner(teamId);
                }
            }
        }

        [ClientRpc]
        private void RpcUpdatePlayerTeam(int playerId, int teamId)
        {
            Debug.Log($"[TeamDeathmatch] Player {playerId} assigned to Team {teamId}");
        }

        [ClientRpc]
        private void RpcUpdateTeamScore(int teamId, int score)
        {
            Debug.Log($"[TeamDeathmatch] Team {teamId} score updated to {score}");
        }

        [ClientRpc]
        private void RpcAnnounceWinner(int teamId)
        {
            Debug.Log($"[TeamDeathmatch] Team {teamId} wins the match!");
        }

        protected override void UpdateGameState()
        {
            if (!isServer) return;

            base.UpdateGameState();

            // Oyun sırasında takım sayılarını kontrol et
            if (currentState == GameState.InProgress)
            {
                var teamCounts = new int[teamsCount];
                foreach (var team in playerTeams.Values)
                {
                    teamCounts[team]++;
                }

                // Eğer bir takımda hiç oyuncu kalmazsa
                for (int i = 0; i < teamCounts.Length; i++)
                {
                    if (teamCounts[i] == 0)
                    {
                        Debug.Log($"[TeamDeathmatch] Team {i} has no players, ending round");
                        EndRound();
                        break;
                    }
                }
            }
        }

        public int GetPlayerTeam(int playerId)
        {
            return playerTeams.TryGetValue(playerId, out int teamId) ? teamId : -1;
        }

        public bool ArePlayersInSameTeam(int player1Id, int player2Id)
        {
            if (playerTeams.TryGetValue(player1Id, out int team1) && 
                playerTeams.TryGetValue(player2Id, out int team2))
            {
                return team1 == team2;
            }
            return false;
        }
    }
} 