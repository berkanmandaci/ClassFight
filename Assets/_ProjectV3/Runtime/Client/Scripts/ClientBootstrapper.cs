using UnityEngine;
using Mirror;
using ProjectV3.Shared.Network;

namespace ProjectV3.Client
{
    public class ClientBootstrapper : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private string serverAddress = "localhost";
        [SerializeField] private ushort port = 7777;
        [SerializeField] private bool autoConnect = true;
        
        private ProjectV3.Shared.Network.NetworkManager networkManager;

        private void Awake()
        {
            networkManager = GetComponent<ProjectV3.Shared.Network.NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("[Client] NetworkManager component not found!");
                return;
            }

            // Network ayarlarını yap
            networkManager.networkAddress = serverAddress;

            // Otomatik bağlantı aktifse
            if (autoConnect)
            {
                Debug.Log("[Client] Auto-connect enabled, will attempt connection on Start");
            }
        }

        private void Start()
        {
            if (autoConnect)
            {
                ConnectToServer();
            }
        }

        public void ConnectToServer()
        {
            if (NetworkClient.isConnected)
            {
                Debug.Log("[Client] Already connected to server!");
                return;
            }

            if (NetworkServer.active)
            {
                Debug.Log("[Client] Cannot connect while server is active!");
                return;
            }

            Debug.Log($"[Client] Initializing connection to {serverAddress}:{port}");
            networkManager.networkAddress = serverAddress;
            networkManager.StartClient();
        }

        public void DisconnectFromServer()
        {
            if (!NetworkClient.isConnected)
            {
                Debug.Log("[Client] Not connected to any server!");
                return;
            }

            Debug.Log("[Client] Disconnecting from server...");
            networkManager.StopClient();
        }

        private void OnApplicationQuit()
        {
            DisconnectFromServer();
        }
    }
} 