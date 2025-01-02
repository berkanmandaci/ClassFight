using System;
using System.Linq;
using _Project.Runtime.Project.Scripts.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nakama;
using Cysharp.Threading.Tasks;

public class MatchmakingUIController : MonoBehaviour
{
    [Header("Mode Selection")]
    [SerializeField] private Button freeForAllButton;
    [SerializeField] private Button teamVsTeamButton;
    
    [Header("Matchmaking Status")]
    [SerializeField] private GameObject matchmakingPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button cancelButton;

    private bool _isMatchmaking;
    private MatchmakingModel.GameMode _selectedMode;

    private void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }

    private void InitializeUI()
    {
        freeForAllButton.onClick.AddListener(() => StartMatchmaking(MatchmakingModel.GameMode.FreeForAll));
        teamVsTeamButton.onClick.AddListener(() => StartMatchmaking(MatchmakingModel.GameMode.TeamVsTeam));
        cancelButton.onClick.AddListener(CancelMatchmaking);
        
        matchmakingPanel.SetActive(false);
    }

    private void SubscribeToEvents()
    {
        var matchmaking = MatchmakingModel.Instance;
        matchmaking.OnMatchFound += OnMatchFound;
        matchmaking.OnMatchError += OnMatchError;
        matchmaking.OnMatchJoined += OnMatchJoined;
        matchmaking.OnMatchLeft += OnMatchLeft;
    }

    private async void StartMatchmaking(MatchmakingModel.GameMode mode)
    {
        _selectedMode = mode;
        _isMatchmaking = true;
        
        await UniTask.SwitchToMainThread();
        matchmakingPanel.SetActive(true);
        UpdateStatusText($"{mode} modu için eşleştirme aranıyor...");

        // TODO: Rank bilgisini UserModel'dan al
        string rank = "Bronze"; // Geçici olarak
        
        try
        {
            await MatchmakingModel.Instance.StartMatchmaking(mode, rank);
        }
        catch (Exception e)
        {
            Debug.LogError($"Matchmaking error: {e.Message}");
            await UniTask.SwitchToMainThread();
            UpdateStatusText("Eşleştirme başlatılırken hata oluştu!");
            _isMatchmaking = false;
        }
    }

    private async void CancelMatchmaking()
    {
        try
        {
            await MatchmakingModel.Instance.CancelMatchmaking();
            await UniTask.SwitchToMainThread();
            _isMatchmaking = false;
            matchmakingPanel.SetActive(false);
            UpdateStatusText("Eşleştirme iptal edildi.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error canceling matchmaking: {e.Message}");
        }
    }

    private async void OnMatchFound(IMatchmakerMatched matched)
    {
        Debug.Log($"Match found! Users count: {matched.Users.Count()}");
        await UniTask.SwitchToMainThread();
        UpdateStatusText("Eşleşme bulundu, maça katılınıyor...");
        
        try
        {
            await MatchmakingModel.Instance.JoinMatch(matched);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error joining match: {e.Message}");
            await UniTask.SwitchToMainThread();
            UpdateStatusText("Maça katılırken hata oluştu!");
            _isMatchmaking = false;
        }
    }

    private async void OnMatchError(Exception error)
    {
        await UniTask.SwitchToMainThread();
        UpdateStatusText($"Hata: {error.Message}");
        _isMatchmaking = false;
        matchmakingPanel.SetActive(false);
    }

    private async void OnMatchJoined(IMatch match)
    {
        await UniTask.SwitchToMainThread();
        matchmakingPanel.SetActive(false);
        UpdateStatusText("Maça bağlanıldı, oyuncular bekleniyor...");
        // Scene geçişini başlat
        // TODO: Scene geçiş mantığını implement et
    }

    private async void OnMatchLeft()
    {
        await UniTask.SwitchToMainThread();
        _isMatchmaking = false;
        matchmakingPanel.SetActive(false);
        UpdateStatusText("Maçtan çıkıldı.");
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void OnDestroy()
    {
        if (_isMatchmaking)
        {
            CancelMatchmaking();
        }

        var matchmaking = MatchmakingModel.Instance;
        if (matchmaking != null)
        {
            matchmaking.OnMatchFound -= OnMatchFound;
            matchmaking.OnMatchError -= OnMatchError;
            matchmaking.OnMatchJoined -= OnMatchJoined;
            matchmaking.OnMatchLeft -= OnMatchLeft;
        }
    }
} 