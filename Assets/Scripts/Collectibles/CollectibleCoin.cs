using UnityEngine;

namespace TreasureHunt
{
    public class CollectibleCoin : MonoBehaviour
    {
        public int coinValue = 1;
        public float rotationSpeed = 120.0f;
        public float bobHeight = 0.12f;
        public float bobSpeed = 3.5f;

        private Vector3 startPos;
        private bool isCollected = false;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            if (isCollected) return;

            // Spin & Bob
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                isCollected = true;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddCoins(coinValue);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.coinPickupClip, 0.75f, 0.08f);
                }

                Destroy(gameObject);
            }
        }
    }
}
