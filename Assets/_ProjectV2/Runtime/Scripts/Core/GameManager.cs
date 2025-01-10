using UnityEngine;
using ProjectV2.Shared;
using Cysharp.Threading.Tasks;
using System;

namespace ProjectV2.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [SerializeField] private NetworkManager networkManagerPrefab;
        
        private NetworkManager networkManager;
        private GameState currentState = GameState.None;

        public GameState CurrentState
        {
            get => currentState;
            private set
            {
                if (currentState != value)
                {
                    var oldState = currentState;
                    currentState = value;
                    OnGameStateChanged?.Invoke(oldState, currentState);
                }
            }
        }

        public event Action<GameState, GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame().Forget();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async UniTaskVoid InitializeGame()
        {
            try
            {
                Debug.Log("Initializing game...");

                // NetworkManager'ı oluştur
                if (networkManager == null)
                {
                    networkManager = Instantiate(networkManagerPrefab);
                }

#if SERVER_BUILD
                Debug.Log("Starting in SERVER mode");
                await InitializeServer();
#else
                Debug.Log("Starting in CLIENT mode");
                await InitializeClient();
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing game: {e.Message}");
                Debug.LogException(e);
            }
        }

#if SERVER_BUILD
        private async UniTask InitializeServer()
        {
            try
            {
                CurrentState = GameState.WaitingForPlayers;
                await networkManager.InitializeNetwork(true);
                
                // Server-specific initialization
                Debug.Log("Server initialized and ready for connections");
            }
            catch (Exception e)
            {
                Debug.LogError($"Server initialization failed: {e.Message}");
                throw;
            }
        }
#else
        private async UniTask InitializeClient()
        {
            try
            {
                await networkManager.InitializeNetwork(false);
                
                // Client-specific initialization
                Debug.Log("Client initialized and connecting to server");
            }
            catch (Exception e)
            {
                Debug.LogError($"Client initialization failed: {e.Message}");
                throw;
            }
        }
#endif

        public void UpdateGameState(GameState newState)
        {
#if SERVER_BUILD
            // Sadece server game state'i değiştirebilir
            CurrentState = newState;
            Debug.Log($"Game state updated to: {newState}");
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
} 