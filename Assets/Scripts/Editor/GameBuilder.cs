#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreasureHunt.Editor
{
    public static class GameBuilder
    {
        [MenuItem("Treasure Hunt/Build All Game Scenes")]
        public static void BuildAllScenes()
        {
            Debug.Log("Starting Treasure Hunt: Lost Cave build process...");

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

            // 1. Main Menu
            string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            BuildMainMenuScene(mainMenuPath);
            buildScenes.Add(new EditorBuildSettingsScene(mainMenuPath, true));

            // 2. Level Select
            string levelSelectPath = "Assets/Scenes/LevelSelect.unity";
            BuildLevelSelectScene(levelSelectPath);
            buildScenes.Add(new EditorBuildSettingsScene(levelSelectPath, true));

            // 3. Instructions
            string instructionsPath = "Assets/Scenes/Instructions.unity";
            BuildInstructionsScene(instructionsPath);
            buildScenes.Add(new EditorBuildSettingsScene(instructionsPath, true));

            // 4. Level 1 - Cave Entrance
            string level1Path = "Assets/Scenes/Level1_CaveEntrance.unity";
            BuildLevel1Scene(level1Path);
            buildScenes.Add(new EditorBuildSettingsScene(level1Path, true));

            // 5. Level 2 - Crystal Tunnels
            string level2Path = "Assets/Scenes/Level2_CrystalTunnels.unity";
            BuildLevel2Scene(level2Path);
            buildScenes.Add(new EditorBuildSettingsScene(level2Path, true));

            // 6. Level 3 - Treasure Chamber
            string level3Path = "Assets/Scenes/Level3_TreasureChamber.unity";
            BuildLevel3Scene(level3Path);
            buildScenes.Add(new EditorBuildSettingsScene(level3Path, true));

            // 7. Game Complete
            string gameCompletePath = "Assets/Scenes/GameComplete.unity";
            BuildGameCompleteScene(gameCompletePath);
            buildScenes.Add(new EditorBuildSettingsScene(gameCompletePath, true));

            // Register all scenes in Build Settings
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();

            Debug.Log("Successfully built all 7 game scenes and configured Build Settings!");
        }

        [MenuItem("Treasure Hunt/Build Windows Standalone Game")]
        public static void BuildStandalonePlayer()
        {
            Debug.Log("Starting Windows Standalone 64-bit Build...");

            string buildFolder = "Build";
            if (!System.IO.Directory.Exists(buildFolder))
                System.IO.Directory.CreateDirectory(buildFolder);

            string exePath = System.IO.Path.Combine(buildFolder, "TreasureHuntLostCave.exe");
            string[] scenes = new string[] {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/LevelSelect.unity",
                "Assets/Scenes/Instructions.unity",
                "Assets/Scenes/Level1_CaveEntrance.unity",
                "Assets/Scenes/Level2_CrystalTunnels.unity",
                "Assets/Scenes/Level3_TreasureChamber.unity",
                "Assets/Scenes/GameComplete.unity"
            };

            BuildPlayerOptions opt = new BuildPlayerOptions {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opt);
            Debug.Log($"Build Standalone Result: {report.summary.result}, Errors: {report.summary.totalErrors}, Path: {exePath}");
        }

        // Scene 1: Main Menu
        private static void BuildMainMenuScene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting & Atmosphere
            SetupAtmosphere(new Color(0.04f, 0.06f, 0.08f), new Color(0.06f, 0.08f, 0.12f));

            // Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.08f);
            camObj.transform.position = new Vector3(0, 2.5f, -8.0f);
            camObj.transform.rotation = Quaternion.Euler(6.0f, 0, 0);
            camObj.AddComponent<AudioListener>();

            // Cave Entrance Backdrop matching PPT Slide 1 & 5
            GameObject caveGroup = new GameObject("MenuCaveEnvironment");

            // Floor
            CreateFloorSlab(caveGroup.transform, new Vector3(0, 0, 0), new Vector3(16f, 0.6f, 24f));

            // Back cave opening arch
            CreateCaveArch(caveGroup.transform, new Vector3(0, 0, 6f), 12f, 8f);

            // Torches flanking the title
            ProceduralMeshHelper.CreateTorch(caveGroup.transform, new Vector3(-4.5f, 0.5f, -1.0f));
            ProceduralMeshHelper.CreateTorch(caveGroup.transform, new Vector3(4.5f, 0.5f, -1.0f));

            // Glowing Crystal Clusters (Turquoise & Emerald)
            ProceduralMeshHelper.CreateCrystalCluster(caveGroup.transform, "TurquoiseCrystal", 7).transform.position = new Vector3(-3.8f, 0.3f, 2.5f);
            ProceduralMeshHelper.CreateCrystalCluster(caveGroup.transform, "EmeraldCrystal", 7).transform.position = new Vector3(3.8f, 0.3f, 2.5f);

            // Stalactites on ceiling
            CreateStalactites(caveGroup.transform, new Vector3(0, 6.5f, 4f), 10f, 8);

            // Explorer character standing in menu
            GameObject explorer = CreateExplorerModel(caveGroup.transform, new Vector3(0, 0.3f, 0), Quaternion.identity);

            // Managers
            CreateCoreManagers();

            // Menu UI
            GameObject uiObj = new GameObject("MainMenuUI");
            MainMenuUI menuUI = uiObj.AddComponent<MainMenuUI>();

            EditorSceneManager.SaveScene(scene, path);
        }

        // Scene 2: Level Select
        private static void BuildLevelSelectScene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupAtmosphere(new Color(0.04f, 0.06f, 0.08f), new Color(0.06f, 0.08f, 0.12f));

            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.08f);
            camObj.transform.position = new Vector3(0, 2.5f, -8.0f);
            camObj.AddComponent<AudioListener>();

            GameObject cave = new GameObject("Backdrop");
            CreateFloorSlab(cave.transform, Vector3.zero, new Vector3(16f, 0.6f, 24f));
            CreateCaveArch(cave.transform, new Vector3(0, 0, 5f), 12f, 8f);
            ProceduralMeshHelper.CreateTorch(cave.transform, new Vector3(-4.5f, 0.5f, 0));
            ProceduralMeshHelper.CreateTorch(cave.transform, new Vector3(4.5f, 0.5f, 0));

            CreateCoreManagers();

            GameObject uiObj = new GameObject("MainMenuUI");
            MainMenuUI menuUI = uiObj.AddComponent<MainMenuUI>();

            EditorSceneManager.SaveScene(scene, path);
        }

        // Scene 3: Instructions
        private static void BuildInstructionsScene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupAtmosphere(new Color(0.04f, 0.06f, 0.08f), new Color(0.06f, 0.08f, 0.12f));

            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.08f);
            camObj.transform.position = new Vector3(0, 2.5f, -8.0f);
            camObj.AddComponent<AudioListener>();

            GameObject cave = new GameObject("Backdrop");
            CreateFloorSlab(cave.transform, Vector3.zero, new Vector3(16f, 0.6f, 24f));
            CreateCaveArch(cave.transform, new Vector3(0, 0, 5f), 12f, 8f);
            ProceduralMeshHelper.CreateTorch(cave.transform, new Vector3(-4.5f, 0.5f, 0));
            ProceduralMeshHelper.CreateTorch(cave.transform, new Vector3(4.5f, 0.5f, 0));

            CreateCoreManagers();

            GameObject uiObj2 = new GameObject("MainMenuUI");
            MainMenuUI menuUI2 = uiObj2.AddComponent<MainMenuUI>();

            EditorSceneManager.SaveScene(scene, path);
        }

        // Scene 4: Level 1 - Cave Entrance (with Obstacle 1: Collapsing Bridge)
        private static void BuildLevel1Scene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupAtmosphere(new Color(0.05f, 0.07f, 0.09f), new Color(0.12f, 0.14f, 0.16f), 0.02f);

            CreateCoreManagers();

            GameObject level = new GameObject("Level1_Environment");

            // Section 1: Outside Cave Mouth & Starting Pathway (Z: -10 to 20)
            CreateFloorSlab(level.transform, new Vector3(0, 0, 5f), new Vector3(7.0f, 0.8f, 30.0f));
            CreateCaveTunnelWalls(level.transform, 5f, 30f, 7.0f, 6.0f);

            // Torches along start path
            ProceduralMeshHelper.CreateTorch(level.transform, new Vector3(-3.0f, 0.5f, -5.0f));
            ProceduralMeshHelper.CreateTorch(level.transform, new Vector3(3.0f, 0.5f, -5.0f));
            ProceduralMeshHelper.CreateTorch(level.transform, new Vector3(-3.0f, 0.5f, 8.0f));
            ProceduralMeshHelper.CreateTorch(level.transform, new Vector3(3.0f, 0.5f, 8.0f));

            // Gold Coins along start
            CreateCoin(level.transform, new Vector3(0, 1.0f, -2.0f));
            CreateCoin(level.transform, new Vector3(0, 1.0f, 2.0f));
            CreateCoin(level.transform, new Vector3(0, 1.0f, 6.0f));
            CreateCrystal(level.transform, new Vector3(-2.2f, 0.8f, 10.0f), "TurquoiseCrystal");

            // Section 2: Deep Abyss Gap (Z: 20 to 36) with OBSTACLE 1: Collapsing Stone Bridge
            GameObject chasm = new GameObject("ChasmGap");
            chasm.transform.SetParent(level.transform, false);

            // KillZone at bottom of pit
            GameObject killZoneObj = new GameObject("ChasmKillZone");
            killZoneObj.transform.SetParent(chasm.transform, false);
            killZoneObj.transform.position = new Vector3(0, -15.0f, 28.0f);
            BoxCollider kzCol = killZoneObj.AddComponent<BoxCollider>();
            kzCol.size = new Vector3(40.0f, 4.0f, 40.0f);
            kzCol.isTrigger = true;
            killZoneObj.AddComponent<KillZone>();

            // Cave gap walls
            CreateChasmWalls(chasm.transform, 28.0f, 16.0f, 18.0f);

            // Collapsing Bridge Tiles (8 tiles across the gap)
            GameObject bridgeGroup = new GameObject("CollapsingStoneBridge");
            bridgeGroup.transform.SetParent(chasm.transform, false);

            for (int i = 0; i < 8; i++)
            {
                float z = 21.5f + i * 1.8f;
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"BridgeTile_{i + 1}";
                tile.transform.SetParent(bridgeGroup.transform, false);
                tile.transform.position = new Vector3(0, 0, z);
                tile.transform.localScale = new Vector3(3.2f, 0.45f, 1.6f);
                tile.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("BridgeTile");

                CollapsingBridgeTile cbt = tile.AddComponent<CollapsingBridgeTile>();
                cbt.collapseDelay = 0.65f + (i * 0.05f); // Fast collapse delay
            }

            // Section 3: Mid Platform with Checkpoint (Z: 36 to 48)
            CreateFloorSlab(level.transform, new Vector3(0, 0, 42f), new Vector3(8.0f, 0.8f, 14.0f));
            CreateCaveTunnelWalls(level.transform, 42f, 14f, 8.0f, 6.5f);

            // Checkpoint Altar
            CreateCheckpoint(level.transform, new Vector3(0, 0.5f, 38.0f));

            // Torches and Crystals
            ProceduralMeshHelper.CreateTorch(level.transform, new Vector3(-3.4f, 0.5f, 42.0f));
            ProceduralMeshHelper.CreateTorch(level.transform, new Vector3(3.4f, 0.5f, 42.0f));
            ProceduralMeshHelper.CreateCrystalCluster(level.transform, "TurquoiseCrystal", 6).transform.position = new Vector3(-3.0f, 0.4f, 45.0f);

            // Coins
            CreateCoin(level.transform, new Vector3(-1.2f, 1.0f, 43.0f));
            CreateCoin(level.transform, new Vector3(1.2f, 1.0f, 43.0f));

            // Section 4: Jump Platforming over smaller chasms (Z: 48 to 68)
            CreateFloorSlab(level.transform, new Vector3(-1.5f, 0.3f, 52.0f), new Vector3(3.5f, 0.8f, 4.0f));
            CreateCoin(level.transform, new Vector3(-1.5f, 1.3f, 52.0f));

            CreateFloorSlab(level.transform, new Vector3(1.8f, 0.6f, 58.0f), new Vector3(3.5f, 0.8f, 4.0f));
            CreateCrystal(level.transform, new Vector3(1.8f, 1.5f, 58.0f), "EmeraldCrystal");

            CreateFloorSlab(level.transform, new Vector3(0, 0.9f, 65.0f), new Vector3(4.0f, 0.8f, 5.0f));
            CreateCoin(level.transform, new Vector3(0, 1.8f, 65.0f));

            // Section 5: Exit Tunnel to Level 2 (Z: 70 to 82)
            CreateFloorSlab(level.transform, new Vector3(0, 1.0f, 76.0f), new Vector3(7.0f, 0.8f, 14.0f));
            CreateCaveTunnelWalls(level.transform, 76f, 14f, 7.0f, 6.0f);
            ProceduralMeshHelper.CreateTorch(level.transform, new Vector3(-2.8f, 1.5f, 74.0f));
            ProceduralMeshHelper.CreateTorch(level.transform, new Vector3(2.8f, 1.5f, 74.0f));

            // Exit Archway & Transition Trigger
            CreateLevelTransitionArch(level.transform, new Vector3(0, 1.4f, 81.0f), "Level2_CrystalTunnels", "Descending into Crystal Tunnels...");

            // Spawn Player & Camera at start
            SpawnPlayerAndCamera(new Vector3(0, 0.5f, -8.0f));

            // HUD
            CreateInGameUI();

            EditorSceneManager.SaveScene(scene, path);
        }

        // Scene 5: Level 2 - Crystal Tunnels (with Obstacles 2, 3, 4)
        private static void BuildLevel2Scene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupAtmosphere(new Color(0.02f, 0.05f, 0.08f), new Color(0.04f, 0.12f, 0.16f), 0.025f);

            CreateCoreManagers();

            GameObject level = new GameObject("Level2_Environment");

            // KillZone for any falls
            GameObject killZoneObj = new GameObject("Level2KillZone");
            killZoneObj.transform.SetParent(level.transform, false);
            killZoneObj.transform.position = new Vector3(0, -18.0f, 40.0f);
            BoxCollider kzCol = killZoneObj.AddComponent<BoxCollider>();
            kzCol.size = new Vector3(60.0f, 4.0f, 120.0f);
            kzCol.isTrigger = true;
            killZoneObj.AddComponent<KillZone>();

            // Section 1: Entrance Cavern (Z: 0 to 18)
            CreateFloorSlab(level.transform, new Vector3(0, 0, 9f), new Vector3(8.0f, 0.8f, 20.0f));
            CreateCaveTunnelWalls(level.transform, 9f, 20f, 8.0f, 7.0f);

            ProceduralMeshHelper.CreateCrystalCluster(level.transform, "TurquoiseCrystal", 8).transform.position = new Vector3(-3.2f, 0.4f, 5.0f);
            ProceduralMeshHelper.CreateCrystalCluster(level.transform, "EmeraldCrystal", 8).transform.position = new Vector3(3.2f, 0.4f, 12.0f);

            CreateCoin(level.transform, new Vector3(0, 1.0f, 6.0f));
            CreateCoin(level.transform, new Vector3(0, 1.0f, 12.0f));

            // Section 2: OBSTACLE 2: Falling Rocks (Z: 20 to 34)
            CreateFloorSlab(level.transform, new Vector3(0, 0, 27f), new Vector3(7.0f, 0.8f, 16.0f));
            CreateCaveTunnelWalls(level.transform, 27f, 16f, 7.0f, 9.0f); // High ceiling for rocks

            CreateFallingRockObstacle(level.transform, new Vector3(-1.2f, 8.5f, 24.0f), new Vector3(-1.2f, 0.45f, 24.0f));
            CreateFallingRockObstacle(level.transform, new Vector3(1.2f, 8.5f, 30.0f), new Vector3(1.2f, 0.45f, 30.0f));

            // Section 3: Mid Checkpoint & Exploration Alcove with Ancient Key (Z: 34 to 48)
            CreateFloorSlab(level.transform, new Vector3(0, 0, 41f), new Vector3(10.0f, 0.8f, 14.0f));
            CreateCaveTunnelWalls(level.transform, 41f, 14f, 10.0f, 7.0f);

            CreateCheckpoint(level.transform, new Vector3(0, 0.5f, 36.0f));

            // Side alcove holding the Ancient Key
            CreateFloorSlab(level.transform, new Vector3(-7.0f, 0.2f, 41.0f), new Vector3(6.0f, 0.8f, 6.0f));
            ProceduralMeshHelper.CreateCrystalCluster(level.transform, "RubyCrystal", 6).transform.position = new Vector3(-7.5f, 0.6f, 41.0f);
            CreateAncientKey(level.transform, new Vector3(-7.0f, 1.3f, 41.0f));

            // Section 4: OBSTACLE 3: Hidden Pressure Plate & Wall Spikes (Z: 48 to 62)
            CreateFloorSlab(level.transform, new Vector3(0, 0, 55f), new Vector3(6.5f, 0.8f, 16.0f));
            CreateCaveTunnelWalls(level.transform, 55f, 16f, 6.5f, 6.5f);

            CreatePressurePlateTrap(level.transform, new Vector3(0, 0.42f, 54.0f), new Vector3(-3.1f, 0.6f, 54.0f));

            // Section 5: OBSTACLE 4: Swinging Stone Pendulum (Z: 62 to 78)
            CreateFloorSlab(level.transform, new Vector3(0, 0, 70f), new Vector3(5.5f, 0.8f, 16.0f));
            CreateCaveTunnelWalls(level.transform, 70f, 16f, 5.5f, 8.5f);

            CreateSwingingStoneObstacle(level.transform, new Vector3(0, 7.5f, 69.0f));

            // Section 6: Sealed Ancient Door & Level Exit (Z: 78 to 92)
            CreateFloorSlab(level.transform, new Vector3(0, 0, 85f), new Vector3(7.0f, 0.8f, 16.0f));
            CreateCaveTunnelWalls(level.transform, 85f, 16f, 7.0f, 7.0f);

            // Ancient locked door requiring the key found in the alcove
            CreateAncientDoor(level.transform, new Vector3(0, 0.4f, 82.0f));

            // Exit Archway behind the door leading to Level 3
            CreateLevelTransitionArch(level.transform, new Vector3(0, 0.4f, 90.0f), "Level3_TreasureChamber", "Approaching the Treasure Chamber!");

            // Spawn Player & Camera
            SpawnPlayerAndCamera(new Vector3(0, 0.5f, 2.0f));

            // HUD
            CreateInGameUI();

            EditorSceneManager.SaveScene(scene, path);
        }

        // Scene 6: Level 3 - Treasure Chamber (The Climax)
        private static void BuildLevel3Scene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupAtmosphere(new Color(0.08f, 0.06f, 0.03f), new Color(0.2f, 0.15f, 0.08f), 0.015f);

            CreateCoreManagers();

            GameObject chamber = new GameObject("Level3_TreasureChamber");

            // Grand cavern dimensions: 36m wide, 45m long, 14m high
            CreateFloorSlab(chamber.transform, new Vector3(0, 0, 20.0f), new Vector3(36.0f, 1.0f, 48.0f));

            // Grand Cavern Walls
            CreateChamberWalls(chamber.transform, new Vector3(0, 6.0f, 20.0f), 36.0f, 48.0f, 13.0f);

            // Ceiling Spotlights / God Rays shining down on the treasure altar (Matching PPT Slide 4 & 6 & 14)
            CreateCeilingLightBeams(chamber.transform, new Vector3(0, 12.0f, 30.0f));

            // Central Elevated Golden Altar
            CreateAltarStairs(chamber.transform, new Vector3(0, 0.5f, 28.0f));

            // The Legendary Treasure Chest on the altar
            CreateTreasureChest(chamber.transform, new Vector3(0, 2.2f, 30.0f));

            // Mounds of gold coins surrounding the chest (Matching PPT Slide 4 & 6)
            CreateGoldCoinMounds(chamber.transform, new Vector3(0, 1.8f, 30.0f));

            // Large Glowing Crystals surrounding the chamber (Turquoise, Emerald, Ruby, Amber)
            ProceduralMeshHelper.CreateCrystalCluster(chamber.transform, "TurquoiseCrystal", 10).transform.position = new Vector3(-12.0f, 0.5f, 15.0f);
            ProceduralMeshHelper.CreateCrystalCluster(chamber.transform, "EmeraldCrystal", 10).transform.position = new Vector3(12.0f, 0.5f, 15.0f);
            ProceduralMeshHelper.CreateCrystalCluster(chamber.transform, "RubyCrystal", 10).transform.position = new Vector3(-12.0f, 0.5f, 32.0f);
            ProceduralMeshHelper.CreateCrystalCluster(chamber.transform, "TurquoiseCrystal", 10).transform.position = new Vector3(12.0f, 0.5f, 32.0f);

            // Flaming Braziers / Torches flanking the grand carpet pathway
            for (int z = 5; z <= 25; z += 6)
            {
                ProceduralMeshHelper.CreateTorch(chamber.transform, new Vector3(-6.0f, 0.5f, z));
                ProceduralMeshHelper.CreateTorch(chamber.transform, new Vector3(6.0f, 0.5f, z));

                // Golden coins along the path
                CreateCoin(chamber.transform, new Vector3(-1.5f, 1.0f, z));
                CreateCoin(chamber.transform, new Vector3(1.5f, 1.0f, z));
            }

            // Spawn Player & Camera at chamber entrance
            SpawnPlayerAndCamera(new Vector3(0, 0.5f, 1.0f));

            // HUD
            CreateInGameUI();

            EditorSceneManager.SaveScene(scene, path);
        }

        // Scene 7: Game Complete / Victory Screen
        private static void BuildGameCompleteScene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupAtmosphere(new Color(0.08f, 0.06f, 0.03f), new Color(0.25f, 0.2f, 0.1f));

            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.06f, 0.03f);
            camObj.transform.position = new Vector3(0, 3.2f, -5.5f);
            camObj.transform.rotation = Quaternion.Euler(14.0f, 0, 0);
            camObj.AddComponent<AudioListener>();

            GameObject vicEnv = new GameObject("VictoryEnvironment");
            CreateFloorSlab(vicEnv.transform, Vector3.zero, new Vector3(16f, 0.6f, 16f));

            // Open treasure chest in spotlight
            GameObject chest = CreateTreasureChest(vicEnv.transform, new Vector3(0, 0.4f, 0));
            Transform lid = chest.transform.Find("ChestLid");
            if (lid != null) lid.localRotation = Quaternion.Euler(-115f, 0, 0);

            CreateGoldCoinMounds(vicEnv.transform, new Vector3(0, 0.3f, 0));

            ProceduralMeshHelper.CreateTorch(vicEnv.transform, new Vector3(-3.5f, 0.5f, -1.0f));
            ProceduralMeshHelper.CreateTorch(vicEnv.transform, new Vector3(3.5f, 0.5f, -1.0f));

            CreateCoreManagers();

            GameObject uiObj = new GameObject("InGameUI");
            UIManager ui = uiObj.AddComponent<UIManager>();
            ui.BuildUIIfMissing();
            ui.ShowVictoryScreen(GameManager.Instance != null ? GameManager.Instance.currentCoins : 25,
                                  GameManager.Instance != null ? GameManager.Instance.currentCrystals : 8,
                                  GameManager.Instance != null ? GameManager.Instance.timeElapsed : 142f);

            EditorSceneManager.SaveScene(scene, path);
        }

        // Helper Methods
        private static void SetupAtmosphere(Color ambientColor, Color fogColor, float fogDensity = 0.02f)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }

        private static void CreateCoreManagers()
        {
            if (Object.FindAnyObjectByType<GameManager>() == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            if (Object.FindAnyObjectByType<AudioManager>() == null)
            {
                GameObject amObj = new GameObject("AudioManager");
                amObj.AddComponent<AudioManager>();
            }

            if (Object.FindAnyObjectByType<SceneLoader>() == null)
            {
                GameObject slObj = new GameObject("SceneLoader");
                slObj.AddComponent<SceneLoader>();
            }

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private static void CreateInGameUI()
        {
            if (Object.FindAnyObjectByType<UIManager>() == null)
            {
                GameObject uiObj = new GameObject("InGameUI");
                UIManager ui = uiObj.AddComponent<UIManager>();
                ui.BuildUIIfMissing();
            }
        }

        private static void SpawnPlayerAndCamera(Vector3 startPos)
        {
            // Player
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = startPos;

            CharacterController cc = player.AddComponent<CharacterController>();
            PlayerVisuals pv = player.AddComponent<PlayerVisuals>();
            pv.BuildHumanExplorerModel();

            PlayerController pc = player.AddComponent<PlayerController>();
            player.AddComponent<HealthManager>();

            // Third Person Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();

            ThirdPersonCamera tpc = camObj.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;
            pc.cameraTransform = camObj.transform;
        }

        private static GameObject CreateExplorerModel(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject obj = new GameObject("MenuExplorer");
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            PlayerVisuals pv = obj.AddComponent<PlayerVisuals>();
            pv.BuildHumanExplorerModel();
            return obj;
        }

        private static GameObject CreateFloorSlab(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "FloorSlab";
            slab.transform.SetParent(parent, false);
            slab.transform.position = pos;
            slab.transform.localScale = size;
            slab.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveFloor");
            return slab;
        }

        private static void CreateCaveTunnelWalls(Transform parent, float centerZ, float length, float width, float height)
        {
            // Left Wall
            GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.name = "Wall_Left";
            left.transform.SetParent(parent, false);
            left.transform.position = new Vector3(-width * 0.5f - 0.75f, height * 0.5f, centerZ);
            left.transform.localScale = new Vector3(1.5f, height, length);
            left.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");

            // Right Wall
            GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.name = "Wall_Right";
            right.transform.SetParent(parent, false);
            right.transform.position = new Vector3(width * 0.5f + 0.75f, height * 0.5f, centerZ);
            right.transform.localScale = new Vector3(1.5f, height, length);
            right.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");

            // Ceiling with stalactites
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(parent, false);
            ceiling.transform.position = new Vector3(0, height + 0.5f, centerZ);
            ceiling.transform.localScale = new Vector3(width + 3f, 1.0f, length);
            ceiling.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");

            CreateStalactites(ceiling.transform, Vector3.zero, width, Mathf.FloorToInt(length * 0.4f));
        }

        private static void CreateCaveArch(Transform parent, Vector3 pos, float width, float height)
        {
            GameObject arch = new GameObject("CaveArch");
            arch.transform.SetParent(parent, false);
            arch.transform.position = pos;

            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postL.transform.SetParent(arch.transform, false);
            postL.transform.localPosition = new Vector3(-width * 0.5f, height * 0.5f, 0);
            postL.transform.localScale = new Vector3(2.5f, height, 3f);
            postL.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");

            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postR.transform.SetParent(arch.transform, false);
            postR.transform.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0);
            postR.transform.localScale = new Vector3(2.5f, height, 3f);
            postR.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(arch.transform, false);
            top.transform.localPosition = new Vector3(0, height + 0.5f, 0);
            top.transform.localScale = new Vector3(width + 2.5f, 2f, 3f);
            top.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");
        }

        private static void CreateChasmWalls(Transform parent, float centerZ, float length, float depth)
        {
            GameObject lWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lWall.name = "ChasmLeft";
            lWall.transform.SetParent(parent, false);
            lWall.transform.position = new Vector3(-6.0f, -depth * 0.5f, centerZ);
            lWall.transform.localScale = new Vector3(3f, depth, length);
            lWall.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");

            GameObject rWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rWall.name = "ChasmRight";
            rWall.transform.SetParent(parent, false);
            rWall.transform.position = new Vector3(6.0f, -depth * 0.5f, centerZ);
            rWall.transform.localScale = new Vector3(3f, depth, length);
            rWall.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");
        }

        private static void CreateChamberWalls(Transform parent, Vector3 center, float width, float length, float height)
        {
            // Perimeter walls
            CreateWall(parent, new Vector3(-width * 0.5f, center.y, center.z), new Vector3(2f, height, length));
            CreateWall(parent, new Vector3(width * 0.5f, center.y, center.z), new Vector3(2f, height, length));
            CreateWall(parent, new Vector3(0, center.y, center.z + length * 0.5f), new Vector3(width + 2f, height, 2f));
            CreateWall(parent, new Vector3(0, center.y, center.z - length * 0.5f), new Vector3(width + 2f, height, 2f));

            // Ceiling
            CreateWall(parent, new Vector3(0, center.y + height * 0.5f, center.z), new Vector3(width + 4f, 2f, length + 4f));
        }

        private static GameObject CreateWall(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = size;
            wall.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");
            return wall;
        }

        private static void CreateStalactites(Transform parent, Vector3 localCenter, float width, int count)
        {
            Mesh coneMesh = ProceduralMeshHelper.CreateCone(0.35f, 2.2f, 7, true);
            Material mat = MaterialFactory.GetMaterial("CaveWall");

            for (int i = 0; i < count; i++)
            {
                GameObject stalactite = new GameObject("Stalactite_" + i);
                stalactite.transform.SetParent(parent, false);
                float x = Random.Range(-width * 0.45f, width * 0.45f);
                float z = Random.Range(-1.5f, 1.5f);
                stalactite.transform.localPosition = localCenter + new Vector3(x, 0, z);

                MeshFilter mf = stalactite.AddComponent<MeshFilter>();
                mf.sharedMesh = coneMesh;
                MeshRenderer mr = stalactite.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                float scaleY = Random.Range(0.7f, 1.4f);
                stalactite.transform.localScale = new Vector3(Random.Range(0.8f, 1.2f), scaleY, Random.Range(0.8f, 1.2f));
            }
        }

        private static void CreateCeilingLightBeams(Transform parent, Vector3 center)
        {
            // Dramatic light rays shining down
            for (int i = -2; i <= 2; i++)
            {
                GameObject beamObj = new GameObject("GodRaySpotlight_" + i);
                beamObj.transform.SetParent(parent, false);
                beamObj.transform.position = center + new Vector3(i * 3.5f, 0, (i % 2) * 1.5f);
                beamObj.transform.rotation = Quaternion.Euler(75.0f, i * 4.0f, 0);

                Light l = beamObj.AddComponent<Light>();
                l.type = LightType.Spot;
                l.color = new Color(1.0f, 0.92f, 0.65f);
                l.spotAngle = 42.0f;
                l.range = 28.0f;
                l.intensity = 5.5f;
                l.shadows = LightShadows.Soft;
            }
        }

        private static void CreateAltarStairs(Transform parent, Vector3 basePos)
        {
            // 3-tiered golden stone steps
            CreateFloorSlab(parent, basePos, new Vector3(14.0f, 0.5f, 12.0f));
            CreateFloorSlab(parent, basePos + new Vector3(0, 0.5f, 1.0f), new Vector3(10.0f, 0.5f, 9.0f));
            CreateFloorSlab(parent, basePos + new Vector3(0, 1.0f, 2.0f), new Vector3(6.5f, 0.5f, 6.0f));
        }

        private static GameObject CreateTreasureChest(Transform parent, Vector3 pos)
        {
            GameObject chest = new GameObject("TreasureChest");
            chest.transform.SetParent(parent, false);
            chest.transform.position = pos;

            // Chest Base
            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "ChestBase";
            baseBox.transform.SetParent(chest.transform, false);
            baseBox.transform.localPosition = new Vector3(0, 0.35f, 0);
            baseBox.transform.localScale = new Vector3(1.6f, 0.7f, 1.0f);
            baseBox.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TreasureChestWood");

            // Chest Lid (Hinged at back)
            GameObject lidPivot = new GameObject("ChestLid");
            lidPivot.transform.SetParent(chest.transform, false);
            lidPivot.transform.localPosition = new Vector3(0, 0.7f, -0.5f); // Pivot at back

            GameObject lidMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lidMesh.name = "LidMesh";
            lidMesh.transform.SetParent(lidPivot.transform, false);
            lidMesh.transform.localPosition = new Vector3(0, 0.15f, 0.5f);
            lidMesh.transform.localScale = new Vector3(1.64f, 0.35f, 1.05f);
            lidMesh.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TreasureChestWood");

            // Golden Trim & Latch
            GameObject latch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            latch.name = "GoldLatch";
            latch.transform.SetParent(lidMesh.transform, false);
            latch.transform.localPosition = new Vector3(0, -0.15f, 0.53f);
            latch.transform.localScale = new Vector3(0.25f, 0.25f, 0.1f);
            latch.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TreasureChestGold");

            // Golden Glow Light inside chest
            GameObject lightObj = new GameObject("ChestGlowLight");
            lightObj.transform.SetParent(chest.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 0.8f, 0);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.82f, 0.25f);
            l.range = 8.0f;
            l.intensity = 2.0f;

            TreasureGoal goal = chest.AddComponent<TreasureGoal>();
            goal.chestLid = lidPivot.transform;
            goal.chestGoldenLight = l;

            return chest;
        }

        private static void CreateGoldCoinMounds(Transform parent, Vector3 center)
        {
            Material goldMat = MaterialFactory.GetMaterial("Gold");

            for (int i = 0; i < 28; i++)
            {
                GameObject pile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pile.name = "CoinPile_" + i;
                pile.transform.SetParent(parent, false);

                float angle = i * (Mathf.PI * 2f / 28);
                float dist = Random.Range(1.2f, 3.8f);
                float x = Mathf.Cos(angle) * dist;
                float z = Mathf.Sin(angle) * dist;

                pile.transform.position = center + new Vector3(x, 0.1f, z);
                pile.transform.localScale = new Vector3(Random.Range(0.6f, 1.2f), Random.Range(0.15f, 0.35f), Random.Range(0.6f, 1.2f));
                pile.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
                Object.DestroyImmediate(pile.GetComponent<Collider>());
            }
        }

        private static void CreateFallingRockObstacle(Transform parent, Vector3 rockSpawnPos, Vector3 groundTargetPos)
        {
            GameObject rockRoot = new GameObject("FallingRockObstacle");
            rockRoot.transform.SetParent(parent, false);
            rockRoot.transform.position = rockSpawnPos;

            // Trigger Zone across path
            GameObject trig = new GameObject("RockTrigger");
            trig.transform.SetParent(rockRoot.transform, false);
            trig.transform.position = new Vector3(groundTargetPos.x, groundTargetPos.y + 1f, groundTargetPos.z - 4f);
            BoxCollider col = trig.AddComponent<BoxCollider>();
            col.size = new Vector3(5f, 3f, 4f);
            col.isTrigger = true;

            // Boulder Mesh
            GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = "RockBoulder";
            boulder.transform.SetParent(rockRoot.transform, false);
            boulder.transform.position = rockSpawnPos;
            boulder.transform.localScale = new Vector3(1.8f, 1.6f, 1.8f);
            boulder.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");

            // Red warning ground decal
            GameObject warningDecal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            warningDecal.name = "WarningCircle";
            warningDecal.transform.SetParent(rockRoot.transform, false);
            warningDecal.transform.position = groundTargetPos;
            warningDecal.transform.localScale = new Vector3(2.4f, 0.02f, 2.4f);
            warningDecal.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("WarningIndicator");
            Object.DestroyImmediate(warningDecal.GetComponent<Collider>());

            FallingRock fr = trig.AddComponent<FallingRock>();
            fr.rockMesh = boulder.transform;
            fr.groundWarningIndicator = warningDecal;
        }

        private static void CreatePressurePlateTrap(Transform parent, Vector3 platePos, Vector3 trapPos)
        {
            GameObject trapRoot = new GameObject("PressurePlateTrapGroup");
            trapRoot.transform.SetParent(parent, false);

            // Pressure Plate floor tile
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "PressurePlate";
            plate.transform.SetParent(trapRoot.transform, false);
            plate.transform.position = platePos;
            plate.transform.localScale = new Vector3(1.8f, 0.12f, 1.8f);
            plate.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("PressurePlate");

            BoxCollider plateCol = plate.GetComponent<BoxCollider>();
            plateCol.isTrigger = true;

            // Wall Spikes
            GameObject spikes = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spikes.name = "WallSpikes";
            spikes.transform.SetParent(trapRoot.transform, false);
            spikes.transform.position = trapPos;
            spikes.transform.localScale = new Vector3(0.5f, 1.4f, 2.0f);
            spikes.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrapSpike");

            TrapSpikes ts = spikes.AddComponent<TrapSpikes>();
            ts.extendedOffset = new Vector3(3.2f, 0, 0); // Thrusts into the path

            PressurePlate pp = plate.AddComponent<PressurePlate>();
            pp.connectedTrap = ts;
        }

        private static void CreateSwingingStoneObstacle(Transform parent, Vector3 ceilingPos)
        {
            GameObject pend = new GameObject("SwingingStoneObstacle");
            pend.transform.SetParent(parent, false);
            pend.transform.position = ceilingPos;

            // Arm/Chain
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.name = "Chain";
            arm.transform.SetParent(pend.transform, false);
            arm.transform.localPosition = new Vector3(0, -3.2f, 0);
            arm.transform.localScale = new Vector3(0.12f, 3.2f, 0.12f);
            arm.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrapSpike");
            Object.DestroyImmediate(arm.GetComponent<Collider>());

            // Heavy Boulder / Spiked Stone at bottom
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = "StoneBob";
            stone.transform.SetParent(pend.transform, false);
            stone.transform.localPosition = new Vector3(0, -6.5f, 0);
            stone.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
            stone.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("SwingingStone");

            SphereCollider sc = stone.GetComponent<SphereCollider>();
            sc.isTrigger = true;

            SwingingStone ss = stone.AddComponent<SwingingStone>();
            ss.pivot = pend.transform;
            ss.stoneBob = stone.transform;
            ss.swingAngle = 55.0f;
            ss.swingSpeed = 2.2f;
        }

        private static GameObject CreateAncientDoor(Transform parent, Vector3 pos)
        {
            GameObject doorRoot = new GameObject("AncientDoorPortal");
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.position = pos;

            // Frame
            CreateCaveArch(doorRoot.transform, Vector3.zero, 6.0f, 6.0f);

            // Door Slab
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "DoorSlab";
            slab.transform.SetParent(doorRoot.transform, false);
            slab.transform.localPosition = new Vector3(0, 3.0f, 0);
            slab.transform.localScale = new Vector3(5.8f, 5.8f, 0.8f);
            slab.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveWall");

            AncientDoor ad = doorRoot.AddComponent<AncientDoor>();
            ad.doorSlab = slab.transform;
            ad.openHeight = 6.0f;

            return doorRoot;
        }

        private static void CreateAncientKey(Transform parent, Vector3 pos)
        {
            GameObject keyRoot = new GameObject("AncientKey");
            keyRoot.transform.SetParent(parent, false);
            keyRoot.transform.position = pos;

            GameObject keyMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            keyMesh.transform.SetParent(keyRoot.transform, false);
            keyMesh.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            keyMesh.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            keyMesh.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("Gold");

            BoxCollider bc = keyRoot.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.2f, 1.2f, 1.2f);
            bc.isTrigger = true;

            // Golden Light
            GameObject lObj = new GameObject("KeyLight");
            lObj.transform.SetParent(keyRoot.transform, false);
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.85f, 0.2f);
            l.range = 5.0f;
            l.intensity = 2.5f;

            keyRoot.AddComponent<AncientKey>();
        }

        private static void CreateCoin(Transform parent, Vector3 pos)
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "GoldCoin";
            coin.transform.SetParent(parent, false);
            coin.transform.position = pos;
            coin.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
            coin.transform.rotation = Quaternion.Euler(90f, 0, 0);
            coin.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("Gold");

            Collider col = coin.GetComponent<Collider>();
            col.isTrigger = true;

            coin.AddComponent<CollectibleCoin>();
        }

        private static void CreateCrystal(Transform parent, Vector3 pos, string type)
        {
            GameObject crystal = new GameObject("Crystal_" + type);
            crystal.transform.SetParent(parent, false);
            crystal.transform.position = pos;

            Mesh crystalMesh = ProceduralMeshHelper.CreateCrystalMesh(0.4f, 1.6f, 6);
            MeshFilter mf = crystal.AddComponent<MeshFilter>();
            mf.sharedMesh = crystalMesh;
            MeshRenderer mr = crystal.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialFactory.GetMaterial(type);

            BoxCollider bc = crystal.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.0f, 1.8f, 1.0f);
            bc.isTrigger = true;

            GameObject lObj = new GameObject("Light");
            lObj.transform.SetParent(crystal.transform, false);
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = mr.sharedMaterial.color;
            l.range = 5.0f;
            l.intensity = 2.5f;

            crystal.AddComponent<CollectibleCrystal>();
        }

        private static void CreateCheckpoint(Transform parent, Vector3 pos)
        {
            GameObject cp = new GameObject("CheckpointPillar");
            cp.transform.SetParent(parent, false);
            cp.transform.position = pos;

            // Pillar
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(cp.transform, false);
            pillar.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);
            pillar.transform.localPosition = new Vector3(0, 1.2f, 0);
            pillar.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveFloor");

            // Beacon Crystal
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.transform.SetParent(cp.transform, false);
            beacon.transform.localPosition = new Vector3(0, 2.5f, 0);
            beacon.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
            beacon.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("CaveFloor");
            Object.DestroyImmediate(beacon.GetComponent<Collider>());

            // Light
            GameObject lObj = new GameObject("CheckpointLight");
            lObj.transform.SetParent(cp.transform, false);
            lObj.transform.localPosition = new Vector3(0, 2.6f, 0);
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 7.0f;
            l.enabled = false;

            BoxCollider bc = cp.AddComponent<BoxCollider>();
            bc.size = new Vector3(3.5f, 3.5f, 3.5f);
            bc.isTrigger = true;

            Checkpoint comp = cp.AddComponent<Checkpoint>();
            comp.checkpointLight = l;
            comp.beaconRenderer = beacon.GetComponent<Renderer>();
        }

        private static void CreateLevelTransitionArch(Transform parent, Vector3 pos, string nextScene, string prompt)
        {
            GameObject arch = new GameObject("LevelTransitionArch");
            arch.transform.SetParent(parent, false);
            arch.transform.position = pos;

            CreateCaveArch(arch.transform, Vector3.zero, 5.0f, 5.5f);

            // Glowing Portal Quad / Cube
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portal.name = "PortalGlow";
            portal.transform.SetParent(arch.transform, false);
            portal.transform.localPosition = new Vector3(0, 2.6f, 0);
            portal.transform.localScale = new Vector3(4.8f, 5.0f, 0.6f);
            portal.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TurquoiseCrystal");

            BoxCollider bc = portal.GetComponent<BoxCollider>();
            bc.isTrigger = true;

            LevelTransitionTrigger trig = portal.AddComponent<LevelTransitionTrigger>();
            trig.nextSceneName = nextScene;
            trig.transitionPrompt = prompt;
        }
    }
}
#endif
