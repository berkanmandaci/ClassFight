using _Project.Runtime.Core.Extensions.Singleton;
using Mirror;
using UnityEngine;

namespace ProjectV3.Client
{
    public class PvpServerModel : SingletonBehaviour<PvpServerModel>
    {
        [Header("Network Settings")]
        [SerializeField] private string serverAddress = "localhost";
        [SerializeField] private ushort port = 7350;
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private string serverKey = "defaultkey";

        [SerializeField] private ProjectV3.Shared.Network.NetworkManager networkManager;


        public void Connect()
        {
            if (autoConnect)
            {
                ConnectToMirrorServer();
            }
        }

        private void ConnectToMirrorServer()
        {
            if (NetworkClient.isConnected)
            {
                Debug.Log("[Client] Already connected to Mirror server!");
                return;
            }

            if (NetworkServer.active)
            {
                Debug.Log("[Client] Cannot connect while server is active!");
                return;
            }

            Debug.Log($"[Client] Initializing Mirror connection to {serverAddress}:{port}");
            networkManager.networkAddress = serverAddress;
            networkManager.StartClient();
        }

        public void StopConnection()
        {
            // Disconnect from Mirror
            if (NetworkClient.isConnected)
            {
                networkManager.StopClient();
            }
        }
    }
}
