using Unity.Cinemachine;
using UnityEngine;

namespace ProjectV3.Shared
{
    public class CombatArenaController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _camera;
        public CinemachineCamera GetCamera() => _camera;
    }
}
