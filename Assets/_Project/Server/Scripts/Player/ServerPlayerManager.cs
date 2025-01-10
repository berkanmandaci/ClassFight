using UnityEngine;
using Fusion;
using System.Collections.Generic;
using _Project.Server.Scripts.Core;

namespace _Project.Server.Scripts.Player
{
    public class ServerPlayerManager : MonoBehaviour
    {
        [SerializeField] private List<Transform> spawnPoints;
        private Dictionary<PlayerRef, ServerPlayer> _players = new Dictionary<PlayerRef, ServerPlayer>();
        private NetworkRunner _runner;
        private ServerGameManager _gameManager;

        public void Initialize(NetworkRunner runner)
        {
            _runner = runner;
            _gameManager = GetComponent<ServerGameManager>();
        }

        public void SpawnPlayer(PlayerRef player)
        {
            Vector3 spawnPosition = GetRandomSpawnPoint();
            NetworkObject playerObject = _runner.Spawn(_gameManager.PlayerPrefab, spawnPosition, Quaternion.identity, player);
            
            if (playerObject.TryGetComponent<ServerPlayer>(out var serverPlayer))
            {
                _players.Add(player, serverPlayer);
                serverPlayer.Initialize(player);
            }
        }

        public void DespawnPlayer(PlayerRef player)
        {
            if (_players.TryGetValue(player, out var serverPlayer))
            {
                _runner.Despawn(serverPlayer.Object);
                _players.Remove(player);
            }
        }

        private Vector3 GetRandomSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, spawnPoints.Count);
                return spawnPoints[randomIndex].position;
            }
            
            // Fallback - eğer spawn noktası yoksa
            return new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        }

        public ServerPlayer GetPlayer(PlayerRef playerRef)
        {
            return _players.TryGetValue(playerRef, out var player) ? player : null;
        }

        public void ValidateAllPlayers()
        {
            foreach (var player in _players.Values)
            {
                ValidatePlayer(player);
            }
        }

        private void ValidatePlayer(ServerPlayer player)
        {
            if (!IsPositionValid(player.transform.position))
            {
                Vector3 lastValidPosition = GetLastValidPosition(player);
                player.TeleportTo(lastValidPosition);
            }
        }

        private bool IsPositionValid(Vector3 position)
        {
            return true; // Şimdilik hep geçerli
        }

        private Vector3 GetLastValidPosition(ServerPlayer player)
        {
            return player.transform.position;
        }
    }
} 