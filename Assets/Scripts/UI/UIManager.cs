using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TreasureHunt
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Elements")]
        public Text healthText;
        public Text coinText;
        public Text crystalText;
        public Text keyText;
        public Text levelTitleText;
        public Text objectiveText;
        public Text promptText;

        [Header("Menus")]
        public GameObject pauseMenuPanel;
        public GameObject victoryPanel;
        public Text victoryStatsText;

        [Header("Sliders")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            MainMenuUI.EnsureEventSystem();

            // In Endless Runner mode, EndlessHUD is the exclusive HUD.
            // Disable legacy UI canvas and do NOT build legacy HUD to prevent overlapping text!
            if (UnityEngine.Object.FindAnyObjectByType<EndlessCaveGenerator>() != null ||
                UnityEngine.Object.FindAnyObjectByType<EndlessHUD>() != null ||
                UnityEngine.Object.FindAnyObjectByType<MainMenuUI>() != null)
            {
                Canvas[] allCanvases = GetComponentsInChildren<Canvas>(true);
                foreach (var c in allCanvases)
                {
                    if (c != null && c.name != "EndlessHUD_Canvas")
                    {
                        c.gameObject.SetActive(false);
                    }
                }
                Canvas rootCanvas = GetComponent<Canvas>();
                if (rootCanvas != null && rootCanvas.name != "EndlessHUD_Canvas") rootCanvas.gameObject.SetActive(false);

                GameObject legacyUI = GameObject.Find("UICanvas");
                if (legacyUI != null) legacyUI.SetActive(false);
                return;
            }

            BuildUIIfMissing();
        }

        private void Start()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);

            UpdateLevelInfo();
            BindButtons();
        }

        public void BindButtons()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string bName = b.gameObject.name.ToLower();
                b.onClick.RemoveAllListeners();
                if (bName.Contains("resume")) b.onClick.AddListener(ResumeClicked);
                else if (bName.Contains("restart") || bName.Contains("retry")) b.onClick.AddListener(RestartClicked);
                else if (bName.Contains("menu")) b.onClick.AddListener(MainMenuClicked);
                else if (bName.Contains("again") || bName.Contains("next")) b.onClick.AddListener(PlayAgainClicked);

                Text t = b.GetComponentInChildren<Text>();
                if (t != null) t.raycastTarget = false;
            }
        }

        public void BuildUIIfMissing()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EndlessCaveGenerator>() != null ||
                UnityEngine.Object.FindAnyObjectByType<EndlessHUD>() != null)
            {
                return;
            }

            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("InGameCanvas");
                canvasObj.transform.SetParent(transform, false);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();

                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

                // HUD Container
                GameObject hud = new GameObject("HUD");
                hud.transform.SetParent(canvasObj.transform, false);

                // Top Left Stats
                GameObject statsPanel = CreatePanel(hud.transform, "StatsPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(360, 150), new Color(0, 0, 0, 0.55f));

                healthText = CreateText(statsPanel.transform, "HealthText", "HEALTH: 3 / 3", font, 24, Color.red, new Vector2(15, -15), new Vector2(320, 30));
                coinText = CreateText(statsPanel.transform, "CoinText", "GOLD COINS: 0", font, 22, new Color(1f, 0.82f, 0.2f), new Vector2(15, -50), new Vector2(320, 30));
                crystalText = CreateText(statsPanel.transform, "CrystalText", "CRYSTALS: 0", font, 22, new Color(0f, 0.95f, 0.85f), new Vector2(15, -85), new Vector2(320, 30));
                keyText = CreateText(statsPanel.transform, "KeyText", "KEYS: 0", font, 20, Color.yellow, new Vector2(15, -118), new Vector2(320, 25));

                // Objective Banner (Top Center)
                GameObject objPanel = CreatePanel(hud.transform, "ObjPanel", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -25), new Vector2(550, 75), new Color(0.08f, 0.12f, 0.16f, 0.75f));
                levelTitleText = CreateText(objPanel.transform, "LevelTitle", "LOST CAVE EXPLORATION", font, 24, new Color(0.95f, 0.95f, 0.95f), new Vector2(0, -8), new Vector2(530, 30), TextAnchor.MiddleCenter);
                objectiveText = CreateText(objPanel.transform, "Objective", "Follow the torch-lit path into the cave", font, 18, new Color(0.8f, 0.85f, 0.9f), new Vector2(0, -38), new Vector2(530, 25), TextAnchor.MiddleCenter);

                // Center Interaction Prompt
                GameObject promptObj = CreatePanel(canvasObj.transform, "PromptPanel", new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(600, 55), new Color(0, 0, 0, 0.7f));
                promptText = CreateText(promptObj.transform, "PromptText", "Press [E] to Interact", font, 22, Color.yellow, Vector2.zero, new Vector2(580, 45), TextAnchor.MiddleCenter);
                promptObj.SetActive(false);

                // Pause Menu Panel
                pauseMenuPanel = CreatePanel(canvasObj.transform, "PauseMenu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 520), new Color(0.08f, 0.1f, 0.14f, 0.92f));
                CreateText(pauseMenuPanel.transform, "Title", "GAME PAUSED", font, 36, Color.white, new Vector2(0, -25), new Vector2(400, 50), TextAnchor.MiddleCenter);

                CreateButton(pauseMenuPanel.transform, "ResumeBtn", "RESUME", font, new Vector2(0, -100), new Vector2(320, 45), ResumeClicked);
                CreateButton(pauseMenuPanel.transform, "RestartBtn", "RESTART LEVEL", font, new Vector2(0, -160), new Vector2(320, 45), RestartClicked);
                CreateButton(pauseMenuPanel.transform, "LevelSelectBtn", "LEVEL SELECT", font, new Vector2(0, -220), new Vector2(320, 45), LevelSelectClicked);
                CreateButton(pauseMenuPanel.transform, "MainMenuBtn", "MAIN MENU", font, new Vector2(0, -280), new Vector2(320, 45), MainMenuClicked);

                // Volume Controls in Pause Menu
                CreateText(pauseMenuPanel.transform, "MusicVolLabel", "Music Volume", font, 18, Color.white, new Vector2(0, -345), new Vector2(320, 25), TextAnchor.MiddleLeft);
                musicVolumeSlider = CreateSlider(pauseMenuPanel.transform, "MusicSlider", new Vector2(0, -375), new Vector2(320, 25), 0.8f, OnMusicSliderChanged);

                CreateText(pauseMenuPanel.transform, "SFXVolLabel", "SFX Volume", font, 18, Color.white, new Vector2(0, -415), new Vector2(320, 25), TextAnchor.MiddleLeft);
                sfxVolumeSlider = CreateSlider(pauseMenuPanel.transform, "SFXSlider", new Vector2(0, -445), new Vector2(320, 25), 1.0f, OnSFXSliderChanged);

                pauseMenuPanel.SetActive(false);

                // Victory Screen Panel
                victoryPanel = CreatePanel(canvasObj.transform, "VictoryPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650, 540), new Color(0.05f, 0.12f, 0.18f, 0.95f));
                CreateText(victoryPanel.transform, "VicTitle", "TREASURE FOUND!", font, 44, new Color(1f, 0.84f, 0.0f), new Vector2(0, -30), new Vector2(600, 60), TextAnchor.MiddleCenter);
                CreateText(victoryPanel.transform, "VicSubtitle", "You successfully navigated the Lost Cave!", font, 22, Color.white, new Vector2(0, -95), new Vector2(580, 30), TextAnchor.MiddleCenter);

                victoryStatsText = CreateText(victoryPanel.transform, "StatsText", "GOLD: 0\nCRYSTALS: 0\nTIME: 00:00", font, 26, new Color(0.85f, 0.9f, 1f), new Vector2(0, -170), new Vector2(500, 120), TextAnchor.MiddleCenter);

                CreateButton(victoryPanel.transform, "PlayAgainBtn", "PLAY AGAIN", font, new Vector2(0, -340), new Vector2(320, 50), PlayAgainClicked);
                CreateButton(victoryPanel.transform, "VicMainMenuBtn", "MAIN MENU", font, new Vector2(0, -415), new Vector2(320, 50), MainMenuClicked);

                victoryPanel.SetActive(false);
            }
        }

        public void UpdateHealth(int current, int max)
        {
            if (healthText != null)
            {
                string hearts = "";
                for (int i = 0; i < max; i++)
                {
                    hearts += (i < current) ? "Ã¢â„¢Â¥ " : "Ã¢â„¢Â¡ ";
                }
                healthText.text = $"HEALTH: {hearts.Trim()}";
            }
        }

        public void UpdateCoins(int coins)
        {
            if (coinText != null) coinText.text = $"GOLD COINS: {coins}";
        }

        public void UpdateCrystals(int crystals)
        {
            if (crystalText != null) crystalText.text = $"CRYSTALS: {crystals}";
        }

        public void UpdateKeys(int keys)
        {
            if (keyText != null) keyText.text = $"ANCIENT KEYS: {keys}";
        }

        public void UpdateLevelInfo()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (levelTitleText != null)
            {
                if (scene.Contains("Level1")) levelTitleText.text = "LEVEL 1: CAVE ENTRANCE";
                else if (scene.Contains("Level2")) levelTitleText.text = "LEVEL 2: CRYSTAL TUNNELS";
                else if (scene.Contains("Level3")) levelTitleText.text = "LEVEL 3: TREASURE CHAMBER";
            }

            if (objectiveText != null)
            {
                if (scene.Contains("Level1")) objectiveText.text = "Cross the collapsing bridge and find the tunnel";
                else if (scene.Contains("Level2")) objectiveText.text = "Dodge falling rocks and traps, find the Ancient Key";
                else if (scene.Contains("Level3")) objectiveText.text = "Unlock the chamber and claim the Lost Treasure!";
            }
        }

        public void ShowPrompt(string msg)
        {
            if (promptText != null)
            {
                promptText.text = msg;
                promptText.transform.parent.gameObject.SetActive(true);
            }
        }

        public void HidePrompt()
        {
            if (promptText != null)
            {
                promptText.transform.parent.gameObject.SetActive(false);
            }
        }

        public void ShowPauseMenu(bool show)
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(show);
            }
        }

        public void ShowVictoryScreen(int coins, int crystals, float time)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                int minutes = Mathf.FloorToInt(time / 60F);
                int seconds = Mathf.FloorToInt(time % 60F);
                string timeStr = string.Format("{0:00}:{1:00}", minutes, seconds);

                if (victoryStatsText != null)
                {
                    victoryStatsText.text = $"GOLD COINS COLLECTED: {coins}\nGLOWING CRYSTALS FOUND: {crystals}\nEXPEDITION TIME: {timeStr}";
                }
            }
        }

        public void ShowGameOver()
        {
            EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
            if (hud != null)
            {
                hud.ShowGameOver(0);
            }
        }

        public void ResumeClicked()
        {
            PlayButtonAudio();
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }

        public void RestartClicked()
        {
            PlayButtonAudio();
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        public void LevelSelectClicked()
        {
            PlayButtonAudio();
            Time.timeScale = 1.0f;
            if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene("LevelSelect");
            else SceneManager.LoadScene("LevelSelect");
        }

        public void MainMenuClicked()
        {
            PlayButtonAudio();
            Time.timeScale = 1.0f;
            if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene("MainMenu");
            else SceneManager.LoadScene("MainMenu");
        }

        public void PlayAgainClicked()
        {
            PlayButtonAudio();
            Time.timeScale = 1.0f;
            if (GameManager.Instance != null) GameManager.Instance.ResetGameStats();
            if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene("Level1_CaveEntrance");
            else SceneManager.LoadScene("Level1_CaveEntrance");
        }

        private void OnMusicSliderChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(val);
        }

        private void OnSFXSliderChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(val);
        }

        private void PlayButtonAudio()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickClip, 0.8f);
        }

        // UI Creation Helpers
        public static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return obj;
        }

        public static Text CreateText(Transform parent, string name, string text, Font font, int size, Color color, Vector2 pos, Vector2 dimensions, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dimensions;
            return t;
        }

        public static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 pos, Vector2 dimensions, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.42f, 0.55f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.18f, 0.45f, 0.55f);
            cb.highlightedColor = new Color(0.25f, 0.65f, 0.75f);
            cb.pressedColor = new Color(0.12f, 0.32f, 0.42f);
            btn.colors = cb;
            btn.onClick.AddListener(action);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dimensions;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text t = textObj.AddComponent<Text>();
            t.text = label;
            t.raycastTarget = false;
            t.font = font;
            t.fontSize = 20;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return btn;
        }

        public static Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 dimensions, float defaultVal, UnityEngine.Events.UnityAction<float> action)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            Slider slider = sliderObj.AddComponent<Slider>();

            RectTransform rt = sliderObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dimensions;

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0f, 0.8f, 0.7f);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;

            slider.fillRect = fillRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultVal;
            slider.onValueChanged.AddListener(action);

            return slider;
        }
    }
}
