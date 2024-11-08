using _Project.Core.Scripts;
using _Project.Core.Scripts.Enums;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace _Project.Scripts
{
    public class FirstScreenView : BaseUIScreenView
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private GameObject _payButton;
        private void Awake()
        {
            _slider.gameObject.SetActive(true);
            _payButton.SetActive(false);
        }
        private void Start()
        {
            _slider.DOValue(1, 2).OnComplete(() =>
            {
                _slider.gameObject.SetActive(false);
                _payButton.gameObject.SetActive(true);
            });
        }

        public void OnClickToWin()
        {
            Debug.Log("Open steam api");
            UIManager.Instance.OpenUI(UIScreenKeys.MessagePopupScreen).Forget();
        }
    }
}
