using UnityEngine;
using Fusion;
using System;
using System.Threading.Tasks;
using _Project.Server.Scripts.Player;
using UnityEngine.SceneManagement;

namespace _Project.Server.Scripts.Core
{
    public class ServerNetworkManager : MonoBehaviour
    {
        private NetworkRunner _runner;
        private ServerGameManager _gameManager;
        private ServerPlayerManager _playerManager;

        private async void Start()
        {
            Debug.Log("Server başlatılıyor...");
            await InitializeServer();
        }

        private async Task InitializeServer()
        {
            try
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
                _runner.ProvideInput = true;

                var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
                
                // Scene referansını oluştur
                var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
                var sceneInfo = new NetworkSceneInfo();
                if (scene.IsValid)
                {
                    sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
                }

                var result = await _runner.StartGame(new StartGameArgs()
                {
                    GameMode = GameMode.Server,
                    SessionName = "DedicatedServer",
                    SceneManager = sceneManager,
                    Scene = sceneInfo
                });

                if (result.Ok)
                {
                    Debug.Log("Server başarıyla başlatıldı!");
                    InitializeServerSystems();
                }
                else
                {
                    Debug.LogError($"Server başlatılamadı: {result.ShutdownReason}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server hatası: {e.Message}");
            }
        }

        private void InitializeServerSystems()
        {
            _gameManager = gameObject.AddComponent<ServerGameManager>();
            _playerManager = gameObject.AddComponent<ServerPlayerManager>();

            _gameManager.Initialize(_runner);
            _playerManager.Initialize(_runner);
        }
    }
} 