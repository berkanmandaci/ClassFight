using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

namespace ProjectV3.Shared.Game
{
    public class FreeForAllMode : BaseGameMode
    {
        [Header("FFA Settings")]
        [SerializeField] private int killsToWin = 15;
        
        private Dictionary<int, int> playerKills = new Dictionary<int, int>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            maxPlayers = 6;
            Debug.Log($"[FreeForAll] Started - Max Players: {maxPlayers}, Kills to win: {killsToWin}");
        }

        [Server]
        public override void OnPlayerJoined(NetworkConnection conn)
        {
            if (!isServer) return;

            int playerId = conn.connectionId;
            if (!playerStats.ContainsKey(playerId))
            {
                var stats = new PlayerStats { TeamId = playerId }; // Her oyuncu kendi takımında
                playerStats.Add(playerId, stats);
                playerKills.Add(playerId, 0);

                Debug.Log($"[FreeForAll] Player {playerId} joined the game");

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
            if (playerKills.ContainsKey(playerId))
            {
                playerKills.Remove(playerId);
            }
            base.OnPlayerLeft(conn);
        }

        [Server]
        public override void AddKill(int killerId)
        {
            if (!isServer) return;

            base.AddKill(killerId);

            if (playerKills.ContainsKey(killerId))
            {
                playerKills[killerId]++;
                RpcUpdatePlayerKills(killerId, playerKills[killerId]);

                // Kazananı kontrol et
                if (playerKills[killerId] >= killsToWin)
                {
                    EndGame();
                    RpcAnnounceWinner(killerId);
                }
            }
        }

        [ClientRpc]
        private void RpcUpdatePlayerKills(int playerId, int kills)
        {
            Debug.Log($"[FreeForAll] Player {playerId} kills updated to {kills}");
        }

        [ClientRpc]
        private void RpcAnnounceWinner(int playerId)
        {
            Debug.Log($"[FreeForAll] Player {playerId} wins the match!");
        }

        protected override void UpdateGameState()
        {
            if (!isServer) return;

            base.UpdateGameState();

            // Oyun sırasında oyuncu sayısını kontrol et
            if (currentState == GameState.InProgress)
            {
                if (playerStats.Count < 2)
                {
                    Debug.Log("[FreeForAll] Not enough players to continue, ending round");
                    EndRound();
                }
            }
        }

        public int GetPlayerKills(int playerId)
        {
            return playerKills.TryGetValue(playerId, out int kills) ? kills : 0;
        }

        public Dictionary<int, int> GetLeaderboard()
        {
            return new Dictionary<int, int>(playerKills.OrderByDescending(x => x.Value)
                                                     .ToDictionary(x => x.Key, x => x.Value));
        }
    }
} 