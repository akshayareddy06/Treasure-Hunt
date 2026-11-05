using UnityEngine;

namespace TreasureHunt
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        public Transform target;
        public Vector3 runnerOffset = new Vector3(0, 2.8f, -4.8f);
        public float followSpeed = 14.0f;
        public float lateralFollowSpeed = 10.0f;

        [Header("Mode")]
        public bool isEndlessRunnerMode = true;

        [Header("Orbit Mode Settings")]
        public Vector3 targetOffset = new Vector3(0, 1.4f, 0);
        public float defaultDistance = 4.2f;
        public float minDistance = 1.0f;
        public float maxDistance = 6.0f;
        public float followSmoothness = 12.0f;
        public float mouseSensitivityX = 140.0f;
        public float mouseSensitivityY = 100.0f;
        public float minPitch = -20.0f;
        public float maxPitch = 65.0f;

        private float currentYaw = 0f;
        private float currentPitch = 14f;
        private float currentDistance;

        private void Start()
        {
            currentDistance = defaultDistance;

            if (target == null)
            {
                PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) target = pc.transform;
            }

            if (isEndlessRunnerMode)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) target = pc.transform;
                if (target == null) return;
            }

            if (isEndlessRunnerMode)
            {
                // Dynamic Endless Runner tracking: Behind runner, looking along runner's heading
                Vector3 targetForward = target.forward;
                Vector3 desiredPos = target.position - targetForward * 4.8f + Vector3.up * 2.8f;

                transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSpeed);

                Vector3 lookPoint = target.position + Vector3.up * 1.4f + targetForward * 5.2f;
                Quaternion targetRot = Quaternion.LookRotation(lookPoint - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12.0f);
            }
            else
            {
                // Standard orbit mode
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
                    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

                    currentYaw += mouseX;
                    currentPitch -= mouseY;
                    currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
                }

                Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                Vector3 focusPoint = target.position + targetOffset;
                Vector3 finalPosition = focusPoint + (rotation * Vector3.back) * defaultDistance;

                transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * followSmoothness);
                transform.LookAt(focusPoint);
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
