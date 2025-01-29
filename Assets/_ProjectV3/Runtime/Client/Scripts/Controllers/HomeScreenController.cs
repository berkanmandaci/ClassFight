using System;
using _Project.Core.Scripts.Enums;
using Cysharp.Threading.Tasks;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core;
using ProjectV3.Shared.Game;
using UnityEngine;

namespace ProjectV3.Client
{
    public class HomeScreenController
    {
        private static MatchmakingModel matchmakingModel => MatchmakingModel.Instance;
        private static UIManager uiManager => UIManager.Instance;
        private static LogModel logModel => LogModel.Instance;

        public static async UniTask Run()
        {
            var view = await uiManager.OpenUI(UIScreenKeys.HomeScreen);
            view.Init();
        }

        public static async UniTaskVoid StartMatchmaking(GameModeType gameMode)
        {
            try
            {
                logModel.Log($"=== Matchmaking başlatılıyor - {gameMode} ===");

                // Loading ekranını göster
                var loadingScreen = await uiManager.OpenUI(UIScreenKeys.LoadingScreen);
                if (loadingScreen != null)
                {
                    loadingScreen.Init();
                }

                try
                {
                    // Matchmaking'e katıl
                    var matchmakingData = new MatchmakingData
                    {
                        GameMode = gameMode,
                        Region = "eu" // TODO: Bölge seçimini ekle
                    };

                    // Matchmaking ticket'ı al
                    var ticket = await matchmakingModel.JoinMatchmaking(matchmakingData);
                    if (ticket == null)
                    {
                        throw new Exception("Matchmaking ticket alınamadı!");
                    }

                    // Eşleşme bekle
                    var match = await matchmakingModel.WaitForMatch(ticket);
                    if (match == null)
                    {
                        throw new Exception("Eşleşme bulunamadı!");
                    }

                    logModel.Log($"Match bulundu! ID: {match.MatchId}");
                }
                catch (Exception e)
                {
                    logModel.Error($"Matchmaking hatası: {e.Message}");
                    await matchmakingModel.CancelMatchmaking();
                    await Run();
                }
            }
            catch (Exception e)
            {
                logModel.Error($"Genel hata: {e.Message}\nStack Trace: {e.StackTrace}");
                await Run();
            }
            finally
            {
                 // uiManager.CloseUI(UIScreenKeys.LoadingScreen);
            }
        }
    }
}
