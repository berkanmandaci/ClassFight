using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using _Project.Runtime.Project.Service.Scripts.Model;
using _Project.Scripts.Vo;
using Cysharp.Threading.Tasks;

namespace _Project.Runtime.Project.Scripts.Combat.Networking
{
    public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static NetworkRunnerHandler Instance { get; private set; }

        public event Action<int, int> OnPlayerCountChanged;
        public event Action<string> OnMatchmakingStateChanged;

        [Header("Network Settings")]
        [SerializeField] private NetworkPrefabRef _playerPrefab;
        [SerializeField] private int _maxPlayers = 2;
        [SerializeField] private int _arenaSceneIndex = 1;

        private NetworkRunner _runner;
        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
        private List<PlayerRef> _pendingSpawns = new List<PlayerRef>();
        private Dictionary<PlayerRef, string> _playerToUserId = new Dictionary<PlayerRef, string>();
        private Dictionary<string, PlayerRef> _userIdToPlayer = new Dictionary<string, PlayerRef>();

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

    

        public async Task JoinPhotonServer()
        {
            try
            {
                await UniTask.SwitchToMainThread();
                Debug.Log("Starting Photon Fusion connection...");
                OnMatchmakingStateChanged?.Invoke("Oyun sunucusuna bağlanılıyor...");

                if (_runner == null)
                {
                    _runner = gameObject.AddComponent<NetworkRunner>();
                    _runner.ProvideInput = true;
                }

                var pvpArenaVo = PvpArenaModel.Instance?.PvpArenaVo;
                if (pvpArenaVo == null)
                {
                    throw new InvalidOperationException("PvpArenaModel veya PvpArenaVo henüz hazır değil!");
                }

                var sceneManager = GetComponent<NetworkSceneManagerDefault>();
                if (sceneManager == null)
                {
                    sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
                }

                Debug.Log("Starting game with scene manager...");
                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.AutoHostOrClient,
                    SessionName = pvpArenaVo.MatchId,
                    PlayerCount = _maxPlayers,
                    SceneManager = sceneManager,
                    CustomLobbyName = "PvpLobby"
                });

                if (!result.Ok)
                {
                    HandleStartGameError(result.ShutdownReason);
                    return;
                }

                Debug.Log("Game started successfully");
                OnMatchmakingStateChanged?.Invoke("Oyuncular hazırlanıyor...");
            }
            catch (Exception e)
            {
                HandleError("Photon Fusion bağlantısı başlatılamadı", e);
            }
        }

        private void HandleStartGameError(ShutdownReason reason)
        {
            string message = reason switch
            {
                ShutdownReason.ServerInRoom => "Sunucu zaten bir odada",
                ShutdownReason.GameNotFound => "Oyun bulunamadı",
                _ => $"Bağlantı hatası: {reason}"
            };
            
            ErrorHandler.Instance.HandleNetworkError(message);
            OnMatchmakingStateChanged?.Invoke(message);
        }

        private void HandleError(string message, Exception e)
        {
            Debug.LogError($"{message}: {e.Message}\nStack Trace: {e.StackTrace}");
            ErrorHandler.Instance.HandleNetworkError(message, e);
            OnMatchmakingStateChanged?.Invoke("Beklenmeyen bir hata oluştu!");
        }
        
        

 

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            try
            {
                if (runner == null || PvpArenaModel.Instance?.PvpArenaVo == null)
                {
                    throw new InvalidOperationException("NetworkRunner veya PvpArenaVo hazır değil!");
                }

                if (_spawnedCharacters.ContainsKey(player) || _pendingSpawns.Contains(player))
                {
                    Debug.LogWarning($"Player {player} already registered!");
                    return;
                }

                _pendingSpawns.Add(player);
                
                int currentPlayers = _spawnedCharacters.Count + _pendingSpawns.Count;
                OnPlayerCountChanged?.Invoke(currentPlayers, _maxPlayers);

                
                
                if (currentPlayers == _maxPlayers)
                {
                    StartArenaMatch(runner);
                }
                else
                {
                    OnMatchmakingStateChanged?.Invoke($"Oyuncular bekleniyor... ({currentPlayers}/{_maxPlayers})");
                }
            }
            catch (Exception e)
            {
                HandleError("Oyuncu katılırken hata oluştu", e);
            }
        }

        private void StartArenaMatch(NetworkRunner runner)
        {
            try
            {
                Debug.Log("All players connected, loading arena...");
                OnMatchmakingStateChanged?.Invoke("Oyun başlatılıyor...");
                runner.SessionInfo.IsOpen = false;

                if (!runner.IsServer)
                {
                    Debug.Log("Client waiting for server to load scene...");
                    return;
                }

                Debug.Log($"Server loading scene {_arenaSceneIndex}...");
                if (runner.SceneManager != null)
                {
                    var sceneManager = runner.SceneManager as NetworkSceneManagerDefault;
                    if (sceneManager != null)
                    {
                        Debug.Log($"Loading scene with build index: {_arenaSceneIndex}");
                        runner.LoadScene(SceneRef.FromIndex(_arenaSceneIndex));
                        PvpArenaModel.Instance.UpdateMatchState(MatchState.Starting);
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid SceneManager type!");
                    }
                }
                else
                {
                    throw new InvalidOperationException("SceneManager not found!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"StartArenaMatch error: {e.Message}\nStack Trace: {e.StackTrace}");
                ErrorHandler.Instance.HandleGameStateError("Arena başlatılamadı", e);
            }
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            try
            {
                Debug.Log($"Scene load completed. IsServer: {runner.IsServer}, Current Scene: {SceneManager.GetActiveScene().buildIndex}");
                
                if (!runner.IsServer)
                {
                    Debug.Log("Client scene load completed");
                    return;
                }

                if (SceneManager.GetActiveScene().buildIndex != _arenaSceneIndex)
                {
                    Debug.LogWarning($"Unexpected scene index: {SceneManager.GetActiveScene().buildIndex}");
                    return;
                }

                Debug.Log($"Processing {_pendingSpawns.Count} pending spawns");
                SpawnPendingPlayers(runner);

                if (_spawnedCharacters.Count == _maxPlayers)
                {
                    Debug.Log("All players spawned successfully");
                    PvpArenaModel.Instance.UpdateMatchState(MatchState.InProgress);
                }
            }
            catch (Exception e)
            {
                HandleError("Sahne yüklenirken hata oluştu", e);
            }
        }

        private void SpawnPendingPlayers(NetworkRunner runner)
        {

            if (_pendingSpawns.ToList().Count>PvpArenaModel.Instance.PvpArenaVo.Users.Count)
            {
                LogModel.Instance.Error(new Exception("PvpArenaModel.Instance.PvpArenaVo.Users.Count > _pendingSpawns.ToList().Count"));
            }
            
            for (var i = 0; i < _pendingSpawns.ToList().Count; i++)
            {
                var player = _pendingSpawns.ToList()[i];
                try
                {
                    if (_spawnedCharacters.ContainsKey(player)) continue;

                    Vector3 spawnPosition = new Vector3((player.RawEncoded % _maxPlayers) * 3, 0, 0);
                    var networkObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

                    if (networkObject != null)
                    {
                        if (networkObject.TryGetComponent<BaseCharacterController>(out var character))
                        {
                            // string teamId = _teams.FirstOrDefault(x => x.Value.Contains(player)).Key ?? "Team1";
                            // character.TeamId = teamId;

                            character.UserId = PvpArenaModel.Instance.PvpArenaVo.Users[i].Id;
                        }

                        _spawnedCharacters[player] = networkObject;
                        Debug.Log($"Spawned player {player} with object {networkObject.Id}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error spawning player {player}: {e.Message}");
                }
            }
            _pendingSpawns.Clear();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            try
            {
                CleanupPlayerData(runner, player);
                OnPlayerCountChanged?.Invoke(runner.ActivePlayers.Count(), _maxPlayers);
                OnMatchmakingStateChanged?.Invoke("Oyuncu ayrıldı!");
            }
            catch (Exception e)
            {
                HandleError("Oyuncu çıkarılırken hata oluştu", e);
            }
        }

        private void CleanupPlayerData(NetworkRunner runner, PlayerRef player)
        {
        

            if (_playerToUserId.TryGetValue(player, out string userId))
            {
                _userIdToPlayer.Remove(userId);
                _playerToUserId.Remove(player);
                Debug.Log($"Player {player} (UserId={userId}) eşleştirmesi kaldırıldı");
            }

            if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            {
                runner.Despawn(networkObject);
                _spawnedCharacters.Remove(player);
            }
            
            _pendingSpawns.Remove(player);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData
            {
                MovementInput = new Vector2(
                    (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)
                ),
                RotationInput = Input.mousePosition,
                AttackPressed = Input.GetMouseButton(0),
                DashPressed = Input.GetMouseButton(1),
                DodgePressed = Input.GetKey(KeyCode.Space),
                NextCharacterPressed = Input.GetKey(KeyCode.Q),
                PreviousCharacterPressed = Input.GetKey(KeyCode.E)
            };

            input.Set(data);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"Session shutdown: {shutdownReason}");
            OnMatchmakingStateChanged?.Invoke($"Oturum kapandı: {shutdownReason}");
            SceneManager.LoadScene(0);
        }

        private void OnDestroy()
        {
            _playerToUserId.Clear();
            _userIdToPlayer.Clear();
            _spawnedCharacters.Clear();
            _pendingSpawns.Clear();
        }

        // Boş implementasyonlar
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
        public void OnSceneLoadStart(NetworkRunner runner)
        {
            Debug.Log($"Scene load starting... IsServer: {runner.IsServer}");
            OnMatchmakingStateChanged?.Invoke("Arena yükleniyor...");
        }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    }
}
