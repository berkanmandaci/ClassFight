using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;
using System;

public class NetworkPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player joined - Local:{runner.LocalPlayer}, Joining:{player}, State Authority:{runner.IsSharedModeMasterClient}");

        // Her oyuncu kendi karakterini spawn eder
        if (player == runner.LocalPlayer)
        {
            // Eğer bu oyuncu için daha önce spawn yapılmamışsa
            if (!_spawnedCharacters.ContainsKey(player))
            {
                Debug.Log($"Local player {player} spawning own character");
                Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));

                // Kendi karakterini spawn et
                NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

                if (networkPlayerObject != null)
                {
                    if (networkPlayerObject.TryGetComponent<BaseCharacterController>(out var controller))
                    {
                        controller.Owner = player;
                    }
                    
                    _spawnedCharacters.Add(player, networkPlayerObject);
                    Debug.Log($"Player {player} spawned own character - Object ID: {networkPlayerObject.Id}, State Authority: {networkPlayerObject.StateAuthority}, Input Authority: {networkPlayerObject.InputAuthority}, Has Input Authority: {networkPlayerObject.HasInputAuthority}");
                }
                else
                {
                    Debug.LogError($"Failed to spawn player {player}");
                }
            }
            else
            {
                Debug.Log($"Player {player} already has a spawned character");
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Sadece local player için input topla
        if (runner.LocalPlayer != PlayerRef.None)
        {
            var data = new NetworkInputData();

            // Movement input (WASD)
            if (Input.GetKey(KeyCode.W)) data.movementInput.y = 1;
            if (Input.GetKey(KeyCode.S)) data.movementInput.y = -1;
            if (Input.GetKey(KeyCode.A)) data.movementInput.x = -1;
            if (Input.GetKey(KeyCode.D)) data.movementInput.x = 1;

            // Normalize movement input
            if (data.movementInput.sqrMagnitude > 0)
            {
                data.movementInput.Normalize();
            }

            // Mouse position for rotation
            data.rotationInput = Input.mousePosition;

            // Action buttons
            data.buttons.Set(NetworkInputData.ATTACK, Input.GetMouseButton(0));  // Sol tık
            data.buttons.Set(NetworkInputData.DASH, Input.GetMouseButton(1));    // Sağ tık
            data.buttons.Set(NetworkInputData.DODGE, Input.GetKey(KeyCode.Space)); // Space
            data.buttons.Set(NetworkInputData.NEXT_CHAR, Input.GetKey(KeyCode.Q)); // Q
            data.buttons.Set(NetworkInputData.PREV_CHAR, Input.GetKey(KeyCode.E)); // E

            input.Set(data);
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log($"Connected to server - Local Player: {runner.LocalPlayer}, Is Master: {runner.IsSharedModeMasterClient}");
    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
} 