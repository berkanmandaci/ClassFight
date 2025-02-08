using ProjectV3.Shared.Vo;
using UnityEngine;

namespace ProjectV3.Shared.Combat
{
    public interface ICharacterController
    {
        void Init(CombatUserVo combatUserVo);
        void OnMove(Vector2 moveInput);
        void OnAim(Vector2 aimInput);
        void OnAttack();
        void OnDash();
        void OnDodge();
    }
}

