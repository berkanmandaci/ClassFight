using UnityEngine;
using Mirror;
using _ProjectV3.Shared.Scripts.Player;

namespace _ProjectV3.Runtime.Scripts.Network
{
    public class CustomNetworkManager : NetworkManager
    {
        [Header("Server Settings")]
        [SerializeField] private int maxConnections = 100;
        [SerializeField] private ushort serverPort = 7777;
        [SerializeField] private NetworkPlayer playerPrefab;
        
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }
        
        #region Unity Callbacks
        
        public override void Awake()
        {
            base.Awake();
            
            // Server ayarlarını yapılandır
            transport.GetComponent<TelepathyTransport>().port = serverPort;
            maxConnections = maxConnections;

            // Player prefab'ını ayarla
            if (playerPrefab != null)
            {
                playerPrefab = spawnPrefabs[0].GetComponent<NetworkPlayer>();
            }
        }
        
        #endregion

        #region Server Sistem

        public override void OnStartServer()
        {
            base.OnStartServer();
            IsServer = true;
            Debug.Log("Server başlatıldı!");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            IsServer = false;
            Debug.Log("Server durduruldu!");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Spawn pozisyonunu belirle
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            // Eğer spawn noktaları varsa, rastgele birini seç
            if (startPositions.Count > 0)
            {
                Transform startPos = startPositions[Random.Range(0, startPositions.Count)];
                spawnPos = startPos.position;
                spawnRot = startPos.rotation;
            }

            // Player'ı spawn et
            GameObject player = Instantiate(playerPrefab.gameObject, spawnPos, spawnRot);
            NetworkServer.AddPlayerForConnection(conn, player);

            Debug.Log($"Player spawn edildi. ConnectionID: {conn.connectionId}");
        }

        #endregion

        #region Client Sistem

        public override void OnStartClient()
        {
            base.OnStartClient();
            IsClient = true;
            Debug.Log("Client başlatıldı!");
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            IsClient = false;
            Debug.Log("Client durduruldu!");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("Sunucuya bağlanıldı!");
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Debug.Log("Sunucudan bağlantı kesildi!");
        }

        #endregion

        #region Utility Methods

        public void StartupServer()
        {
            networkAddress = "localhost";
            StartServer();
        }

        public void StartupClient()
        {
            networkAddress = "localhost"; // Test için localhost, gerçek IP için değiştirin
            StartClient();
        }

        public void StartupHost()
        {
            networkAddress = "localhost";
            StartHost();
        }

        #endregion
    }
} 