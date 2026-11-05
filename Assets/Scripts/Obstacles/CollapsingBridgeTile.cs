using System.Collections;
using UnityEngine;

namespace TreasureHunt
{
    public class CollapsingBridgeTile : MonoBehaviour
    {
        [Header("Timing")]
        public float collapseDelay = 0.75f;
        public float shakeIntensity = 0.08f;
        public float fallSpeed = 16.0f;

        [Header("Components")]
        public Renderer tileRenderer;
        public Material warningMaterial;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Material originalMaterial;
        private bool isTriggered = false;
        private bool isFalling = false;
        private Collider tileCollider;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            tileCollider = GetComponent<Collider>();

            if (tileRenderer == null)
                tileRenderer = GetComponent<Renderer>();

            if (tileRenderer != null)
                originalMaterial = tileRenderer.sharedMaterial;

            if (warningMaterial == null)
                warningMaterial = MaterialFactory.GetMaterial("BridgeWarning");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isTriggered || isFalling) return;

            if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerController>() != null)
            {
                // Check if player landed on top of the tile
                foreach (ContactPoint contact in collision.contacts)
                {
                    if (contact.normal.y < -0.5f)
                    {
                        StartCoroutine(CollapseRoutine());
                        break;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered || isFalling) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                StartCoroutine(CollapseRoutine());
            }
        }

        private IEnumerator CollapseRoutine()
        {
            isTriggered = true;

            // Warning sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAt(AudioManager.Instance.bridgeCrackClip, transform.position, 0.8f);
            }

            // Warning visual change
            if (tileRenderer != null && warningMaterial != null)
            {
                tileRenderer.material = warningMaterial;
            }

            // Shake vibration
            float elapsed = 0f;
            while (elapsed < collapseDelay)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / collapseDelay;
                float offset = Mathf.Sin(elapsed * 45f) * shakeIntensity * progress;
                transform.position = initialPosition + new Vector3(offset, -offset * 0.5f, offset * 0.5f);
                yield return null;
            }

            // Begin falling into abyss
            isFalling = true;
            if (tileCollider != null) tileCollider.enabled = false;

            float fallElapsed = 0f;
            Vector3 vel = Vector3.zero;
            while (fallElapsed < 3.0f)
            {
                fallElapsed += Time.deltaTime;
                vel += Vector3.down * 25f * Time.deltaTime;
                transform.position += vel * Time.deltaTime;
                transform.Rotate(new Vector3(15f, 25f, 10f) * Time.deltaTime);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        public void ResetTile()
        {
            StopAllCoroutines();
            isTriggered = false;
            isFalling = false;
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            if (tileRenderer != null && originalMaterial != null)
            {
                tileRenderer.material = originalMaterial;
            }

            if (tileCollider != null) tileCollider.enabled = true;
            gameObject.SetActive(true);
        }
    }
}
