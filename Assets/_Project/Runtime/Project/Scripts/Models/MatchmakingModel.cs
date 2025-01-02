using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Runtime.Core.Extensions.Singleton;
using _Project.Runtime.Project.Service.Scripts.Model;
using _Project.Scripts.Vo;
using Cysharp.Threading.Tasks;
using Fusion;
using Nakama;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Runtime.Project.Scripts.Models
{
    public class MatchmakingModel : SingletonBehaviour<MatchmakingModel>
    {
        [SerializeField] private NetworkRunner _networkRunnerPrefab;
        [SerializeField] private string _gameSceneName = "GameScene";
        
        private NetworkRunner _runner;
        private ISocket Socket => ServiceModel.Instance.Socket;
        private IMatch _currentMatch;
        
        // Test için değerler
        private const int MinPlayers = 2; // Test için 2 oyuncu
        private const int MaxPlayers = 2; // Test için 2 oyuncu
        
        // Normal değerler (daha sonra kullanılacak)
        private const int DefaultMinPlayers = 6;
        private const int DefaultMaxPlayers = 6;
        
        private const int MinTeamPlayers = 3; // 3v3 için
        private const int MaxTeamPlayers = 3;

        // Events
        public event Action<IMatchmakerMatched> OnMatchFound;
        public event Action<Exception> OnMatchError;
        public event Action<IMatch> OnMatchJoined;
        public event Action OnMatchLeft;

        private string _currentMatchmakerTicket;

        public enum GameMode
        {
            FreeForAll,
            TeamVsTeam
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Init()
        {
            if (Socket != null)
            {
                Socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
                Socket.ReceivedMatchPresence += OnMatchPresence;
            }
            else
            {
                Debug.LogError("Socket is not initialized in ServiceModel!");
            }
        }

        private async UniTask StartFusionGame(string roomName)
        {
            try
            {
                // NetworkRunner oluştur
                if (_runner == null)
                {
                    _runner = Instantiate(_networkRunnerPrefab);
                    DontDestroyOnLoad(_runner.gameObject);
                }

                Debug.Log($"Connecting to Fusion room: {roomName}");

                // Önce sahneyi yükle
                SceneManager.LoadScene(_gameSceneName);
                await UniTask.WaitForEndOfFrame();

                // Bağlantı ayarları
                var args = new StartGameArgs()
                {
                    GameMode = Fusion.GameMode.Shared,
                    SessionName = roomName,
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
                };

                var result = await _runner.StartGame(args);
                
                if (result.Ok)
                {
                    Debug.Log("Successfully connected to Fusion room: " + roomName);
                }
                else
                {
                    Debug.LogError($"Failed to connect to Fusion room: {result.ShutdownReason}");
                    OnMatchError?.Invoke(new Exception($"Fusion connection failed: {result.ShutdownReason}"));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in StartFusionGame: {e.Message}");
                OnMatchError?.Invoke(e);
            }
        }

        public async UniTask StartMatchmaking(GameMode mode, string rank)
        {
            if (Socket == null)
            {
                Debug.LogError("Socket not initialized!");
                return;
            }

            var query = "*";
            int minCount = mode == GameMode.FreeForAll ? MinPlayers : MinTeamPlayers * 2;
            int maxCount = mode == GameMode.FreeForAll ? MaxPlayers : MaxTeamPlayers * 2;

            Debug.Log($"Starting matchmaking... Mode: {mode}, MinPlayers: {minCount}, MaxPlayers: {maxCount}");

            try
            {
                var matchmakerTicket = await Socket.AddMatchmakerAsync(
                    query,
                    minCount,
                    maxCount,
                    new Dictionary<string, string>
                    {
                        { "mode", mode.ToString() },
                        { "rank", rank }
                    });

                _currentMatchmakerTicket = matchmakerTicket.Ticket;
                Debug.Log($"Started matchmaking with ticket: {matchmakerTicket.Ticket}");
            }
            catch (Exception e)
            {
                OnMatchError?.Invoke(e);
                throw;
            }
        }

        public async UniTask CancelMatchmaking()
        {
            if (string.IsNullOrEmpty(_currentMatchmakerTicket)) return;

            try
            {
                await Socket.RemoveMatchmakerAsync(_currentMatchmakerTicket);
                _currentMatchmakerTicket = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error canceling matchmaking: {e.Message}");
                throw;
            }
        }

        private async void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            try
            {
                Debug.Log($"Matchmaker matched! Users count: {matched.Users.Count()}, Mode: {matched.Self.StringProperties["mode"]}");
                OnMatchFound?.Invoke(matched);

                // Match'e katılmayı bekle (kullanıcı kabul ederse)
                _currentMatchmakerTicket = null;
            }
            catch (Exception e)
            {
                OnMatchError?.Invoke(e);
                Debug.LogError($"Error in matchmaker matched: {e.Message}");
            }
        }

        public async UniTask JoinMatch(IMatchmakerMatched matched)
        {
            try
            {
                _currentMatch = await Socket.JoinMatchAsync(matched);
                OnMatchJoined?.Invoke(_currentMatch);

                var userIds = new List<string>();
                foreach (var matchedUser in matched.Users)
                {
                    userIds.Add(matchedUser.Presence.UserId);
                }
                var apiUsers = await ServiceModel.Instance.GetUser(userIds.ToArray());

                // PvpArenaModel'i başlat
                var teams = CreateTeams(matched);
                var users = CreateUsers(apiUsers);
                var pvpArenaVo = new PvpArenaVo(
                    teams,
                    users,
                    _currentMatch.Id,
                    matched.Self.StringProperties["mode"] == GameMode.TeamVsTeam.ToString());

                PvpArenaModel.Instance.Init(pvpArenaVo);

                // Fusion sunucusuna bağlan
                await StartFusionGame(_currentMatch.Id);
            }
            catch (Exception e)
            {
                OnMatchError?.Invoke(e);
                throw;
            }
        }

        private List<TeamVo> CreateTeams(IMatchmakerMatched matched)
        {
            var teams = new List<TeamVo>();
            if (matched.Self.StringProperties["mode"] == GameMode.TeamVsTeam.ToString())
            {
                // 2 takım oluştur
                teams.Add(new TeamVo("team1", "Team 1"));
                teams.Add(new TeamVo("team2", "Team 2"));
            }
            else
            {
                // FFA için her oyuncu kendi takımında
                foreach (var user in matched.Users)
                {
                    teams.Add(new TeamVo($"team_{user.Presence.UserId}", $"Player {user.Presence.Username}"));
                }
            }
            return teams;
        }

        private List<PvpUserVo> CreateUsers(IApiUsers matched)
        {
            var users = new List<PvpUserVo>();
            foreach (var matchedUser in matched.Users)
            {
                var user = new PvpUserVo(matchedUser);
                users.Add(user);
            }
            return users;
        }

        private void OnMatchPresence(IMatchPresenceEvent presenceEvent)
        {
            foreach (var presence in presenceEvent.Joins)
            {
                Debug.Log($"Player joined: {presence.Username}");
            }

            foreach (var presence in presenceEvent.Leaves)
            {
                Debug.Log($"Player left: {presence.Username}");
                // Eğer oyun başlamadıysa ve yeterli oyuncu kalmadıysa maçı iptal et
                if (PvpArenaModel.Instance.CurrentState == MatchState.WaitingForPlayers)
                {
                    var remainingPlayers = _currentMatch.Presences.Count();
                    if (remainingPlayers < MinPlayers)
                    {
                        Debug.Log($"Not enough players ({remainingPlayers}/{MinPlayers}), canceling match...");
                        LeaveMatch();
                    }
                }
            }
        }

        public async void LeaveMatch()
        {
            if (_currentMatch != null)
            {
                await Socket?.LeaveMatchAsync(_currentMatch);
                _currentMatch = null;
                OnMatchLeft?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (Socket != null)
            {
                Socket.ReceivedMatchmakerMatched -= OnMatchmakerMatched;
                Socket.ReceivedMatchPresence -= OnMatchPresence;
            }
        }
    }
}
