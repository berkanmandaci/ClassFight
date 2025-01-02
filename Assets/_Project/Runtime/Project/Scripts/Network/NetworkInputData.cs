using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;    // WASD / Sol Analog
    public Vector2 rotationInput;    // Mouse / Sağ Analog
    public NetworkButtons buttons;
    
    public bool IsAttackPressed => buttons.IsSet(ATTACK);      // Sol tık / L1
    public bool IsDashPressed => buttons.IsSet(DASH);         // Sağ tık / R1
    public bool IsDodgePressed => buttons.IsSet(DODGE);       // Space / X
    public bool IsNextCharPressed => buttons.IsSet(NEXT_CHAR); // Q / R2
    public bool IsPrevCharPressed => buttons.IsSet(PREV_CHAR); // E / L2
    
    public const byte ATTACK = 0;     // Sol tık / L1
    public const byte DASH = 1;       // Sağ tık / R1
    public const byte DODGE = 2;      // Space / X
    public const byte NEXT_CHAR = 3;  // Q / R2
    public const byte PREV_CHAR = 4;  // E / L2
} 