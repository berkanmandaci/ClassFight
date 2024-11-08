using _Project.Core.Scripts;
using _Project.Core.Scripts.Enums;
namespace _Project.Scripts
{
    public class WinMessagePopupScreenView : BaseUIScreenView
    {
        public void OnClickButton()
        {
            UIManager.Instance.CloseUI(UIScreenKeys.WinMessagePopupScreen);
        }
    }
}
