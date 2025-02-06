using _Project.Runtime.Core.Extensions.Singleton;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectV3.Client
{
    public class CombatArenaModel : SingletonBehaviour<CombatArenaModel>
    {
         [SerializeField] private CinemachineCamera _camera;
         
         public CinemachineCamera GetCamera() => _camera;
    }
}
