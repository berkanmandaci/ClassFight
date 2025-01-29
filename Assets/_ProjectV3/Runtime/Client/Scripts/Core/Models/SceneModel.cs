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
        // private const string GAME_SCENE_NAME = "ProjectV3/Runtime/Client/Scenes/Client/GameScene";
        private const string GAME_SCENE_NAME = "ProjectV3/Runtime/Client/Scenes/Client/GameScene";
        private const string MENU_SCENE_NAME = "ProjectV3/Runtime/Client/Scenes/Client/MenuScene";

        public async UniTask LoadGameScene()
        {
            try
            {
                LogModel.Instance.Log($"=== Oyun sahnesi yükleniyor: {GAME_SCENE_NAME} ===");
                
                // Sahnenin varlığını kontrol et
                if (!Application.CanStreamedLevelBeLoaded(GAME_SCENE_NAME))
                {
                    throw new Exception($"Sahne bulunamadı: {GAME_SCENE_NAME}. Build Settings'de sahnenin ekli olduğundan emin olun.");
                }
                
                // Yükleme ekranını göster
                var loadingScreen = await UIManager.Instance.OpenUI(UIScreenKeys.LoadingScreen);
                if (loadingScreen != null)
                {
                    loadingScreen.Init();
                }

                // Sahneyi asenkron olarak yükle
                var operation = SceneManager.LoadSceneAsync(GAME_SCENE_NAME);
                if (operation == null)
                {
                    throw new Exception($"Sahne yükleme işlemi başlatılamadı: {GAME_SCENE_NAME}");
                }

                operation.allowSceneActivation = false;

                // Yükleme ilerlemesini bekle
                while (operation.progress < 0.9f)
                {
                    LogModel.Instance.Log($"Sahne yükleme ilerlemesi: {operation.progress:P}");
                    await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
                }

                // Yükleme tamamlandı, sahneyi aktifleştir
                operation.allowSceneActivation = true;
                await UniTask.WaitUntil(() => operation.isDone);

                // Yükleme ekranını kapat
                UIManager.Instance.CloseUI(UIScreenKeys.LoadingScreen);

                LogModel.Instance.Log("=== Oyun sahnesi başarıyla yüklendi ===");
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Sahne yükleme hatası: {e.Message}\nStack Trace: {e.StackTrace}");
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