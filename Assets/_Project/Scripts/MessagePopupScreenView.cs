using _Project.Core.Scripts;
using _Project.Core.Scripts.Enums;
using Cysharp.Threading.Tasks;
namespace _Project.Scripts
{
    public class MessagePopupScreenView : BaseUIScreenView
    {
        public void OnClickPayButton()
        {
            UIManager.Instance.OpenUI(UIScreenKeys.WinMessagePopupScreen).Forget();
        }
    }
}
