using UnityEngine;
using Fusion;

public class NetworkInputHandler : MonoBehaviour
{
    private NetworkRunner _runner;

    private void Awake()
    {
        _runner = GetComponent<NetworkRunner>();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // Movement Input
        data.MovementInput.x = Input.GetAxis("Horizontal");
        data.MovementInput.y = Input.GetAxis("Vertical");

        // Combat Input
        data.AttackPressed = Input.GetMouseButton(0);
        data.DashPressed = Input.GetMouseButton(1);
        data.DodgePressed = Input.GetKey(KeyCode.Space);

        // Character Switch Input
        data.NextCharacterPressed = Input.GetKey(KeyCode.Q);
        data.PreviousCharacterPressed = Input.GetKey(KeyCode.E);

        input.Set(data);
    }
}
