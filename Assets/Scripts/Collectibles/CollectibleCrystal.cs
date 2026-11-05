using UnityEngine;

namespace TreasureHunt
{
    public class CollectibleCrystal : MonoBehaviour
    {
        public int crystalValue = 1;
        public float rotationSpeed = 60.0f;
        public float pulseSpeed = 4.0f;

        private Light crystalLight;
        private float baseLightIntensity = 2.0f;
        private bool isCollected = false;

        private void Start()
        {
            crystalLight = GetComponentInChildren<Light>();
            if (crystalLight != null) baseLightIntensity = crystalLight.intensity;
        }

        private void Update()
        {
            if (isCollected) return;

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            if (crystalLight != null)
            {
                crystalLight.intensity = baseLightIntensity * (0.85f + 0.3f * Mathf.Sin(Time.time * pulseSpeed));
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                isCollected = true;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddCrystals(crystalValue);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.crystalPickupClip, 0.85f, 0.05f);
                }

                Destroy(gameObject);
            }
        }
    }
}
