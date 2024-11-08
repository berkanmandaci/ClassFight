using _Project.Core.Scripts;
using _Project.Core.Scripts.Enums;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace _Project.Scripts
{
    public class GameManager : MonoBehaviour
    {
        private void Start()
        {
            UIManager.Instance.Init();
            UIManager.Instance.OpenUI(UIScreenKeys.FirstScreen).Forget();
        }
    }
}
