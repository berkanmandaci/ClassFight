using System;
using _Project.Core.Scripts.Enums;
using _Project.Runtime.Core.Extensions.Singleton;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core
{
    public class SceneModel : Singleton<SceneModel>
    {
        private const string GAME_SCENE_NAME = "GameScene";
        private const string MENU_SCENE_NAME = "MenuScene";

        public async UniTask LoadGameScene()
        {
            try
            {
                LogModel.Instance.Log($"Loading game scene: {GAME_SCENE_NAME}");
                
                // Yükleme ekranını göster
                var loadingScreen = await UIManager.Instance.OpenUI(UIScreenKeys.LoadingScreen);
                loadingScreen.Init();

                // Sahneyi asenkron olarak yükle
                var operation = SceneManager.LoadSceneAsync(GAME_SCENE_NAME);
                operation.allowSceneActivation = false;

                // Yükleme ilerlemesini bekle
                while (operation.progress < 0.9f)
                {
                    await UniTask.Yield();
                }

                // Yükleme tamamlandı, sahneyi aktifleştir
                operation.allowSceneActivation = true;
                await UniTask.WaitUntil(() => operation.isDone);

                // Yükleme ekranını kapat
                UIManager.Instance.CloseUI(UIScreenKeys.LoadingScreen);

                LogModel.Instance.Log("Game scene loaded successfully");
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }
        }

        public async UniTask LoadMenuScene()
        {
            try
            {
                LogModel.Instance.Log($"Loading menu scene: {MENU_SCENE_NAME}");
                
                var operation = SceneManager.LoadSceneAsync(MENU_SCENE_NAME);
                await UniTask.WaitUntil(() => operation.isDone);
                
                LogModel.Instance.Log("Menu scene loaded successfully");
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }
        }
    }
} 