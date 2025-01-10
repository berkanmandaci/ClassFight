using UnityEngine;

namespace ProjectV2.Client
{
    public class CameraFollow : MonoBehaviour
    {
        private Transform target;
        private Vector3 offset = new Vector3(0, 5, -8);
        private float smoothSpeed = 5f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            transform.position = target.position + offset;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;

            transform.LookAt(target);
        }
    }
} 