using UnityEngine;
using Fusion;

public class SimpleCharacterController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Camera _mainCamera;

    [Networked] private Vector3 NetworkedPosition { get; set; }
    [Networked] private Quaternion NetworkedRotation { get; set; }

    private void Awake()
    {
        // Removed GetComponent call as it's no longer needed
    }

    public override void Spawned()
    {
        _mainCamera = Camera.main;
        NetworkedPosition = transform.position;
        NetworkedRotation = transform.rotation;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            if (HasStateAuthority)
            {
                // Movement - World space'de hareket et
                Vector3 moveDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y);
                Vector3 movement = moveDirection * moveSpeed * Runner.DeltaTime;
                NetworkedPosition += movement;
            }
        }

        // Pozisyon ve rotasyonu networked değerlerle senkronize et
        if (HasStateAuthority)
        {
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
        }
        else
        {
            transform.position = NetworkedPosition;
            transform.rotation = NetworkedRotation;
        }
    }
}
