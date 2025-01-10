using System;
using UnityEngine;
using Fusion;
using System.Collections.Generic;
using _Project.Server.Scripts.Player;
using Fusion.Sockets;
using _Project.Shared.Scripts.Enums;
using UnityEngine.SceneManagement;

namespace _Project.Server.Scripts.Core
{
    public class ServerGameManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Network Settings")]
        [SerializeField] private NetworkPrefabRef _playerPrefab;
        [SerializeField] private string _gameSceneName = "Arena";
        
        private ServerPlayerManager _playerManager;
        private NetworkRunner _runner;
        private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
        private bool _gameSceneLoaded;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        public void Initialize(NetworkRunner runner)
        {
            Init(runner);
        }

        public void Init(NetworkRunner runner)
        {
            _runner = runner;
            _runner.AddCallbacks(this);

            // PlayerManager'ı oluştur
            var playerManagerObj = new GameObject("PlayerManager");
            playerManagerObj.transform.parent = transform;
            _playerManager = playerManagerObj.AddComponent<ServerPlayerManager>();
            
            // Oyun sahnesini yükle
            if (_runner.IsServer || _runner.IsSharedModeMasterClient)
            {
                LoadGameScene();
            }
        }

        private void LoadGameScene()
        {
            if (!_gameSceneLoaded)
            {
                Debug.Log($"[ServerGameManager] Loading game scene: {_gameSceneName}, Build Index: 1");
                
                try 
                {
                    var sceneRef = SceneRef.FromIndex(1);
                    if (sceneRef.IsValid)
                    {
                        _runner.LoadScene(sceneRef, LoadSceneMode.Additive);
                        _gameSceneLoaded = true;
                        Debug.Log($"[ServerGameManager] Scene load initiated with SceneRef: {sceneRef}");
                    }
                    else
                    {
                        Debug.LogError($"[ServerGameManager] Invalid SceneRef for index: 1");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ServerGameManager] Error loading scene: {e.Message}");
                }
            }
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (runner.IsServer || runner.IsSharedModeMasterClient)
            {
                Debug.Log($"[ServerGameManager] Scene load completed: {_gameSceneName}");
                var scene = SceneManager.GetSceneByName(_gameSceneName);
                if (scene.IsValid())
                {
                    SceneManager.SetActiveScene(scene);
                }
            }
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            Debug.Log($"[ServerGameManager] Starting to load scene: {_gameSceneName}");
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Oyuncu katıldı: {player}");
            
            if (_playerManager != null && runner.IsServer)
            {
                // Eğer oyuncunun zaten bir karakteri varsa, yeni karakter spawn etme
                if (!_spawnedPlayers.ContainsKey(player))
                {
                    var spawnedObject = _playerManager.SpawnPlayer(runner, player);
                    if (spawnedObject != null)
                    {
                        _spawnedPlayers[player] = spawnedObject;
                    }
                }
                else
                {
                    Debug.LogWarning($"Player {player} already has a spawned character!");
                }
            }
            else
            {
                Debug.LogError($"PlayerManager is not initialized or not running on server! IsServer: {runner.IsServer}");
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_playerManager != null && runner.IsServer)
            {
                _playerManager.DespawnPlayer(runner, player);
                _spawnedPlayers.Remove(player);
            }
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            // AOI (Area of Interest) içine giren objeleri işle
            if (runner.IsServer)
            {
                Debug.Log($"Object {obj.Id} entered AOI for player {player}");
            }
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            // AOI (Area of Interest) dışına çıkan objeleri işle
            if (runner.IsServer)
            {
                Debug.Log($"Object {obj.Id} exited AOI for player {player}");
            }
        }

        public void UpdateMatchState(ServerMatchState newState)
        {
            // Match state update işlemi
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

        public NetworkPrefabRef GetPlayerPrefab()
        {
            return _playerPrefab;
        }
    }
} 