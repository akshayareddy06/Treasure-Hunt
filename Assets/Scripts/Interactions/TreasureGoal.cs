using System.Collections;
using UnityEngine;

namespace TreasureHunt
{
    public class TreasureGoal : MonoBehaviour
    {
        [Header("Chest Components")]
        public Transform chestLid;
        public Light chestGoldenLight;
        public ParticleSystem goldenSparkles;
        public float interactionDistance = 3.5f;

        private bool isOpened = false;
        private Transform playerTransform;

        private void Start()
        {
            if (chestLid == null)
            {
                chestLid = transform.Find("ChestLid");
            }

            if (chestGoldenLight != null)
            {
                chestGoldenLight.intensity = 1.5f;
            }
        }

        private void Update()
        {
            if (isOpened) return;

            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= interactionDistance)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowPrompt("Press [E] to Claim the Lost Treasure!");
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        ClaimTreasure();
                    }
                }
            }
        }

        public void ClaimTreasure()
        {
            if (isOpened) return;
            StartCoroutine(VictorySequence());
        }

        private IEnumerator VictorySequence()
        {
            isOpened = true;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.HidePrompt();
            }

            // Stop normal background music and play Victory Theme!
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
                AudioManager.Instance.PlayMusic(AudioManager.Instance.victoryThemeClip, false);
            }

            // Smoothly open chest lid
            if (chestLid != null)
            {
                Quaternion startRot = chestLid.localRotation;
                Quaternion targetRot = Quaternion.Euler(-115f, 0, 0);

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * 1.5f;
                    chestLid.localRotation = Quaternion.Slerp(startRot, targetRot, Mathf.SmoothStep(0, 1, t));

                    if (chestGoldenLight != null)
                    {
                        chestGoldenLight.intensity = Mathf.Lerp(1.5f, 6.0f, t);
                    }

                    yield return null;
                }
            }

            if (goldenSparkles != null)
            {
                goldenSparkles.Play();
            }

            yield return new WaitForSeconds(1.2f);

            // Trigger Game Victory on GameManager and UIManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteGame();
            }
        }
    }
}
