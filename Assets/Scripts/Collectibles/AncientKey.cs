using UnityEngine;

namespace TreasureHunt
{
    public class AncientKey : MonoBehaviour
    {
        public string keyID = "CaveKey_Level3";
        public float rotationSpeed = 90.0f;
        public float bobHeight = 0.15f;
        public float bobSpeed = 3.0f;

        private Vector3 startPos;
        private bool isCollected = false;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            if (isCollected) return;

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
                    GameManager.Instance.AddKey(keyID);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.keyPickupClip, 1.0f);
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowPrompt("ANCIENT KEY OBTAINED! Find the Sealed Door.");
                }

                Destroy(gameObject);
            }
        }
    }
}
