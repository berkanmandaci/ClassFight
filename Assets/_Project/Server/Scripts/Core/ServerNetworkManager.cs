using UnityEngine;
using Fusion;
using Cysharp.Threading.Tasks;
using Fusion.Sockets;
using System.IO;

namespace _Project.Server.Scripts.Core
{
    public class ServerNetworkManager : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private NetworkRunner _networkRunnerPrefab;
        [SerializeField] private string _gameSceneName = "GameScene";
        
        [Header("Development Settings")]
        [SerializeField] private bool _isDevelopmentBuild = true;
        [SerializeField] private string _developmentIP = "127.0.0.1";
        [SerializeField] private ushort _developmentPort = 27016;

        private NetworkRunner _runner;
        private ServerGameManager _serverGameManager;
        private string _logFilePath;

        private async void Awake()
        {
            Application.targetFrameRate = 60;
            DontDestroyOnLoad(gameObject);
            
            // Log dosyası yolunu ayarla
            _logFilePath = Path.Combine(Application.dataPath, "..", "server_logs.txt");
            
            // Log dosyasını temizle ve yeniden başlat
            File.WriteAllText(_logFilePath, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] Server logs başlatıldı\n");
            
            WriteToLog("Server başlatılıyor...");
            
            // Log callback'ini ekle
            Application.logMessageReceived += HandleLog;

            try 
            {
                await InitializeServer();
                WriteToLog("Server başlatma işlemi tamamlandı.");
            }
            catch (System.Exception e)
            {
                WriteToLog($"Server başlatma işlemi başarısız oldu: {e.Message}");
                WriteToLog($"Stack Trace: {e.StackTrace}");
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
            
            if (_runner != null)
            {
                _runner.Shutdown();
                Destroy(_runner.gameObject);
                _runner = null;
            }
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
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{timestamp}] {message}";
            
            try
            {
                File.AppendAllText(_logFilePath, logMessage + "\n");
                Debug.Log(logMessage);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Log dosyasına yazılamadı: {e.Message}\nDosya yolu: {_logFilePath}");
            }
        }

        public async UniTask InitializeServer()
        {
            try
            {
                WriteToLog("Server başlatma işlemi başladı...");

                // Eğer önceki runner varsa temizle
                if (_runner != null)
                {
                    WriteToLog("Önceki NetworkRunner temizleniyor...");
                    _runner.Shutdown();
                    Destroy(_runner.gameObject);
                    _runner = null;
                }

                WriteToLog("Server ayarları yapılandırılıyor...");

                // NetworkRunner'ı oluştur
                _runner = Instantiate(_networkRunnerPrefab);
                if (_runner == null)
                {
                    throw new System.Exception("NetworkRunner prefab oluşturulamadı!");
                }
                DontDestroyOnLoad(_runner.gameObject);
                WriteToLog("NetworkRunner oluşturuldu.");

                // Mevcut ServerGameManager'ı bul
                _serverGameManager = FindObjectOfType<ServerGameManager>();
                if (_serverGameManager == null)
                {
                    throw new System.Exception("ServerGameManager bulunamadı! Lütfen sahnede olduğundan emin olun.");
                }
                WriteToLog("ServerGameManager bulundu.");

                if (_isDevelopmentBuild)
                {
                    WriteToLog($"Development modunda çalışıyor... IP: {_developmentIP}, Port: {_developmentPort}");
                }

                var args = new StartGameArgs()
                {
                    GameMode = GameMode.Server,
                    SessionName = "DedicatedServer",
                    CustomLobbyName = "MainLobby",
                    SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>(),
                    Address = NetAddress.CreateFromIpPort(_developmentIP, _developmentPort),
                    SessionProperties = new System.Collections.Generic.Dictionary<string, SessionProperty>()
                    {
                        { "GameType", "Dedicated" }
                    }
                };

                WriteToLog($"Server başlatılıyor... SessionName: {args.SessionName}, Lobby: {args.CustomLobbyName}, IP: {args.Address}");
                
                StartGameResult result = await _runner.StartGame(args);
                
                if (result.Ok)
                {
                    WriteToLog($"Server başarıyla başlatıldı! SessionID: {_runner.SessionInfo.Name}");
                    WriteToLog($"Lobby: {_runner.LobbyInfo?.Name}");
                    
                    // Server sistemlerini başlat
                    await InitializeServerSystems();
                }
                else
                {
                    throw new System.Exception($"Server başlatılamadı: {result.ShutdownReason} - {result.ErrorMessage}");
                }
            }
            catch (System.Exception e)
            {
                WriteToLog($"Server başlatma hatası: {e.Message}\nStack Trace: {e.StackTrace}");
                throw;
            }
        }

        private async UniTask InitializeServerSystems()
        {
            try
            {
                WriteToLog("Server sistemleri başlatılıyor...");
                // ServerGameManager'ı başlat
                _serverGameManager.Initialize(_runner);
                WriteToLog("Server sistemleri başarıyla başlatıldı.");
            }
            catch (System.Exception e)
            {
                WriteToLog($"Server sistemleri başlatılırken hata: {e.Message}\nStack Trace: {e.StackTrace}");
                throw;
            }
        }
    }
} 