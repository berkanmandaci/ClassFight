using UnityEngine;
using Fusion;

public class SimpleCharacterController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private NetworkCharacterController _characterController;
    private Camera _mainCamera;

    private void Awake()
    {
        _characterController = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        _mainCamera = Camera.main;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            // Movement
            Vector3 moveDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y).normalized;
            _characterController.Move(moveDirection * moveSpeed * Runner.DeltaTime);

            // Rotation - make character face movement direction
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }
        }
    }
}
