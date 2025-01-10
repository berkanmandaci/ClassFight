using UnityEngine;
using Fusion;

namespace ProjectV2.Shared
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 MovementInput;
        public NetworkButtons Buttons;
        public Vector3 AimDirection;

        public bool IsAttackPressed => Buttons.IsSet(InputButtons.Attack);
        public bool IsDashPressed => Buttons.IsSet(InputButtons.Dash);
        public bool IsJumpPressed => Buttons.IsSet(InputButtons.Jump);
    }

    public static class InputButtons
    {
        public const int Attack = 1;
        public const int Dash = 2;
        public const int Jump = 3;
    }
} 