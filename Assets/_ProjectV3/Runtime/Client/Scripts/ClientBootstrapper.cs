using UnityEngine;
using Mirror;

namespace ProjectV3.Client
{
    public class ClientBootstrapper : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private string serverAddress = "localhost";
        [SerializeField] private ushort port = 7777;
        
        private ProjectV3.Shared.Network.NetworkManager networkManager;

        private void Awake()
        {
            networkManager = GetComponent<ProjectV3.Shared.Network.NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager component not found!");
                return;
            }
        }

        public void ConnectToServer()
        {
            networkManager.networkAddress = serverAddress;
            networkManager.StartClient();
            Debug.Log($"Connecting to server at {serverAddress}:{port}");
        }

        public void DisconnectFromServer()
        {
            if (NetworkClient.isConnected)
            {
                networkManager.StopClient();
                Debug.Log("Disconnected from server");
            }
        }

        private void OnApplicationQuit()
        {
            DisconnectFromServer();
        }
    }
} 