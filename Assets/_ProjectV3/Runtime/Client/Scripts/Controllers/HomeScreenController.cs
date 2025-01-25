using _Project.Core.Scripts.Enums;
using Cysharp.Threading.Tasks;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core;

namespace ProjectV3.Client
{
    public class HomeScreenController
    {

        public static async UniTask Run()
        {
           var result = await UIManager.Instance.OpenUI(UIScreenKeys.HomeScreen);
           result.Init();
        }
    }
}
