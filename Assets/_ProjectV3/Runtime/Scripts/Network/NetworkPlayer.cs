using UnityEngine;
using Mirror;

namespace ProjectV3.Network
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPlayerIdChanged))]
        private string playerId;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            CmdSetupPlayer(System.Guid.NewGuid().ToString());
        }

        [Command]
        private void CmdSetupPlayer(string newPlayerId)
        {
            playerId = newPlayerId;
            Debug.Log($"Player setup completed - ID: {playerId}");
        }

        private void OnPlayerIdChanged(string oldPlayerId, string newPlayerId)
        {
            Debug.Log($"Player ID changed from {oldPlayerId} to {newPlayerId}");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log($"Player started on server - Object ID: {netId}");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            Debug.Log($"Player stopped on server - Object ID: {netId}");
        }
    }
} 