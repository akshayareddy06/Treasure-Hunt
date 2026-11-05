using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreasureHunt
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private Image fadeOverlay;
        private Canvas fadeCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeOverlay();
        }

        private void CreateFadeOverlay()
        {
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform);
            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999;
            canvasObj.AddComponent<CanvasScaler>();

            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);
            fadeOverlay = imageObj.AddComponent<Image>();
            fadeOverlay.color = new Color(0, 0, 0, 0);

            RectTransform rt = imageObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fadeOverlay.raycastTarget = false;
        }

        public void LoadScene(string sceneName, float fadeDuration = 0.5f)
        {
            StartCoroutine(FadeAndLoadRoutine(sceneName, fadeDuration));
        }

        private IEnumerator FadeAndLoadRoutine(string sceneName, float duration)
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.raycastTarget = true;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeOverlay.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
                fadeOverlay.color = Color.black;
            }

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncOp.isDone)
            {
                yield return null;
            }

            if (fadeOverlay != null)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeOverlay.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
                fadeOverlay.color = new Color(0, 0, 0, 0);
                fadeOverlay.raycastTarget = false;
            }
        }
    }
}
