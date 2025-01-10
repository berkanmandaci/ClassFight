using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using ProjectV2.Shared;
using System;
using Cysharp.Threading.Tasks;

namespace ProjectV2.Core
{
    public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkRunner networkRunnerPrefab;
        
        private NetworkRunner runner;
        private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

        public static NetworkManager Instance { get; private set; }
        public NetworkRunner Runner => runner;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async UniTask InitializeNetwork(bool isServer)
        {
            try
            {
                Debug.Log($"Initializing network as {(isServer ? "Server" : "Client")}");

                if (runner != null)
                {
                    await runner.Shutdown();
                    Destroy(runner.gameObject);
                }

                runner = Instantiate(networkRunnerPrefab);
                runner.name = isServer ? "Server Network Runner" : "Client Network Runner";
                
                var args = new StartGameArgs()
                {
                    GameMode = isServer ? GameMode.Server : GameMode.Client,
                    SessionName = GameDefines.NetworkSettings.LOBBY_NAME,
                    SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
                };

                if (isServer)
                {
                    args.Address = NetAddress.CreateFromIpPort(
                        GameDefines.NetworkSettings.DEFAULT_IP, 
                        GameDefines.NetworkSettings.DEFAULT_PORT
                    );
                }

                var result = await runner.StartGame(args);
                
                if (!result.Ok)
                {
                    throw new Exception($"Failed to start {(isServer ? "server" : "client")}: {result.ShutdownReason}");
                }

                Debug.Log($"Successfully started {(isServer ? "server" : "client")}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing network: {e.Message}");
                throw;
            }
        }

        #region INetworkRunnerCallbacks Implementation

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player} joined");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player} left");
            if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
            {
                runner.Despawn(playerObject);
                spawnedPlayers.Remove(player);
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
        {
            Debug.Log($"Network shutdown: {shutdownReason}");
        }
        
        public void OnConnectedToServer(NetworkRunner runner) 
        {
            Debug.Log("Connected to server");
        }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            throw new NotImplementedException();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner) 
        {
            Debug.Log("Disconnected from server");
        }
        
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            throw new NotImplementedException();
        }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        #endregion
    }
} 