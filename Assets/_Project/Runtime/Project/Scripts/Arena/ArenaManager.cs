using _Project.Runtime.Core.Extensions.Singleton;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;

public class ArenaManager : SingletonBehaviour<ArenaManager>
{
    [SerializeField] private CinemachineVirtualCameraBase _virtualCamera;
    
    
    public void SetupLocalPlayerCamera(NetworkObject playerObject)
    {
        if (_virtualCamera != null && playerObject != null)
        {
            _virtualCamera.Follow = playerObject.transform;
            Debug.Log($"Camera following player: {playerObject.name}, Object ID: {playerObject.Id}");
        }
    }
}
