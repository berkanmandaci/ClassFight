using System.Collections.Generic;
using ProjectV3.Shared.Vo;
using ProjectV3.Shared.Game;
using Unity.Cinemachine;
using UnityEngine;
using Mirror;

namespace ProjectV3.Shared.Combat
{
    public class CombatArenaModel : NetworkBehaviour
    {
        private static CombatArenaModel _instance;
        public static CombatArenaModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CombatArenaModel>();
                    if (_instance == null)
                    {
                        Debug.LogError("[CombatArena] Instance bulunamadı! Prefab sahneye eklenmiş mi?");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[CombatArena] Sahnede birden fazla CombatArenaModel var! Fazla olan yok ediliyor.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CombatArena] Singleton başlatıldı");
        }

        [SerializeField] private CinemachineCamera _camera;

        private Dictionary<int, List<CombatUserVo>> _teams = new Dictionary<int, List<CombatUserVo>>();
        private const int MAX_TEAM_SIZE_TDM = 2; // TeamDeathmatch için takım başına 2 oyuncu
        private const int MAX_TEAM_SIZE_FFA = 1; // FreeForAll için takım başına 1 oyuncu
        private GameModeType _currentGameMode = GameModeType.None;
        private int _teamCounter = 0;

        // Tüm oyuncuların combat verilerini tutan dictionary
        private readonly SyncDictionary<int, CombatUserVo> _combatUsers = new SyncDictionary<int, CombatUserVo>();

        [SyncVar(hook = nameof(OnMatchStateChanged))]
        private bool _isMatchStarted = false;
        public bool IsMatchStarted => _isMatchStarted;

        private readonly SyncDictionary<int, bool> _readyPlayers = new SyncDictionary<int, bool>();
        private bool _isCountdownStarted = false;

        [SerializeField] private float _countdownDuration = 3f;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _combatUsers.Clear();
            Debug.Log("[CombatArena] Server başlatıldı, combat verileri temizlendi");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("[CombatArena] Client başlatıldı");
        }

        public CinemachineCamera GetCamera() => _camera;

        public GameModeType GetCurrentGameMode() => _currentGameMode;

        // Combat verilerine erişim metodu
        public CombatUserVo GetCombatData(int connectionId)
        {
            if (_combatUsers.TryGetValue(connectionId, out var combatData))
            {
                Debug.Log($"[CombatArena] Combat verisi bulundu - Connection ID: {connectionId}, Oyuncu: {combatData.UserData.DisplayName}");
                return combatData;
            }

            Debug.LogWarning($"[CombatArena] Combat verisi bulunamadı - Connection ID: {connectionId}");
            return null;
        }

        // Combat verilerini kaydetme metodu (sadece server)
        [Server]
        public void RegisterCombatData(int connectionId, CombatUserVo combatData)
        {
            if (combatData == null)
            {
                Debug.LogError($"[CombatArena] Null combat verisi kaydedilemez - Connection ID: {connectionId}");
                return;
            }

            _combatUsers[connectionId] = combatData;
            Debug.Log($"[CombatArena] Combat verisi kaydedildi - Connection ID: {connectionId}, Oyuncu: {combatData.UserData.DisplayName}");
        }

        // Combat verilerini silme metodu (sadece server)
        [Server]
        public void UnregisterCombatData(int connectionId)
        {
            if (_combatUsers.ContainsKey(connectionId))
            {
                var removedData = _combatUsers[connectionId];
                _combatUsers.Remove(connectionId);
                Debug.Log($"[CombatArena] Combat verisi silindi - Connection ID: {connectionId}, Oyuncu: {removedData.UserData.DisplayName}");
            }
        }

        public void SetGameMode(GameModeType gameMode)
        {
            _currentGameMode = gameMode;
            _teamCounter = 0;
            ClearTeams();
            Debug.Log($"[CombatArena] Oyun modu ayarlandı: {gameMode}");
        }

        public void RegisterPlayer(CombatUserVo player, int connectionId)
        {
            int teamId;
            int maxTeamSize;

            switch ( _currentGameMode )
            {
                case GameModeType.TeamDeathmatch:
                    maxTeamSize = MAX_TEAM_SIZE_TDM;
                    // İki takım için oyuncuları dağıt (Team 1 ve Team 2)
                    teamId = _teamCounter < 2 ? 1 : 2;
                    break;

                case GameModeType.FreeForAll:
                    maxTeamSize = MAX_TEAM_SIZE_FFA;
                    // Her oyuncu kendi takımında
                    teamId = connectionId;
                    break;

                default:
                    Debug.LogError("[CombatArena] Geçersiz oyun modu!");
                    return;
            }

            // Takım listesini oluştur veya al
            if (!_teams.ContainsKey(teamId))
            {
                _teams[teamId] = new List<CombatUserVo>();
            }

            // Takım dolu mu kontrol et
            if (_teams[teamId].Count >= maxTeamSize)
            {
                if (_currentGameMode == GameModeType.TeamDeathmatch)
                {
                    // Diğer takıma ekle
                    teamId = teamId == 1 ? 2 : 1;
                    if (!_teams.ContainsKey(teamId))
                    {
                        _teams[teamId] = new List<CombatUserVo>();
                    }
                }
                else
                {
                    Debug.LogWarning($"[CombatArena] Team {teamId} dolu! Oyuncu {player.UserData.DisplayName} eklenemedi.");
                    return;
                }
            }

            // Oyuncuyu takıma ekle
            _teams[teamId].Add(player);
            player.Initialize(player.UserData, teamId);
            _teamCounter++;

            string modeInfo = _currentGameMode == GameModeType.TeamDeathmatch ?
                $"(Takım {teamId})" : "(FFA)";

            Debug.Log($"[CombatArena] {player.UserData.DisplayName} Team {teamId}'ye eklendi {modeInfo}. " +
                      $"Takım büyüklüğü: {_teams[teamId].Count}/{maxTeamSize}");
        }

        public void UnregisterPlayer(CombatUserVo player)
        {
            foreach (var team in _teams.Values)
            {
                if (team.Remove(player))
                {
                    _teamCounter--;
                    Debug.Log($"[CombatArena] {player.UserData.DisplayName} takımdan çıkarıldı. " +
                              $"Kalan oyuncu sayısı: {_teamCounter}");
                    return;
                }
            }
        }

        public List<CombatUserVo> GetTeamMembers(int teamId)
        {
            return _teams.TryGetValue(teamId, out var team) ? team : new List<CombatUserVo>();
        }

        public bool AreTeammates(CombatUserVo player1, CombatUserVo player2)
        {
            if (_currentGameMode == GameModeType.FreeForAll)
                return false; // FFA'da takım arkadaşı yok

            return player1.TeamId == player2.TeamId;
        }

        public void ClearTeams()
        {
            _teams.Clear();
            _teamCounter = 0;
            Debug.Log("[CombatArena] Tüm takımlar temizlendi.");
        }

        public int GetTotalPlayerCount()
        {
            return _teamCounter;
        }

        [Server]
        public void RegisterPlayerReady(int connectionId)
        {
            if (!_readyPlayers.ContainsKey(connectionId))
            {
                _readyPlayers[connectionId] = true;
                Debug.Log($"[CombatArena] Oyuncu hazır - Connection ID: {connectionId}");
                CheckAllPlayersReady();
            }
        }

        [Server]
        private void CheckAllPlayersReady()
        {
            if (_isCountdownStarted || _isMatchStarted) return;

            int totalPlayers = _combatUsers.Count;
            int readyPlayers = _readyPlayers.Count;

            Debug.Log($"[CombatArena] Hazır oyuncu kontrolü - Toplam: {totalPlayers}, Hazır: {readyPlayers}");

            if (totalPlayers > 0 && totalPlayers == readyPlayers)
            {
                _isCountdownStarted = true;
                StartMatchCountdown();
            }
        }

        [Server]
        private async void StartMatchCountdown()
        {
            Debug.Log("[CombatArena] Maç geri sayımı başlıyor...");
            RpcStartCountdown(_countdownDuration);

            float remainingTime = _countdownDuration;
            while (remainingTime > 0)
            {
                await System.Threading.Tasks.Task.Delay(1000); // 1 saniye bekle
                remainingTime--;
                RpcUpdateCountdown(remainingTime);
            }

            if (!_isMatchStarted)
            {
                _isMatchStarted = true;
                RpcStartMatch();
                Debug.Log("[CombatArena] Maç başladı!");
            }
        }

        [ClientRpc]
        private void RpcStartCountdown(float duration)
        {
            Debug.Log($"[CombatArena] Geri sayım başladı: {duration} saniye");
            OnMatchCountdownStarted?.Invoke(duration);
        }

        [ClientRpc]
        private void RpcUpdateCountdown(float remainingTime)
        {
            Debug.Log($"[CombatArena] Geri sayım: {remainingTime} saniye");
            OnCountdownUpdated?.Invoke(remainingTime);
        }

        [ClientRpc]
        private void RpcStartMatch()
        {
            Debug.Log("[CombatArena] Maç başladı!");
            OnMatchStarted?.Invoke();
        }

        private void OnMatchStateChanged(bool oldValue, bool newValue)
        {
            Debug.Log($"[CombatArena] Maç durumu değişti: {oldValue} -> {newValue}");
        }

        public event System.Action<float> OnMatchCountdownStarted;
        public event System.Action<float> OnCountdownUpdated;
        public event System.Action OnMatchStarted;
    }
}
