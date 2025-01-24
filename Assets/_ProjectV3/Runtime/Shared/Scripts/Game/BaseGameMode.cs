using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace ProjectV3.Shared.Game
{
    public abstract class BaseGameMode : NetworkBehaviour
    {
        [Header("Game Mode Settings")]
        [SerializeField] protected int maxPlayers = 6;
        [SerializeField] protected int roundsToWin = 3;
        [SerializeField] protected float roundTime = 300f; // 5 dakika
        [SerializeField] protected float warmupTime = 5f;  // 5 saniye

        [SyncVar]
        protected GameState currentState = GameState.WaitingForPlayers;

        [SyncVar]
        protected float currentTimer;

        protected Dictionary<int, PlayerStats> playerStats = new Dictionary<int, PlayerStats>();

        public enum GameState
        {
            WaitingForPlayers,
            WarmUp,
            InProgress,
            RoundEnd,
            GameEnd
        }

        protected struct PlayerStats
        {
            public int Kills;
            public int Deaths;
            public int Assists;
            public float DamageDealt;
            public float DamageTaken;
            public int Score;
            public int TeamId;
        }

        #region Server Methods

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log($"[GameMode] Starting {GetType().Name}");
            currentState = GameState.WaitingForPlayers;
            currentTimer = 0;
        }

        [Server]
        protected virtual void StartWarmup()
        {
            if (!isServer) return;
            currentState = GameState.WarmUp;
            currentTimer = warmupTime;
            RpcUpdateGameState(currentState, currentTimer);
            Debug.Log($"[GameMode] Warmup started - Duration: {warmupTime}s");
        }

        [Server]
        protected virtual void StartRound()
        {
            if (!isServer) return;
            currentState = GameState.InProgress;
            currentTimer = roundTime;
            RpcUpdateGameState(currentState, currentTimer);
            Debug.Log($"[GameMode] Round started - Duration: {roundTime}s");
        }

        [Server]
        protected virtual void EndRound()
        {
            if (!isServer) return;
            currentState = GameState.RoundEnd;
            RpcUpdateGameState(currentState, 0);
            Debug.Log("[GameMode] Round ended");
        }

        [Server]
        protected virtual void EndGame()
        {
            if (!isServer) return;
            currentState = GameState.GameEnd;
            RpcUpdateGameState(currentState, 0);
            Debug.Log("[GameMode] Game ended");
        }

        [Server]
        public virtual void OnPlayerJoined(NetworkConnection conn)
        {
            if (!isServer) return;
            
            int playerId = conn.connectionId;
            if (!playerStats.ContainsKey(playerId))
            {
                playerStats.Add(playerId, new PlayerStats());
                Debug.Log($"[GameMode] Player {playerId} joined");

                // Yeterli oyuncu varsa ısınmayı başlat
                if (playerStats.Count >= maxPlayers && currentState == GameState.WaitingForPlayers)
                {
                    StartWarmup();
                }
            }
        }

        [Server]
        public virtual void OnPlayerLeft(NetworkConnection conn)
        {
            if (!isServer) return;

            int playerId = conn.connectionId;
            if (playerStats.ContainsKey(playerId))
            {
                playerStats.Remove(playerId);
                Debug.Log($"[GameMode] Player {playerId} left");
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        protected virtual void RpcUpdateGameState(GameState newState, float timer)
        {
            currentState = newState;
            currentTimer = timer;
            Debug.Log($"[GameMode] State updated to: {newState}, Timer: {timer}");
        }

        #endregion

        #region Utility Methods

        protected virtual void Update()
        {
            if (isServer)
            {
                UpdateGameState();
            }
        }

        [Server]
        protected virtual void UpdateGameState()
        {
            if (!isServer) return;

            switch (currentState)
            {
                case GameState.WarmUp:
                    currentTimer -= Time.deltaTime;
                    if (currentTimer <= 0)
                    {
                        StartRound();
                    }
                    break;

                case GameState.InProgress:
                    currentTimer -= Time.deltaTime;
                    if (currentTimer <= 0)
                    {
                        EndRound();
                    }
                    break;
            }
        }

        #endregion

        #region Score Methods

        [Server]
        public virtual void AddKill(int playerId)
        {
            if (!isServer) return;
            if (playerStats.TryGetValue(playerId, out PlayerStats stats))
            {
                stats.Kills++;
                stats.Score += 100;
                playerStats[playerId] = stats;
                Debug.Log($"[GameMode] Player {playerId} scored a kill. Total: {stats.Kills}");
            }
        }

        [Server]
        public virtual void AddDeath(int playerId)
        {
            if (!isServer) return;
            if (playerStats.TryGetValue(playerId, out PlayerStats stats))
            {
                stats.Deaths++;
                playerStats[playerId] = stats;
                Debug.Log($"[GameMode] Player {playerId} died. Total deaths: {stats.Deaths}");
            }
        }

        [Server]
        public virtual void AddAssist(int playerId)
        {
            if (!isServer) return;
            if (playerStats.TryGetValue(playerId, out PlayerStats stats))
            {
                stats.Assists++;
                stats.Score += 50;
                playerStats[playerId] = stats;
                Debug.Log($"[GameMode] Player {playerId} got an assist. Total: {stats.Assists}");
            }
        }

        [Server]
        public virtual void AddDamageDealt(int playerId, float damage)
        {
            if (!isServer) return;
            if (playerStats.TryGetValue(playerId, out PlayerStats stats))
            {
                stats.DamageDealt += damage;
                playerStats[playerId] = stats;
            }
        }

        [Server]
        public virtual void AddDamageTaken(int playerId, float damage)
        {
            if (!isServer) return;
            if (playerStats.TryGetValue(playerId, out PlayerStats stats))
            {
                stats.DamageTaken += damage;
                playerStats[playerId] = stats;
            }
        }

        #endregion
    }
} 