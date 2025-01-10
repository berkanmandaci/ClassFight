using System;
using UnityEngine;
using Fusion;
using System.Collections.Generic;
using _Project.Server.Scripts.Player;
using Fusion.Sockets;
using _Project.Shared.Scripts.Enums;

namespace _Project.Server.Scripts.Core
{
    public class ServerGameManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Network Settings")]
        [SerializeField] private NetworkPrefabRef _playerPrefab;
        [SerializeField] private ServerPlayerManager _playerManager;
        private NetworkRunner _runner;
        private ServerMatchState _currentMatchState;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _currentMatchState = ServerMatchState.WaitingForPlayers;
        }

        public void Initialize(NetworkRunner runner)
        {
            _runner = runner;
            
            // PlayerManager'ı bul veya oluştur
            _playerManager = FindObjectOfType<ServerPlayerManager>();
            if (_playerManager == null)
            {
                var playerManagerObj = new GameObject("ServerPlayerManager");
                _playerManager = playerManagerObj.AddComponent<ServerPlayerManager>();
            }
            
            // Player prefab kontrolü
            if (!_playerPrefab.IsValid)
            {
                Debug.LogError("Player prefab is not valid in ServerGameManager! Please assign it in the inspector.");
                return;
            }
            
            // PlayerManager'ı başlat
            _playerManager.SetPlayerPrefab(_playerPrefab);
            _playerManager.Init(_runner);
            
            Debug.Log($"ServerGameManager initialized successfully! Player Prefab: {_playerPrefab}");
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Oyuncu katıldı: {player}");
            
            if (_playerManager != null && runner.IsServer)
            {
                _playerManager.SpawnPlayer(player);
            }
            else
            {
                Debug.LogError($"PlayerManager is not initialized or not running on server! IsServer: {runner.IsServer}");
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Oyuncu ayrıldı: {player}");
            if (_playerManager != null && runner.IsServer)
            {
                _playerManager.DespawnPlayer(player);
            }
        }

        public void UpdateMatchState(ServerMatchState newState)
        {
            _currentMatchState = newState;
            Debug.Log($"Match state changed to: {newState}");
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            throw new NotImplementedException();
        }
        public void OnDisconnectedFromServer(NetworkRunner runner) { }
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
    }
} 