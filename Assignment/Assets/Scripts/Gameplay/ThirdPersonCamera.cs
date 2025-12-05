using UnityEngine;

namespace GapeLabs.Gameplay
{
    /// <summary>
    /// Third-person camera that smoothly follows the player with collision detection
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Offset from player position

        [Header("Camera Position")]
        [SerializeField] private float distance = 6f;
        [SerializeField] private float height = 2f;
        [SerializeField] private float followSpeed = 10f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Camera Rotation")]
        [SerializeField] private float minVerticalAngle = -20f;
        [SerializeField] private float maxVerticalAngle = 60f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private bool autoRotateBehindPlayer = true; // Auto-rotate camera behind player
        [SerializeField] private float autoRotateSpeed = 3f; // Speed of auto-rotation

        [Header("Collision Detection")]
        [SerializeField] private bool enableCollision = true;
        [SerializeField] private float collisionOffset = 0.3f;
        [SerializeField] private LayerMask collisionLayers = -1;


        // Private variables
        private float currentX = 0f;
        private float currentY = 20f;
        private Vector3 desiredPosition;
        private float currentDistance;

        private void Start()
        {
            currentDistance = distance;

            // If no target assigned, try to find player
            if (target == null)
            {
                Debug.LogWarning("[ThirdPersonCamera] No target assigned. Waiting for SetTarget() call.");
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Auto-rotate camera to follow player's rotation
            if (autoRotateBehindPlayer)
            {
                AutoRotateBehindPlayer();
            }

            CalculateCameraPosition();
            UpdateCameraPosition();
        }

        /// <summary>
        /// Set the target for the camera to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (target != null)
            {
                // Initialize camera position behind player
                Vector3 targetPos = target.position + targetOffset;
                currentX = target.eulerAngles.y;

                transform.position = targetPos - target.forward * distance + Vector3.up * height;
                transform.LookAt(targetPos);

                //Debug.Log($"[ThirdPersonCamera] Target set to: {newTarget.name}");
            }
        }

        /// <summary>
        /// Auto-rotate camera to follow behind player when they turn
        /// </summary>
        private void AutoRotateBehindPlayer()
        {
            // Get player's Y rotation
            float targetRotation = target.eulerAngles.y;

            // Smoothly rotate camera to match player's rotation
            currentX = Mathf.LerpAngle(currentX, targetRotation, Time.deltaTime * autoRotateSpeed);
        }

        /// <summary>
        /// Calculate desired camera position
        /// </summary>
        private void CalculateCameraPosition()
        {
            // Calculate target position with offset
            Vector3 targetPos = target.position + targetOffset;

            // Calculate rotation based on player's forward direction
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

            // Calculate camera position behind player
            Vector3 backward = rotation * Vector3.back;
            desiredPosition = targetPos + backward * distance + Vector3.up * height;

            // Handle collision
            if (enableCollision)
            {
                HandleCameraCollision(targetPos, ref desiredPosition);
            }
        }

        /// <summary>
        /// Handle camera collision with environment
        /// </summary>
        private void HandleCameraCollision(Vector3 targetPos, ref Vector3 desiredPos)
        {
            Vector3 direction = desiredPos - targetPos;
            float targetDistance = direction.magnitude;

            // Raycast from target to desired camera position
            if (Physics.Raycast(targetPos, direction.normalized, out RaycastHit hit, targetDistance, collisionLayers))
            {
                // Move camera closer to avoid clipping
                currentDistance = Mathf.Lerp(currentDistance, hit.distance - collisionOffset, Time.deltaTime * followSpeed);
                desiredPos = targetPos + direction.normalized * currentDistance;
            }
            else
            {
                // Smoothly return to original distance
                currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * followSpeed);
            }
        }

        /// <summary>
        /// Update camera position and rotation
        /// </summary>
        private void UpdateCameraPosition()
        {
            // Smooth movement to follow player
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

            // LOCK camera to always look at player center (target with offset)
            Vector3 lookTarget = target.position + targetOffset;
            transform.LookAt(lookTarget);
        }


        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            Gizmos.color = Color.yellow;
            Vector3 targetPos = target.position + targetOffset;
            Gizmos.DrawWireSphere(targetPos, 0.3f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetPos, transform.position);
        }
    }
}