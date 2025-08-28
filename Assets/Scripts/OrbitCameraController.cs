using UnityEngine;

namespace UnityTemplateProjects
{
    public class OrbitCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("The target object to orbit around. If not set, will orbit around world origin.")]
        public Transform target;
        
        [Header("Orbit Settings")]
        [Tooltip("Initial distance from target")]
        public float initialDistance = 5f;
        
        [Tooltip("Minimum distance to target")]
        public float minDistance = 2f;
        
        [Tooltip("Maximum distance to target")]
        public float maxDistance = 20f;
        
        [Header("Mouse Controls")]
        [Tooltip("Mouse sensitivity for horizontal rotation")]
        public float horizontalSensitivity = 2f;
        
        [Tooltip("Mouse sensitivity for vertical rotation")]
        public float verticalSensitivity = 2f;
        
        [Tooltip("Whether to invert vertical mouse movement")]
        public bool invertVertical = false;
        
        [Header("Zoom Settings")]
        [Tooltip("Scroll wheel zoom sensitivity")]
        public float zoomSensitivity = 1f;
        
        [Tooltip("Zoom smoothing speed")]
        public float zoomSmoothTime = 0.3f;
        
        [Header("Rotation Limits")]
        [Tooltip("Minimum vertical angle in degrees")]
        public float minVerticalAngle = -80f;
        
        [Tooltip("Maximum vertical angle in degrees")]
        public float maxVerticalAngle = 80f;
        
        [Header("Smoothing")]
        [Tooltip("Rotation smoothing speed")]
        public float rotationSmoothTime = 0.1f;
        
        // Private variables
        private float currentDistance;
        private float targetDistance;
        private float horizontalAngle;
        private float verticalAngle;
        private Vector3 targetPosition;
        
        // Smoothing variables
        private float zoomVelocity;
        private float horizontalVelocity;
        private float verticalVelocity;
        
        private bool isDragging = false;
        
        void Start()
        {
            // Initialize values
            currentDistance = initialDistance;
            targetDistance = initialDistance;
            
            // Set target position (world origin if no target specified)
            if (target != null)
                targetPosition = target.position;
            else
                targetPosition = Vector3.zero;
            
            // Calculate initial angles from current camera position
            Vector3 direction = (targetPosition-transform.position).normalized;
            horizontalAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            verticalAngle = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
        }
        
        void Update()
        {
            HandleMouseInput();
            HandleZoomInput();
            UpdateCameraPosition();
        }
        
        void HandleMouseInput()
        {
            // Check for mouse drag (left or right button)
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                isDragging = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            {
                isDragging = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            // Apply rotation when dragging
            if (isDragging)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                
                if (invertVertical)
                    mouseY = -mouseY;
                
                horizontalAngle += mouseX * horizontalSensitivity;
                verticalAngle -= mouseY * verticalSensitivity;
                
                // Clamp vertical angle
                verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
            }
        }
        
        void HandleZoomInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (scroll != 0f)
            {
                targetDistance -= scroll * zoomSensitivity;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }
        
        void UpdateCameraPosition()
        {
            // Update target position if we have a target object
            if (target != null)
                targetPosition = target.position;
            
            // Smooth the distance
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);
            
            // Calculate rotation
            Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
            
            // Calculate position
            Vector3 direction = rotation * Vector3.back;
            Vector3 position = targetPosition + direction * currentDistance;
            
            // Apply to transform
            transform.position = position;
            transform.LookAt(targetPosition);
        }
        
        // Method to set a new target at runtime
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
                targetPosition = target.position;
        }
        
        // Method to reset camera to initial state
        public void ResetCamera()
        {
            targetDistance = initialDistance;
            horizontalAngle = 0f;
            verticalAngle = 0f;
        }
        
        // Method to focus on target with specific distance
        public void FocusOnTarget(float distance = -1f)
        {
            if (distance > 0)
                targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            else
                targetDistance = initialDistance;
        }
        
        void OnDrawGizmosSelected()
        {
            // Draw orbit visualization in editor
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.position, minDistance);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(target.position, maxDistance);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}