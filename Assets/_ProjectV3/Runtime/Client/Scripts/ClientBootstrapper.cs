using System;
using _Project.Core.Scripts.Enums;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core.Models;

namespace ProjectV3.Client
{
    public class ClientBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool useDeviceLogin = true;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private AudioListener _mainAudioListener;

        private AuthenticationModel authModel => AuthenticationModel.Instance;
        private ServiceModel serviceModel => ServiceModel.Instance;
        private bool isQuitting = false;

        private void Start()
        {
            Application.runInBackground = true;
            CheckAudioListeners();
            Init();
        }

        private void CheckAudioListeners()
        {
            var listeners = FindObjectsOfType<AudioListener>();
            if (listeners.Length > 1)
            {
                LogModel.Instance.Warning($"Sahnede {listeners.Length} adet AudioListener bulundu. Fazla olanlar kaldırılıyor...");
                foreach (var listener in listeners)
                {
                    if (listener != _mainAudioListener)
                    {
                        Destroy(listener);
                    }
                }
            }
        }

        private async void Init()
        {
            try
            {
                _uiManager.Init();
                await InitializeServices();
                NotificationsModel.Instance.Init();
                await HomeScreenController.Run();
                
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Başlatma hatası: {e.Message}");
                // Hata durumunda UI göster
                // var errorScreen = await UIManager.Instance.OpenUI(UIScreenKeys.ErrorScreen);
                // if (errorScreen != null)
                // {
                //     errorScreen.Init("Bağlantı Hatası", "Oyun başlatılırken bir hata oluştu. Lütfen tekrar deneyin.");
                // }
            }
        }

        private async UniTask InitializeServices()
        {
            try
            {
                // ServiceModel'i başlat
                serviceModel.Init();

                // Otomatik giriş dene
                bool loginSuccess = await authModel.TryAutoLogin();

                if (loginSuccess)
                {
                    LogModel.Instance.Log("Otomatik giriş başarılı!");
                    await ConnectToServices();
                }
                else if (useDeviceLogin)
                {
                    LogModel.Instance.Log("Cihaz girişi deneniyor...");
                    await authModel.LoginWithDeviceIdAsync();
                    await ConnectToServices();
                }
                else
                {
                    LogModel.Instance.Warning("Giriş gerekli!");
                    // TODO: Giriş UI'ı göster
                }
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Servis başlatma hatası: {e.Message}");
                throw;
            }
        }

        private async UniTask ConnectToServices()
        {
            if (authModel.ActiveSession == null)
            {
                throw new Exception("Aktif oturum bulunamadı!");
            }

            try
            {
                // Nakama socket bağlantısı
                await serviceModel.ConnectSocketAsync(authModel.ActiveSession);
                LogModel.Instance.Log("Nakama bağlantısı başarılı!");

                // Önceki bağlantıları temizle
                await PvpServerModel.Instance.StopConnection();
                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Servis bağlantı hatası: {e.Message}");
                throw;
            }
        }

        public async UniTask DisconnectFromServices()
        {
            if (isQuitting) return;

            try
            {
                // Nakama bağlantısını kapat
                if (serviceModel.Socket != null && serviceModel.Socket.IsConnected)
                {
                    await serviceModel.Socket.CloseAsync();
                    LogModel.Instance.Log("Nakama bağlantısı kapatıldı");
                }

                // Mirror bağlantısını kapat
                await PvpServerModel.Instance.StopConnection();
                LogModel.Instance.Log("Game server bağlantısı kapatıldı");

                // Nakama oturumunu kapat
                if (authModel.ActiveSession != null)
                {
                    await authModel.Logout();
                    LogModel.Instance.Log("Oturum kapatıldı");
                }
            }
            catch (Exception e)
            {
                LogModel.Instance.Error($"Servis kapatma hatası: {e.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            DisconnectFromServices().Forget();
        }

        private async void OnApplicationPause(bool pauseStatus)
        {
            if (!isQuitting)
            {
                if (pauseStatus)
                {
                    await DisconnectFromServices();
                }
                else
                {
                    await InitializeServices();
                }
            }
        }
    }
}
