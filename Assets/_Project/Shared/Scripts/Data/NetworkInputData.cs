using Fusion;
using UnityEngine;

namespace _Project.Shared.Scripts.Data
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 movementInput;
        public NetworkButtons buttons;
        public Vector3 aimDirection;

        public const int ATTACK = 1;
        public const int DASH = 2;
        public const int DODGE = 3;
        public const int NEXT_CHAR = 4;
        public const int PREV_CHAR = 5;
    }
} 