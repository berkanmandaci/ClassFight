using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using _Project.Scripts.Vo;
using _Project.Runtime.Project.Service.Scripts.Model;
using Cysharp.Threading.Tasks;

namespace _Project.Runtime.Project.Scripts.Combat.Networking
{
    public class MatchmakingHandler : MonoBehaviour
    {
        public static MatchmakingHandler Instance { get; private set; }

        [SerializeField] private NetworkRunnerHandler networkRunnerHandler;
        [SerializeField] private int maxPlayers = 2;

        private IMatch currentMatch;
        private IMatchmakerTicket matchmakerTicket;
        private ISocket socket;
        private ISession session;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task StartMatchmaking(ISession userSession)
        {
            try
            {
                if (userSession == null)
                {
                    throw new ArgumentNullException(nameof(userSession), "Kullanıcı oturumu geçersiz!");
                }

                session = userSession;
                socket = ServiceModel.Instance.Socket;

                if (socket == null)
                {
                    throw new InvalidOperationException("Nakama socket bağlantısı bulunamadı!");
                }

                // Socket bağlantısını kontrol et
                if (!socket.IsConnected)
                {
                    Debug.Log("Socket bağlantısı kuruluyor...");
                    await socket.ConnectAsync(session, true);
                }

                // Önceki matchmaking işlemini temizle
                if (matchmakerTicket != null)
                {
                    await StopMatchmaking();
                }

                Debug.Log("Matchmaking başlatılıyor...");

                // Matchmaking kriterleri
                var query = "+properties.gameMode:pvp"; // Sadece PvP oyunları
                var minCount = maxPlayers;
                var maxCount = maxPlayers;

                matchmakerTicket = await socket.AddMatchmakerAsync(
                    query: query,
                    minCount: minCount,
                    maxCount: maxCount,
                    stringProperties: new Dictionary<string, string>
                    {
                        { "gameMode", "pvp" }
                    },
                    numericProperties: new Dictionary<string, double>
                    {
                        { "level", PvpArenaModel.Instance?.PvpArenaVo?.GetUser(session.UserId)?.Level ?? 1 }
                    }
                );

                Debug.Log($"Matchmaking ticket alındı: {matchmakerTicket.Ticket}");

                // Matchmaker mesajlarını dinle
                socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
            }
            catch (Exception e)
            {
                ErrorHandler.Instance.HandleMatchmakingError("Matchmaking başlatılamadı", e);
                throw; // Üst katmanda işlenebilmesi için hatayı yeniden fırlat
            }
        }

        private async void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            try
            {
                Debug.Log("Eşleşme bulundu, hazırlıklar başlıyor...");

                // Ana thread'e geç
                await UniTask.SwitchToMainThread();

                // Nakama match'e katıl
                currentMatch = await socket.JoinMatchAsync(matched);
                Debug.Log($"Match'e katılındı: {currentMatch.Id}");

                // Önce PvpArenaModel'in hazır olduğundan emin ol
                if (PvpArenaModel.Instance == null)
                {
                    throw new InvalidOperationException("PvpArenaModel bulunamadı!");
                }

                // Oyuncuları hazırla
                var userNames = currentMatch.Presences
                    .Where(p => p.UserId != session.UserId)
                    .Select(p => p.Username)
                    .ToList();

                Debug.Log($"Oyuncu bilgileri alınıyor... ({userNames.Count} oyuncu)");

                // Mevcut kullanıcıyı ekle
                userNames.Add(session.Username);

                var apiUsers = await ServiceModel.Instance.GetUser(userNames.ToArray());
                if (apiUsers?.Users == null || !apiUsers.Users.Any())
                {
                    throw new InvalidOperationException("Oyuncu bilgileri alınamadı!");
                }

                var users = apiUsers.Users
                    .Select(apiUser => new PvpUserVo(apiUser))
                    .ToList();

                Debug.Log($"Toplam {users.Count} oyuncu hazır");

                // Takımları oluştur
                var teams = users.Select((user, i) => CreateTeam(i, new List<PvpUserVo>
                    {
                        user
                    }))
                    .ToList();

                // Ana thread'de PvpArenaModel'i güncelle
                await UniTask.SwitchToMainThread();

                // PvpArenaVo ve Model'i hazırla
                var pvpArenaVo = new PvpArenaVo(teams, users, currentMatch.Id);
                PvpArenaModel.Instance.Init(pvpArenaVo);

                // Model'in hazır olduğunu doğrula
                await Task.Delay(100); // Model'in hazırlanması için kısa bir bekleme

                if (PvpArenaModel.Instance.PvpArenaVo == null)
                {
                    throw new InvalidOperationException("PvpArenaModel başlatılamadı!");
                }

                Debug.Log("PvpArenaModel hazır, Photon bağlantısı başlatılıyor...");

                // Ana thread'de Photon bağlantısını başlat
                await UniTask.SwitchToMainThread();
                await networkRunnerHandler.JoinPhotonServer();
                Debug.Log("Photon bağlantısı başarılı!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Match hazırlığında hata: {e.Message}\nStack Trace: {e.StackTrace}");
                ErrorHandler.Instance.HandleMatchmakingError("Match'e katılırken hata oluştu", e);
                // Hata durumunda matchmaking'i temizle
                await StopMatchmaking();
            }
        }
        private TeamVo CreateTeam(int teamIndex, List<PvpUserVo> users)
        {
            var team = new TeamVo { Id = $"Team{teamIndex + 1}" };
            team.Members.AddRange(users.Select(u => u.Id));
            return team;
        }

        public async Task StopMatchmaking()
        {
            if (matchmakerTicket != null)
            {
                try
                {
                    await socket.RemoveMatchmakerAsync(matchmakerTicket);
                    matchmakerTicket = null;
                }
                catch (Exception e)
                {
                    ErrorHandler.Instance.HandleMatchmakingError("Matchmaking durdurulamadı", e);
                }
            }
        }

        private void OnDestroy()
        {
            if (socket != null)
            {
                socket.ReceivedMatchmakerMatched -= OnMatchmakerMatched;
                socket.CloseAsync();
            }
        }
    }
}
