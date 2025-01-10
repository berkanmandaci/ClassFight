using UnityEngine;
using Fusion;
using System.Collections.Generic;
using ProjectV2.Shared;
using ProjectV2.Core;

namespace ProjectV2.Client
{
    public class ClientPlayerManager : NetworkBehaviour
    {
        private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
        private NetworkObject localPlayerObject;

        public static ClientPlayerManager Instance { get; private set; }
        public NetworkObject LocalPlayerObject => localPlayerObject;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                Debug.Log("ClientPlayerManager initialized");
                NetworkManager.Instance.Runner.AddCallbacks(this);
            }
        }

        public void RegisterSpawnedPlayer(PlayerRef player, NetworkObject playerObject)
        {
            if (spawnedPlayers.ContainsKey(player))
            {
                Debug.LogWarning($"Player {player} already registered!");
                return;
            }

            spawnedPlayers[player] = playerObject;

            if (playerObject.HasInputAuthority)
            {
                localPlayerObject = playerObject;
                Debug.Log("Local player registered");
            }
        }

        public void UnregisterPlayer(PlayerRef player)
        {
            if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
            {
                if (playerObject == localPlayerObject)
                {
                    localPlayerObject = null;
                }

                spawnedPlayers.Remove(player);
                Debug.Log($"Player {player} unregistered");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
} 