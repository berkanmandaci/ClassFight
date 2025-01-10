using UnityEngine;
using Fusion;
using System.Collections.Generic;
using ProjectV2.Shared;
using ProjectV2.Core;
using System;
using Fusion.Sockets;

namespace ProjectV2.Client
{
    public class ClientPlayerManager : NetworkBehaviour, INetworkRunnerCallbacks
    {
        private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
        private NetworkObject localPlayerObject;

        public static ClientPlayerManager Instance { get; private set; }
        public NetworkObject LocalPlayerObject => localPlayerObject;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                Debug.Log("ClientPlayerManager initialized");
                NetworkManager.Instance.Runner.AddCallbacks(this);
            }
        }

        public void RegisterSpawnedPlayer(PlayerRef player, NetworkObject playerObject)
        {
            if (spawnedPlayers.ContainsKey(player))
            {
                Debug.LogWarning($"Player {player} already registered!");
                return;
            }

            spawnedPlayers[player] = playerObject;

            if (playerObject.HasInputAuthority)
            {
                localPlayerObject = playerObject;
                Debug.Log("Local player registered");
            }
        }

        public void UnregisterPlayer(PlayerRef player)
        {
            if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
            {
                if (playerObject == localPlayerObject)
                {
                    localPlayerObject = null;
                }

                spawnedPlayers.Remove(player);
                Debug.Log($"Player {player} unregistered");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region INetworkRunnerCallbacks Implementation

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player} joined");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player} left");
            UnregisterPlayer(player);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // Input handling will be done in ClientPlayer
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"Shutdown: {shutdownReason}");
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("Connected to server");
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
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        #endregion
    }
} 