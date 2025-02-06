using System;
using System.Collections.Generic;
using _Project.Runtime.Core.Extensions.Signal;
using _Project.Runtime.Core.Extensions.Singleton;
using _Project.Runtime.Project.Game.Scripts.Vo;
using Cysharp.Threading.Tasks;
using Nakama;
namespace ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core.Models
{
    public class NotificationsModel : Singleton<NotificationsModel>
    {
        public List<NotificationsVo> NotificationsVo { get; set; }


        //Friends
        private const long FriendDeleteResponseCode = 1;
        private const long FriendRequestCode = -2;
        private const long FriendAcceptedResponseCode = -3;


        //Log
        private const long ServerShutdownCode = 1000;


        //Pvp

        private const long MatchJoinedCode = 1001;

        public bool NotificationPermission { get; set; }


        private Dictionary<long, Action<IApiNotification>> _codeActionsDictionary;

        public void Init()
        {
            NotificationsVo = new List<NotificationsVo>();
            _codeActionsDictionary = new Dictionary<long, Action<IApiNotification>>
            {
                { MatchJoinedCode, MatchMakingNotificationController.Instance.Run },
                { ServerShutdownCode, ShutdownLog }
            };

            Signals.Get<ApiNotificationReceivedSignal>().AddListener(OnNotificationReceived);
        }
        private void ShutdownLog(IApiNotification obj)
        {
            // LogModel.Instance.Log("Server Shutdown", Color.red.ColorToHtml());
            // OpenConfirmPopupController.MessageRun("Server Shutdown", "").Forget();
        }


        public void RemoveListener()
        {
            Signals.Get<ApiNotificationReceivedSignal>().RemoveListener(OnNotificationReceived);
        }

        public void InitNotification(List<NotificationsVo> list)
        {
            NotificationsVo = list;
        }

        public async UniTask HandleNotification(IApiNotification apiNotification)
        {
            LogModel.Instance.Log("Received>Notification Code:" + apiNotification.Code);
            await UniTask.SwitchToMainThread();
            if (_codeActionsDictionary.TryGetValue(apiNotification.Code, out var action))
            {
                action?.Invoke(apiNotification);
                await UniTask.CompletedTask;
            }
        }
        private void OnNotificationReceived(IApiNotification apiNotification)
        {
            HandleNotification(apiNotification).Forget();
        }

        public void RemoveNotification(NotificationsVo notificationsVo)
        {
            NotificationsVo.Remove(notificationsVo);
        }

        public void AddNotification(NotificationsVo notificationsVo)
        {
            NotificationsVo.Add(notificationsVo);
        }

        public async UniTask ClearNotification()
        {
            NotificationsVo = new List<NotificationsVo>();

            await FriendsModel.Instance.ClearFriendRequest();
        }
    }
}
