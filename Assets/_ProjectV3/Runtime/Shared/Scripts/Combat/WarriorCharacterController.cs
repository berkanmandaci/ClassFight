using ProjectV3.Shared.Vo;
using UnityEngine;
namespace ProjectV3.Shared.Combat
{
    public class WarriorCharacterController : MonoBehaviour, ICharacterController
    {
        private CombatUserVo _combatData;

        public void Init(CombatUserVo combatUserVo)
        {
            _combatData = combatUserVo;
        }
        public void OnMove(Vector2 moveInput)
        {
        }
        public void OnAim(Vector2 aimInput)
        {
        }
        public void OnAttack()
        {
        }
        public void OnDash()
        {
        }
        public void OnDodge()
        {
        }
    }
}
