using System;
using _Project.Runtime.Project.Service.Scripts.Model;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            DontDestroyOnLoad(gameObject);
            ServiceModel.Instance.Init();
            var success = await AuthenticationModel.Instance.TryAutoLogin();
            if (success)
            {

                UserModel.Instance.Init();
                LogModel.Instance.Log("Open Main Scene");
                LogModel.Instance.Log("DisplayName: " + UserModel.Instance.UserVo.DisplayName);
            }
            else
            {
                LogModel.Instance.Log("Open Login Scene");
            }
        }
        catch (Exception e)
        {
            LogModel.Instance.Error(e);
        }
    }
}
