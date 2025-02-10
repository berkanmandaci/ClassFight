using UnityEngine;
using Mirror;
using ProjectV3.Shared.Core;
using System.Collections.Generic;
using System.Linq;
using ProjectV3.Shared.Combat;
using ProjectV3.Shared.Vo;
using ProjectV3.Shared.Game;
using Nakama;

namespace ProjectV3.Shared.Network
{
    public class ProjectNetworkManager : NetworkManager
    {
        public new static ProjectNetworkManager singleton { get; private set; }
        public bool isShuttingDown { get; private set; }

        [Header("Network Settings")]
        [SerializeField] private int maxMessageSize = 16384; // 16 KB
        [SerializeField] private int messageQueueSize = 10000;
        [SerializeField] private int noDelay = 1; // Nagle algoritmasını devre dışı bırak
        [SerializeField] private int sendTimeout = 5000; // 5 saniye
        [SerializeField] private int receiveTimeout = 5000; // 5 saniye

        // [Header("Spawn Settings")]
        private readonly Vector3[] spawnPoints =
        {
            new(-5, 1, 0),
            new(5, 1, 0),
            new(0, 1, -5),
            new(0, 1, 5),
            new(-5, 1, -5),
            new(5, 1, 5)
        };

        private int nextSpawnPointIndex;
        private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
        [SerializeField] private LogManager logManager;
        private const float DISCONNECT_TIMEOUT = 10f;

        private Dictionary<int, CombatUserVo> _combatUsers = new Dictionary<int, CombatUserVo>();
        private Dictionary<int, IMatchmakerMatched> _pendingMatches = new Dictionary<int, IMatchmakerMatched>();
        private Dictionary<string, IMatch> _activeMatches = new Dictionary<string, IMatch>();


        public override void Awake()
        {
            Debug.Log("[NetworkManager] Initializing...");
            if (singleton != null && singleton != this)
            {
                Destroy(gameObject);
                return;
            }
            singleton = this;
            DontDestroyOnLoad(gameObject);

            ConfigureTransport();

            base.Awake();
        }

        private void ConfigureTransport()
        {
            var transport = GetComponent<Transport>();
            if (transport == null)
            {
                Debug.LogError("[NetworkManager] Transport component bulunamadı!");
                return;
            }

            // Transport ayarlarını yap
            var transportType = transport.GetType().Name;
            Debug.Log($"[NetworkManager] Transport type: {transportType}");

            // Transport özelliklerini ayarla
            var properties = transport.GetType().GetProperties();
            foreach (var property in properties)
            {
                switch ( property.Name )
                {
                    case "MaxMessageSize":
                        property.SetValue(transport, maxMessageSize);
                        break;
                    case "NoDelay":
                        property.SetValue(transport, noDelay);
                        break;
                    case "SendTimeout":
                        property.SetValue(transport, sendTimeout);
                        break;
                    case "ReceiveTimeout":
                        property.SetValue(transport, receiveTimeout);
                        break;
                }
            }

            Debug.Log($"[NetworkManager] Transport ayarları yapılandırıldı:");
            Debug.Log($"- Max Message Size: {maxMessageSize}");
            Debug.Log($"- No Delay: {noDelay}");
            Debug.Log($"- Send Timeout: {sendTimeout}ms");
            Debug.Log($"- Receive Timeout: {receiveTimeout}ms");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            spawnedPlayers.Clear();
            nextSpawnPointIndex = 0;
            isShuttingDown = false;

            if (logManager != null)
            {
                logManager.Initialize(true);
            }

            Debug.Log($"[Server] Started");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            isShuttingDown = false;

            if (logManager != null && !NetworkServer.active)
            {
                logManager.Initialize(false);
            }

            Debug.Log($"[Client] Starting client...");
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            Debug.Log($"[Server] Client connected - Connection ID: {conn.connectionId}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (!isShuttingDown)
            {
                Debug.Log($"[Server] Client disconnecting - Connection ID: {conn.connectionId}");

                // Combat verilerini temizle
                if (_combatUsers.TryGetValue(conn.connectionId, out var combatData))
                {
                    // Oyuncuyu takımdan çıkar
                    CombatArenaModel.Instance.UnregisterPlayer(combatData);
                    CombatArenaModel.Instance.UnregisterCombatData(conn.connectionId);
                    _combatUsers.Remove(conn.connectionId);
                }

                // Oyuncu objesini temizle
                if (spawnedPlayers.TryGetValue(conn.connectionId, out GameObject playerObj))
                {
                    NetworkServer.Destroy(playerObj);
                    spawnedPlayers.Remove(conn.connectionId);
                }
            }

            base.OnServerDisconnect(conn);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log($"[Client] Connected successfully");
        }

        public override void OnClientDisconnect()
        {
            if (!isShuttingDown)
            {
                Debug.Log("[Client] Disconnecting from server...");
                base.OnClientDisconnect();
            }
        }

        [Server]
        public void RegisterMatchData(int connectionId, IMatchmakerMatched matchData)
        {
            if (matchData != null)
            {
                _pendingMatches[connectionId] = matchData;

                // Oyun modunu belirle ve ayarla
                GameModeType gameMode = GameModeType.FreeForAll; // Varsayılan mod

                if (matchData.Self != null &&
                    matchData.Self.NumericProperties != null &&
                    matchData.Self.NumericProperties.TryGetValue("gameMode", out double gameModeValue))
                {
                    gameMode = (GameModeType)((int)gameModeValue);
                    Debug.Log($"[Server] Oyun modu ayarlandı: {gameMode}");
                }

                CombatArenaModel.Instance.SetGameMode(gameMode);
                Debug.Log($"[Server] Match data registered for connection {connectionId}");
            }
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return Vector3.zero;

            Vector3 position = spawnPoints[nextSpawnPointIndex];
            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
            return position;
        }

        private (string userId, string username) GetUserDataFromMatch(int connectionId)
        {
            if (_pendingMatches.TryGetValue(connectionId, out var matchData))
            {
                var matchUser = matchData.Users.FirstOrDefault(u => u.Presence.SessionId == connectionId.ToString());
                if (matchUser != null)
                {
                    return (matchUser.Presence.UserId, matchUser.Presence.Username);
                }
            }
            return ($"local_{connectionId}", $"Player_{connectionId}");
        }

        private GameObject CreatePlayerInstance(Vector3 spawnPosition)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[Server] Player Prefab atanmamış!");
                return null;
            }

            var player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

            if (player == null)
            {
                Debug.LogError("[Server] Player prefab oluşturulamadı!");
                return null;
            }

            var characterController = player.GetComponent<BaseCharacterController>();
            var networkIdentity = player.GetComponent<NetworkIdentity>();

            if (characterController == null || networkIdentity == null)
            {
                Debug.LogError("[Server] Player prefab'ında gerekli bileşenler eksik!");
                Destroy(player);
                return null;
            }

            return player;
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            try
            {
                // Oyun modunu kontrol et ve gerekirse varsayılan modu ayarla
                if (CombatArenaModel.Instance.GetCurrentGameMode() == GameModeType.None)
                {
                    Debug.LogWarning("[Server] Oyun modu ayarlanmamış, varsayılan mod (FreeForAll) kullanılıyor.");
                    CombatArenaModel.Instance.SetGameMode(GameModeType.FreeForAll);
                }

                // Spawn pozisyonunu al ve oyuncuyu oluştur
                Vector3 spawnPos = GetNextSpawnPosition();
                GameObject player = CreatePlayerInstance(spawnPos);

                if (player == null) return;

                // Gerekli bileşenleri al
                var characterController = player.GetComponent<BaseCharacterController>();

                // Kullanıcı verilerini al
                var (userId, username) = GetUserDataFromMatch(conn.connectionId);

                // Combat verilerini oluştur
                var userData = new UserVo(id: userId, username: username);
                var combatData = characterController.GetCombatData();

                // Combat verilerini başlat
                combatData.Initialize(userData,  conn.connectionId);

                // Combat verilerini kaydet
                _combatUsers[conn.connectionId] = combatData;
                CombatArenaModel.Instance.RegisterCombatData(conn.connectionId, combatData);

                // Karakter kontrolcüsünü başlat
                characterController.Init(combatData);

                // Oyuncuyu arena sistemine kaydet
                CombatArenaModel.Instance.RegisterPlayer(combatData, conn.connectionId);

                // Oyuncuyu ağa ekle
                NetworkServer.AddPlayerForConnection(conn, player);
                spawnedPlayers[conn.connectionId] = player;

                Debug.Log($"[Server] Oyuncu spawn edildi - Bağlantı ID: {conn.connectionId}, Konum: {spawnPos}, Kullanıcı: {userData.DisplayName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Server] Spawn hatası: {e.Message}\n{e.StackTrace}");
            }
        }

        public override void OnStopClient()
        {
            isShuttingDown = true;
            base.OnStopClient();
        }

        public override void OnStopServer()
        {
            isShuttingDown = true;
            base.OnStopServer();
        }

        public override void OnServerError(NetworkConnectionToClient conn, TransportError error, string reason)
        {
            if (!isShuttingDown)
            {
                Debug.LogError($"[Server] Error - Connection: {conn?.connectionId}, Type: {error}, Reason: {reason}");
            }
            base.OnServerError(conn, error, reason);
        }

        public override void OnClientError(TransportError error, string reason)
        {
            if (!isShuttingDown)
            {
                Debug.LogError($"[Client] Error - Type: {error}, Reason: {reason}");
            }
            base.OnClientError(error, reason);
        }

        public override void OnValidate()
        {
            base.OnValidate();

            // Transport ayarlarını kontrol et
            var transport = GetComponent<Transport>();
            if (transport != null)
            {
                Debug.Log($"[Setup] Transport: {transport.GetType().Name}");
                Debug.Log($"[Setup] Network Address: {networkAddress}");
                Debug.Log($"[Setup] Max Connections: {maxConnections}");
            }
            else
            {
                Debug.LogError("[Setup] No Transport component found!");
            }

            // Player Prefab kontrolü
            if (playerPrefab == null)
            {
                Debug.LogError("[Setup] Player Prefab is not assigned!");
            }
            else if (!playerPrefab.GetComponent<NetworkIdentity>())
            {
                Debug.LogError("[Setup] Player Prefab must have a NetworkIdentity component!");
            }
        }
    }
}
