using UnityEngine;

namespace ProjectV3.Test
{
    public interface ICharacterController
    {
        void OnMove(Vector2 moveInput);
        void OnAim(Vector2 aimInput);
        void OnAttack();
        void OnDash();
        void OnDodge();
    }
}

