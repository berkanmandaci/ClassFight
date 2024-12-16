using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MovementInput;
    public Vector2 RotationInput;
    public NetworkBool AttackPressed;
    public NetworkBool DashPressed;
    public NetworkBool DodgePressed;
    public NetworkBool NextCharacterPressed;
    public NetworkBool PreviousCharacterPressed;
}
