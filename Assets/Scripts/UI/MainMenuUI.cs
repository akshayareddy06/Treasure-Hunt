using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TreasureHunt
{
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        [Header("Menu Canvas & Panels")]
        public GameObject mainPanel;
        public GameObject levelSelectPanel;
        public GameObject controlsPanel;
        public GameObject settingsPanel;
        public GameObject homePanel;
        public bool isAtHome = true;

        [Header("Audio Sliders (Optional)")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // Compatibility stubs for Editor scripts
        public void BuildMainMenuIfMissing() { }
        public void StartGameClicked() => StartRunLevel(1);
        public void QuitGame()
        {
            PlayClickSFX();
            Debug.Log("[MainMenuUI] Quitting game...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void Awake()
        {
            Instance = this;
            EnsureEventSystem();

            // Deactivate old legacy objects (CaveArch, FloorSlab, MenuExplorer)
            CleanLegacyObjects();
        }

        private void Start()
        {
            EnsureEventSystem();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Ensure Player, Camera, Generator, and HUD exist
            EnsureTempleRunRunner();

            // Locate panels if not assigned
            LocateScenePanels();
            FormatLevelSelectLayout();

            // Open clean Main Menu
            OpenMainMenu();

            // Auto-wire all buttons in the scene so every single click works!
            WireAllSceneButtons();

            if (AudioManager.Instance != null && AudioManager.Instance.caveAmbienceClip != null)
            {
                AudioManager.Instance.PlayMusic(AudioManager.Instance.caveAmbienceClip, true);
            }
        }

        private void Update()
        {
            if (isAtHome)
            {
                // Quick start hotkeys from home
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    StartRunLevel(1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha1)) StartRunLevel(1);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) StartRunLevel(2);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) StartRunLevel(3);
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    OpenMainMenu();
                }
            }
        }

        private void CleanLegacyObjects()
        {
            string[] legacyNames = new string[] { "CaveArch", "FloorSlab", "MenuExplorer", "MenuCaveEnvironment" };
            foreach (string name in legacyNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    obj.SetActive(false);
                    Destroy(obj);
                }
            }

            // Also find and destroy any stray spikes, stalactites, or legacy obstacles left in the root scene
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject go in rootObjects)
            {
                if (go != null && (go.name.StartsWith("Spike_") || go.name.StartsWith("Stalactite_") ||
                    go.name == "FloorSlab" || go.name == "CaveArch" || go.name == "MenuExplorer" || go.name == "MenuCaveEnvironment"))
                {
                    go.SetActive(false);
                    Destroy(go);
                }
            }
        }

        private void LocateScenePanels()
        {
            if (mainPanel == null)
            {
                GameObject go = GameObject.Find("MainPanel");
                if (go == null) go = GameObject.Find("MainMenuPanel");
                if (go != null) mainPanel = go;
            }

            if (levelSelectPanel == null)
            {
                GameObject go = GameObject.Find("LevelSelectPanel");
                if (go != null) levelSelectPanel = go;
            }

            if (controlsPanel == null)
            {
                GameObject go = GameObject.Find("ControlsPanel");
                if (go != null) controlsPanel = go;
            }

            if (settingsPanel == null)
            {
                GameObject go = GameObject.Find("SettingsPanel");
                if (go == null) go = GameObject.Find("SoundPanel");
                if (go != null) settingsPanel = go;
            }
        }

        private int DetectLevelFromTransform(Transform t)
        {
            Transform cur = t;
            while (cur != null)
            {
                string n = cur.name.ToLower();
                if (n.Contains("stage1") || n.Contains("level1") || n == "stage1") return 1;
                if (n.Contains("stage2") || n.Contains("level2") || n == "stage2") return 2;
                if (n.Contains("stage3") || n.Contains("level3") || n == "stage3") return 3;
                cur = cur.parent;
            }
            return 1;
        }

        public void WireAllSceneButtons()
        {
            Canvas c = GetComponentInParent<Canvas>();
            if (c == null) c = GetComponentInChildren<Canvas>(true);
            if (c == null)
            {
                GameObject mc = GameObject.Find("MainMenuCanvas");
                if (mc != null) c = mc.GetComponent<Canvas>();
            }

            if (c == null) return;

            // ONLY wire buttons belonging to the Main Menu Canvas!
            // Never touch buttons under EndlessHUD or Game Over dialogs!
            Button[] menuButtons = c.GetComponentsInChildren<Button>(true);
            foreach (Button btn in menuButtons)
            {
                string goName = btn.gameObject.name.ToLower();
                Text txt = btn.GetComponentInChildren<Text>();
                string btnText = (txt != null ? txt.text.ToLower() : "");

                btn.onClick.RemoveAllListeners();

                // 1. Level cards Play buttons (PlayBtn inside Stage1, Stage2, Stage3)
                if (goName.Contains("play") || btnText.Contains("play"))
                {
                    int lvl = DetectLevelFromTransform(btn.transform);
                    btn.onClick.AddListener(() => StartRunLevel(lvl));
                }
                // 2. Start game button from main panel
                else if (goName.Contains("start") || btnText.Contains("start"))
                {
                    btn.onClick.AddListener(() => StartRunLevel(1));
                }
                // 3. Level Select panel opener or individual level buttons
                else if (goName.Contains("levels") || btnText.Contains("level"))
                {
                    if (goName.Contains("1") || btnText.Contains("1") || btn.transform.parent.name.Contains("1"))
                        btn.onClick.AddListener(() => StartRunLevel(1));
                    else if (goName.Contains("2") || btnText.Contains("2") || btn.transform.parent.name.Contains("2"))
                        btn.onClick.AddListener(() => StartRunLevel(2));
                    else if (goName.Contains("3") || btnText.Contains("3") || btn.transform.parent.name.Contains("3"))
                        btn.onClick.AddListener(() => StartRunLevel(3));
                    else
                        btn.onClick.AddListener(OpenLevelSelect);
                }
                // 4. Controls
                else if (goName.Contains("control") || btnText.Contains("control") || btnText.Contains("how to play"))
                {
                    btn.onClick.AddListener(OpenControls);
                }
                // 5. Settings
                else if (goName.Contains("setting") || goName.Contains("sound") || btnText.Contains("sound") || btnText.Contains("setting"))
                {
                    btn.onClick.AddListener(OpenSettings);
                }
                // 6. Quit
                else if (goName.Contains("quit") || btnText.Contains("quit"))
                {
                    btn.onClick.AddListener(QuitGame);
                }
                // 7. Back to Menu / Home
                else if (goName.Contains("back") || btnText.Contains("back") || btnText.Contains("home") || btnText.Contains("close"))
                {
                    btn.onClick.AddListener(OpenMainMenu);
                }
            }

            // Wire audio sliders if present
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveAllListeners();
                masterVolumeSlider.onValueChanged.AddListener((v) => {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(v);
                });
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener((v) => {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
                });
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                sfxVolumeSlider.onValueChanged.AddListener((v) => {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v);
                });
            }
        }

        public void HideMenu()
        {
            isAtHome = false;
            CloseAllPanels();

            Canvas c = GetComponentInParent<Canvas>();
            if (c == null) c = GetComponentInChildren<Canvas>(true);
            if (c == null)
            {
                GameObject mc = GameObject.Find("MainMenuCanvas");
                if (mc != null) c = mc.GetComponent<Canvas>();
            }
            if (c != null) c.gameObject.SetActive(false);
        }

        public void StartRunLevel(int level)
        {
            PlayClickSFX();
            HideMenu();

            if (EndlessHUD.Instance != null)
            {
                EndlessHUD.Instance.CloseAllModals();
                EndlessHUD.Instance.SetHUDVisible(true);
            }

            PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                pc.ResetPlayer();
                HealthManager hm = pc.GetComponent<HealthManager>();
                if (hm != null) hm.ResetHealth();
            }

            if (EndlessCaveGenerator.Instance != null)
            {
                EndlessCaveGenerator.Instance.ResetGenerator(level);
            }

            if (EndlessHUD.Instance != null)
            {
                EndlessHUD.Instance.UpdateLevelDisplay(level);
            }

            Debug.Log($"[MainMenuUI] Starting Temple Run for Level {level}!");
        }

        public void ShowHomeScreen() => OpenMainMenu();

        public void OpenMainMenu()
        {
            PlayClickSFX();
            isAtHome = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Canvas c = GetComponentInParent<Canvas>();
            if (c == null) c = GetComponentInChildren<Canvas>(true);
            if (c == null)
            {
                GameObject mc = GameObject.Find("MainMenuCanvas");
                if (mc != null) c = mc.GetComponent<Canvas>();
            }
            if (c != null) c.gameObject.SetActive(true);

            CloseAllPanels();
            if (mainPanel != null) mainPanel.SetActive(true);

            if (EndlessHUD.Instance != null)
            {
                EndlessHUD.Instance.CloseAllModals();
                EndlessHUD.Instance.SetHUDVisible(false);
            }

            PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                pc.autoRun = false;
            }
        }

        public void OpenLevelSelect()
        {
            PlayClickSFX();
            isAtHome = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EnsureCanvasActive();
            CloseAllPanels();

            if (levelSelectPanel != null)
            {
                FormatLevelSelectLayout();
                levelSelectPanel.SetActive(true);
            }
            if (EndlessHUD.Instance != null)
            {
                EndlessHUD.Instance.CloseAllModals();
                EndlessHUD.Instance.SetHUDVisible(false);
            }

            WireAllSceneButtons();
        }

        private void FormatLevelSelectLayout()
        {
            if (levelSelectPanel == null) return;

            RectTransform panelRt = levelSelectPanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.sizeDelta = new Vector2(740, 560);
            }

            // Clean Title with ample headroom
            Transform title = levelSelectPanel.transform.Find("LSTitle");
            if (title != null)
            {
                RectTransform tRt = title.GetComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0.5f, 1f);
                tRt.anchorMax = new Vector2(0.5f, 1f);
                tRt.pivot = new Vector2(0.5f, 1f);
                tRt.anchoredPosition = new Vector2(0, -20);
                tRt.sizeDelta = new Vector2(700, 48);

                Text t = title.GetComponent<Text>();
                if (t != null)
                {
                    t.fontSize = 32;
                    t.alignment = TextAnchor.MiddleCenter;
                }
            }

            // Cards: Stage 1, Stage 2, Stage 3
            // Position them clearly below the title without any clipping or overlap!
            string[] cardNames = new string[] { "Stage1", "Stage2", "Stage3" };
            float[] cardY = new float[] { -105, -225, -345 };

            for (int i = 0; i < cardNames.Length; i++)
            {
                Transform card = levelSelectPanel.transform.Find(cardNames[i]);
                if (card != null)
                {
                    RectTransform cRt = card.GetComponent<RectTransform>();
                    cRt.anchorMin = new Vector2(0.5f, 1f);
                    cRt.anchorMax = new Vector2(0.5f, 1f);
                    cRt.pivot = new Vector2(0.5f, 0.5f);
                    cRt.anchoredPosition = new Vector2(0, cardY[i]);
                    cRt.sizeDelta = new Vector2(660, 95);
                }
            }

            // Back button
            Transform back = levelSelectPanel.transform.Find("LSBackBtn");
            if (back == null) back = levelSelectPanel.transform.Find("BackBtn");
            if (back != null)
            {
                RectTransform bRt = back.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.anchoredPosition = new Vector2(0, -470);
                bRt.sizeDelta = new Vector2(260, 46);
            }
        }

        public void OpenControls()
        {
            PlayClickSFX();
            isAtHome = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EnsureCanvasActive();
            CloseAllPanels();

            if (controlsPanel != null)
            {
                controlsPanel.SetActive(true);
            }
            if (EndlessHUD.Instance != null)
            {
                EndlessHUD.Instance.CloseAllModals();
                EndlessHUD.Instance.SetHUDVisible(false);
            }

            WireAllSceneButtons();
        }

        public void OpenSettings()
        {
            PlayClickSFX();
            isAtHome = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EnsureCanvasActive();
            CloseAllPanels();

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
            if (EndlessHUD.Instance != null)
            {
                EndlessHUD.Instance.CloseAllModals();
                EndlessHUD.Instance.SetHUDVisible(false);
            }

            WireAllSceneButtons();
        }

        private void EnsureCanvasActive()
        {
            Canvas c = GetComponentInParent<Canvas>();
            if (c == null) c = GetComponentInChildren<Canvas>(true);
            if (c == null)
            {
                GameObject mc = GameObject.Find("MainMenuCanvas");
                if (mc != null) c = mc.GetComponent<Canvas>();
            }
            if (c != null) c.gameObject.SetActive(true);
        }

        public void CloseAllPanels()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (homePanel != null) homePanel.SetActive(false);
        }

        private void PlayClickSFX()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.buttonClickClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickClip, 0.85f);
            }
        }



        public static void EnsureTempleRunRunner()
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("MenuExplorer");
                if (playerObj != null)
                {
                    playerObj.name = "Player";
                }
                else
                {
                    playerObj = new GameObject("Player");
                }
            }

            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(0, 0.5f, 0);
            playerObj.transform.rotation = Quaternion.identity;

            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null) cc = playerObj.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.95f, 0);
            cc.radius = 0.35f;
            cc.height = 1.85f;

            HealthManager hm = playerObj.GetComponent<HealthManager>();
            if (hm == null) hm = playerObj.AddComponent<HealthManager>();

            PlayerVisuals visuals = playerObj.GetComponent<PlayerVisuals>();
            if (visuals == null) visuals = playerObj.AddComponent<PlayerVisuals>();
            visuals.isFemale = true;
            visuals.BuildHumanExplorerModel();

            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc == null) pc = playerObj.AddComponent<PlayerController>();
            pc.autoRun = false; // Start at menu; auto-runs when Play/Start is clicked
            pc.baseForwardSpeed = 10.0f;
            pc.steeringSpeed = 12.0f;
            pc.pathWidthLimit = 3.2f;
            pc.visuals = visuals;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                ThirdPersonCamera tpc = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpc == null) tpc = mainCam.gameObject.AddComponent<ThirdPersonCamera>();
                tpc.target = playerObj.transform;
                tpc.isEndlessRunnerMode = true;
                tpc.runnerOffset = new Vector3(0, 2.8f, -4.8f);
            }

            EndlessCaveGenerator gen = UnityEngine.Object.FindAnyObjectByType<EndlessCaveGenerator>();
            if (gen == null)
            {
                GameObject genObj = new GameObject("EndlessCaveGenerator");
                gen = genObj.AddComponent<EndlessCaveGenerator>();
            }
            gen.player = playerObj.transform;

            EndlessHUD hud = UnityEngine.Object.FindAnyObjectByType<EndlessHUD>();
            if (hud == null)
            {
                GameObject hudObj = new GameObject("EndlessHUD");
                hud = hudObj.AddComponent<EndlessHUD>();
            }
            hud.player = pc;

            Debug.Log("[MainMenuUI] Temple Run Runner ready! Auto-run initialized.");
        }
    }
}
