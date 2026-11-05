using System.Collections;
using UnityEngine;

namespace TreasureHunt
{
    public class FallingRock : MonoBehaviour
    {
        [Header("Rock Settings")]
        public Transform rockMesh;
        public GameObject groundWarningIndicator;
        public float warningDuration = 0.85f;
        public float fallGravity = 30.0f;
        public int damage = 1;

        private Vector3 initialRockPos;
        private Quaternion initialRockRot;
        private bool isTriggered = false;
        private bool isFalling = false;
        public bool IsFalling => isFalling;
        private bool hasHitGround = false;
        public bool HasHitGround => hasHitGround;
        private Vector3 velocity = Vector3.zero;

        private void Awake()
        {
            if (rockMesh == null)
            {
                rockMesh = transform.Find("RockBoulder");
                if (rockMesh == null) rockMesh = transform;
            }

            initialRockPos = rockMesh.position;
            initialRockRot = rockMesh.rotation;

            if (groundWarningIndicator != null)
            {
                groundWarningIndicator.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                TriggerFall();
            }
        }

        public void TriggerFall()
        {
            if (isTriggered) return;
            StartCoroutine(FallSequence());
        }

        private IEnumerator FallSequence()
        {
            isTriggered = true;

            // Show ground red warning decal/circle
            if (groundWarningIndicator != null)
            {
                groundWarningIndicator.SetActive(true);
            }

            // Rumble audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAt(AudioManager.Instance.bridgeCrackClip, rockMesh.position, 0.6f);
            }

            // Tremble
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 50f) * 0.08f;
                rockMesh.position = initialRockPos + new Vector3(shake, 0, shake);
                yield return null;
            }

            // Rock drops
            isFalling = true;
            velocity = Vector3.zero;

            float groundY = initialRockPos.y - 8.0f;
            if (groundWarningIndicator != null)
            {
                groundY = groundWarningIndicator.transform.position.y + 0.6f;
            }

            while (rockMesh.position.y > groundY)
            {
                velocity += Vector3.down * fallGravity * Time.deltaTime;
                rockMesh.position += velocity * Time.deltaTime;
                rockMesh.Rotate(new Vector3(45f, 60f, 30f) * Time.deltaTime);

                // Check hit with player during fall (Instant Death on dash/hit)
                Collider[] hits = Physics.OverlapSphere(rockMesh.position, 1.1f);
                foreach (Collider hit in hits)
                {
                    PlayerController pc = hit.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        pc.TriggerDeath();
                    }
                    else
                    {
                        HealthManager health = hit.GetComponent<HealthManager>();
                        if (health != null) health.TakeDamage(999, Vector3.down * 10f);
                    }
                }

                yield return null;
            }

            // Impact ground
            hasHitGround = true;
            isFalling = false;
            rockMesh.position = new Vector3(rockMesh.position.x, groundY, rockMesh.position.z);

            if (groundWarningIndicator != null)
            {
                groundWarningIndicator.SetActive(false);
            }

            // Impact sound & shake
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAt(AudioManager.Instance.rockCrashClip, rockMesh.position, 1.0f);
            }
        }

        public void ResetRock()
        {
            StopAllCoroutines();
            isTriggered = false;
            isFalling = false;
            hasHitGround = false;
            velocity = Vector3.zero;

            if (rockMesh != null)
            {
                rockMesh.position = initialRockPos;
                rockMesh.rotation = initialRockRot;
            }

            if (groundWarningIndicator != null)
            {
                groundWarningIndicator.SetActive(false);
            }
        }
    }
}
