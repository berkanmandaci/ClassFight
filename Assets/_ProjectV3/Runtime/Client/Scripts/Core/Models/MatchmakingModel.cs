using System;
using System.Threading.Tasks;
using System.Linq;
using _Project.Runtime.Core.Extensions.Singleton;
using Cysharp.Threading.Tasks;
using Nakama;
using ProjectV3.Shared.Game;
using UnityEngine;

namespace ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core
{
    public class MatchmakingModel : Singleton<MatchmakingModel>
    {
        private const float MATCHMAKING_TIMEOUT = 60f; // 60 saniye
        private IMatchmakerTicket currentTicket;
        private IMatchmakerMatched matchmakerMatched;
        private bool isMatchFound;

        public async UniTask<IMatchmakerTicket> JoinMatchmaking(MatchmakingData data)
        {
            try
            {
                // Eğer zaten matchmaking'deyse iptal et
                if (currentTicket != null)
                {
                    await CancelMatchmaking();
                }

                // Nakama socket'i al
                var socket = ServiceModel.Instance.Socket;
                if (socket == null || !socket.IsConnected)
                {
                    throw new Exception("Nakama socket is not connected");
                }

                // Event listener'ı ekle
                isMatchFound = false;
                socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;

                // Matchmaking kriterleri
                var query = $"gameMode:{(int)data.GameMode} region:{data.Region}";
                var minCount = data.GameMode == GameModeType.TeamDeathmatch ? 2 : 2; // 3v3 veya 6 FFA
                var maxCount = minCount;

                // Matchmaking'e katıl
                currentTicket = await socket.AddMatchmakerAsync(
                    query: query,
                    minCount: minCount,
                    maxCount: maxCount,
                    stringProperties: null,
                    numericProperties: null
                );

                LogModel.Instance.Log($"Joined matchmaking with ticket: {currentTicket.Ticket}");
                return currentTicket;
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }
        }

        private void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            matchmakerMatched = matched;
            isMatchFound = true;
            LogModel.Instance.Log($"Match found with {matched.Users.Count()} players");
        }

        public async UniTask<MatchResult> WaitForMatch(IMatchmakerTicket ticket)
        {
            try
            {
                var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(MATCHMAKING_TIMEOUT));
                var matchFoundTask = UniTask.WaitUntil(() => isMatchFound);

                // Hangisi önce tamamlanırsa
                var result = await UniTask.WhenAny(matchFoundTask, timeoutTask);

                if (result == 0 && matchmakerMatched != null) // Eşleşme bulundu
                {
                    return new MatchResult
                    {
                        MatchId = matchmakerMatched.Self.Presence.SessionId,
                        Players = matchmakerMatched.Users.ToArray(),
                        ServerHost = "localhost", // Local test için
                        ServerPort = 7777 // Default port
                    };
                }
                else // Timeout
                {
                    await CancelMatchmaking();
                    return null;
                }
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }
            finally
            {
                // Event listener'ı kaldır
                if (ServiceModel.Instance.Socket != null)
                {
                    ServiceModel.Instance.Socket.ReceivedMatchmakerMatched -= OnMatchmakerMatched;
                }
            }
        }

        public async UniTask CancelMatchmaking()
        {
            if (currentTicket != null)
            {
                try
                {
                    var socket = ServiceModel.Instance.Socket;
                    await socket.RemoveMatchmakerAsync(currentTicket);
                    currentTicket = null;
                    LogModel.Instance.Log("Matchmaking cancelled");
                }
                catch (Exception e)
                {
                    LogModel.Instance.Error(e);
                    throw;
                }
            }
        }
    }

    public class MatchmakingData
    {
        public GameModeType GameMode { get; set; }
        public string Region { get; set; }
    }

    public class MatchResult
    {
        public string MatchId { get; set; }
        public IMatchmakerUser[] Players { get; set; }
        public string ServerHost { get; set; }
        public int ServerPort { get; set; }
    }

    public static class RegionCode
    {
        public const string EU = "eu";
        public const string US = "us";
        public const string ASIA = "asia";
    }
} 