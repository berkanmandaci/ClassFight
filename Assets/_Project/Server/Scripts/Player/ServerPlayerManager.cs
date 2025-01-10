using System;
using UnityEngine;
using Fusion;
using System.Collections.Generic;
using _Project.Server.Scripts.Core;
using _Project.Shared.Scripts.Enums;
using Fusion.Sockets;

namespace _Project.Server.Scripts.Player
{
    public class ServerPlayerManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private Transform _spawnPointsParent;
        private NetworkPrefabRef _playerPrefab;
        private NetworkRunner _runner;
        private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
        private List<Transform> _spawnPoints = new List<Transform>();
        private int _currentSpawnIndex = 0;

        private void Awake()
        {
            if (_spawnPointsParent != null)
            {
                foreach (Transform child in _spawnPointsParent)
                {
                    _spawnPoints.Add(child);
                }
            }
        }

        public void Init(NetworkRunner runner)
        {
            _runner = runner;
            _runner.AddCallbacks(this);

            // ServerGameManager'dan player prefab'ı al
            var serverGameManager = FindObjectOfType<ServerGameManager>();
            if (serverGameManager != null)
            {
                var prefabRef = serverGameManager.GetPlayerPrefab();
                if (prefabRef.IsValid)
                {
                    _playerPrefab = prefabRef;
                    Debug.Log($"[ServerPlayerManager] Player prefab set successfully");
                }
                else
                {
                    Debug.LogError("[ServerPlayerManager] Player prefab from ServerGameManager is not valid!");
                }
            }
            else
            {
                Debug.LogError("[ServerPlayerManager] ServerGameManager not found!");
            }
        }

        public void SetPlayerPrefab(NetworkPrefabRef prefab)
        {
            _playerPrefab = prefab;
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (_spawnPoints.Count == 0)
            {
                Debug.LogWarning("No spawn points found! Using default position.");
                return Vector3.zero;
            }

            Vector3 position = _spawnPoints[_currentSpawnIndex].position;
            _currentSpawnIndex = (_currentSpawnIndex + 1) % _spawnPoints.Count;
            return position;
        }

        public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (!_playerPrefab.IsValid)
            {
                Debug.LogError("Player prefab is not valid!");
                return null;
            }

            // Oyuncu zaten spawn edilmiş mi kontrol et
            if (_spawnedPlayers.TryGetValue(player, out NetworkObject existingPlayer))
            {
                Debug.Log($"Player {player} already spawned!");
                return existingPlayer;
            }

            Vector3 spawnPosition = GetNextSpawnPosition();
            NetworkObject playerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            
            if (playerObject != null)
            {
                _spawnedPlayers[player] = playerObject;
                Debug.Log($"Spawned player {player} at position {spawnPosition}");
            }
            else
            {
                Debug.LogError($"Failed to spawn player {player}!");
            }

            return playerObject;
        }

        public void DespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
            {
                runner.Despawn(playerObject);
                _spawnedPlayers.Remove(player);
                Debug.Log($"Despawned player {player}");
            }
        }

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
            if (runner.IsServer || runner.IsSharedModeMasterClient)
            {
                SpawnPlayer(runner, player);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer || runner.IsSharedModeMasterClient)
            {
                DespawnPlayer(runner, player);
            }
        }
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            throw new NotImplementedException();
        }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            throw new NotImplementedException();
        }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            throw new NotImplementedException();
        }
        public void OnConnectedToServer(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            throw new NotImplementedException();
        }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            throw new NotImplementedException();
        }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            throw new NotImplementedException();
        }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            throw new NotImplementedException();
        }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            throw new NotImplementedException();
        }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            throw new NotImplementedException();
        }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            throw new NotImplementedException();
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            Debug.Log("Scene load completed in ServerPlayerManager");
        }
        
        public void OnSceneLoadStart(NetworkRunner runner)
        {
            Debug.Log("Scene load started in ServerPlayerManager");
        }

        // Diğer INetworkRunnerCallbacks metodları...
    }
} 