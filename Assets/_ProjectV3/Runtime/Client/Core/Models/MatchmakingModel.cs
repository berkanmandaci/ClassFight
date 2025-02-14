using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nakama;
using _Project.Runtime.Core.Extensions.Signal;
using ProjectV3.Shared.Core;
using ProjectV3.Shared.Extensions;
using ProjectV3.Shared.Game;

namespace ProjectV3.Client
{
    public class MatchFoundSignal : ASignal<IMatchmakerMatched>
    {
    }

    public class MatchmakingModel : Singleton<MatchmakingModel>
    {
        private ServiceModel serviceModel => ServiceModel.Instance;
        private ISocket Socket => serviceModel.Socket;
        private bool isMatchmaking = false;
        private IMatchmakerTicket currentTicket;
        private const int MATCHMAKING_TIMEOUT = 60; // 60 saniye
        private IMatchmakerMatched currentMatch;


        public void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            currentMatch = matched;
            LogModel.Instance.Log($"=== Eşleşme bulundu! ===");
            LogModel.Instance.Log($"Match ID: {matched.MatchId}");
            LogModel.Instance.Log($"Oyuncu sayısı: {matched.Users.Count()}");

            currentMatch = matched;
            // Match bulundu sinyali gönder
            Signals.Get<MatchFoundSignal>().Dispatch(matched);
        }

        public async UniTask<IMatchmakerTicket> JoinMatchmaking(MatchmakingData data)
        {
            try
            {
                if (isMatchmaking)
                {
                    LogModel.Instance.Warning("Zaten matchmaking'e katılmış durumdasınız!");
                    return currentTicket;
                }

                if (Socket == null || !Socket.IsConnected)
                {
                    throw new Exception("Nakama socket bağlantısı bulunamadı!");
                }

                isMatchmaking = true;
                LogModel.Instance.Log($"=== Matchmaking başlatılıyor ===");
                LogModel.Instance.Log($"Game Mode: {data.GameMode}");
                LogModel.Instance.Log($"Region: {data.Region}");

                // Matchmaking kriterlerini ayarla
                var query = "*"; // Tüm oyuncularla eşleş
                var minCount = 2;
                var maxCount = 2;
                
                // Matchmaking özelliklerini ayarla
                var stringProperties = new Dictionary<string, string>
                {
                    { "region", data.Region }
                };

                var numericProperties = new Dictionary<string, double>
                {
                    { "gameMode", (double)data.GameMode }
                };

                // Matchmaking'e katıl
                currentTicket = await Socket.AddMatchmakerAsync(
                    query,
                    minCount,
                    maxCount,
                    stringProperties,
                    numericProperties
                );

                LogModel.Instance.Log($"Matchmaking ticket alındı: {currentTicket.Ticket}");
                return currentTicket;
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Matchmaking hatası: {e.Message}");
                await CancelMatchmaking();
                throw;
            }
        }

        public async UniTask JoinMatch(string matchId)
        {
            LogModel.Instance.Log($"JoinMatch: {matchId}");
        }

        public async UniTask<IMatchmakerMatched> WaitForMatch(IMatchmakerTicket ticket)
        {
            try
            {
                if (ticket == null)
                {
                    throw new Exception("Geçersiz matchmaking ticket!");
                }

                LogModel.Instance.Log("Eşleşme bekleniyor...");

                // Match bulunana kadar bekle
                var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(MATCHMAKING_TIMEOUT));
                var matchTask = UniTask.WaitUntil(() => currentMatch != null);

                var result = await UniTask.WhenAny(matchTask, timeoutTask);

                if (result == 1)
                {
                    throw new Exception($"Matchmaking zaman aşımına uğradı ({MATCHMAKING_TIMEOUT} saniye)");
                }

                if (currentMatch == null)
                {
                    throw new Exception("Eşleşme bulunamadı!");
                }

                return currentMatch;
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Eşleşme hatası: {e.Message}");
                await CancelMatchmaking();
                throw;
            }
            finally
            {
                isMatchmaking = false;
                currentTicket = null;
            }
        }

        public async UniTask CancelMatchmaking()
        {
            try
            {
                if (currentTicket != null && Socket != null && Socket.IsConnected)
                {
                    LogModel.Instance.Log("Matchmaking iptal ediliyor...");
                    await Socket.RemoveMatchmakerAsync(currentTicket);
                    LogModel.Instance.Log("Matchmaking iptal edildi");
                }
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Matchmaking iptal hatası: {e.Message}");
            }
            finally
            {
                isMatchmaking = false;
                currentTicket = null;
                currentMatch = null;
            }
        }
    }

    public class MatchmakingData
    {
        public GameModeType GameMode { get; set; }
        public string Region { get; set; }
    }
} 