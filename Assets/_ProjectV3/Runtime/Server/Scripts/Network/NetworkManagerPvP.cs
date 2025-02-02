using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace ProjectV3.Server.Network
{
    /// <summary>
    /// PvP arena için özelleştirilmiş NetworkManager implementasyonu
    /// </summary>
    public class NetworkManagerPvP : NetworkManager
    {
        #region Singleton Pattern
        public static NetworkManagerPvP Instance { get; private set; }
        #endregion

        #region Properties
        [Header("Arena Settings")]
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private int _maxPlayersPerArena = 10;
        
        private readonly HashSet<NetworkConnection> _connectedPlayers = new();
        #endregion

        #region Unity Lifecycle
        public override void Awake()
        {
            base.Awake();
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region Network Callbacks
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            
            if (_connectedPlayers.Count >= _maxPlayersPerArena)
            {
                conn.Disconnect();
                Debug.LogWarning($"Player connection rejected: Arena is full. Max players: {_maxPlayersPerArena}");
                return;
            }

            _connectedPlayers.Add(conn);
            Debug.Log($"Player connected. Total players: {_connectedPlayers.Count}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            _connectedPlayers.Remove(conn);
            Debug.Log($"Player disconnected. Remaining players: {_connectedPlayers.Count}");
            
            base.OnServerDisconnect(conn);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _connectedPlayers.Clear();
            Debug.Log("PvP Server started");
        }

        public override Transform GetStartPosition()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("No spawn points configured!");
                return null;
            }

            return _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Arenada aktif oyuncu sayısını döndürür
        /// </summary>
        public int GetActivePlayerCount() => _connectedPlayers.Count;
        #endregion
    }
} 