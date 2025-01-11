using UnityEngine;
using UnityEngine.UI;
using _ProjectV3.Runtime.Scripts.Network;
using Mirror;

namespace _ProjectV3.Runtime.Scripts.UI
{
    public class ConnectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button connectButton;
        [SerializeField] private GameObject connectionPanel;
        
        [Header("Network References")]
        [SerializeField] private CustomNetworkManager networkManager;

        private void Start()
        {
            // NetworkManager referansını al
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<CustomNetworkManager>();
            }

            // Button event'ini ayarla
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        private void OnConnectButtonClicked()
        {
            if (networkManager != null)
            {
                // Localhost'a bağlan
                networkManager.networkAddress = "localhost";
                networkManager.StartClient();
                
                // Bağlantı panelini gizle
                connectionPanel.SetActive(false);
            }
            else
            {
                Debug.LogError("NetworkManager bulunamadı!");
            }
        }

        private void OnEnable()
        {
            if (networkManager != null)
            {
                // Client disconnect event'ini dinle
                NetworkClient.OnDisconnectedEvent += OnClientDisconnect;
            }
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                // Event dinlemeyi durdur
                NetworkClient.OnDisconnectedEvent -= OnClientDisconnect;
            }
        }

        private void OnClientDisconnect()
        {
            // UI thread'inde çalıştır
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(true);
            }
        }
    }
} 