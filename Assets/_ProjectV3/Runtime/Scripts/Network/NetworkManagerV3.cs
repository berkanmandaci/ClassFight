using UnityEngine;
using Mirror;

namespace ProjectV3.Network
{
    public class NetworkManagerV3 : NetworkManager
    {
        public static new NetworkManagerV3 singleton { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private Vector3[] spawnPoints;
        private int nextSpawnPointIndex;

        public override void Awake()
        {
            if (singleton != null && singleton != this)
            {
                Destroy(gameObject);
                return;
            }
            singleton = this;
            base.Awake();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("Server Started");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            Debug.Log("Server Stopped");
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            Debug.Log($"Client connected to server: {conn.connectionId}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.Log($"Client disconnected from server: {conn.connectionId}");
            base.OnServerDisconnect(conn);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("Connected to server!");
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Debug.Log("Disconnected from server!");
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
            
            Debug.Log($"Player spawned for connection {conn.connectionId} at position {spawnPos}");
        }

        public override void OnServerError(NetworkConnectionToClient conn, TransportError error, string reason)
        {
            Debug.LogError($"Server Error - Connection: {conn.connectionId}, Error: {error}, Reason: {reason}");
            base.OnServerError(conn, error, reason);
        }

        public override void OnClientError(TransportError error, string reason)
        {
            Debug.LogError($"Client Error - Error: {error}, Reason: {reason}");
            base.OnClientError(error, reason);
        }
    }
} 