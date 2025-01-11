using UnityEngine;
using Mirror;
using _ProjectV3.Shared.Scripts.Player;
using kcp2k;
using System;
using System.IO;

namespace _ProjectV3.Runtime.Scripts.Network
{
    public class CustomNetworkManager : NetworkManager
    {
        [Header("Server Settings")]
        [SerializeField] private ushort serverPort = 7777;
        [SerializeField] private NetworkPlayer playerPrefabRef;
        [SerializeField] private Transform spawnPointsParent;
        
        private string logFilePath;
        private readonly object logLock = new object();
        
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }

        private void InitializeTransport()
        {
            // Önce mevcut transport'u temizle
            if (transport != null)
            {
                Destroy(transport);
            }

            // Yeni KCP Transport ekle
            var kcpTransport = gameObject.AddComponent<KcpTransport>();
            transport = kcpTransport;
            kcpTransport.Port = serverPort;
            WriteToLog("Yeni KCP Transport oluşturuldu ve ayarlandı.");
        }
        
        #region Unity Callbacks
        
        public override void Awake()
        {
            base.Awake();
            
            // Log dosyası yolunu ayarla
            string logDirectory = Path.Combine(Application.dataPath, "../Logs");
            Directory.CreateDirectory(logDirectory);
            logFilePath = Path.Combine(logDirectory, $"Server_Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            
            WriteToLog("Server başlatılıyor...");
            
            // Transport kontrolü ve kurulumu
            InitializeTransport();

            // Player prefab'ını ayarla
            if (playerPrefabRef != null)
            {
                playerPrefab = playerPrefabRef.gameObject;
                spawnPrefabs.Clear(); // Önce listeyi temizle
                spawnPrefabs.Add(playerPrefabRef.gameObject);
                WriteToLog("Player prefab ayarlandı.");
            }
            else
            {
                WriteToLog("HATA: Player Prefab atanmamış!", LogType.Error);
            }

            // Debug.Log yerine kendi log sistemimizi kullanalım
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            WriteToLog("Server kapatılıyor...");
        }

        private void Start()
        {
            // Transport aktif olduğundan emin ol
            if (transport == null)
            {
                InitializeTransport();
            }

            // Player prefab kontrolü
            if (playerPrefab == null && playerPrefabRef != null)
            {
                playerPrefab = playerPrefabRef.gameObject;
                if (!spawnPrefabs.Contains(playerPrefabRef.gameObject))
                {
                    spawnPrefabs.Add(playerPrefabRef.gameObject);
                }
            }

            // Spawn noktalarını ayarla
            if (spawnPointsParent != null)
            {
                startPositions.Clear();
                
                for (int i = 0; i < spawnPointsParent.childCount; i++)
                {
                    Transform spawnPoint = spawnPointsParent.GetChild(i);
                    if (!startPositions.Contains(spawnPoint))
                    {
                        startPositions.Add(spawnPoint);
                        WriteToLog($"Spawn noktası eklendi: {spawnPoint.name}");
                    }
                }
            }
            else
            {
                WriteToLog("UYARI: Spawn Points Parent atanmamış! Spawn noktaları manuel olarak atanmalı.", LogType.Warning);
            }
        }
        
        #endregion

        #region Server Sistem

        public override void OnStartServer()
        {
            base.OnStartServer();
            IsServer = true;
            WriteToLog("Server başlatıldı!");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            IsServer = false;
            WriteToLog("Server durduruldu!");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Player prefab son kontrol
            if (playerPrefab == null)
            {
                WriteToLog("HATA: Player Prefab null! Oyuncu spawn edilemedi.", LogType.Error);
                return;
            }

            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (startPositions.Count > 0)
            {
                Transform startPos = startPositions[UnityEngine.Random.Range(0, startPositions.Count)];
                spawnPos = startPos.position;
                spawnRot = startPos.rotation;
                WriteToLog($"Spawn noktası seçildi: {startPos.name}");
            }

            GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
            NetworkServer.AddPlayerForConnection(conn, player);

            WriteToLog($"Player spawn edildi. ConnectionID: {conn.connectionId}");
        }

        #endregion

        #region Client Sistem

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Transport ve prefab son kontrol
            if (transport == null)
            {
                WriteToLog("HATA: Transport bulunamadı! Client başlatılamıyor.", LogType.Error);
                StopClient();
                return;
            }

            if (playerPrefab == null)
            {
                WriteToLog("HATA: Player Prefab bulunamadı! Client başlatılamıyor.", LogType.Error);
                StopClient();
                return;
            }

            IsClient = true;
            WriteToLog("Client başlatıldı!");
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            IsClient = false;
            WriteToLog("Client durduruldu!");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            WriteToLog("Sunucuya bağlanıldı!");
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            WriteToLog("Sunucudan bağlantı kesildi!");
        }

        #endregion

        #region Utility Methods

        public void StartupServer()
        {
            if (transport == null) InitializeTransport();
            networkAddress = "localhost";
            StartServer();
        }

        public void StartupClient()
        {
            if (transport == null) InitializeTransport();
            networkAddress = "localhost";
            StartClient();
        }

        public void StartupHost()
        {
            if (transport == null) InitializeTransport();
            networkAddress = "localhost";
            StartHost();
        }

        private void WriteToLog(string message, LogType logType = LogType.Log)
        {
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMessage = $"[{timeStamp}] [{logType}] {message}";

            // Thread-safe log yazma
            lock (logLock)
            {
                try
                {
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Log dosyasına yazılamadı: {e.Message}");
                }
            }

            // Console'a da yazdır
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Unity'nin kendi log mesajlarını da yakala
            if (!string.IsNullOrEmpty(condition))
            {
                WriteToLog($"{condition}\n{stackTrace}", type);
            }
        }

        #endregion
    }
} 