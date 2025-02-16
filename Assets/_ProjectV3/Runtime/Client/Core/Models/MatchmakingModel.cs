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
        private const int MAX_RETRY_ATTEMPTS = 3;
        private bool isRetrying = false;


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

        public async UniTask<IMatchmakerTicket> JoinMatchmaking(MatchmakingData data, int retryAttempt = 0)
        {
            try
            {
                if (isMatchmaking && !isRetrying)
                {
                    LogModel.Instance.Warning("Zaten matchmaking'e katılmış durumdasınız!");
                    return currentTicket;
                }

                if (Socket == null || !Socket.IsConnected)
                {
                    throw new Exception("Nakama socket bağlantısı bulunamadı!");
                }

                isMatchmaking = true;
                isRetrying = retryAttempt > 0;

                LogModel.Instance.Log($"=== Matchmaking başlatılıyor {(isRetrying ? $"(Deneme {retryAttempt}/{MAX_RETRY_ATTEMPTS})" : "")} ===");
                LogModel.Instance.Log($"Game Mode: {data.GameMode}");
                LogModel.Instance.Log($"Region: {data.Region}");

                var query = "*";
                var minCount = 2;
                var maxCount = 2;
                
                var stringProperties = new Dictionary<string, string>
                {
                    { "region", data.Region }
                };

                var numericProperties = new Dictionary<string, double>
                {
                    { "gameMode", (double)data.GameMode }
                };

                currentTicket = await Socket.AddMatchmakerAsync(
                    query,
                    minCount,
                    maxCount,
                    stringProperties,
                    numericProperties
                );

                LogModel.Instance.Log($"Matchmaking ticket alındı: {currentTicket.Ticket}");
                isRetrying = false;
                return currentTicket;
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Matchmaking hatası: {e.Message}");
                
                if (retryAttempt < MAX_RETRY_ATTEMPTS)
                {
                    LogModel.Instance.Log($"Yeniden deneniyor ({retryAttempt + 1}/{MAX_RETRY_ATTEMPTS})...");
                    await UniTask.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // Exponential backoff
                    return await JoinMatchmaking(data, retryAttempt + 1);
                }
                
                await CancelMatchmaking();
                throw;
            }
        }

        public async UniTask JoinMatch(string matchId)
        {
            LogModel.Instance.Log($"JoinMatch: {matchId}");
        }

        public async UniTask<IMatchmakerMatched> WaitForMatch(IMatchmakerTicket ticket, float timeout = 60f)
        {
            try
            {
                if (ticket == null)
                {
                    throw new Exception("Geçersiz matchmaking ticket!");
                }

                LogModel.Instance.Log($"Eşleşme bekleniyor (Timeout: {timeout} saniye)...");

                var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeout));
                var matchTask = UniTask.WaitUntil(() => currentMatch != null);

                var result = await UniTask.WhenAny(matchTask, timeoutTask);

                if (result == 1)
                {
                    throw new Exception($"Matchmaking zaman aşımına uğradı ({timeout} saniye)");
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