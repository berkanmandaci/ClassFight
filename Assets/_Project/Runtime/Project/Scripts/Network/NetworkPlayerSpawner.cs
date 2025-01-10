using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;
using System;
using _Project.Runtime.Project.Service.Scripts.Model;

public class NetworkPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player joined - Local:{runner.LocalPlayer}, Joining:{player}, State Authority:{runner.IsSharedModeMasterClient}");

        // Sunucu tarafında spawn işlemini ServerPlayerManager yapacak
        if (runner.IsServer || runner.IsSharedModeMasterClient)
        {
            Debug.Log($"Skipping spawn on server side for player {player}");
            return;
        }

        // Client tarafında sadece kendi karakterini takip et
        if (player == runner.LocalPlayer)
        {
            Debug.Log($"Local player {player} waiting for character spawn");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            Debug.Log($"Player {player} left, cleaning up their character");
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner.LocalPlayer == PlayerRef.None)
            return;

        var data = new NetworkInputData();

        // Movement input (WASD)
        data.movementInput.x = Input.GetAxisRaw("Horizontal");
        data.movementInput.y = Input.GetAxisRaw("Vertical");

        // Normalize movement input
        if (data.movementInput.sqrMagnitude > 0)
        {
            data.movementInput.Normalize();
        }

        // Mouse position for rotation
        data.rotationInput = Input.mousePosition;

        input.Set(data);
    }

    // Spawn edilen karakteri kaydet
    public void OnSpawned(NetworkRunner runner, NetworkObject networkObject)
    {
        if (networkObject.HasInputAuthority)
        {
            Debug.Log($"Character spawned with input authority - Object ID: {networkObject.Id}");
            _spawnedCharacters[runner.LocalPlayer] = networkObject;

            if (networkObject.TryGetComponent<BaseCharacterController>(out var controller))
            {
                // PvpArenaModel'den user bilgilerini al
                var pvpArenaVo = PvpArenaModel.Instance.PvpArenaVo;
                var user = pvpArenaVo.GetUser(ServiceModel.Instance.Session.UserId);
                
                controller.TeamId = user.TeamId;
                controller.RPC_SetUserId(user.Id);
                Debug.Log($"Set user info - UserId: {user.Id}, TeamId: {controller.TeamId}");
            }
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
