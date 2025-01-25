using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core;

namespace ProjectV3.Client
{
    public class ClientBootstrapper : MonoBehaviour
    {

        [SerializeField] private bool useDeviceLogin = true;
        [SerializeField] private UIManager _uiManager;


        private AuthenticationModel authModel => AuthenticationModel.Instance;
        private ServiceModel serviceModel => ServiceModel.Instance;

        private void Start()
        {
            Init();
        }

        private async void Init()
        {
            try
            {
                _uiManager.Init();
                InitializeServices().Forget();
                await HomeScreenController.Run();
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw; // TODO handle exception
            }
        }

        private async UniTaskVoid InitializeServices()
        {
            // Initialize ServiceModel
            serviceModel.Init();

            // Try auto login
            bool loginSuccess = await authModel.TryAutoLogin();

            if (loginSuccess)
            {
                Debug.Log("[Client] Auto login successful!");
                await ConnectToServices();
            }
            else if (useDeviceLogin)
            {
                Debug.Log("[Client] Auto login failed, trying device login...");
                await authModel.LoginWithDeviceIdAsync();
                await ConnectToServices();
            }
            else
            {
                Debug.Log("[Client] Authentication required!");
                // TODO: Show login UI
            }
        }

        private async UniTask ConnectToServices()
        {
            if (authModel.ActiveSession == null)
            {
                Debug.LogError("[Client] No active session found!");
                return;
            }

            try
            {
                // Connect to Nakama socket
                await serviceModel.ConnectSocketAsync(authModel.ActiveSession);

                // Connect to Mirror network if auto connect is enabled

            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Client] Failed to connect to services: {e.Message}");
            }
        }



        public async UniTask DisconnectFromServices()
        {
            // Disconnect from Nakama
            if (serviceModel.Socket != null && serviceModel.Socket.IsConnected)
            {
                await serviceModel.Socket.CloseAsync();
            }

            // Disconnect from Mirror
            PvpServerModel.Instance.StopConnection();

            // Logout from Nakama
            if (authModel.ActiveSession != null)
            {
                await authModel.Logout();
            }
        }

        private void OnApplicationQuit()
        {
            DisconnectFromServices().Forget();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // serviceModel.OnApplicationPause(pauseStatus).Forget();
        }
    }
}
