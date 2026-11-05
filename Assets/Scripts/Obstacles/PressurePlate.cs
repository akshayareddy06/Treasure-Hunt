using System.Collections;
using UnityEngine;

namespace TreasureHunt
{
    public class PressurePlate : MonoBehaviour
    {
        [Header("Plate Settings")]
        public Transform plateMesh;
        public TrapSpikes connectedTrap;
        public float depressDistance = 0.08f;
        public float depressSpeed = 8.0f;
        public float resetDelay = 3.0f;

        private Vector3 initialPos;
        private Vector3 depressedPos;
        private bool isPressed = false;

        private void Awake()
        {
            if (plateMesh == null) plateMesh = transform.Find("PlateTop");
            if (plateMesh == null) plateMesh = transform;

            initialPos = plateMesh.localPosition;
            depressedPos = initialPos - new Vector3(0, depressDistance, 0);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isPressed) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                StartCoroutine(TriggerRoutine());
            }
        }

        private IEnumerator TriggerRoutine()
        {
            isPressed = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAt(AudioManager.Instance.plateClickClip, transform.position, 0.9f);
            }

            // Move plate down
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * depressSpeed;
                plateMesh.localPosition = Vector3.Lerp(initialPos, depressedPos, t);
                yield return null;
            }

            // Trigger trap
            if (connectedTrap != null)
            {
                connectedTrap.FireTrap();
            }

            yield return new WaitForSeconds(resetDelay);

            // Return up
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * (depressSpeed * 0.5f);
                plateMesh.localPosition = Vector3.Lerp(depressedPos, initialPos, t);
                yield return null;
            }

            isPressed = false;
        }

        public void ResetPlate()
        {
            StopAllCoroutines();
            isPressed = false;
            if (plateMesh != null) plateMesh.localPosition = initialPos;
            if (connectedTrap != null) connectedTrap.ResetTrap();
        }
    }
}
