using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _joinMatchButton;
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _playerCountText;

    private void Start()
    {
        _joinMatchButton.onClick.AddListener(OnJoinMatchClicked);
        _loadingPanel.SetActive(false);
        
        // NetworkRunnerHandler event'lerine subscribe ol
        NetworkRunnerHandler.Instance.OnPlayerCountChanged += UpdatePlayerCount;
        NetworkRunnerHandler.Instance.OnMatchmakingStateChanged += UpdateMatchmakingState;
    }

    private void OnJoinMatchClicked()
    {
        _joinMatchButton.interactable = false;
        _loadingPanel.SetActive(true);
        _statusText.text = "Matchmaking başlatılıyor...";
        _playerCountText.text = "";
        
        NetworkRunnerHandler.Instance.StartMatchmaking();
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
