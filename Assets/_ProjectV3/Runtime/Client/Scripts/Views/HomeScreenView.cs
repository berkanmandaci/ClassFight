using _Project.Core.Scripts;
using UnityEngine;
using UnityEngine.UI;
using ProjectV3.Shared.Game;

namespace ProjectV3.Client
{
    public class HomeScreenView : BaseUIScreenView
    {
        
        [SerializeField] private Button _3vs3Button;
        [SerializeField] private Button _freeForAllButton;


        private void Awake()
        {
            _3vs3Button.onClick.AddListener(OnClick3vs3);
            _freeForAllButton.onClick.AddListener(OnClickFreeForAll);
        }

        public override void Init()
        {
            LogModel.Instance.Log("HomeScreenView Init");
        }
        
        private void OnClick3vs3()
        {
            LogModel.Instance.Log("3vs3 Button Clicked");
            _ = HomeScreenController.StartMatchmaking(GameModeType.TeamDeathmatch);
        }
        
        private void OnClickFreeForAll()
        {
            LogModel.Instance.Log("Free For All Button Clicked");
            _ = HomeScreenController.StartMatchmaking(GameModeType.FreeForAll);
        }

        private void OnDestroy()
        {
            _3vs3Button.onClick.RemoveListener(OnClick3vs3);
            _freeForAllButton.onClick.RemoveListener(OnClickFreeForAll);
        }
    }
}
