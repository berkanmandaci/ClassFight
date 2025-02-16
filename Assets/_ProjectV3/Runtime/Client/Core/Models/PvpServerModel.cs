using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirror;
using Nakama;
using ProjectV3.Shared.Core;
using ProjectV3.Shared.Extensions;
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
        private readonly object connectionLock = new object();
        private string lastProcessedMatchId;
        private const string SERVER_INFO_RPC = "get_match_server";

        public async UniTask OnMatchFound(IMatchmakerMatched match = null)
        {
            if (match != null && !string.IsNullOrEmpty(lastProcessedMatchId) && lastProcessedMatchId == match.MatchId)
            {
                LogModel.Instance.Warning($"Bu match ({match.MatchId}) zaten işlendi, tekrar işlenmiyor.");
                return;
            }

            lock (connectionLock)
            {
                if (isConnecting)
                {
                    LogModel.Instance.Warning("Zaten bağlantı kurulmaya çalışılıyor...");
                    return;
                }
                isConnecting = true;
            }

            try
            {
                if (match != null)
                {
                    currentMatch = match;
                    lastProcessedMatchId = match.MatchId;
                    LogModel.Instance.Log($"=== Match bulundu, sunucu bilgileri alınıyor ===");
                    LogModel.Instance.Log($"Match ID: {match.MatchId}");
                }

                // Nakama'dan server bilgisini al
                var serverInfo = await GetServerInfo(currentMatch);

                LogModel.Instance.Log($"Sunucu bilgileri alındı:");
                LogModel.Instance.Log($"Host: {serverInfo.host}");
                LogModel.Instance.Log($"Port: {serverInfo.port}");

                await ConnectToGameServer(serverInfo);
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Match bağlantı hatası: {e.Message}");
                throw;
            }
            finally
            {
                lock (connectionLock)
                {
                    isConnecting = false;
                }
            }
        }

        private async UniTask ConnectToGameServer(ServerInfo serverInfo)
        {
            try
            {
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

                LogModel.Instance.Log("Mirror bağlantısı başlatılıyor...");
                networkManager.StartClient();

                // Bağlantıyı bekle
                var connected = await WaitForConnection(connectionTimeout);
                
                if (!connected)
                {
                    throw new Exception("Sunucuya bağlanılamadı");
                }

                // Bağlantı başarılı, biraz bekle
                await UniTask.Delay(TimeSpan.FromSeconds(1));

                // Match bilgisini gönder
                if (currentMatch != null)
                {
                    NetworkClient.Send(new MatchInfoMessage { matchId = currentMatch.MatchId });
                    LogModel.Instance.Log($"Match bilgisi gönderildi: {currentMatch.MatchId}");
                }

                LogModel.Instance.Log("=== Mirror sunucu bağlantısı başarılı ===");
            }
            catch (Exception e)
            {
                UnsubscribeFromNetworkEvents();
                await CleanupPreviousConnection();
                throw new Exception($"Mirror bağlantı hatası: {e.Message}");
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
            // Önce mevcut event'leri temizle
            UnsubscribeFromNetworkEvents();
            
            // Sonra yeni event'leri ekle
            NetworkClient.OnConnectedEvent += OnClientConnected;
            NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
            NetworkClient.RegisterHandler<ErrorMessage>(OnErrorMessage);
            LogModel.Instance.Log("Network event'leri kaydedildi");
        }

        private void UnsubscribeFromNetworkEvents()
        {
            NetworkClient.OnConnectedEvent -= OnClientConnected;
            NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
            try 
            {
                NetworkClient.UnregisterHandler<ErrorMessage>();
            }
            catch (Exception) 
            {
                // Handler zaten unregister edilmiş olabilir
            }
            LogModel.Instance.Log("Network event'leri temizlendi");
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

        public async UniTask<ServerInfo> GetServerInfo(IMatchmakerMatched match)
        {
            try 
            {
                LogModel.Instance.Log($"=== Server bilgisi alınıyor ===");
                LogModel.Instance.Log($"Match ID: {match.MatchId}");

                var payload = new Dictionary<string, string> 
                { 
                    ["match_id"] = match.MatchId 
                };

                var rpcResponse = await Socket.RpcAsync(SERVER_INFO_RPC, 
                    JsonUtility.ToJson(payload));
                    
                var serverInfo = JsonUtility.FromJson<ServerInfo>(rpcResponse.Payload);
                LogModel.Instance.Log($"Server bilgisi alındı: {serverInfo.host}:{serverInfo.port}");
                
                return serverInfo;
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Server bilgisi alınamadı: {e.Message}");
                throw;
            }
        }

        private async UniTask<bool> WaitForConnection(float timeout = 10f)
        {
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeout));
            var connectionTask = UniTask.WaitUntil(() => NetworkClient.isConnected);

            var result = await UniTask.WhenAny(connectionTask, timeoutTask);

            if (result == 1)
            {
                throw new Exception($"Bağlantı zaman aşımına uğradı ({timeout} saniye)");
            }

            return NetworkClient.isConnected;
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