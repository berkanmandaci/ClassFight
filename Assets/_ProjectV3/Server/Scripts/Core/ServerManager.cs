using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace _ProjectV3.Server.Scripts.Core
{
    public class ServerManager : MonoBehaviour
    {
        private static ServerManager _instance;
        public static ServerManager Instance => _instance;

        private Dictionary<int, NetworkConnection> _connectedPlayers = new Dictionary<int, NetworkConnection>();
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Server başlatıldığında event'lere abone ol
            NetworkServer.OnConnectedEvent += OnPlayerConnected;
            NetworkServer.OnDisconnectedEvent += OnPlayerDisconnected;
        }

        private void OnDestroy()
        {
            // Event aboneliklerini kaldır
            NetworkServer.OnConnectedEvent -= OnPlayerConnected;
            NetworkServer.OnDisconnectedEvent -= OnPlayerDisconnected;
        }

        private void OnPlayerConnected(NetworkConnection conn)
        {
            Debug.Log($"Oyuncu bağlandı! ID: {conn.connectionId}");
            _connectedPlayers.Add(conn.connectionId, conn);
            
            // Burada oyuncu bağlandığında yapılacak işlemleri ekleyebilirsiniz
        }

        private void OnPlayerDisconnected(NetworkConnection conn)
        {
            Debug.Log($"Oyuncu ayrıldı! ID: {conn.connectionId}");
            _connectedPlayers.Remove(conn.connectionId);
            
            // Burada oyuncu ayrıldığında yapılacak işlemleri ekleyebilirsiniz
        }

        public void KickPlayer(int connectionId, string reason = "Kicked by server")
        {
            if (_connectedPlayers.TryGetValue(connectionId, out NetworkConnection conn))
            {
                conn.Disconnect();
                Debug.Log($"Oyuncu atıldı! ID: {connectionId}, Sebep: {reason}");
            }
        }

        public int GetConnectedPlayerCount()
        {
            return _connectedPlayers.Count;
        }
    }
} 