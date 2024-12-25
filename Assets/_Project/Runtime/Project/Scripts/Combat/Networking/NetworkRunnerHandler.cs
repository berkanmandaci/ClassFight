using System;
using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;
using Unity.Cinemachine;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private void Start()
    {
        StartGame(GameMode.Single);  // 2.0.3'te Single mode kullanıyoruz
    }

    private async void StartGame(GameMode mode)
    {
        // Create the NetworkRunner
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        Debug.Log($"Starting the game in mode: {mode}");

        try
        {
            var startGameArgs = new StartGameArgs
            {
                GameMode = mode,
                SessionName = "TestRoom",
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            };

            var result = await _runner.StartGame(startGameArgs);
            
            if (result.Ok)
            {
                Debug.Log($"Game started successfully! IsServer: {_runner.IsServer}, IsClient: {_runner.IsClient}, LocalPlayer: {_runner.LocalPlayer}");
            }
            else
            {
                Debug.LogError($"Failed to start game: {result.ShutdownReason}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error starting the game: {e.Message}");
        }
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Called when an object exits the Area of Interest
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Called when an object enters the Area of Interest
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined called. IsServer: {runner.IsServer}, Player: {player}, HasPlayerPrefab: {_playerPrefab.IsValid}");
        
        if (runner.IsServer)
        {
            Debug.Log("We are the server, attempting to spawn player");
            
            if (_playerPrefab.IsValid == false)
            {
                Debug.LogError("Player Prefab is not set or not valid!");
                return;
            }

            try
            {
                // Spawn player character
                Vector3 spawnPosition = new Vector3(0, 0, 0);
                Debug.Log($"Attempting to spawn player at position: {spawnPosition}");
                
                var playerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
              
                if (playerObject == null)
                {
                    Debug.LogError("Failed to spawn player object - returned null!");
                }
                else
                {
                    Debug.Log($"Successfully spawned player object: {playerObject.name} with NetworkId: {playerObject.Id}");
                    _spawnedCharacters.Add(player, playerObject);
                    
                    // Sadece local player'ın kamerasını ayarla
                    if (player == runner.LocalPlayer)
                    {
                        Debug.Log("This is our local player, setting up camera");
                        _camera.Follow = playerObject.transform;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception while spawning player: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            Debug.Log("We are not the server, waiting for server to spawn our player");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} left");
        
        // Clean up spawned player object
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var inputHandler = GetComponent<NetworkInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.OnInput(runner, input);
        }
    }
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    
    public void OnConnectedToServer(NetworkRunner runner) 
    {
        Debug.Log("Connected to server");
    }
    
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) 
    {
        Debug.LogError($"Failed to connect: {reason}");
    }
    
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // Handle reliable data with key
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // Handle reliable data progress
    }

    public void OnSceneLoadDone(NetworkRunner runner) 
    {
        Debug.Log("Scene load completed");
    }
    
    public void OnSceneLoadStart(NetworkRunner runner) 
    {
        Debug.Log("Scene load started");
    }
    
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
    {
        Debug.Log($"Shutdown: {shutdownReason}");
    }
}
