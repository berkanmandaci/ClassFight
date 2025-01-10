using System;
using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using Fusion.Sockets;

namespace _Project.Server.Scripts.Player
{
    public class ServerPlayerManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        private NetworkPrefabRef _playerPrefab;
        private NetworkRunner _runner;
        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

        public void SetPlayerPrefab(NetworkPrefabRef prefab)
        {
            if (!prefab.IsValid)
            {
                Debug.LogError("Attempting to set invalid player prefab in ServerPlayerManager!");
                return;
            }
            
            _playerPrefab = prefab;
            Debug.Log($"Player prefab set successfully in ServerPlayerManager: {prefab}");
        }

        public void Init(NetworkRunner runner)
        {
            if (runner == null)
            {
                Debug.LogError("NetworkRunner is null in ServerPlayerManager.Init!");
                return;
            }
            
            _runner = runner;
            _runner.AddCallbacks(this);
            
            if (!_playerPrefab.IsValid)
            {
                Debug.LogError("Player prefab is not set in ServerPlayerManager! Make sure it's assigned before initialization.");
                return;
            }
            
            Debug.Log($"ServerPlayerManager initialized with runner {runner.name} and player prefab {_playerPrefab}");
        }

        public void Initialize(NetworkRunner runner)
        {
            Init(runner);
        }

        public void SpawnPlayer(PlayerRef player)
        {
            if (!_runner.IsServer)
            {
                Debug.LogError("Trying to spawn player on non-server instance!");
                return;
            }

            // Prefab kontrolü
            if (!_playerPrefab.IsValid)
            {
                Debug.LogError($"Player prefab is not valid! Please check NetworkPrefabRef in ServerPlayerManager.");
                return;
            }

            try
            {
                // Spawn pozisyonu
                var spawnPosition = Vector3.zero;
                var spawnRotation = Quaternion.identity;

                Debug.Log($"Attempting to spawn player {player} at position {spawnPosition}");
                
                NetworkObject networkPlayerObject = _runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, player);
                
                if (networkPlayerObject == null)
                {
                    Debug.LogError($"Failed to spawn player object for player {player}");
                    return;
                }

                // Player'ı kaydet
                _spawnedCharacters[player] = networkPlayerObject;
                Debug.Log($"Successfully spawned player {player} with NetworkObject ID: {networkPlayerObject.Id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error spawning player: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        public void DespawnPlayer(PlayerRef player)
        {
            if (!_runner.IsServer)
            {
                return;
            }

            if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            {
                try
                {
                    _runner.Despawn(networkObject);
                    _spawnedCharacters.Remove(player);
                    Debug.Log($"Successfully despawned player {player}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error despawning player: {e.Message}");
                }
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                DespawnPlayer(player);
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            // Tüm oyuncuları temizle
            foreach (var player in _spawnedCharacters.Keys.ToList())
            {
                DespawnPlayer(player);
            }
            _spawnedCharacters.Clear();
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            // AOI dışına çıkan objeleri işle
        }
        
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            // AOI içine giren objeleri işle
        }
        
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                SpawnPlayer(player);
            }
        }
        
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // Input işleme gerekli değilse boş bırak
        }
        
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            // Eksik input işleme gerekli değilse boş bırak
        }
        
        public void OnConnectedToServer(NetworkRunner runner)
        {
            // Server'a bağlanma işlemleri
        }
        
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            // Server'dan kopma işlemleri
        }
        
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            // Bağlantı isteklerini işle
        }
        
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            // Bağlantı hatalarını işle
        }
        
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            // Simulasyon mesajlarını işle
        }
        
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            // Session listesi güncellemelerini işle
        }
        
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            // Özel authentication yanıtlarını işle
        }
        
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            // Host migration işlemlerini yönet
        }
        
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            // Güvenilir veri alımını işle
        }
        
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            // Güvenilir veri ilerleme durumunu işle
        }
        
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            // Sahne yükleme tamamlandığında işle
        }
        
        public void OnSceneLoadStart(NetworkRunner runner)
        {
            // Sahne yükleme başladığında işle
        }
    }
} 