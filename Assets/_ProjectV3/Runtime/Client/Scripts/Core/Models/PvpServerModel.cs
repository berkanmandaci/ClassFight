using System;
using _Project.Runtime.Core.Extensions.Signal;
using _Project.Runtime.Core.Extensions.Singleton;
using Cysharp.Threading.Tasks;
using Mirror;
using Nakama;
using ProjectV3.Shared.Network;
using UnityEngine;

namespace ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core
{
    public class PvpServerModel : Singleton<PvpServerModel>
    {
        private ProjectNetworkManager networkManager => ProjectNetworkManager.singleton;
        private ServiceModel serviceModel => ServiceModel.Instance;
        private ISocket Socket => serviceModel.Socket;
        private float connectionTimeout = 10f;
        private bool isConnecting = false;
        private bool isReconnecting = false;
        private IMatchmakerMatched currentMatch;

    

        public async void OnMatchFound(IMatchmakerMatched match)
        {
            try
            {
                currentMatch = match;
                LogModel.Instance.Log($"=== Match bulundu, sunucu bilgileri alınıyor ===");
                LogModel.Instance.Log($"Match ID: {match.MatchId}");

                // Sunucu bilgilerini hazırla
                var serverInfo = new ServerInfo
                {
                    host = "localhost", // TODO: Gerçek sunucu bilgilerini al
                    port = 7777
                };

                LogModel.Instance.Log($"Sunucu bilgileri alındı:");
                LogModel.Instance.Log($"Host: {serverInfo.host}");
                LogModel.Instance.Log($"Port: {serverInfo.port}");

                // Mirror sunucusuna bağlan
                await ConnectToGameServer(serverInfo);
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Match bağlantı hatası: {e.Message}");
                throw;
            }
        }
        
        public async UniTask OnMatchFound()
        {
            try
            {
                var serverInfo = new ServerInfo
                {
                    host = "localhost", // TODO: Gerçek sunucu bilgilerini al
                    port = 7777
                };
                LogModel.Instance.Log($"Sunucu bilgileri alındı:");
                LogModel.Instance.Log($"Host: {serverInfo.host}");
                LogModel.Instance.Log($"Port: {serverInfo.port}");

                // Mirror sunucusuna bağlan
                await ConnectToGameServer(serverInfo);
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }
  
        }

        private async UniTask ConnectToGameServer(ServerInfo serverInfo)
        {
            try
            {
                if (isConnecting)
                {
                    LogModel.Instance.Warning("Zaten bağlantı kurulmaya çalışılıyor...");
                    return;
                }

                isConnecting = true;
                LogModel.Instance.Log($"=== Oyun sunucusuna bağlanılıyor ===");

                if (networkManager == null)
                {
                    throw new Exception("NetworkManager bulunamadı!");
                }

                // Önceki bağlantıyı temizle
                await CleanupPreviousConnection();

                // Transport ayarlarını yap
                ConfigureTransport(serverInfo);

                // Event listener'ları ekle
                SubscribeToNetworkEvents();

                try
                {
                    LogModel.Instance.Log("Bağlantı başlatılıyor...");
                    networkManager.StartClient();

                    // Bağlantıyı bekle
                    var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(connectionTimeout));
                    var connectionTask = UniTask.WaitUntil(() => NetworkClient.isConnected);

                    var result = await UniTask.WhenAny(connectionTask, timeoutTask);

                    if (result == 1)
                    {
                        throw new Exception($"Bağlantı zaman aşımına uğradı ({connectionTimeout} saniye)");
                    }

                    if (!NetworkClient.isConnected)
                    {
                        throw new Exception("Sunucuya bağlanılamadı");
                    }

                    // Bağlantı başarılı, biraz bekle
                    await UniTask.Delay(TimeSpan.FromSeconds(1));

                    // Match bilgisini gönder
                    if (currentMatch != null)
                    {
                        NetworkClient.Send(new MatchInfoMessage { matchId = currentMatch.MatchId });
                    }

                    LogModel.Instance.Log("=== Sunucu bağlantısı başarılı ===");
                }
                catch (Exception e)
                {
                    await CleanupPreviousConnection();
                    throw new Exception($"Bağlantı hatası: {e.Message}");
                }
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Sunucu bağlantı hatası: {e.Message}");
                throw;
            }
            finally
            {
                isConnecting = false;
                UnsubscribeFromNetworkEvents();
            }
        }

        private async UniTask CleanupPreviousConnection()
        {
            if (NetworkClient.isConnected)
            {
                LogModel.Instance.Log("Önceki bağlantı kapatılıyor...");
                networkManager.StopClient();
                await UniTask.Delay(TimeSpan.FromSeconds(2));
            }
        }

        private void ConfigureTransport(ServerInfo serverInfo)
        {
            networkManager.networkAddress = serverInfo.host;
            var transport = Transport.active;
            if (transport == null)
            {
                throw new Exception("Transport bulunamadı!");
            }

            transport.GetType().GetProperty("Port")?.SetValue(transport, (ushort)serverInfo.port);
            LogModel.Instance.Log($"Transport ayarlandı: {transport.GetType().Name}");
        }

        private void SubscribeToNetworkEvents()
        {
            NetworkClient.OnConnectedEvent += OnClientConnected;
            NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
            NetworkClient.RegisterHandler<ErrorMessage>(OnErrorMessage);
        }

        private void UnsubscribeFromNetworkEvents()
        {
            NetworkClient.OnConnectedEvent -= OnClientConnected;
            NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
            NetworkClient.UnregisterHandler<ErrorMessage>();
        }

        private void OnClientConnected()
        {
            LogModel.Instance.Log("Client bağlantısı başarılı!");
        }

        private void OnClientDisconnected()
        {
            if (!networkManager.isShuttingDown && !isReconnecting)
            {
                LogModel.Instance.Warning("Client bağlantısı koptu!");
            }
            isConnecting = false;
        }

        private void OnErrorMessage(ErrorMessage msg)
        {
            LogModel.Instance.Error($"Network Error: {msg.Value}");
        }

        public async UniTask StopConnection()
        {
            try
            {
                isReconnecting = true;
                if (NetworkClient.active)
                {
                    LogModel.Instance.Log("Sunucu bağlantısı kapatılıyor...");
                    networkManager.StopClient();
                    await UniTask.Delay(TimeSpan.FromSeconds(1));
                    LogModel.Instance.Log("Sunucu bağlantısı kapatıldı");
                }
            }
            finally
            {
                isReconnecting = false;
                currentMatch = null;
            }
        }
    }

    public struct ErrorMessage : NetworkMessage
    {
        public string Value;
    }

    public struct MatchInfoMessage : NetworkMessage
    {
        public string matchId;
    }

    [Serializable]
    public class ServerInfo
    {
        public string host;
        public int port;
    }
} 