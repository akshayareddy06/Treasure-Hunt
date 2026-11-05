using UnityEngine;

namespace TreasureHunt
{
    public class SwingingStone : MonoBehaviour
    {
        [Header("Pendulum Motion")]
        public Transform pivot;
        public Transform stoneBob;
        public float swingAngle = 55.0f;
        public float swingSpeed = 2.2f;
        public float phaseOffset = 0f;
        public Vector3 swingAxis = Vector3.forward;

        [Header("Damage")]
        public int damage = 1;
        public float knockbackStrength = 14.0f;

        private float lastAngle = 0f;
        private AudioSource whooshSource;

        private void Awake()
        {
            if (pivot == null) pivot = transform;
            if (stoneBob == null) stoneBob = transform.Find("PendulumArm/StoneBob");

            whooshSource = gameObject.AddComponent<AudioSource>();
            whooshSource.spatialBlend = 1.0f;
            whooshSource.minDistance = 2f;
            whooshSource.maxDistance = 25f;
            whooshSource.playOnAwake = false;
        }

        private void Update()
        {
            if (pivot == null) return;

            float currentAngle = Mathf.Sin((Time.time * swingSpeed) + phaseOffset) * swingAngle;
            pivot.localRotation = Quaternion.AngleAxis(currentAngle, swingAxis);

            // Play whoosh when crossing near center
            if ((lastAngle < 0f && currentAngle >= 0f) || (lastAngle > 0f && currentAngle <= 0f))
            {
                if (AudioManager.Instance != null && AudioManager.Instance.stoneSwingClip != null)
                {
                    whooshSource.pitch = Random.Range(0.9f, 1.1f);
                    whooshSource.PlayOneShot(AudioManager.Instance.stoneSwingClip, 0.7f * AudioManager.Instance.sfxVolume);
                }
            }

            lastAngle = currentAngle;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TriggerDeath();
            }
            else
            {
                HealthManager health = other.GetComponent<HealthManager>();
                if (health != null) health.TakeDamage(999, Vector3.up * 10f);
            }
        }
    }
}
