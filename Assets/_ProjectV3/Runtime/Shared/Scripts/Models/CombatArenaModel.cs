using System.Collections.Generic;
using ProjectV3.Shared.Vo;
using ProjectV3.Shared.Game;
using Unity.Cinemachine;
using UnityEngine;
using Mirror;
using System.Linq;
using System.Threading.Tasks;

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

        [Header("Round Ayarları")]
        [SerializeField] private int _maxRounds = 3;
        [SerializeField] private float _roundStartDelay = 3f;

        [SyncVar(hook = nameof(OnCurrentRoundChanged))]
        private int _currentRound = 0;
        public int CurrentRound => _currentRound;

        [SyncVar]
        private bool _isRoundActive = false;
        public bool IsRoundActive => _isRoundActive;

        private Dictionary<int, RoundStats> _playerRoundStats = new Dictionary<int, RoundStats>();
        private Dictionary<int, int> _playerTotalScores = new Dictionary<int, int>();

        [System.Serializable]
        public class RoundStats
        {
            public float damageDealt;
            public int kills;
            public bool isLastSurvivor;
            public int roundScore;
        }

        [System.Serializable]
        public struct RoundStatsMsg
        {
            public int connectionId;
            public float damageDealt;
            public int kills;
            public bool isLastSurvivor;
            public int roundScore;

            public RoundStatsMsg(int connectionId, RoundStats stats)
            {
                this.connectionId = connectionId;
                this.damageDealt = stats.damageDealt;
                this.kills = stats.kills;
                this.isLastSurvivor = stats.isLastSurvivor;
                this.roundScore = stats.roundScore;
            }
        }

        [System.Serializable]
        public struct ScoreMsg
        {
            public int connectionId;
            public int score;

            public ScoreMsg(int connectionId, int score)
            {
                this.connectionId = connectionId;
                this.score = score;
            }
        }

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
            Debug.Log("[CombatArena] Maç başlatılıyor...");

            if (!_isMatchStarted)
            {
                _isMatchStarted = true;
                _currentRound = 0; // İlk round için sıfırla
                RpcStartMatch();
                Debug.Log("[CombatArena] Maç başladı!");
                
                // İlk roundu başlat
                await StartNextRound();
            }
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

        [Server]
        private async Task StartNextRound()
        {
            if (_currentRound >= _maxRounds)
            {
                EndMatch();
                return;
            }

            _currentRound++;
            ResetRoundStats();
            ReviveAllPlayers();
            await StartRoundCountdown();
            Debug.Log($"[CombatArena] Round {_currentRound} başlıyor!");
        }

        [Server]
        private void ResetRoundStats()
        {
            foreach (var combatUser in _combatUsers.Values)
            {
                int connectionId = combatUser.netIdentity.connectionToClient.connectionId;
                _playerRoundStats[connectionId] = new RoundStats();
            }
        }

        [Server]
        private void ReviveAllPlayers()
        {
            Debug.Log("[CombatArena] Tüm oyuncular yeniden canlandırılıyor...");

            // Spawn pozisyonlarını karıştır
            var spawnPoints = new Vector3[]
            {
                new(-5, 1, 0),
                new(5, 1, 0),
                new(0, 1, -5),
                new(0, 1, 5),
                new(-5, 1, -5),
                new(5, 1, 5)
            };

            var shuffledSpawnPoints = spawnPoints.OrderBy(x => UnityEngine.Random.value).ToList();
            int spawnIndex = 0;

            foreach (var combatUser in _combatUsers.Values)
            {
                // Spawn pozisyonunu al
                var spawnPos = shuffledSpawnPoints[spawnIndex % shuffledSpawnPoints.Count];
                spawnIndex++;

                // Oyuncuyu canlandır ve pozisyonunu ayarla
                combatUser.Respawn(spawnPos);
                Debug.Log($"[CombatArena] {combatUser.UserData.DisplayName} {spawnPos} konumunda yeniden doğdu");
            }
        }

        [Server]
        private async Task StartRoundCountdown()
        {
            _isRoundActive = false;
            Debug.Log("[CombatArena] Round geri sayımı başlıyor...");
            RpcStartRoundCountdown(_roundStartDelay);

            float remainingTime = _roundStartDelay;
            while (remainingTime > 0)
            {
                await Task.Delay(1000); // 1 saniye bekle
                remainingTime--;
                RpcUpdateRoundCountdown(remainingTime);
            }

            StartRound();
        }

        [Server]
        private void StartRound()
        {
            _isRoundActive = true;
            RpcStartRound();
            Debug.Log($"[CombatArena] Round {_currentRound} başladı!");
        }

        [Server]
        public async void OnPlayerDeath(CombatUserVo player)
        {
            if (!_isRoundActive) return;

            Debug.Log($"[CombatArena] Oyuncu öldü: {player.UserData.DisplayName}");

            // Hayatta kalan oyuncuları say
            var alivePlayers = _combatUsers.Values.Where(u => !u.IsDead).ToList();
            Debug.Log($"[CombatArena] Hayatta kalan oyuncu sayısı: {alivePlayers.Count}");
            Debug.Log($"[CombatArena] Oyun modu: {_currentGameMode}");

            foreach (var p in _combatUsers.Values)
            {
                Debug.Log($"[CombatArena] Oyuncu durumu - {p.UserData.DisplayName}: IsDead={p.IsDead}");
            }

            // FFA modunda son hayatta kalan oyuncu kazanır
            if (_currentGameMode == GameModeType.FreeForAll)
            {
                Debug.Log("[CombatArena] FFA modu round bitiş kontrolü yapılıyor...");
                if (alivePlayers.Count == 1)
                {
                    var lastSurvivor = alivePlayers.First();
                    int connectionId = lastSurvivor.netIdentity.connectionToClient.connectionId;
                    _playerRoundStats[connectionId].isLastSurvivor = true;
                    _playerRoundStats[connectionId].roundScore += 5;
                    Debug.Log($"[CombatArena] Son hayatta kalan: {lastSurvivor.UserData.DisplayName}");

                    // Round'u bitir
                    await EndRound();
                }
                else if (alivePlayers.Count == 0)
                {
                    Debug.Log("[CombatArena] Tüm oyuncular öldü, round berabere!");
                    await EndRound();
                }
                else 
                {
                    Debug.Log("[CombatArena] Round devam ediyor - Hayatta kalan oyuncu sayısı > 1");
                }
            }
            else
            {
                Debug.Log($"[CombatArena] FFA modu değil, mevcut mod: {_currentGameMode}");
            }
        }

        [Server]
        private async Task EndRound()
        {
            if (!_isRoundActive) return;
            
            _isRoundActive = false;
            Debug.Log($"[CombatArena] Round {_currentRound} sona eriyor...");

            // En çok hasarı ve kill'i yapanları bul
            var mostDamagePlayer = _playerRoundStats.OrderByDescending(x => x.Value.damageDealt).First();
            var mostKillsPlayer = _playerRoundStats.OrderByDescending(x => x.Value.kills).First();

            // Puanları dağıt
            mostDamagePlayer.Value.roundScore += 5;
            mostKillsPlayer.Value.roundScore += 5;

            Debug.Log($"[CombatArena] En çok hasar: {_combatUsers[mostDamagePlayer.Key].UserData.DisplayName} - {mostDamagePlayer.Value.damageDealt}");
            Debug.Log($"[CombatArena] En çok kill: {_combatUsers[mostKillsPlayer.Key].UserData.DisplayName} - {mostKillsPlayer.Value.kills}");

            // Round puanlarını toplam skora ekle
            foreach (var stat in _playerRoundStats)
            {
                if (!_playerTotalScores.ContainsKey(stat.Key))
                    _playerTotalScores[stat.Key] = 0;
                
                _playerTotalScores[stat.Key] += stat.Value.roundScore;
            }

            // Dictionary'leri array'e çevir
            var roundStatsArray = _playerRoundStats
                .Select(kvp => new RoundStatsMsg(kvp.Key, kvp.Value))
                .ToArray();

            var scoresArray = _playerTotalScores
                .Select(kvp => new ScoreMsg(kvp.Key, kvp.Value))
                .ToArray();

            RpcEndRound(roundStatsArray, scoresArray);
            
            // Yeni round'u başlat
            if (_currentRound < _maxRounds)
            {
                Debug.Log("[CombatArena] 5 saniye sonra yeni round başlayacak...");
                await Task.Delay(5000); // 5 saniye bekle
                await StartNextRound();
            }
            else
            {
                EndMatch();
            }
        }

        [Server]
        private void EndMatch()
        {
            var winner = _playerTotalScores.OrderByDescending(x => x.Value).First();
            var second = _playerTotalScores.OrderByDescending(x => x.Value).Skip(1).First();
            var third = _playerTotalScores.OrderByDescending(x => x.Value).Skip(2).First();

            RpcEndMatch(winner.Key, second.Key, third.Key);
            Debug.Log("[CombatArena] Maç sona erdi!");
        }

        [ClientRpc]
        private void RpcStartRoundCountdown(float duration)
        {
            Debug.Log($"[CombatArena] Round {_currentRound} geri sayımı başladı: {duration} saniye");
            OnRoundCountdownStarted?.Invoke(_currentRound, duration);
        }

        [ClientRpc]
        private void RpcUpdateRoundCountdown(float remainingTime)
        {
            Debug.Log($"[CombatArena] Round geri sayım: {remainingTime} saniye");
            OnRoundCountdownUpdated?.Invoke(remainingTime);
        }

        [ClientRpc]
        private void RpcStartRound()
        {
            Debug.Log($"[CombatArena] Round {_currentRound} başladı!");
            OnRoundStarted?.Invoke(_currentRound);
        }

        [ClientRpc]
        private void RpcEndRound(RoundStatsMsg[] roundStats, ScoreMsg[] totalScores)
        {
            Debug.Log($"[CombatArena] Round {_currentRound} bitti!");

            var roundStatsDict = new Dictionary<int, RoundStats>();
            var scoresDict = new Dictionary<int, int>();

            foreach (var stat in roundStats)
            {
                var rs = new RoundStats
                {
                    damageDealt = stat.damageDealt,
                    kills = stat.kills,
                    isLastSurvivor = stat.isLastSurvivor,
                    roundScore = stat.roundScore
                };
                roundStatsDict[stat.connectionId] = rs;
            }

            foreach (var score in totalScores)
            {
                scoresDict[score.connectionId] = score.score;
            }

            OnRoundEnded?.Invoke(_currentRound, roundStatsDict, scoresDict);
        }

        [ClientRpc]
        private void RpcEndMatch(int winnerId, int secondId, int thirdId)
        {
            Debug.Log("[CombatArena] Maç sona erdi!");
            OnMatchEnded?.Invoke(winnerId, secondId, thirdId);
        }

        private void OnCurrentRoundChanged(int oldValue, int newValue)
        {
            Debug.Log($"[CombatArena] Round değişti: {oldValue} -> {newValue}");
        }

        public event System.Action<int, float> OnRoundCountdownStarted;
        public event System.Action<float> OnRoundCountdownUpdated;
        public event System.Action<int> OnRoundStarted;
        public event System.Action<int, Dictionary<int, RoundStats>, Dictionary<int, int>> OnRoundEnded;
        public event System.Action<int, int, int> OnMatchEnded;
    }
}
