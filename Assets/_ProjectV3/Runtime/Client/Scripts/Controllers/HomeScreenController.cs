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
        private static PvpServerModel pvpServerModel => PvpServerModel.Instance;

        private const string LOG_PREFIX = "[Matchmaking]";
        private const string ERROR_PREFIX = "[Hata]";

        public static async UniTask Run()
        {
            var view = await uiManager.OpenUI(UIScreenKeys.HomeScreen);
            view.Init();
        }

        public static async UniTask StartMatchmaking(GameModeType gameMode)
        {
            // var loadingScreen = default(UIScreen);
            try
            {
                LogMatchmakingStatus($"Matchmaking başlatılıyor - Oyun Modu: {gameMode}");

                var loadingScreen = await uiManager.OpenUI(UIScreenKeys.LoadingScreen);
                if (loadingScreen != null)
                {
                    loadingScreen.Init();
                    LogMatchmakingStatus("Yükleme ekranı açıldı");
                }

                var matchmakingData = new MatchmakingData
                {
                    GameMode = gameMode,
                    Region = "eu" // TODO: Bölge seçimini ekle
                };

                var ticket = await matchmakingModel.JoinMatchmaking(matchmakingData);
                if (ticket == null)
                {
                    throw new MatchmakingException("Matchmaking ticket alınamadı");
                }
                LogMatchmakingStatus($"Ticket alındı - ID: {ticket.Ticket}");

                var match = await matchmakingModel.WaitForMatch(ticket);
                if (match == null)
                {
                    throw new MatchmakingException("Eşleşme bulunamadı");
                }
                LogMatchmakingStatus($"Eşleşme bulundu - Match ID: {match.MatchId}");

                // Sunucuya bağlan
                await pvpServerModel.OnMatchFound(match);
            }
            catch (MatchmakingException e)
            {
                LogMatchmakingError($"Matchmaking işlemi başarısız: {e.Message}");
                await HandleMatchmakingError();
            }
            catch (Exception e)
            {
                LogMatchmakingError($"Beklenmeyen hata: {e.Message}");
                await HandleMatchmakingError();
            }
            finally
            {
                uiManager.CloseUI(UIScreenKeys.LoadingScreen);
                LogMatchmakingStatus("Yükleme ekranı kapatıldı");
            }
        }

        private static void LogMatchmakingStatus(string message)
        {
            logModel.Log($"{LOG_PREFIX} {message}");
        }

        private static void LogMatchmakingError(string message)
        {
            logModel.Error($"{ERROR_PREFIX} {message}");
        }

        private static async UniTask HandleMatchmakingError()
        {
            await matchmakingModel.CancelMatchmaking();
            LogMatchmakingStatus("Matchmaking iptal edildi");
            await Run();
        }
    }

    public class MatchmakingException : Exception
    {
        public MatchmakingException(string message) : base(message) { }
    }
}
