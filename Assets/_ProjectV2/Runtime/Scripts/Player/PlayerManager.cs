using UnityEngine;
using Fusion;
using System.Collections.Generic;
using ProjectV2.Shared;

namespace ProjectV2.Player
{
    public class PlayerManager : NetworkBehaviour
    {
        [SerializeField] private NetworkPrefabRef playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
        private int currentSpawnIndex;

        public static PlayerManager Instance { get; private set; }

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

#if SERVER_BUILD
        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                Debug.Log("PlayerManager initialized on server");
            }
        }

        public NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (!Object.HasStateAuthority) return null;

            if (spawnedPlayers.TryGetValue(player, out NetworkObject existingPlayer))
            {
                Debug.Log($"Player {player} already spawned!");
                return existingPlayer;
            }

            Vector3 spawnPosition = GetNextSpawnPosition();
            NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            if (playerObject != null)
            {
                spawnedPlayers[player] = playerObject;
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
            if (!Object.HasStateAuthority) return;

            if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
            {
                runner.Despawn(playerObject);
                spawnedPlayers.Remove(player);
                Debug.Log($"Despawned player {player}");
            }
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("No spawn points defined! Using default position.");
                return Vector3.zero;
            }

            Vector3 position = spawnPoints[currentSpawnIndex].position;
            currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Length;
            return position;
        }
#else
        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                Debug.Log("PlayerManager initialized on client");
            }
        }
#endif

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
} 