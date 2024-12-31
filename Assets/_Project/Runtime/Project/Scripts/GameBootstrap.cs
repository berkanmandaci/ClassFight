using _Project.Runtime.Project.Service.Scripts.Model;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        ServiceModel.Instance.Init();
        AuthenticationModel.Instance.TryAutoLogin();
    }
}
