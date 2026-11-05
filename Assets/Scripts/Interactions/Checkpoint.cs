using UnityEngine;

namespace TreasureHunt
{
    public class Checkpoint : MonoBehaviour
    {
        public Transform respawnPoint;
        public Light checkpointLight;
        public Renderer beaconRenderer;
        public Material activeMaterial;

        private bool isActivated = false;

        private void Start()
        {
            if (respawnPoint == null) respawnPoint = transform;

            if (checkpointLight != null)
                checkpointLight.enabled = false;

            if (activeMaterial == null)
                activeMaterial = MaterialFactory.GetMaterial("TurquoiseCrystal");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isActivated) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                ActivateCheckpoint();
            }
        }

        public void ActivateCheckpoint()
        {
            isActivated = true;

            if (checkpointLight != null)
            {
                checkpointLight.enabled = true;
                checkpointLight.color = new Color(0.0f, 0.95f, 0.85f);
                checkpointLight.intensity = 3.5f;
            }

            if (beaconRenderer != null && activeMaterial != null)
            {
                beaconRenderer.material = activeMaterial;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.checkpointClip, 0.9f);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(respawnPoint.position, respawnPoint.rotation);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPrompt("CHECKPOINT REACHED!");
            }
        }
    }
}
