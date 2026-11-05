using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreasureHunt
{
    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        Victory
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        public GameState currentState = GameState.Playing;
        public int currentCoins = 0;
        public int currentCrystals = 0;
        public float timeElapsed = 0f;
        public string currentLevelName = "Level1_CaveEntrance";

        [Header("Keys Collected")]
        private HashSet<string> acquiredKeys = new HashSet<string>();

        [Header("Checkpoint System")]
        private Vector3 activeCheckpointPosition;
        private Quaternion activeCheckpointRotation;
        private bool hasCheckpoint = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                timeElapsed += Time.deltaTime;
            }

            // Pause toggle via ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                string activeScene = SceneManager.GetActiveScene().name;
                if (activeScene != "MainMenu" && activeScene != "LevelSelect" && activeScene != "Instructions" && activeScene != "GameComplete")
                {
                    TogglePause();
                }
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentLevelName = scene.name;
            hasCheckpoint = false;
            currentState = GameState.Playing;
            Time.timeScale = 1.0f;

            // Find player and establish initial start position as fallback checkpoint
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetCheckpoint(player.transform.position, player.transform.rotation);
            }

            // Background music selection
            if (AudioManager.Instance != null)
            {
                if (scene.name == "GameComplete")
                {
                    AudioManager.Instance.PlayMusic(AudioManager.Instance.victoryThemeClip, false);
                }
                else
                {
                    AudioManager.Instance.PlayMusic(AudioManager.Instance.caveAmbienceClip, true);
                }
            }
        }

        public void AddCoins(int amount)
        {
            currentCoins += amount;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCoins(currentCoins);
            }
        }

        public void AddCrystals(int amount)
        {
            currentCrystals += amount;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCrystals(currentCrystals);
            }
        }

        public void AddKey(string keyID)
        {
            acquiredKeys.Add(keyID);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateKeys(acquiredKeys.Count);
            }
        }

        public bool HasKey(string keyID)
        {
            return acquiredKeys.Contains(keyID);
        }

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            activeCheckpointPosition = position;
            activeCheckpointRotation = rotation;
            hasCheckpoint = true;
        }

        public void RespawnPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                HealthManager hm = player.GetComponent<HealthManager>();

                if (pc != null && hasCheckpoint)
                {
                    pc.TeleportTo(activeCheckpointPosition, activeCheckpointRotation);
                }

                if (hm != null)
                {
                    hm.ResetHealth();
                }

                // Reset level obstacles
                ResetObstaclesInScene();

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HidePrompt();
                }
            }
        }

        public void ResetObstaclesInScene()
        {
            // Reset collapsing bridge tiles
            CollapsingBridgeTile[] bridgeTiles = FindObjectsByType<CollapsingBridgeTile>(FindObjectsInactive.Exclude);
            foreach (var tile in bridgeTiles) tile.ResetTile();

            // Reset falling rocks
            FallingRock[] rocks = FindObjectsByType<FallingRock>(FindObjectsInactive.Exclude);
            foreach (var r in rocks) r.ResetRock();

            // Reset pressure plates
            PressurePlate[] plates = FindObjectsByType<PressurePlate>(FindObjectsInactive.Exclude);
            foreach (var p in plates) p.ResetPlate();
        }

        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                currentState = GameState.Paused;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (UIManager.Instance != null)
                    UIManager.Instance.ShowPauseMenu(true);
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        public void ResumeGame()
        {
            currentState = GameState.Playing;
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (UIManager.Instance != null)
                UIManager.Instance.ShowPauseMenu(false);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1.0f;
            currentState = GameState.Playing;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void CompleteGame()
        {
            currentState = GameState.Victory;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowVictoryScreen(currentCoins, currentCrystals, timeElapsed);
            }
        }

        public void ResetGameStats()
        {
            currentCoins = 0;
            currentCrystals = 0;
            timeElapsed = 0f;
            acquiredKeys.Clear();
            hasCheckpoint = false;
        }
    }
}
