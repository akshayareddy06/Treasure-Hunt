using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TreasureHunt
{
    public class EndlessHUD : MonoBehaviour
    {
        public static EndlessHUD Instance { get; private set; }

        [Header("Runtime References")]
        public PlayerController player;
        public Canvas hudCanvas;

        private Text distanceText;
        private Text coinText;
        private Text bestText;
        private Text speedText;
        private Text levelTitleText;

        // Modals
        private GameObject gameOverPanel;
        private Text gameOverSummaryText;

        private GameObject levelCompletePanel;
        private Text levelCompleteTitleText;
        private Text levelCompleteDescText;

        private GameObject victoryPanel;
        private Text victorySummaryText;

        // In-game Celebratory Toast Banner (non-blocking)
        private GameObject toastPanel;
        private Text toastTitleText;
        private Text toastSubtitleText;
        private Coroutine toastCoroutine;

        // In-game Dynamic Turn Prompt (Temple Run style)
        private GameObject turnPromptPanel;
        private Text turnPromptText;

        private bool isModalOpen = false;
        private int currentLevelNum = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Destroy any leftover duplicate HUD canvases in scene
            GameObject[] oldCanvases = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in oldCanvases)
            {
                if (go != null && go.name == "EndlessHUD_Canvas")
                {
                    DestroyImmediate(go);
                }
            }

            EnsureHUDCanvas();
        }

        private void Start()
        {
            if (player == null)
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            }

            // If at Main Menu, keep HUD hidden so it never overlaps!
            if (MainMenuUI.Instance != null && MainMenuUI.Instance.isAtHome)
            {
                SetHUDVisible(false);
            }
            else
            {
                SetHUDVisible(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetHUDVisible(bool visible)
        {
            if (hudCanvas != null)
            {
                hudCanvas.gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (player == null) return;
            }

            if (!isModalOpen)
            {
                int dist = Mathf.Max(0, (int)player.DistanceTraveled);
                int coins = (GameManager.Instance != null) ? GameManager.Instance.currentCoins : 0;
                int best = PlayerPrefs.GetInt("BestRunDistance", 0);

                if (distanceText != null) distanceText.text = $"DISTANCE: {dist} m";
                if (coinText != null) coinText.text = $"COINS: {coins}";
                if (bestText != null) bestText.text = $"BEST: {best} m";
                if (speedText != null) speedText.text = $"SPEED: {player.CurrentSpeed:F1} m/s";

                // Quick level select keys: 1, 2, 3
                if (Input.GetKeyDown(KeyCode.Alpha1)) JumpToLevel(1);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) JumpToLevel(2);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) JumpToLevel(3);

                // Home shortcut: H or Escape
                if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Escape))
                {
                    ReturnToHome();
                }
            }
            else
            {
                // In Modals: Space / Enter performs primary action
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (gameOverPanel != null && gameOverPanel.activeSelf) RestartRun();
                    else if (levelCompletePanel != null && levelCompletePanel.activeSelf) NextLevelClicked();
                    else if (victoryPanel != null && victoryPanel.activeSelf) RestartRun();
                }
                else if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Escape))
                {
                    ReturnToHome();
                }
            }
        }

        public void JumpToLevel(int lvl)
        {
            PlayClickSFX();
            currentLevelNum = lvl;
            CloseAllModals();

            if (EndlessCaveGenerator.Instance != null)
            {
                EndlessCaveGenerator.Instance.SwitchToLevel(lvl);
            }
            UpdateLevelDisplay(lvl);
        }

        public void UpdateLevelDisplay(int lvl)
        {
            currentLevelNum = lvl;
            if (levelTitleText == null) return;
            switch (lvl)
            {
                case 1:
                    levelTitleText.text = "★  LEVEL 1 : CAVE ENTRANCE  ★";
                    levelTitleText.color = new Color(0.98f, 0.85f, 0.25f);
                    break;
                case 2:
                    levelTitleText.text = "★  LEVEL 2 : CRYSTAL TUNNELS  ★";
                    levelTitleText.color = new Color(0.1f, 0.95f, 0.85f);
                    break;
                case 3:
                    levelTitleText.text = "★  LEVEL 3 : TREASURE CHAMBER  ★";
                    levelTitleText.color = new Color(1.0f, 0.78f, 0.18f);
                    break;
            }
        }

        public void ShowGameOver(int finalDistance)
        {
            CloseAllModals();
            isModalOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            int coins = (GameManager.Instance != null) ? GameManager.Instance.currentCoins : 0;
            int best = PlayerPrefs.GetInt("BestRunDistance", finalDistance);

            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (gameOverSummaryText != null)
            {
                gameOverSummaryText.text = $"DASHED INTO OBSTACLE!\n\n" +
                    $"DISTANCE REACHED: {finalDistance} m\n" +
                    $"COINS COLLECTED: {coins}\n" +
                    $"BEST HIGH SCORE: {best} m";
            }
        }

        public void ShowLevelNotification(string title, string subtitle, float duration = 3.5f)
        {
            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(ToastRoutine(title, subtitle, duration));
        }

        private System.Collections.IEnumerator ToastRoutine(string title, string subtitle, float duration)
        {
            if (toastPanel != null)
            {
                if (toastTitleText != null) toastTitleText.text = title;
                if (toastSubtitleText != null) toastSubtitleText.text = subtitle;
                toastPanel.SetActive(true);

                if (AudioManager.Instance != null && AudioManager.Instance.checkpointClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.checkpointClip, 1.0f);
                }

                yield return new WaitForSeconds(duration);

                toastPanel.SetActive(false);
            }
        }

        public void ShowTurnPrompt(TurnDirection direction)
        {
            if (turnPromptPanel == null || turnPromptText == null) return;
            if (direction == TurnDirection.Left)
            {
                turnPromptText.text = "◄◄  TURN LEFT  (A / ←)  ◄◄";
                turnPromptText.color = new Color(0.1f, 0.95f, 1.0f);
            }
            else
            {
                turnPromptText.text = "►►  TURN RIGHT  (D / →)  ►►";
                turnPromptText.color = new Color(1.0f, 0.85f, 0.15f);
            }
            turnPromptPanel.SetActive(true);
        }

        public void HideTurnPrompt()
        {
            if (turnPromptPanel != null) turnPromptPanel.SetActive(false);
        }

        private void CreateTurnPromptBanner(Transform parent, Font font)
        {
            turnPromptPanel = new GameObject("TurnPromptBanner");
            turnPromptPanel.transform.SetParent(parent, false);
            RectTransform rt = turnPromptPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.76f);
            rt.anchorMax = new Vector2(0.5f, 0.76f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(520, 56);

            Image bg = turnPromptPanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.14f, 0.90f);

            turnPromptText = CreateHUDText(turnPromptPanel.transform, "TurnText", "◄◄ TURN LEFT ◄◄", font, 24, Color.yellow, Vector2.zero, new Vector2(500, 48), TextAnchor.MiddleCenter);

            turnPromptPanel.SetActive(false);
        }

        public void ShowLevelBanner(string title, string desc, Action onContinue = null)
        {
            // Auto-advance without asking permission!
            ShowLevelNotification(title, desc, 3.5f);
            onContinue?.Invoke();
        }

        public void ShowLevelComplete(int levelJustCompleted)
        {
            // Auto-advance without asking permission!
            if (levelJustCompleted == 1)
            {
                ShowLevelNotification("★ LEVEL 1 COMPLETED! ★", "ENTERING LEVEL 2: CRYSTAL TUNNELS...", 3.5f);
            }
            else if (levelJustCompleted == 2)
            {
                ShowLevelNotification("★ LEVEL 2 COMPLETED! ★", "ENTERING LEVEL 3: TREASURE CHAMBER...", 3.5f);
            }
        }

        public void NextLevelClicked()
        {
            PlayClickSFX();
            int next = Mathf.Min(currentLevelNum + 1, 3);
            JumpToLevel(next);
        }

        public void ShowVictory()
        {
            CloseAllModals();
            isModalOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            int coins = (GameManager.Instance != null) ? GameManager.Instance.currentCoins : 0;
            int dist = player != null ? (int)player.DistanceTraveled : 300;

            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (victorySummaryText != null)
            {
                victorySummaryText.text = $"MISSION ACCOMPLISHED — TREASURE CLAIMED!\n\n" +
                    $"TOTAL DISTANCE: {dist} m   |   GOLD COINS: {coins}\n\n" +
                    $"B.Tech Capstone Project — SR University\n" +
                    $"Team: Vem Sai Akshaya, Bathini Vaishnavi, Erra Vaishnavi\n" +
                    $"Project Guide: Dr. Vijaya Chandra Jadala";
            }
        }

        public void RestartRun()
        {
            PlayClickSFX();
            Time.timeScale = 1.0f;
            CloseAllModals();

            if (MainMenuUI.Instance != null)
            {
                MainMenuUI.Instance.HideMenu();
            }

            if (player == null) player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.ResetPlayer();
                HealthManager hm = player.GetComponent<HealthManager>();
                if (hm != null) hm.ResetHealth();
            }

            Camera mainCam = Camera.main;
            if (mainCam != null && player != null)
            {
                ThirdPersonCamera tpc = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpc != null)
                {
                    mainCam.transform.position = player.transform.position + tpc.runnerOffset;
                    mainCam.transform.LookAt(player.transform.position + new Vector3(0, 1.4f, 6.0f));
                }
            }

            if (EndlessCaveGenerator.Instance != null)
            {
                EndlessCaveGenerator.Instance.ResetGenerator(currentLevelNum);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentCoins = 0;
            }

            SetHUDVisible(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[EndlessHUD] Run cleanly restarted like Temple Run!");
        }

        public void ReturnToHome()
        {
            PlayClickSFX();
            Time.timeScale = 1.0f;
            CloseAllModals();
            SetHUDVisible(false);

            if (player == null) player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.ResetPlayer();
                player.autoRun = false;
            }

            if (MainMenuUI.Instance != null)
            {
                MainMenuUI.Instance.OpenMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        public void CloseAllModals()
        {
            isModalOpen = false;
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }

        private void PlayClickSFX()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.buttonClickClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickClip, 0.85f);
            }
        }

        private void EnsureHUDCanvas()
        {
            GameObject canvasObj = new GameObject("EndlessHUD_Canvas");
            hudCanvas = canvasObj.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Top HUD Bar (Temple Run style: Clean, minimal, non-intrusive)
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(canvasObj.transform, false);
            RectTransform topRt = topBar.AddComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1);
            topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot = new Vector2(0.5f, 1);
            topRt.sizeDelta = new Vector2(0, 75);
            topRt.anchoredPosition = Vector2.zero;

            // Coins (Top Left - clearly pinned to the left edge with generous margin)
            coinText = CreateHUDText(topBar.transform, "CoinText", "🪙 COINS: 0", font, 22, new Color(1.0f, 0.84f, 0.15f), new Vector2(35, -20), new Vector2(240, 36), TextAnchor.MiddleLeft);
            RectTransform cRt = coinText.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(0, 1);
            cRt.pivot = new Vector2(0, 1);

            // Level Title Badge Background (Top Center - stylish translucent pill backing)
            GameObject badgeBg = new GameObject("LevelBadgeBg");
            badgeBg.transform.SetParent(topBar.transform, false);
            RectTransform bgRt = badgeBg.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 1);
            bgRt.anchorMax = new Vector2(0.5f, 1);
            bgRt.pivot = new Vector2(0.5f, 1);
            bgRt.anchoredPosition = new Vector2(0, -12);
            bgRt.sizeDelta = new Vector2(460, 40);
            Image bgImg = badgeBg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.14f, 0.85f);
            bgImg.raycastTarget = false;

            // Level Title Badge Text (Top Center - clear wide spacing)
            levelTitleText = CreateHUDText(badgeBg.transform, "LevelBadge", "★  LEVEL 1 : CAVE ENTRANCE  ★", font, 20, new Color(0.98f, 0.85f, 0.25f), Vector2.zero, new Vector2(450, 36), TextAnchor.MiddleCenter);
            RectTransform lRt = levelTitleText.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.5f, 0.5f);
            lRt.anchorMax = new Vector2(0.5f, 0.5f);
            lRt.pivot = new Vector2(0.5f, 0.5f);

            // Distance & Best (Top Right - clearly pinned to the right edge with generous margin)
            distanceText = CreateHUDText(topBar.transform, "DistanceText", "DISTANCE: 0 m", font, 22, Color.white, new Vector2(-35, -14), new Vector2(250, 30), TextAnchor.MiddleRight);
            RectTransform dRt = distanceText.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(1, 1);
            dRt.anchorMax = new Vector2(1, 1);
            dRt.pivot = new Vector2(1, 1);

            bestText = CreateHUDText(topBar.transform, "BestText", "BEST: 0 m", font, 18, new Color(0.3f, 0.95f, 0.85f), new Vector2(-35, -42), new Vector2(250, 26), TextAnchor.MiddleRight);
            RectTransform bRt = bestText.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(1, 1);
            bRt.anchorMax = new Vector2(1, 1);
            bRt.pivot = new Vector2(1, 1);

            // 2. In-Game Celebratory Toast Banner (Auto-transitions without stopping)
            CreateToastBanner(canvasObj.transform, font);

            // 3. In-Game Dynamic Turn Prompt Banner
            CreateTurnPromptBanner(canvasObj.transform, font);

            // 4. Modals
            CreateGameOverModal(canvasObj.transform, font);
            CreateLevelCompleteModal(canvasObj.transform, font);
            CreateVictoryModal(canvasObj.transform, font);
        }

        private void CreateToastBanner(Transform parent, Font font)
        {
            toastPanel = new GameObject("LevelToastBanner");
            toastPanel.transform.SetParent(parent, false);
            RectTransform rt = toastPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -85);
            rt.sizeDelta = new Vector2(600, 75);

            Image bg = toastPanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.14f, 0.92f);

            toastTitleText = CreateHUDText(toastPanel.transform, "ToastTitle", "★ LEVEL 1 COMPLETED! ★", font, 24, new Color(1.0f, 0.85f, 0.2f), new Vector2(0, -12), new Vector2(580, 30), TextAnchor.MiddleCenter);
            toastSubtitleText = CreateHUDText(toastPanel.transform, "ToastSub", "ENTERING LEVEL 2: CRYSTAL TUNNELS...", font, 16, new Color(0.2f, 0.95f, 0.85f), new Vector2(0, -42), new Vector2(580, 25), TextAnchor.MiddleCenter);

            toastPanel.SetActive(false);
        }

        private void CreateGameOverModal(Transform parent, Font font)
        {
            gameOverPanel = new GameObject("GameOverModal");
            gameOverPanel.transform.SetParent(parent, false);
            RectTransform rt = gameOverPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(500, 360);

            Image img = gameOverPanel.AddComponent<Image>();
            img.color = new Color(0.06f, 0.10f, 0.16f, 0.96f);

            CreateHUDText(gameOverPanel.transform, "GOTitle", "RUN OVER", font, 36, new Color(0.98f, 0.28f, 0.25f), new Vector2(0, 125), new Vector2(460, 45), TextAnchor.MiddleCenter);
            gameOverSummaryText = CreateHUDText(gameOverPanel.transform, "GOSummary", "", font, 20, Color.white, new Vector2(0, 45), new Vector2(460, 95), TextAnchor.MiddleCenter);

            // Clean, prominent Temple Run style buttons: [PLAY AGAIN] and [MENU]
            CreateInteractiveBtn(gameOverPanel.transform, "RestartBtn", "▶️  PLAY AGAIN (SPACE)", font, new Vector2(0, -50), new Vector2(320, 52), RestartRun);
            CreateInteractiveBtn(gameOverPanel.transform, "HomeBtn", "🏠  MAIN MENU (H)", font, new Vector2(0, -118), new Vector2(220, 42), ReturnToHome);

            gameOverPanel.SetActive(false);
        }

        private void CreateLevelCompleteModal(Transform parent, Font font)
        {
            levelCompletePanel = new GameObject("LevelCompleteModal");
            levelCompletePanel.transform.SetParent(parent, false);
            RectTransform rt = levelCompletePanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620, 440);

            Image img = levelCompletePanel.AddComponent<Image>();
            img.color = new Color(0.06f, 0.12f, 0.18f, 0.96f);

            levelCompleteTitleText = CreateHUDText(levelCompletePanel.transform, "LCTitle", "LEVEL COMPLETE!", font, 32, new Color(0.1f, 0.95f, 0.85f), new Vector2(0, -40), new Vector2(580, 70), TextAnchor.MiddleCenter);
            levelCompleteDescText = CreateHUDText(levelCompletePanel.transform, "LCDesc", "", font, 20, Color.white, new Vector2(0, -150), new Vector2(560, 120), TextAnchor.MiddleCenter);

            // Buttons: [NEXT LEVEL] [REPLAY] [HOME]
            CreateInteractiveBtn(levelCompletePanel.transform, "NextBtn", "▶️ NEXT LEVEL (SPACE)", font, new Vector2(0, -250), new Vector2(320, 52), NextLevelClicked);
            CreateInteractiveBtn(levelCompletePanel.transform, "ReplayBtn", "🔄 REPLAY THIS LEVEL", font, new Vector2(-120, -325), new Vector2(220, 44), RestartRun);
            CreateInteractiveBtn(levelCompletePanel.transform, "HomeBtn2", "🏠 HOME", font, new Vector2(120, -325), new Vector2(180, 44), ReturnToHome);

            levelCompletePanel.SetActive(false);
        }

        private void CreateVictoryModal(Transform parent, Font font)
        {
            victoryPanel = new GameObject("VictoryModal");
            victoryPanel.transform.SetParent(parent, false);
            RectTransform rt = victoryPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700, 500);

            Image img = victoryPanel.AddComponent<Image>();
            img.color = new Color(0.06f, 0.10f, 0.16f, 0.96f);

            CreateHUDText(victoryPanel.transform, "VicTitle", "TREASURE HUNT: LOST CAVE", font, 34, new Color(1.0f, 0.82f, 0.18f), new Vector2(0, -40), new Vector2(660, 50), TextAnchor.MiddleCenter);
            victorySummaryText = CreateHUDText(victoryPanel.transform, "VicSummary", "", font, 20, Color.white, new Vector2(0, -210), new Vector2(640, 260), TextAnchor.MiddleCenter);

            CreateInteractiveBtn(victoryPanel.transform, "VicRestartBtn", "🔄 PLAY AGAIN (SPACE)", font, new Vector2(-140, -390), new Vector2(240, 52), RestartRun);
            CreateInteractiveBtn(victoryPanel.transform, "VicHomeBtn", "🏠 HOME MENU", font, new Vector2(140, -390), new Vector2(220, 52), ReturnToHome);

            victoryPanel.SetActive(false);
        }

        private Text CreateHUDText(Transform parent, string name, string text, Font font, int size, Color color, Vector2 pos, Vector2 sizeDelta, TextAnchor align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;

            Text t = go.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false; // NEVER block button raycasts!
            return t;
        }

        public GameObject CreateInteractiveBtn(Transform parent, string name, string label, Font font, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.42f, 0.60f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.08f, 0.42f, 0.60f);
            cb.highlightedColor = new Color(0.0f, 0.85f, 0.85f);
            cb.pressedColor = new Color(0.04f, 0.22f, 0.35f);
            btn.colors = cb;
            btn.onClick.AddListener(action);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.sizeDelta = size;
            trt.anchoredPosition = Vector2.zero;

            Text t = textObj.AddComponent<Text>();
            t.text = label;
            t.font = font;
            t.fontSize = 18;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false; // Never block clicks!

            return btnObj;
        }
    }
}
