using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _Project.Runtime.Core.Extensions.Signal;
using _Project.Runtime.Core.Extensions.Singleton;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nakama;
using UnityEngine;

namespace _Project.Runtime.Project.Service.Scripts.Model
{
    public class SocketConnected : ASignal
    {
    }

    public class ReceivedMessageSignal : ASignal<IApiChannelMessage>
    {
    }


    public class ServiceModel : Singleton<ServiceModel>
    {
        private CancellationTokenSource _reconnectCancel;
        public IClient Client { get; set; }
        private ISession Session => AuthenticationModel.Instance.ActiveSession;

        public ISocket Socket { get; private set; }

        public void Init()
        {
            Client = new Client("http", "13.61.21.22", 7350, "defaultkey");
            // Signals.Get<SocketConnected>().AddListener(GetNotifications);
        }

        public async Task ConnectSocketAsync(ISession session)
        {
            Socket = Client.NewSocket();
            Socket.ReceivedChannelMessage += ReceiveMessagesAsync;
            Socket.ReceivedError += SocketOnReceivedError;
            Socket.Closed += OnSocketClose;
            await Socket.ConnectAsync(session);

            Debug.Log("Socket Connected! " + Socket.IsConnected);
            AuthenticationModel.Instance.SetStatusOnline(true).Forget();
            Signals.Get<SocketConnected>().Dispatch();
        }

        public async UniTask OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                await Socket.CloseAsync();
            }
            else
            {
                await ConnectSocketAsync(Session);
            }
        }

    
        public async UniTask<T> RpcRequest<T>(string serviceKey, object data)
        {
            var payload = JsonConvert.SerializeObject(data);
            LogRequest(serviceKey + "> RpcRequest: " + payload);

            try
            {
                var result = await Client.RpcAsync(Session, serviceKey, payload);
                LogResponse(serviceKey + "> Response: " + result.Payload);
                var value = JsonConvert.DeserializeObject<T>(result.Payload);
                return value;
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask<T> RpcRequest<T>(string serviceKey)
        {
            var payload = JsonConvert.SerializeObject("");
            LogRequest(serviceKey + "> RpcRequest: " + payload);

            try
            {
                var result = await Client.RpcAsync(Session, serviceKey, payload);
                LogResponse(serviceKey + "> Response: " + result.Payload);
                var value = JsonConvert.DeserializeObject<T>(result.Payload);
                return value;
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask SendMatchState(string matchId, long opCode, object data = null)
        {
            var payload = JsonConvert.SerializeObject(data);
            LogRequest(matchId + ": OpCode:" + opCode + "> SendState: " + payload);

            try
            {
                await Socket.SendMatchStateAsync(matchId, opCode, payload);
                // LogRequest(matchId+": OpCode:"+ opCode+ "> StateResponse: " + payload);
                // var value = JsonConvert.DeserializeObject<T>(result.Payload);
                // return value;
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        private void OnError(Exception exception)
        {
            if (exception.InnerException != null)
                LogModel.Instance.Error(exception);
            if (exception is ApiResponseException)
            {
                var apiResponseException = (ApiResponseException)exception;
                LogModel.Instance.Error(apiResponseException);
            }
        }

        private void LogRequest(string message)
        {
            LogModel.Instance.Log(message, "#FF4500");
        }

        private void LogResponse(string message)
        {
            LogModel.Instance.Log(message, "#FFA500");
        }

        public async UniTask RpcRequest(string serviceKey, object data)
        {
            string payload = JsonConvert.SerializeObject(data);
            LogRequest(serviceKey + "> RpcRequest: " + payload);

            try
            {
                var result = await Client.RpcAsync(Session, serviceKey, payload);
                LogResponse(serviceKey + "> Response: " + result.Payload);
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async void GetNotifications()
        {
            var result = await Client.ListNotificationsAsync(Session, 10);
            foreach (var n in result.Notifications)
            {
                var log = $"Subject '{n.Subject}' content '{n.Content}'" + n.SenderId + "  " + n.Id;
                LogModel.Instance.Log(log);
            }
        }

        public async UniTask<List<T>> ListMatches<T>(string query, string data = "", int min = 0, int max = 16,
            int limit = 100)
        {
            string payload = JsonConvert.SerializeObject(data);
            LogRequest(typeof(T) + "> MatchListRequest: " + payload);

            try
            {
                var result = await Client.ListMatchesAsync(Session, min, max, limit, false, payload, query);
                var resultList = new List<T>();

                var responsePayload = "";
                foreach (var match in result.Matches)
                {
                    responsePayload += match.Label;
                    var element = JsonConvert.DeserializeObject<T>(match.Label);

                    resultList.Add(element);
                }

                LogResponse(typeof(T) + "> MatchListResponse: " + responsePayload);
                return resultList;
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask<List<T>> ReadStorageObjectsAsListAsync<T>(string serviceKey, int limit = 100)
        {
            LogRequest(serviceKey + "> RpcRequest: empty ");
            try
            {
                var cursor = string.Empty;
                var list = new List<T>();
                while (cursor != null)
                {
                    var storageObjectList = await Client.ListStorageObjectsAsync(Session, serviceKey, limit, cursor);
                    cursor = storageObjectList.Cursor;

                    foreach (var storageObject in storageObjectList.Objects)
                    {
                        LogResponse(serviceKey + "> Response: " + storageObject.Value);
                        var element = JsonConvert.DeserializeObject<T>(storageObject.Value);
                        list.Add(element);
                    }
                }

                return (list);
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask<T> ReadStorageObject<T>(string serviceKey)
        {
            try
            {
                var readObjectId = new StorageObjectId
                {
                    Collection = serviceKey,
                    Key = "0",
                    UserId = Session.UserId
                };

                var result =
                    await Client.ReadStorageObjectsAsync(Session, new IApiReadStorageObjectId[] { readObjectId });

                var storageObject = result.Objects.First();
                LogResponse(serviceKey + "> Response: " + storageObject.Value);
                var element = JsonConvert.DeserializeObject<T>(storageObject.Value);
                return element;
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }


        private async void ReceiveMessagesAsync(IApiChannelMessage message)
        {
            await UniTask.SwitchToMainThread();
            Signals.Get<ReceivedMessageSignal>().Dispatch(message);
            LogRequest(message.ToString());
        }

      

        public async UniTask FollowUsersAsync(IEnumerable<string> userIDs, IEnumerable<string> usernames = null)
        {
            await Socket.FollowUsersAsync(userIDs, usernames);
        }

        public async UniTask UnfollowUsersAsync(IEnumerable<string> userIDs)
        {
            await Socket.UnfollowUsersAsync(userIDs);
        }


    
        public void OnDestroy()
        {

            if (Socket != null)
            {
                _reconnectCancel?.Cancel();
                Socket.ReceivedChannelMessage -= ReceiveMessagesAsync;
                Socket.ReceivedError -= SocketOnReceivedError;
                Socket.Closed -= OnSocketClose;
                Socket.CloseAsync();
                Socket = null;
                LogRequest("success");
            }
        }

        public async UniTask AddFriend(string clubId)
        {
            try
            {
                await Client.AddFriendsAsync(Session, new[]
                {
                    clubId
                });
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask BlockFriend(string clubId)
        {
            try
            {
                await Client.BlockFriendsAsync(Session, new[]
                {
                    clubId
                });
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask DeleteFriend(string clubId)
        {
            try
            {
                await Client.DeleteFriendsAsync(Session, new[]
                {
                    clubId
                });
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask DeleteFriend(string[] clubIds)
        {
            try
            {
                await Client.DeleteFriendsAsync(Session, clubIds);
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask<IApiUsers> GetUser(string userName)
        {
            try
            {
                var result = await Client.GetUsersAsync(Session, null, new[]
                {
                    userName
                });
                return result;
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        public async UniTask<IApiUsers> GetUserWithId(string id)
        {
            try
            {
                var result = await Client.GetUsersAsync(Session, new[] { id }, null);
                return result;
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        private async UniTask ReconnectSocketAsync(int attemptsNumber = 6)
        {
            if (attemptsNumber <= 0)
            {
                LogModel.Instance.Error(new Exception("exceeded number of retry attempts"));
                return;
            }

            try
            {
                await Socket.ConnectAsync(Session);
            }
            catch (Exception e)
            {
                _reconnectCancel = new CancellationTokenSource();
                LogModel.Instance.Error(e);
                await Task.Delay(TimeSpan.FromSeconds(5), _reconnectCancel.Token);
                await ReconnectSocketAsync(attemptsNumber - 1);
            }
        }

        private async void SocketOnReceivedError(Exception exception)
        {
            await UniTask.SwitchToMainThread();
        }

        private async void OnSocketClose()
        {
            await UniTask.SwitchToMainThread();
            Signals.Get<SocketCloseSignal>().Dispatch();
        }

        public async UniTask<IApiNotificationList> GetOfflineNotifications()
        {
            return await Client.ListNotificationsAsync(Session, 100);
        }

        public async void DeleteOfflineNotifications(IApiNotification notification)
        {
            await Client.DeleteNotificationsAsync(Session, new[] { notification.Id });
        }
    }

    public class SocketCloseSignal : ASignal
    {
    }
}
