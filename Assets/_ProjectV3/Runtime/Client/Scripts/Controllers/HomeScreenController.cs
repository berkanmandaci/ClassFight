using _Project.Core.Scripts.Enums;
using Cysharp.Threading.Tasks;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core;
using ProjectV3.Shared.Game;
using UnityEngine;

namespace ProjectV3.Client
{
    public class HomeScreenController
    {
        public static async UniTask Run()
        {
            var result = await UIManager.Instance.OpenUI(UIScreenKeys.HomeScreen);
            result.Init();
        }

        public static async void JoinMatchmaking(GameModeType gameMode)
        {
            LogModel.Instance.Log($"Joining matchmaking for {gameMode}");
            
            try
            {
                // Matchmaking UI'ını göster
                var loadingScreen = await UIManager.Instance.OpenUI(UIScreenKeys.LoadingScreen);
                loadingScreen.Init();

                // Nakama matchmaking'e katıl
                var matchTicket = await MatchmakingModel.Instance.JoinMatchmaking(new MatchmakingData
                {
                    GameMode = gameMode,
                    Region = RegionCode.EU // Veya kullanıcının bölgesine göre
                });

                LogModel.Instance.Log($"Matchmaking ticket received: {matchTicket.Ticket}");

                // Matchmaking sonucunu bekle
                var matchResult = await MatchmakingModel.Instance.WaitForMatch(matchTicket);

                if (matchResult != null)
                {
                    LogModel.Instance.Log($"Match found! Match ID: {matchResult.MatchId}");
                    
                    // Mirror sunucusuna bağlan
                    await ConnectToGameServer(matchResult);
                }
                else
                {
                    LogModel.Instance.Warning("Matchmaking failed or timed out");
                    // Hata ekranını göster
                    await ShowMatchmakingError("Eşleşme başarısız oldu. Lütfen tekrar deneyin.");
                }
            }
            catch (System.Exception ex)
            {
                LogModel.Instance.Error(ex);
                await ShowMatchmakingError("Bir hata oluştu. Lütfen tekrar deneyin.");
            }
        }

        private static async UniTask ConnectToGameServer(MatchResult matchResult)
        {
            try
            {
                // PvpServerModel üzerinden Mirror sunucusuna bağlan
                await PvpServerModel.Instance.ConnectToMatch(matchResult);
                
                // Oyun sahnesini yükle
                // Not: Bu kısım Mirror NetworkManager tarafından da yapılabilir
                await SceneModel.Instance.LoadGameScene();
            }
            catch (System.Exception ex)
            {
                LogModel.Instance.Error(ex);
                await ShowMatchmakingError("Oyun sunucusuna bağlanılamadı.");
            }
        }

        private static async UniTask ShowMatchmakingError(string message)
        {
            // Loading ekranını kapat
            UIManager.Instance.CloseUI(UIScreenKeys.LoadingScreen);
            
            // Hata popup'ını göster
            var errorPopup = await UIManager.Instance.OpenUI(UIScreenKeys.ErrorPopup);
            errorPopup.Init();
            // Hata mesajını ayarla (popup'ın kendi implementasyonuna göre)
        }
    }
}
