using ProjectV3.Shared.Extensions;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectV3.Shared.Combat
{
    public class CombatArenaModel : SingletonBehaviour<CombatArenaModel>
    {
         [SerializeField] private CinemachineCamera _camera;
         
         public CinemachineCamera GetCamera() => _camera;
    }
}
