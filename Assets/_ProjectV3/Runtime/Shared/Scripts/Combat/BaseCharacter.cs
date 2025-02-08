using Mirror;
using ProjectV3.Shared.Vo;
using UnityEngine;
namespace ProjectV3.Shared.Combat
{
    public abstract class BaseCharacter : NetworkBehaviour
    {
        [SerializeField] protected BaseCharacterController _characterController;

        protected CombatUserVo _combatData => _characterController.GetCombatData();
    }
}
