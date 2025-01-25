using System;
using _Project.Runtime.Core.Extensions.Singleton;
using Cysharp.Threading.Tasks;
using Mirror;
using ProjectV3.Shared.Network;

namespace ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core
{
    public class PvpServerModel : Singleton<PvpServerModel>
    {
        private ProjectNetworkManager networkManager => ProjectNetworkManager.singleton;


        public async UniTask ConnectToMatch(MatchResult matchResult)
        {
            try
            {
                LogModel.Instance.Log($"Connecting to game server: {matchResult.ServerHost}:{matchResult.ServerPort}");

                // NetworkManager ayarlarını güncelle
                networkManager.networkAddress = matchResult.ServerHost;
                
                // Transport ayarlarını güncelle
                var transport = Transport.active;
                if (transport != null)
                {
                    transport.GetType().GetProperty("Port")?.SetValue(transport, (ushort)matchResult.ServerPort);
                }
                else
                {
                    throw new Exception("Transport not found! Please add a transport component to NetworkManager");
                }

                // Sunucuya bağlan
                networkManager.StartClient();

                // Bağlantı durumunu bekle
                await UniTask.WaitUntil(() => NetworkClient.isConnected || !NetworkClient.active);

                if (!NetworkClient.isConnected)
                {
                    throw new Exception("Failed to connect to game server");
                }

                LogModel.Instance.Log("Successfully connected to game server");
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }
        }

        public void StopConnection()
        {
            if (NetworkClient.active)
            {
                networkManager.StopClient();
                LogModel.Instance.Log("Disconnected from game server");
            }
        }
    }
} 