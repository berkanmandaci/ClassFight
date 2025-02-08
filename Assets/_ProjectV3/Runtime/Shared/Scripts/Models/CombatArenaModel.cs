using System.Collections.Generic;
using ProjectV3.Shared.Extensions;
using ProjectV3.Shared.Vo;
using ProjectV3.Shared.Game;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectV3.Shared.Combat
{
    public class CombatArenaModel : SingletonBehaviour<CombatArenaModel>
    {
        [SerializeField] private CinemachineCamera _camera;
        
        private Dictionary<int, List<CombatUserVo>> _teams = new Dictionary<int, List<CombatUserVo>>();
        private const int MAX_TEAM_SIZE_TDM = 2; // TeamDeathmatch için takım başına 2 oyuncu
        private const int MAX_TEAM_SIZE_FFA = 1; // FreeForAll için takım başına 1 oyuncu
        private GameModeType _currentGameMode = GameModeType.None;
        private int _teamCounter = 0;

        public CinemachineCamera GetCamera() => _camera;

        public GameModeType GetCurrentGameMode() => _currentGameMode;

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

            switch (_currentGameMode)
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
            player.Initialize(player.UserData, player.CharacterController, player.NetworkIdentity, teamId);
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
    }
}
