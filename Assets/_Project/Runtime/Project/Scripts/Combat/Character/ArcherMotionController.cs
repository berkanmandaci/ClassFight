using UnityEngine;
using Fusion;
using MxM;
using MxMGameplay;

public class ArcherMotionController : NetworkBehaviour
{
    [SerializeField] private MxMTrajectoryGenerator _trajectoryGenerator;
    [SerializeField] private MxMAnimator _mxmAnimator;
    private GenericControllerWrapper _charController;
    
    [Networked] private NetworkedMotionData MotionData { get; set; }

    private void Awake()
    {
        _trajectoryGenerator = GetComponentInChildren<MxMTrajectoryGenerator>();
        _mxmAnimator = GetComponent<MxMAnimator>();
        _charController = GetComponent<GenericControllerWrapper>();

        if(_trajectoryGenerator == null)
        {
            Debug.LogError("ArcherMotionController cannot find a trajectory generator component");
            enabled = false;
            return;
        }

        if(_mxmAnimator == null)
        {
            Debug.LogError("ArcherMotionController cannot find MxMAnimator component");
            enabled = false;
            return;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            // Update trajectory based on input
            Vector3 moveDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y);
            
            if (HasStateAuthority)
            {
                UpdateMovement(Runner.DeltaTime);
                
                // Update networked motion data
                MotionData = new NetworkedMotionData
                {
                    Position = transform.position,
                    Rotation = transform.rotation.eulerAngles,
                    AnimationSpeed = _mxmAnimator.PlaybackSpeed,
                    InputVector = moveDirection
                };
            }
            else
            {
                // Apply networked motion data for remote players
                transform.position = MotionData.Position;
                transform.rotation = Quaternion.Euler(MotionData.Rotation);
                _mxmAnimator.PlaybackSpeed = MotionData.AnimationSpeed;
                _trajectoryGenerator.InputVector = MotionData.InputVector;
            }
        }
    }

    private void UpdateMovement(float deltaTime)
    {
        var motion = _trajectoryGenerator.ExtractMotion(0.3f);
        
        Quaternion rotDelta = Quaternion.Inverse(transform.rotation) * 
                             Quaternion.AngleAxis(motion.angleDelta, Vector3.up);

        motion.angleDelta = rotDelta.eulerAngles.y;
        motion.moveDelta *= (deltaTime / 0.3f);
        motion.angleDelta *= (deltaTime / 0.3f);

        if (!_charController.IsGrounded)
        {
            motion.moveDelta.y += Physics.gravity.y * deltaTime;
        }

        _charController.Move(motion.moveDelta);
        transform.rotation = _trajectoryGenerator.transform.rotation;
    }
}

// Network üzerinden senkronize edilecek hareket verileri
public struct NetworkedMotionData : INetworkStruct
{
    public Vector3 Position;
    public Vector3 Rotation;  // Euler angles
    public float AnimationSpeed;
    public Vector3 InputVector;
}
