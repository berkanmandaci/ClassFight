using Nakama;
using Newtonsoft.Json;
namespace ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core.Controllers
{
    public abstract class BaseNotificationController<T> where T : BaseNotificationController<T>, new()
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new T();

                return _instance;
            }
            set => _instance = value;
        }

        public abstract void Run(IApiNotification obj);

        public T Deserialize<T>(IApiNotification notification)
        {
            var data = notification.Content;
            LogModel.Instance.Log("Received> Notification Code: (" + notification.Code + ") Data: " + data);
            var deserializeObject = JsonConvert.DeserializeObject<T>(data);
            return deserializeObject;
        }
    }
}
