using System.Collections;
using UnityEngine;

namespace TreasureHunt
{
    public class HealthManager : MonoBehaviour
    {
        public int maxHealth = 3;
        public int currentHealth = 3;
        public float invulnerabilityDuration = 1.2f;

        private bool isInvulnerable = false;
        private PlayerController playerController;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage, Vector3 knockback)
        {
            if (isInvulnerable || (playerController != null && playerController.IsDead)) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
            }

            if (playerController != null)
            {
                playerController.ApplyKnockback(knockback);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDeathClip, 0.6f);
                }
                StartCoroutine(InvulnerabilityRoutine());
            }
        }

        public void InstantKill()
        {
            if (playerController != null && playerController.IsDead) return;

            currentHealth = 0;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(0, maxHealth);
            }
            Die();
        }

        private void Die()
        {
            if (playerController != null)
            {
                playerController.SetDead(true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDeathClip, 1.0f);
            }

            // In endless runner mode, EndlessHUD handles the Game Over screen with Restart Run / Home options.
            // No auto-reload coroutine needed.
        }

        private IEnumerator RespawnDelayRoutine()
        {
            yield return new WaitForSeconds(1.8f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnPlayer();
            }
            else
            {
                // Fallback reload
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isInvulnerable = false;
            if (playerController != null)
            {
                playerController.SetDead(false);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
            }
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(invulnerabilityDuration);
            isInvulnerable = false;
        }
    }
}
