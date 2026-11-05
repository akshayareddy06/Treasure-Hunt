using UnityEngine;

namespace TreasureHunt
{
    public class LevelTransitionTrigger : MonoBehaviour
    {
        public string nextSceneName = "Level2_CrystalTunnels";
        public string transitionPrompt = "Entering Deeper Tunnels...";

        private bool hasTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                hasTriggered = true;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowPrompt(transitionPrompt);
                }

                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.LoadScene(nextSceneName);
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
                }
            }
        }
    }
}
