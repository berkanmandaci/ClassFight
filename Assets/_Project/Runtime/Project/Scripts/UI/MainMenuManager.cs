using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Runtime.Project.Scripts.Combat.Networking;
using _Project.Runtime.Project.Service.Scripts.Model;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _joinMatchButton;
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private MatchmakingHandler matchmakingHandler;

    private void Start()
    {
        _joinMatchButton.onClick.AddListener(OnJoinMatchClicked);
        _loadingPanel.SetActive(false);
        
        // NetworkRunnerHandler event'lerine subscribe ol
        NetworkRunnerHandler.Instance.OnPlayerCountChanged += UpdatePlayerCount;
        NetworkRunnerHandler.Instance.OnMatchmakingStateChanged += UpdateMatchmakingState;
    }

    private async void OnJoinMatchClicked()
    {
        try
        {
            _joinMatchButton.interactable = false;
            _loadingPanel.SetActive(true);
            _statusText.text = "Matchmaking başlatılıyor...";
            _playerCountText.text = "";
            
            // Session kontrolü
            if (!ServiceModel.Instance.IsSessionValid)
            {
                UpdateMatchmakingState("Geçerli bir oturum bulunamadı!");
                return;
            }

            // PvpArenaModel kontrolü
            if (PvpArenaModel.Instance == null)
            {
                UpdateMatchmakingState("PvpArenaModel bulunamadı!");
                return;
            }

            // Matchmaking'i başlat
            await matchmakingHandler.StartMatchmaking(ServiceModel.Instance.Session);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Matchmaking başlatılamadı: {e.Message}");
            UpdateMatchmakingState($"Hata: {e.Message}");
        }
        finally
        {
            _joinMatchButton.interactable = true;
        }
    }

    private void UpdatePlayerCount(int currentPlayers, int maxPlayers)
    {
        _playerCountText.text = $"Oyuncular: {currentPlayers}/{maxPlayers}";
    }

    private void UpdateMatchmakingState(string state)
    {
        _statusText.text = state;
    }

    private void OnDestroy()
    {
        if (_joinMatchButton != null)
            _joinMatchButton.onClick.RemoveListener(OnJoinMatchClicked);

        // Event'lerden unsubscribe ol
        if (NetworkRunnerHandler.Instance != null)
        {
            NetworkRunnerHandler.Instance.OnPlayerCountChanged -= UpdatePlayerCount;
            NetworkRunnerHandler.Instance.OnMatchmakingStateChanged -= UpdateMatchmakingState;
        }
    }
}
