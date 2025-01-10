using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _Project.Server.Scripts.Player;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

namespace _Project.Server.Scripts.Core
{
    public class ServerNetworkManager : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool _isDevelopmentBuild = true;
        [SerializeField] private string _developmentIP = "127.0.0.1";
        [SerializeField] private ushort _developmentPort = 27016;

        private NetworkRunner _runner;
        private ServerGameManager _gameManager;
        private ServerPlayerManager _playerManager;
        private string _logFilePath;

        private void Awake()
        {
            // Log dosyası yolunu ayarla
            _logFilePath = System.IO.Path.Combine(Application.dataPath, "..", "server_logs.txt");
            WriteToLog("Server başlatılıyor...");
            Application.logMessageReceived += HandleLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, UnityEngine.LogType type)
        {
            string formattedLog = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {logString}";
            if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception)
            {
                formattedLog += $"\nStack Trace: {stackTrace}";
            }
            WriteToLog(formattedLog);
        }

        private void WriteToLog(string message)
        {
            try
            {
                System.IO.File.AppendAllText(_logFilePath, message + "\n");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Log dosyasına yazılamadı: {e.Message}");
            }
        }

        private async void Start()
        {
            Debug.Log("Server başlatılıyor...");
            await InitializeServer();
        }

        private async Task InitializeServer()
        {
            try
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
                _runner.ProvideInput = true;

                var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
                
                // Scene referansını oluştur
                var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
                var sceneInfo = new NetworkSceneInfo();
                if (scene.IsValid)
                {
                    sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
                }

                Debug.Log("Server ayarları yapılandırılıyor...");
                var args = new StartGameArgs()
                {
                    GameMode = GameMode.Server,
                    SessionName = "DedicatedServer",
                    SceneManager = sceneManager,
                    Scene = sceneInfo,
                    CustomLobbyName = "MainLobby",
                    SessionProperties = new Dictionary<string, SessionProperty>()
                    {
                        { "GameType", "Dedicated" }
                    }
                };

                // Development build için özel ayarlar
                if (_isDevelopmentBuild)
                {
                    args.Address = NetAddress.CreateFromIpPort(_developmentIP, _developmentPort);
                    Debug.Log($"Development modunda çalışıyor... IP: {_developmentIP}, Port: {_developmentPort}");
                }

                Debug.Log($"Server başlatılıyor... SessionName: {args.SessionName}, Lobby: {args.CustomLobbyName}");
                var result = await _runner.StartGame(args);

                if (result.Ok)
                {
                    Debug.Log($"Server başarıyla başlatıldı! SessionID: {_runner.SessionInfo.Name}");
                    Debug.Log($"Lobby: {_runner.LobbyInfo?.Name}");
                    InitializeServerSystems();
                }
                else
                {
                    Debug.LogError($"Server başlatılamadı: {result.ShutdownReason} - {result.ErrorMessage}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server hatası: {e.Message}");
                Debug.LogError($"Stack Trace: {e.StackTrace}");
            }
        }

        private void InitializeServerSystems()
        {
            _gameManager = gameObject.AddComponent<ServerGameManager>();
            _playerManager = gameObject.AddComponent<ServerPlayerManager>();

            _gameManager.Initialize(_runner);
            _playerManager.Initialize(_runner);
        }
    }
} 