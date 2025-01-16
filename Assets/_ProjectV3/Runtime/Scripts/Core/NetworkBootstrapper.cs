using UnityEngine;
using Mirror;

namespace ProjectV3.Core
{
    public class NetworkBootstrapper : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private string ipAddress = "localhost";
        [SerializeField] private ushort port = 7777;
        
        private NetworkManager networkManager;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager component not found!");
                return;
            }

            // Transport ayarlarını yap
            var transport = networkManager.GetComponent<kcp2k.KcpTransport>();
            if (transport != null)
            {
                transport.Port = port;
            }
        }

        public void StartHost()
        {
            networkManager.StartHost();
            Debug.Log("Host started on port: " + port);
        }

        public void StartClient()
        {
            networkManager.networkAddress = ipAddress;
            networkManager.StartClient();
            Debug.Log($"Client connecting to {ipAddress}:{port}");
        }

        public void StartServer()
        {
            networkManager.StartServer();
            Debug.Log("Server started on port: " + port);
        }

        public void StopNetwork()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                networkManager.StopHost();
                Debug.Log("Host stopped");
            }
            else if (NetworkServer.active)
            {
                networkManager.StopServer();
                Debug.Log("Server stopped");
            }
            else if (NetworkClient.isConnected)
            {
                networkManager.StopClient();
                Debug.Log("Client stopped");
            }
        }
    }
} 