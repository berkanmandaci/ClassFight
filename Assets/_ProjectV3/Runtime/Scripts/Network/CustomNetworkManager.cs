using UnityEngine;
using Mirror;
using _ProjectV3.Shared.Scripts.Player;

namespace _ProjectV3.Runtime.Scripts.Network
{
    public class CustomNetworkManager : NetworkManager
    {
        [Header("Server Settings")]
        [SerializeField] private ushort serverPort = 7777;
        [SerializeField] private NetworkPlayer playerPrefabRef;
        
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }
        
        #region Unity Callbacks
        
        public override void Awake()
        {
            base.Awake();
            
            // Server ayarlarını yapılandır
            if (transport != null && transport.GetComponent<TelepathyTransport>() != null)
            {
                transport.GetComponent<TelepathyTransport>().port = serverPort;
            }
            else
            {
                Debug.LogError("Transport component bulunamadı!");
            }

            // Player prefab'ını ayarla
            if (playerPrefabRef != null)
            {
                playerPrefab = playerPrefabRef.gameObject;
                spawnPrefabs.Add(playerPrefabRef.gameObject);
            }
            else
            {
                Debug.LogError("Player Prefab atanmamış!");
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
            GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
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