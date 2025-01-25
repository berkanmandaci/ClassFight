using UnityEngine;
using Mirror;
using ProjectV3.Shared.Core;

namespace ProjectV3.Shared.Network
{
    public class ProjectNetworkManager : Mirror.NetworkManager
    {
        public static new ProjectNetworkManager singleton { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private Vector3[] spawnPoints;
        private int nextSpawnPointIndex;

        private LogManager logManager;

        public override void Awake()
        {
            if (singleton != null && singleton != this)
            {
                Destroy(gameObject);
                return;
            }
            singleton = this;

            // LogManager'ı ekle ve başlat
            logManager = gameObject.AddComponent<LogManager>();
            logManager.Initialize(NetworkServer.active);

            // Transport bilgilerini logla
            var transport = GetComponent<Transport>();
            if (transport != null)
            {
                Debug.Log($"[Transport] Type: {transport.GetType().Name}");
                Debug.Log($"[Transport] Network Address: {networkAddress}");
                Debug.Log($"[Transport] Max Connections: {maxConnections}");
            }

            base.Awake();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            // Server başladığında LogManager'ı yeniden başlat
            if (logManager != null)
            {
                logManager.Initialize(true);
            }
            
            Debug.Log($"[Server] Started");
            Debug.Log($"[Server] Max Connections: {maxConnections}");
            Debug.Log($"[Server] Network Address: {networkAddress}");
            Debug.Log($"[Server] Transport Type: {transport.GetType().Name}");
            Debug.Log($"[Server] Server Active: {NetworkServer.active}");
            Debug.Log($"[Server] Client Active: {NetworkClient.active}");
            Debug.Log($"[Server] Is Host Mode: {NetworkServer.active && NetworkClient.active}");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // Client başladığında LogManager'ı yeniden başlat
            if (logManager != null && !NetworkServer.active)
            {
                logManager.Initialize(false);
            }

            Debug.Log($"[Client] Starting client...");
            Debug.Log($"[Client] Transport Type: {transport.GetType().Name}");
            Debug.Log($"[Client] Network Address: {networkAddress}");

            // Client'ı başlat ve bağlantıyı dene
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                Debug.Log($"[Client] Attempting to connect to: {networkAddress}");
                NetworkClient.Connect(networkAddress);
                Debug.Log($"[Client] Connection attempt initiated");
                Debug.Log($"[Client] Client Active: {NetworkClient.active}");
                Debug.Log($"[Client] Is Connected: {NetworkClient.isConnected}");
                Debug.Log($"[Client] Is Host Mode: {NetworkClient.active && NetworkServer.active}");
            }
            else
            {
                Debug.Log($"[Client] Already connected or in server mode");
                Debug.Log($"[Client] Client Active: {NetworkClient.active}");
                Debug.Log($"[Client] Is Connected: {NetworkClient.isConnected}");
                Debug.Log($"[Client] Is Host Mode: {NetworkClient.active && NetworkServer.active}");
            }
        }

        public override void OnStopServer()
        {
            Debug.Log("[Server] Stopping server...");
            Debug.Log($"[Server] Active Connections: {NetworkServer.connections.Count}");
            base.OnStopServer();
            Debug.Log("[Server] Stopped");
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            Debug.Log($"[Server] Client connected - Connection ID: {conn.connectionId}, Address: {conn.address}");
            Debug.Log($"[Server] Total connections: {NetworkServer.connections.Count}");
            Debug.Log($"[Server] Is Host Mode: {NetworkServer.active && NetworkClient.active}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.Log($"[Server] Client disconnecting - Connection ID: {conn.connectionId}, Address: {conn.address}");
            Debug.Log($"[Server] Remaining connections: {NetworkServer.connections.Count - 1}");
            base.OnServerDisconnect(conn);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log($"[Client] Connected successfully to {networkAddress}");
            Debug.Log($"[Client] Connection ID: {NetworkClient.connection.connectionId}");
            Debug.Log($"[Client] Is Connected: {NetworkClient.isConnected}");
            Debug.Log($"[Client] Is Host Mode: {NetworkClient.active && NetworkServer.active}");
        }

        public override void OnClientDisconnect()
        {
            Debug.Log("[Client] Disconnecting from server...");
            Debug.Log($"[Client] Last known Connection ID: {NetworkClient.connection?.connectionId}");
            Debug.Log($"[Client] Is Connected: {NetworkClient.isConnected}");
            base.OnClientDisconnect();
            Debug.Log("[Client] Disconnected");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Spawn noktasını belirle
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                spawnPos = spawnPoints[nextSpawnPointIndex];
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
            }

            // Oyuncuyu spawn et
            GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
            NetworkServer.AddPlayerForConnection(conn, player);
            
            Debug.Log($"[Server] Player spawned - Connection ID: {conn.connectionId}");
            Debug.Log($"[Server] Player Object ID: {player.GetComponent<NetworkIdentity>().netId}");
            Debug.Log($"[Server] Spawn Position: {spawnPos}");
            Debug.Log($"[Server] Total players: {NetworkServer.connections.Count}");
        }

        public override void OnServerError(NetworkConnectionToClient conn, TransportError error, string reason)
        {
            Debug.LogError($"[Server] Error - Connection ID: {conn?.connectionId}");
            Debug.LogError($"[Server] Error Type: {error}");
            Debug.LogError($"[Server] Error Reason: {reason}");
            Debug.LogError($"[Server] Is Host Mode: {NetworkServer.active && NetworkClient.active}");
            base.OnServerError(conn, error, reason);
        }

        public override void OnClientError(TransportError error, string reason)
        {
            Debug.LogError($"[Client] Connection Error");
            Debug.LogError($"[Client] Target Address: {networkAddress}");
            Debug.LogError($"[Client] Is Connected: {NetworkClient.isConnected}");
            Debug.LogError($"[Client] Error Type: {error}");
            Debug.LogError($"[Client] Error Reason: {reason}");
            Debug.LogError($"[Client] Is Host Mode: {NetworkClient.active && NetworkServer.active}");
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