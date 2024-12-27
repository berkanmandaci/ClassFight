using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance { get; private set; }

    // Events
    public event Action<int, int> OnPlayerCountChanged;
    public event Action<string> OnMatchmakingStateChanged;

    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private int _maxPlayers = 2;
    [SerializeField] private int _arenaSceneIndex = 1;
    [SerializeField] private string _lobbyName = "DefaultLobby";

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private List<PlayerRef> _pendingSpawns = new List<PlayerRef>();

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

    public async void StartMatchmaking()
    {
        Debug.Log("Starting matchmaking...");
        OnMatchmakingStateChanged?.Invoke("Matchmaking başlatılıyor...");
        
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        // Matchmaking'i başlat
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = _lobbyName,
            PlayerCount = _maxPlayers,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError($"Failed to start game: {result.ShutdownReason}");
            OnMatchmakingStateChanged?.Invoke($"Bağlantı hatası: {result.ShutdownReason}");
        }
        else
        {
            OnMatchmakingStateChanged?.Invoke("Oyuncu bekleniyor...");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} joined, total players: {runner.ActivePlayers.Count()}");

        // Oyuncuyu spawn edilecekler listesine ekle
        if (!_pendingSpawns.Contains(player))
        {
            _pendingSpawns.Add(player);
            Debug.Log($"Added player {player} to pending spawns");
        }

        // Player count'u güncelle
        int currentPlayers = runner.ActivePlayers.Count();
        OnPlayerCountChanged?.Invoke(currentPlayers, _maxPlayers);

        // Yeterli oyuncu varsa arena sahnesine geç
        if (runner.IsServer && currentPlayers >= _maxPlayers)
        {
            Debug.Log("Required player count reached, loading arena...");
            OnMatchmakingStateChanged?.Invoke("Oyun başlatılıyor...");
            runner.SessionInfo.IsOpen = false;
            
            var scene = SceneRef.FromIndex(_arenaSceneIndex);
            if (scene.IsValid)
            {
                runner.LoadScene(scene);
            }
            else
            {
                Debug.LogError($"Invalid scene index: {_arenaSceneIndex}");
                OnMatchmakingStateChanged?.Invoke("Sahne yükleme hatası!");
            }
        }
        else
        {
            OnMatchmakingStateChanged?.Invoke($"Oyuncu bekleniyor... ({currentPlayers}/{_maxPlayers})");
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("Scene load completed");
        OnMatchmakingStateChanged?.Invoke("Arena yükleniyor...");
        
        // Arena sahnesi yüklendiyse bekleyen oyuncuları spawn et
        if (runner.IsServer && SceneManager.GetActiveScene().buildIndex == _arenaSceneIndex)
        {
            foreach (var player in _pendingSpawns.ToList())
            {
                if (!_spawnedCharacters.ContainsKey(player))
                {
                    Debug.Log($"Spawning pending player {player}");
                    Vector3 spawnPosition = new Vector3((player.RawEncoded % _maxPlayers) * 3, 0, 0);
                    NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

                    // TeamId'yi oyuncu sırasına göre ata (0, 1, 2, 3...)
                    if (networkPlayerObject.TryGetComponent<BaseCharacterController>(out var character))
                    {
                        character.TeamId = _spawnedCharacters.Count;
                    }
                    
                    _spawnedCharacters.Add(player, networkPlayerObject);
                    Debug.Log($"Spawned player object: {networkPlayerObject.Id} for player {player}");
                }
            }
            _pendingSpawns.Clear();
            OnMatchmakingStateChanged?.Invoke("Oyun başladı!");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} left");
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
        _pendingSpawns.Remove(player);

        // Player count'u güncelle
        OnPlayerCountChanged?.Invoke(runner.ActivePlayers.Count(), _maxPlayers);
        OnMatchmakingStateChanged?.Invoke("Oyuncu ayrıldı!");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Session shutdown: {shutdownReason}");
        OnMatchmakingStateChanged?.Invoke($"Oturum kapandı: {shutdownReason}");
        SceneManager.LoadScene(0);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.W))
            data.MovementInput.y += 1;
        if (Input.GetKey(KeyCode.S))
            data.MovementInput.y -= 1;
        if (Input.GetKey(KeyCode.A))
            data.MovementInput.x -= 1;
        if (Input.GetKey(KeyCode.D))
            data.MovementInput.x += 1;

        // Mouse Position for Rotation
        data.RotationInput = Input.mousePosition;

        // Combat Input
        data.AttackPressed = Input.GetMouseButton(0);
        data.DashPressed = Input.GetMouseButton(1);
        data.DodgePressed = Input.GetKey(KeyCode.Space);

        // Character Switch Input
        data.NextCharacterPressed = Input.GetKey(KeyCode.Q);
        data.PreviousCharacterPressed = Input.GetKey(KeyCode.E);

        input.Set(data);
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
