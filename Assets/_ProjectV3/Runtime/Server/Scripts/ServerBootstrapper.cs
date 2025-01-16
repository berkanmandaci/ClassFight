using UnityEngine;
using Mirror;
using ProjectV3.Shared.Network;

namespace ProjectV3.Server
{
    public class ServerBootstrapper : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private ushort port = 7777;
        
         [SerializeField] private ProjectV3.Shared.Network.NetworkManager networkManager;


        private void Start()
        {
            StartServer();
        }

        public void StartServer()
        {
            networkManager.StartServer();
            Debug.Log($"Server started on port: {port}");
        }

        public void StopServer()
        {
            if (NetworkServer.active)
            {
                networkManager.StopServer();
                Debug.Log("Server stopped");
            }
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }
    }
} 