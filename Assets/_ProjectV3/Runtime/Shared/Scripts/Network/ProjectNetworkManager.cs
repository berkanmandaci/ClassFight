using UnityEngine;
using Mirror;
using ProjectV3.Shared.Core;
using System.Collections.Generic;

namespace ProjectV3.Shared.Network
{
    public class ProjectNetworkManager : Mirror.NetworkManager
    {
        public static new ProjectNetworkManager singleton { get; private set; }
        public bool isShuttingDown { get; private set; }

        [Header("Network Settings")]
        [SerializeField] private int maxMessageSize = 16384; // 16 KB
        [SerializeField] private int messageQueueSize = 10000;
        [SerializeField] private int noDelay = 1; // Nagle algoritmasını devre dışı bırak
        [SerializeField] private int sendTimeout = 5000; // 5 saniye
        [SerializeField] private int receiveTimeout = 5000; // 5 saniye

        [Header("Spawn Settings")]
        [SerializeField] private Vector3[] spawnPoints = new Vector3[]
        {
            new Vector3(-5, 1, 0),
            new Vector3(5, 1, 0),
            new Vector3(0, 1, -5),
            new Vector3(0, 1, 5),
            new Vector3(-5, 1, -5),
            new Vector3(5, 1, 5)
        };

        private int nextSpawnPointIndex;
        private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
        private LogManager logManager;
        private const float DISCONNECT_TIMEOUT = 10f;

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

            logManager = gameObject.AddComponent<LogManager>();
            logManager.Initialize(NetworkServer.active);

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
                switch (property.Name)
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

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            try
            {
                if (playerPrefab == null)
                {
                    Debug.LogError("[Server] Player Prefab is not assigned!");
                    return;
                }

                Vector3 spawnPos = Vector3.zero;
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    spawnPos = spawnPoints[nextSpawnPointIndex];
                    nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
                }

                GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                if (player == null)
                {
                    Debug.LogError("[Server] Failed to instantiate player prefab!");
                    return;
                }

                NetworkServer.AddPlayerForConnection(conn, player);
                spawnedPlayers[conn.connectionId] = player;
                
                Debug.Log($"[Server] Player spawned - Connection ID: {conn.connectionId}, Position: {spawnPos}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Server] Spawn error: {e.Message}");
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