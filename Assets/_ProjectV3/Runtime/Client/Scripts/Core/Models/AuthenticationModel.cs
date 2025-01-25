using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using _Project.Runtime.Core.Extensions.Singleton;
using Cysharp.Threading.Tasks;
using Nakama;
using Unity.Multiplayer.Playmode;
using UnityEngine;
namespace ProjectV3.Client
{
    public class AuthenticationKeys
    {
        public const string AuthTokenKey = "AuthToken";
        public const string RefreshTokenKey = "RefreshToken";
    }

    public class AuthenticationModel : Singleton<AuthenticationModel>
    {


        public string CurrentUserAuthToken => ActiveSession?.AuthToken;
        public string CurrentUserRefreshToken => ActiveSession?.RefreshToken;

        public IApiAccount Account { get; private set; }

        private IClient _client => ServiceModel.Instance.Client;
        public ISession ActiveSession { get; private set; }

        public ISocket Socket => ServiceModel.Instance.Socket;


        public async UniTask<bool> TryAutoLogin()
        {
            Debug.Log("TryingAutoLogin");

            var stayLogin = (PlayerPrefs.GetInt("StayLogin") != 0);

            if (ActiveSession != null)
            {
                Debug.Log("Already Has ActiveSession!");

                return true;
            }
            bool success = false;
            if (stayLogin && PlayerPrefs.HasKey(AuthenticationKeys.AuthTokenKey) &&
                PlayerPrefs.HasKey(AuthenticationKeys.RefreshTokenKey))
            {
                var authToken = PlayerPrefs.GetString(AuthenticationKeys.AuthTokenKey);
                var refreshToken = PlayerPrefs.GetString(AuthenticationKeys.RefreshTokenKey);

                success = await LoginWithAuthToken(authToken, refreshToken);
                if (!success)
                    await LoginWithDeviceIdAsync(false);

            }
            else
            {
                await LoginWithDeviceIdAsync();
                success = true;
            }
            return success;
        }

        public async UniTask LoginGuest()
        {
            if (PlayerPrefs.HasKey(AuthenticationKeys.AuthTokenKey) &&
                PlayerPrefs.HasKey(AuthenticationKeys.RefreshTokenKey))
            {
                var authToken = PlayerPrefs.GetString(AuthenticationKeys.AuthTokenKey);
                var refreshToken = PlayerPrefs.GetString(AuthenticationKeys.RefreshTokenKey);
                bool success;
                success = await LoginWithAuthToken(authToken, refreshToken);
                if (!success)
                    await LoginWithDeviceIdAsync(false);
            }
            else
            {
                await LoginWithDeviceIdAsync();
                LogModel.Instance.Log("Guest Login Successful");
            }
        }


        public string GetSelfId() => ActiveSession.UserId;
        public bool IsSelf(string playerId) => GetSelfId() == playerId;

        public async UniTask<IApiAccount> GetUpdatedAccountAsync()
        {
            Account = await _client.GetAccountAsync(ActiveSession);
            return Account;
        }

        private async UniTask UpdateAccountAsync()
        {
            Account = await _client.GetAccountAsync(ActiveSession);
        }

        public async UniTask LoginWithDeviceIdAsync(bool register = true)
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            deviceId += Application.platform.ToString();

            // deviceId += CurrentPlayer.ReadOnlyTags().First();
            LogModel.Instance.Log("DeviceId: " + deviceId);
            Dictionary<string, string> vars = GetLanguage();
            try
            {
                ActiveSession = await _client.AuthenticateDeviceAsync(deviceId, null, register, vars);
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                if (e.Message == "User account not found.")
                {
                    ClearLoginPlayerPrefs();
                }

                throw;
            }

            CacheAuthToken();

            await GetAccountAsync();

            if (!Socket.IsConnected)
            {
                throw new Exception("connection fail");
            }
        }

        public async UniTask Logout()
        {
            await _client.SessionLogoutAsync(ActiveSession);
        }

        public async Task<bool> TryRegisterUserAsync(IClient client, string email, string password)
        {
            try
            {
                var session = await client.AuthenticateEmailAsync(email, password, create: true);
                ActiveSession = session;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Register Error: " + e.Message);
                return false;
            }
        }

        public async Task<bool> TryAuthenticateUserAsync(IClient client, string email, string password)
        {
            try
            {
                var session = await client.AuthenticateEmailAsync(email, password);
                ActiveSession = session;
                CacheAuthToken();
                var account = await client.GetAccountAsync(session);
                LogModel.Instance.Log("UserName:" + account.User.Username);
                LogModel.Instance.Log("Id:" + account.User.Id);
                await GetAccountAsync();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Login Error: " + e.Message);
                return false;
            }
        }

        private async UniTask<bool> LoginWithAuthToken(string authToken, string refreshToken)
        {
            if (string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentException("Invalid token", nameof(authToken));
            }

            try
            {
                try
                {
                    ActiveSession = Session.Restore(authToken, refreshToken);
                }
                catch (Exception e)
                {
                    LogModel.Instance.Error(e);
                    return false;
                }

                if (ActiveSession.IsExpired || ActiveSession.HasExpired(DateTime.UtcNow.AddDays(1)))
                {
                    try
                    {
                        // Attempt to refresh the existing session.
                        ActiveSession = await _client.SessionRefreshAsync(ActiveSession);
                        CacheAuthToken();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        return false;
                    }
                }

                await GetAccountAsync();
            }
            catch (ApiResponseException e)
            {
                Debug.Log(e.StatusCode);
                return false;
            }

            return true;
        }

        public async UniTask SetStatusOnline(bool isOnline)
        {
            string status = isOnline ? "Online" : null;
            try
            {
                await Socket.UpdateStatusAsync(status);
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                return;
            }
        }

        private void CacheAuthToken()
        {
            PlayerPrefs.SetString(AuthenticationKeys.AuthTokenKey, CurrentUserAuthToken);
            PlayerPrefs.SetString(AuthenticationKeys.RefreshTokenKey, CurrentUserRefreshToken);
        }

        public void ClearLoginPlayerPrefs()
        {
            PlayerPrefs.DeleteKey(AuthenticationKeys.AuthTokenKey);
            PlayerPrefs.DeleteKey(AuthenticationKeys.RefreshTokenKey);
            PlayerPrefs.DeleteKey("StayLogin");
        }

        private async Task GetAccountAsync()
        {
            try
            {
                Account = await _client.GetAccountAsync(ActiveSession);
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }


            try
            {
                await ServiceModel.Instance.ConnectSocketAsync(ActiveSession);
            }
            catch (Exception e)
            {
                Debug.LogError(e);

                if (e.Message == "User account not found.")
                {
                    ClearLoginPlayerPrefs();
                    // await OpenLoginScreenController.Run();
                }
                else
                {
                    await GetAccountAsync();
                }

                throw;
            }

            // await LoginSuccessController.Run();
            PlayerPrefs.SetString("LoginSuccess", "true");
            Debug.Log("AuthenticationModel>Connected.");
        }

        private static Dictionary<string, string> GetLanguage()
        {
            Dictionary<string, string> language = new Dictionary<string, string>();
            const string defaultLanguage = "en";
            string twoLetterIsoLanguageName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (twoLetterIsoLanguageName != "en" && twoLetterIsoLanguageName != "zh" &&
                twoLetterIsoLanguageName != "es" && twoLetterIsoLanguageName != "tr")
            {
                twoLetterIsoLanguageName = defaultLanguage;
            }

            language.Add("Language", twoLetterIsoLanguageName);

            return language;
        }

        // private static void SetLanguagePlayerPref(string lang)
        // {
        //     int langInt = lang switch
        //     {
        //         "tr" => (int)Language.Turkish,
        //         _ => (int)Language.English
        //     };
        //
        //     PlayerPrefs.SetInt(PlayerPrefHashes.Language, langInt);
        // }




        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
            {
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
            }
        }


    }
}
