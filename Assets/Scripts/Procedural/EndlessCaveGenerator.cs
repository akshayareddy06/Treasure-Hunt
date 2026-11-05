using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreasureHunt
{
    public class RockTriggerHelper : MonoBehaviour
    {
        public FallingRock rock;
        private void OnTriggerEnter(Collider other)
        {
            if (rock != null && (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null))
            {
                rock.TriggerFall();
            }
        }
    }

    public class EndlessCaveGenerator : MonoBehaviour
    {
        [Header("Player & Generation Settings")]
        public Transform player;
        public float chunkLength = 25.0f;
        public int initialChunks = 7;
        public float pathWidth = 7.0f;
        public float wallHeight = 6.5f;

        [Header("Levels (PPT Slide 6: 1. Cave Entrance, 2. Crystal Tunnels, 3. Treasure Chamber)")]
        public int currentLevel = 1;
        public float level1Length = 200.0f;
        public float level2Length = 250.0f;
        public float level3Length = 300.0f;

        private float levelStartZ = 0f;
        private Vector3 nextSpawnPos = Vector3.zero;
        private float currentHeadingYaw = 0f;
        private int chunksSinceLastTurn = 0;
        private TurnDirection nextTurnDirection = TurnDirection.Left;
        private readonly List<GameObject> activeChunks = new List<GameObject>();
        private int chunkIndex = 0;
        private bool hasSpawnedTreasureChamberGoal = false;
        private bool levelCompleted = false;
        private float levelChunkZ = 0f;

        public static EndlessCaveGenerator Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            SetupCavernAtmosphere();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeForActiveScene()
        {
            if (Object.FindAnyObjectByType<PlayerController>() != null)
            {
                if (Object.FindAnyObjectByType<EndlessCaveGenerator>() == null)
                {
                    GameObject genObj = new GameObject("EndlessCaveGenerator");
                    genObj.AddComponent<EndlessCaveGenerator>();
                }

                if (Object.FindAnyObjectByType<EndlessHUD>() == null)
                {
                    GameObject hudObj = new GameObject("EndlessHUD");
                    hudObj.AddComponent<EndlessHUD>();
                }
            }
        }

        private void Start()
        {
            if (player == null)
            {
                PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
            }

            SetupCavernAtmosphere();

            // Spawn initial safe runway
            for (int i = 0; i < initialChunks; i++)
            {
                SpawnChunk(i < 2);
            }
        }

        private void Update()
        {
            if (player == null)
            {
                PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
                if (player == null) return;
            }

            // Keyboard level jumping: Press 1, 2, or 3 anytime!
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToLevel(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToLevel(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToLevel(3);

            // Level progression checks
            CheckLevelProgression();

            // Stream chunks ahead in the current heading direction
            while (Vector3.Distance(nextSpawnPos, player.position) < chunkLength * initialChunks)
            {
                SpawnChunk(false);
            }

            // Clean up chunks behind player regardless of heading
            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                GameObject chunk = activeChunks[i];
                if (chunk != null)
                {
                    Vector3 toChunk = chunk.transform.position - player.position;
                    if (Vector3.Dot(toChunk, player.forward) < -25.0f && Vector3.Distance(chunk.transform.position, player.position) > 40.0f)
                    {
                        activeChunks.RemoveAt(i);
                        Destroy(chunk);
                    }
                }
            }
        }

        public void SwitchToLevel(int lvl)
        {
            currentLevel = Mathf.Clamp(lvl, 1, 3);
            levelCompleted = false;
            hasSpawnedTreasureChamberGoal = false;
            levelChunkZ = 0f;

            PlayerController pc = (player != null) ? player.GetComponent<PlayerController>() : null;
            levelStartZ = (pc != null) ? pc.DistanceTraveled : 0f;

            if (currentLevel == 1)
            {
                if (pc != null) { pc.DeactivateJetpack(); pc.DeactivateTrolley(); }
            }
            else if (currentLevel == 2)
            {
                if (pc != null) pc.ActivateJetpack(7.5f);
            }
            else if (currentLevel == 3)
            {
                if (pc != null) pc.ActivateTrolley();
            }

            // Clear existing chunks ahead of player
            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                if (activeChunks[i] != null && player != null)
                {
                    Vector3 toChunk = activeChunks[i].transform.position - player.position;
                    if (Vector3.Dot(toChunk, player.forward) > 2.0f)
                    {
                        Destroy(activeChunks[i]);
                        activeChunks.RemoveAt(i);
                    }
                }
            }

            // When shifting to Level 2 (or any level change), strip ALL arches, pillars, stalactites, and obstacles
            // from any remaining chunks behind or under the player so the takeoff runway and airspace are 100% CLEAR!
            foreach (GameObject ch in activeChunks)
            {
                if (ch == null) continue;
                for (int c = ch.transform.childCount - 1; c >= 0; c--)
                {
                    Transform child = ch.transform.GetChild(c);
                    string cName = child.name;
                    if (cName.Contains("Arch") || cName.Contains("Pillar") || cName.Contains("Stalactite") ||
                        cName.Contains("Spike") || cName.Contains("Rock") || cName.Contains("Stone") ||
                        cName.Contains("Bridge") || cName.Contains("Trap") || cName.Contains("Obstacle") ||
                        cName.Contains("Sign"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            // Align nextSpawnPos seamlessly at the end of the current chunk so chunks NEVER overlap or clip
            if (activeChunks.Count > 0 && activeChunks[activeChunks.Count - 1] != null)
            {
                GameObject currentChunk = activeChunks[activeChunks.Count - 1];
                nextSpawnPos = currentChunk.transform.position + currentChunk.transform.forward * chunkLength;
                currentHeadingYaw = currentChunk.transform.eulerAngles.y;
            }
            else if (player != null)
            {
                nextSpawnPos = player.position + player.forward * chunkLength;
                currentHeadingYaw = player.eulerAngles.y;
            }

            chunksSinceLastTurn = 0;
            nextTurnDirection = TurnDirection.Left;

            // Immediately replenish active chunks ahead with clear, vaulted airspace!
            while (activeChunks.Count < initialChunks)
            {
                SpawnChunk(false);
            }

            SetupCavernAtmosphere();

            EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
            if (hud != null) hud.UpdateLevelDisplay(currentLevel);

            Debug.Log($"[EndlessCaveGenerator] Switched to Level {currentLevel}");
        }

        public void ResetGenerator(int level = 1)
        {
            currentLevel = Mathf.Clamp(level, 1, 3);
            levelCompleted = false;
            hasSpawnedTreasureChamberGoal = false;
            levelStartZ = 0f;
            levelChunkZ = 0f;

            // Destroy ALL active chunks completely
            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                if (activeChunks[i] != null)
                {
                    Destroy(activeChunks[i]);
                }
            }
            activeChunks.Clear();

            // Destroy any leftover chunk or obstacle objects
            GameObject[] allGos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (GameObject go in allGos)
            {
                if (go != null && (go.name.StartsWith("CaveChunk_") || go.name.Contains("FallingRock") || go.name.Contains("TrapSpike") || go.name.Contains("SwingingStone") || go.name.Contains("Railway") || go.name.Contains("DirectionSign") || go.name.Contains("SkyCoin")))
                {
                    Destroy(go);
                }
            }

            nextSpawnPos = Vector3.zero;
            currentHeadingYaw = 0f;
            chunksSinceLastTurn = 0;
            nextTurnDirection = TurnDirection.Left;
            chunkIndex = 0;

            PlayerController pc = (player != null) ? player.GetComponent<PlayerController>() : null;
            if (pc != null)
            {
                if (currentLevel == 1) { pc.DeactivateJetpack(); pc.DeactivateTrolley(); }
                else if (currentLevel == 2) pc.ActivateJetpack(7.5f);
                else if (currentLevel == 3) pc.ActivateTrolley();
            }

            SetupCavernAtmosphere();

            // Spawn fresh safe runway from (0,0,0)
            for (int i = 0; i < initialChunks; i++)
            {
                SpawnChunk(i < 2);
            }

            EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
            if (hud != null) hud.UpdateLevelDisplay(currentLevel);

            Debug.Log($"[EndlessCaveGenerator] Cleanly reset generator for Level {currentLevel} from (0,0,0)!");
        }

        private void CheckLevelProgression()
        {
            if (player == null) return;
            PlayerController pc = player.GetComponent<PlayerController>();
            float totalDist = (pc != null) ? pc.DistanceTraveled : (player.position.z - levelStartZ);
            float distInLevel = totalDist - levelStartZ;

            if (currentLevel == 1 && distInLevel >= level1Length && !levelCompleted)
            {
                levelCompleted = true;
                SwitchToLevel(2);
                if (pc != null) pc.ActivateJetpack(7.5f);
                EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
                if (hud != null) hud.ShowLevelNotification("★ LEVEL 1 COMPLETED! ★", "ROCKET JETPACK ENGAGED — SOAR INTO CRYSTAL CAVERN!", 4.0f);
            }
            else if (currentLevel == 2 && distInLevel >= level2Length && !levelCompleted)
            {
                levelCompleted = true;
                SwitchToLevel(3);
                if (pc != null) pc.ActivateTrolley();
                EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
                if (hud != null) hud.ShowLevelNotification("★ LEVEL 2 COMPLETED! ★", "MINING TROLLEY ENGAGED — RIDE THE ANCIENT RAILS!", 4.0f);
            }
        }

        private void SetupCavernAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            switch (currentLevel)
            {
                case 1: // Cave Entrance: Deep subterranean slate & warm torches
                    RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.18f);
                    RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.08f);
                    RenderSettings.fogDensity = 0.018f;
                    break;

                case 2: // Crystal Tunnels: Luminous Turquoise & Emerald glow
                    RenderSettings.ambientLight = new Color(0.05f, 0.22f, 0.20f);
                    RenderSettings.fogColor = new Color(0.02f, 0.08f, 0.10f);
                    RenderSettings.fogDensity = 0.022f;
                    break;

                case 3: // Treasure Chamber: Warm Ancient Gold & Ruby Vault
                    RenderSettings.ambientLight = new Color(0.25f, 0.18f, 0.08f);
                    RenderSettings.fogColor = new Color(0.10f, 0.07f, 0.03f);
                    RenderSettings.fogDensity = 0.015f;
                    break;
            }
        }

        private void SpawnChunk(bool isSafeRunway)
        {
            GameObject chunk = new GameObject($"CaveChunk_L{currentLevel}_{chunkIndex:D4}");
            chunk.transform.SetParent(transform);
            chunk.transform.position = nextSpawnPos;
            chunk.transform.rotation = Quaternion.Euler(0, currentHeadingYaw, 0);

            PlayerController pc = (player != null) ? player.GetComponent<PlayerController>() : null;
            float totalDist = (pc != null) ? pc.DistanceTraveled : 0f;
            float chunkDistInLevel = totalDist - levelStartZ;

            float thisChunkZ = levelChunkZ;
            levelChunkZ += chunkLength;

            // In Level 2:
            // 0m to 50m: High Altitude Jetpack Soaring (vaulted corridor, airborne coins at y = 5.2m)
            // 50m to 75m: Jetpack Coin Descent Ramp (vaulted corridor, coins sloping down 5.2m -> 1.0m as player descends)
            // 75m to 100m: Safe Ground Landing Runway (vaulted corridor, coins on ground at y = 1.0m)
            // 100m+: On Foot Crystal Caves with ground obstacles and coins at ground y = 1.0m
            bool isHighSkyFlight = (currentLevel == 2 && thisChunkZ < 50.0f);
            bool isCoinDescentRamp = (currentLevel == 2 && thisChunkZ >= 50.0f && thisChunkZ < 75.0f);
            bool isGroundLandingRunway = (currentLevel == 2 && thisChunkZ >= 75.0f && thisChunkZ < 100.0f);
            bool isAnySkyCorridor = (isHighSkyFlight || isCoinDescentRamp || isGroundLandingRunway);

            // Check if this chunk should be a Turn Chunk! (Never spawn turns during the sky corridor or altar)
            bool shouldTurn = !isSafeRunway && !isAnySkyCorridor && chunksSinceLastTurn >= 4 && !hasSpawnedTreasureChamberGoal;

            if (currentLevel == 3 && chunkDistInLevel >= (level3Length - 30.0f) && !hasSpawnedTreasureChamberGoal)
            {
                hasSpawnedTreasureChamberGoal = true;
                BuildTreasureChamberAltar(chunk);
                Vector3 forward = Quaternion.Euler(0, currentHeadingYaw, 0) * Vector3.forward;
                nextSpawnPos += forward * chunkLength;
                chunksSinceLastTurn++;
            }
            else if (isHighSkyFlight)
            {
                // 100% CLEAR AIRSPACE: High vaulted crystal skyway, ZERO arches, ZERO stalactites, airborne coins at y = 5.2m!
                BuildSkyFlightCorridor(chunk);
                BuildSkyCoinRun(chunk);
                Vector3 forward = Quaternion.Euler(0, currentHeadingYaw, 0) * Vector3.forward;
                nextSpawnPos += forward * chunkLength;
                chunksSinceLastTurn++;
            }
            else if (isCoinDescentRamp)
            {
                // Vaulted corridor with coins sloping smoothly down from 5.2m to 1.0m as player descends!
                BuildSkyFlightCorridor(chunk);
                BuildDescentCoinRun(chunk);
                Vector3 forward = Quaternion.Euler(0, currentHeadingYaw, 0) * Vector3.forward;
                nextSpawnPos += forward * chunkLength;
                chunksSinceLastTurn++;
            }
            else if (isGroundLandingRunway)
            {
                // Safe ground runway: player has landed on foot, coins are at ground level y = 1.0m!
                BuildSkyFlightCorridor(chunk);
                SpawnCoinArc(chunk, new Vector3(0, 1.0f, chunkLength * 0.4f), 5);
                Vector3 forward = Quaternion.Euler(0, currentHeadingYaw, 0) * Vector3.forward;
                nextSpawnPos += forward * chunkLength;
                chunksSinceLastTurn++;
            }
            else if (shouldTurn)
            {
                bool isLeft = (nextTurnDirection == TurnDirection.Left);
                BuildTurnChunk(chunk, isLeft);
                chunksSinceLastTurn = 0;
                nextTurnDirection = isLeft ? TurnDirection.Right : TurnDirection.Left; // Alternate to avoid self-overlap!
            }
            else if (isSafeRunway)
            {
                BuildSafeCorridor(chunk);
                Vector3 forward = Quaternion.Euler(0, currentHeadingYaw, 0) * Vector3.forward;
                nextSpawnPos += forward * chunkLength;
                chunksSinceLastTurn++;
            }
            else
            {
                switch (currentLevel)
                {
                    case 1: // Level 1: Collapsing Bridge + Cave Entrance
                        if (Random.value < 0.55f) BuildCollapsingBridgeSegment(chunk);
                        else BuildSafeCorridor(chunk);
                        break;

                    case 2: // Level 2: Crystal Tunnels + Falling Rocks + Pressure Plates (on foot after jetpack)
                        float r2 = Random.value;
                        if (r2 < 0.40f) BuildFallingRocksSegment(chunk);
                        else if (r2 < 0.75f) BuildPressurePlateSegment(chunk);
                        else BuildCrystalCorridorSegment(chunk);
                        break;

                    case 3: // Level 3: Treasure Chamber + Swinging Stone Pendulums
                        float r3 = Random.value;
                        if (r3 < 0.50f) BuildSwingingPendulumsSegment(chunk);
                        else if (r3 < 0.75f) BuildCollapsingBridgeSegment(chunk);
                        else BuildVaultCorridorSegment(chunk);
                        break;
                }

                Vector3 forward = Quaternion.Euler(0, currentHeadingYaw, 0) * Vector3.forward;
                nextSpawnPos += forward * chunkLength;
                chunksSinceLastTurn++;
            }

            activeChunks.Add(chunk);
            chunkIndex++;
        }

        // -------------------------------------------------------------
        // PPT THEME: Authentic Cave Architecture (Walls, Stalactites, Arches)
        // -------------------------------------------------------------
        private void BuildCaveShell(GameObject chunk, string wallMat, string floorMat)
        {
            // 1. Rugged Textured Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "StoneFloor";
            floor.transform.SetParent(chunk.transform, false);
            floor.transform.localPosition = new Vector3(0, -0.25f, chunkLength * 0.5f);
            floor.transform.localScale = new Vector3(pathWidth, 0.5f, chunkLength);
            floor.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(floorMat);

            // 2. Natural Rugged Rock Walls
            CreateRuggedWall(chunk, -1, wallMat);
            CreateRuggedWall(chunk, 1, wallMat);

            // 3. Rocky Arched Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(chunk.transform, false);
            ceiling.transform.localPosition = new Vector3(0, wallHeight + 0.5f, chunkLength * 0.5f);
            ceiling.transform.localScale = new Vector3(pathWidth + 3.0f, 1.0f, chunkLength);
            ceiling.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);

            // 4. Stalactites hanging from ceiling (Slide 7)
            SpawnStalactites(chunk, wallMat);

            // 5. Stalagmites / Rock Outcrops along wall edges (Slide 7)
            SpawnStalagmites(chunk, wallMat);

            // 6. Stone Arch Portal framing the tunnel depth (Slide 7)
            CreateCaveArch(chunk, wallMat, chunkLength * 0.5f);

            // 7. Level 3 Mining Railway Tracks (Rails & Sleepers on floor)
            if (currentLevel == 3)
            {
                BuildRailwayTracks(chunk);
            }
        }

        // ====================================================================
        // LEVEL 3 MECHANIC: Real 3D Railway Tracks (Wooden Ties & Iron Rails)
        // ====================================================================
        private void BuildRailwayTracks(GameObject chunk)
        {
            GameObject tracks = new GameObject("RailwayTracks");
            tracks.transform.SetParent(chunk.transform, false);

            Material tieMat = MaterialFactory.GetMaterial("RailTie");
            Material railMat = MaterialFactory.GetMaterial("IronRail");

            // Wooden sleepers every 1.2m
            int numTies = Mathf.FloorToInt(chunkLength / 1.2f);
            for (int i = 0; i < numTies; i++)
            {
                float z = (i + 0.5f) * 1.2f;
                GameObject tie = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tie.name = $"Sleeper_{i}";
                tie.transform.SetParent(tracks.transform, false);
                tie.transform.localPosition = new Vector3(0, 0.04f, z);
                tie.transform.localScale = new Vector3(3.8f, 0.12f, 0.38f);
                tie.GetComponent<MeshRenderer>().sharedMaterial = tieMat;
                SafeDestroyCollider(tie);
            }

            // Twin polished iron rails along the path
            float[] railX = new float[] { -1.25f, 1.25f };
            foreach (float rx in railX)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"Rail_{rx:F1}";
                rail.transform.SetParent(tracks.transform, false);
                rail.transform.localPosition = new Vector3(rx, 0.16f, chunkLength * 0.5f);
                rail.transform.localScale = new Vector3(0.12f, 0.16f, chunkLength);
                rail.GetComponent<MeshRenderer>().sharedMaterial = railMat;
                SafeDestroyCollider(rail);
            }
        }

        // ====================================================================
        // LEVEL 2 MECHANIC: Sky Flight Corridor (100% Clear Airspace for Jetpack)
        // ====================================================================
        private void BuildSkyFlightCorridor(GameObject chunk)
        {
            float skyHeight = 12.0f;
            string wallMat = "CaveWall_Crystal";
            string floorMat = "CaveFloor_Crystal";

            // 1. Crystal cavern floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "StoneFloor";
            floor.transform.SetParent(chunk.transform, false);
            floor.transform.localPosition = new Vector3(0, -0.25f, chunkLength * 0.5f);
            floor.transform.localScale = new Vector3(pathWidth, 0.5f, chunkLength);
            floor.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(floorMat);

            // 2. High soaring crystal rock walls (no low overhangs, no blocking towers)
            for (int side = -1; side <= 1; side += 2)
            {
                float posX = side * (pathWidth * 0.5f + 0.6f);
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = side < 0 ? "LeftSkyWall" : "RightSkyWall";
                wall.transform.SetParent(chunk.transform, false);
                wall.transform.localPosition = new Vector3(posX, skyHeight * 0.5f, chunkLength * 0.5f);
                wall.transform.localScale = new Vector3(1.4f, skyHeight, chunkLength);
                wall.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);

                // Luminous crystal clusters mounted along the high walls
                SpawnCrystalCluster(chunk, new Vector3(side * (pathWidth * 0.5f - 0.4f), 2.5f, chunkLength * 0.35f), "TurquoiseCrystal");
                SpawnCrystalCluster(chunk, new Vector3(side * (pathWidth * 0.5f - 0.4f), 7.5f, chunkLength * 0.70f), "EmeraldCrystal");
            }

            // 3. High vaulted cavern ceiling (12.5m - completely above flight altitude of 5.2m)
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "SkyVaultCeiling";
            ceiling.transform.SetParent(chunk.transform, false);
            ceiling.transform.localPosition = new Vector3(0, skyHeight + 0.5f, chunkLength * 0.5f);
            ceiling.transform.localScale = new Vector3(pathWidth + 3.0f, 1.0f, chunkLength);
            ceiling.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);

            // NOTE: Absolutely ZERO arch crossbeams, ZERO stalactite pillars, and ZERO towers!
        }

        // ====================================================================
        // LEVEL 2 MECHANIC: Sky Coin Run (Airborne Coin Wave for Jetpack Flight)
        // ====================================================================
        private void BuildSkyCoinRun(GameObject chunk)
        {
            GameObject group = new GameObject("SkyCoinRun");
            group.transform.SetParent(chunk.transform, false);

            Material coinMat = MaterialFactory.GetMaterial("GoldCoin");

            // Spawn soaring coin wave high in the air
            for (float z = 3.0f; z < chunkLength - 2.0f; z += 3.2f)
            {
                float normZ = z / chunkLength;
                float x = Mathf.Sin(normZ * Mathf.PI * 2.0f) * 2.0f;
                float y = 5.2f + Mathf.Sin(normZ * Mathf.PI * 4.0f) * 0.8f;

                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "SkyCoin";
                coin.transform.SetParent(group.transform, false);
                coin.transform.localPosition = new Vector3(x, y, z);
                coin.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                coin.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);
                coin.GetComponent<MeshRenderer>().sharedMaterial = coinMat;

                SphereCollider sc = coin.GetComponent<SphereCollider>();
                if (sc == null) sc = coin.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.0f;

                CollectibleCoin cc = coin.AddComponent<CollectibleCoin>();
                cc.coinValue = 1;

                Light cLight = coin.AddComponent<Light>();
                cLight.type = LightType.Point;
                cLight.range = 3.5f;
                cLight.intensity = 1.5f;
                cLight.color = new Color(1.0f, 0.85f, 0.2f);
            }
        }

        // ====================================================================
        // LEVEL 2 MECHANIC: Descent Coin Ramp (Guiding Player from Air to Ground)
        // ====================================================================
        private void BuildDescentCoinRun(GameObject chunk)
        {
            GameObject group = new GameObject("DescentCoinRun");
            group.transform.SetParent(chunk.transform, false);

            Material coinMat = MaterialFactory.GetMaterial("GoldCoin");

            // Sloping coin trail guiding the player smoothly down from sky (5.2m) to floor (1.0m)
            int count = 8;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float z = Mathf.Lerp(2.5f, chunkLength - 2.5f, t);
                float y = Mathf.Lerp(5.2f, 1.0f, t);
                float x = Mathf.Sin(t * Mathf.PI) * 0.8f;

                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "DescentCoin";
                coin.transform.SetParent(group.transform, false);
                coin.transform.localPosition = new Vector3(x, y, z);
                coin.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                coin.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);
                coin.GetComponent<MeshRenderer>().sharedMaterial = coinMat;

                SphereCollider sc = coin.GetComponent<SphereCollider>();
                if (sc == null) sc = coin.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.0f;

                CollectibleCoin cc = coin.AddComponent<CollectibleCoin>();
                cc.coinValue = 1;

                Light cLight = coin.AddComponent<Light>();
                cLight.type = LightType.Point;
                cLight.range = 3.5f;
                cLight.intensity = 1.5f;
                cLight.color = new Color(1.0f, 0.85f, 0.2f);
            }
        }

        // ====================================================================
        // TURNING MECHANIC: Authentic Temple Run 90° Corner Turns & Signs
        // ====================================================================
        private void BuildTurnChunk(GameObject chunk, bool isLeft)
        {
            string wallMat = (currentLevel == 2) ? "CaveWall_Crystal" : ((currentLevel == 3) ? "CaveWall_Vault" : "CaveWall");
            string floorMat = (currentLevel == 2) ? "CaveFloor_Crystal" : ((currentLevel == 3) ? "CaveFloor_Vault" : "CaveFloor");

            // 1. Entrance Runway Floor (z: 0 to 22)
            GameObject inFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            inFloor.name = "EntranceFloor";
            inFloor.transform.SetParent(chunk.transform, false);
            inFloor.transform.localPosition = new Vector3(0, -0.25f, 11.0f);
            inFloor.transform.localScale = new Vector3(pathWidth, 0.5f, 22.0f);
            inFloor.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(floorMat);

            // 2. Branch Floor (extends 18m out to the side at z=18)
            float branchLen = 18.0f;
            float branchCenterOffset = isLeft ? (-pathWidth * 0.5f - branchLen * 0.5f) : (pathWidth * 0.5f + branchLen * 0.5f);
            GameObject bFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bFloor.name = "BranchFloor";
            bFloor.transform.SetParent(chunk.transform, false);
            bFloor.transform.localPosition = new Vector3(branchCenterOffset, -0.25f, 18.0f);
            bFloor.transform.localScale = new Vector3(branchLen + 2.0f, 0.5f, pathWidth);
            bFloor.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(floorMat);

            // 3. Ceiling
            GameObject ceil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceil.name = "TurnCeiling";
            ceil.transform.SetParent(chunk.transform, false);
            ceil.transform.localPosition = new Vector3(0, wallHeight + 0.5f, 11.0f);
            ceil.transform.localScale = new Vector3(pathWidth + 3.0f, 1.0f, 22.0f);
            ceil.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);

            // 4. Outer Wall (Continuous solid rock wall on opposite side of turn)
            float outerSide = isLeft ? 1.0f : -1.0f;
            float outerPosX = outerSide * (pathWidth * 0.5f + 0.6f);
            GameObject outerWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outerWall.name = "OuterRockWall";
            outerWall.transform.SetParent(chunk.transform, false);
            outerWall.transform.localPosition = new Vector3(outerPosX, wallHeight * 0.5f, 12.5f);
            outerWall.transform.localScale = new Vector3(1.4f, wallHeight, 25.0f);
            outerWall.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);

            // 5. Inner Wall (Only up to z=14, leaving opening for the branch)
            float innerSide = -outerSide;
            float innerPosX = innerSide * (pathWidth * 0.5f + 0.6f);
            GameObject innerWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            innerWall.name = "InnerRockWall";
            innerWall.transform.SetParent(chunk.transform, false);
            innerWall.transform.localPosition = new Vector3(innerPosX, wallHeight * 0.5f, 7.0f);
            innerWall.transform.localScale = new Vector3(1.4f, wallHeight, 14.0f);
            innerWall.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);

            // 6. Dead End Wall straight ahead (Crash Hazard if failed to turn!)
            GameObject deadEndWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deadEndWall.name = "DeadEndObstacle_Wall";
            deadEndWall.tag = "Obstacle";
            deadEndWall.transform.SetParent(chunk.transform, false);
            deadEndWall.transform.localPosition = new Vector3(0, wallHeight * 0.5f, 23.5f);
            deadEndWall.transform.localScale = new Vector3(pathWidth + 2.0f, wallHeight, 2.0f);
            deadEndWall.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);

            // Warning barricade in front of dead end wall
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "DeadEndBarricade";
            bar.transform.SetParent(chunk.transform, false);
            bar.transform.localPosition = new Vector3(0, 0.7f, 22.0f);
            bar.transform.localScale = new Vector3(pathWidth - 0.4f, 1.4f, 0.35f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HazardStripe");

            // 7. Large Prominent Direction Sign facing oncoming player
            BuildDirectionSign(chunk, isLeft, new Vector3(0, 0, 20.8f));

            // 8. Railway tracks curving into branch (if Level 3)
            if (currentLevel == 3)
            {
                BuildRailwayTurnTracks(chunk, isLeft);
            }

            // 9. TurnTrigger object
            GameObject ttObj = new GameObject("TurnTriggerZone");
            ttObj.transform.SetParent(chunk.transform, false);
            ttObj.transform.localPosition = new Vector3(0, 1.5f, 16.0f);
            BoxCollider bc = ttObj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(pathWidth * 1.2f, 4.0f, 12.0f);

            TurnTrigger tt = ttObj.AddComponent<TurnTrigger>();
            tt.direction = isLeft ? TurnDirection.Left : TurnDirection.Right;
            tt.targetYaw = currentHeadingYaw + (isLeft ? -90f : 90f);
            tt.cornerPosition = chunk.transform.TransformPoint(new Vector3(0, 0.5f, 18.0f));

            // 10. Update generator nextSpawnPos & heading
            currentHeadingYaw += (isLeft ? -90f : 90f);
            Vector3 branchExitLocal = new Vector3(isLeft ? (-branchLen - pathWidth * 0.5f) : (branchLen + pathWidth * 0.5f), 0, 18.0f);
            nextSpawnPos = chunk.transform.TransformPoint(branchExitLocal);
        }

        private void BuildDirectionSign(GameObject parent, bool isLeft, Vector3 pos)
        {
            GameObject sign = new GameObject(isLeft ? "DirectionSign_Left" : "DirectionSign_Right");
            sign.transform.SetParent(parent.transform, false);
            sign.transform.localPosition = pos;

            // Wooden sign post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(sign.transform, false);
            post.transform.localPosition = new Vector3(0, 1.8f, 0);
            post.transform.localScale = new Vector3(0.35f, 1.8f, 0.35f);
            post.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
            SafeDestroyCollider(post);

            // Large wooden board facing oncoming runner
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(sign.transform, false);
            board.transform.localPosition = new Vector3(0, 3.4f, 0);
            board.transform.localScale = new Vector3(4.8f, 1.5f, 0.25f);
            board.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("SignWood");
            SafeDestroyCollider(board);

            // 3D Emissive Glowing Arrow Chevrons
            string arrowMat = isLeft ? "SignArrowCyan" : "SignArrow";
            for (int a = -1; a <= 1; a++)
            {
                float xOffset = a * 1.3f;
                GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.name = $"Arrow_{a}";
                arrow.transform.SetParent(board.transform, false);
                arrow.transform.localPosition = new Vector3(xOffset, 0, -0.2f);
                arrow.transform.localScale = new Vector3(0.24f, 0.65f, 0.15f);
                arrow.transform.localRotation = Quaternion.Euler(0, 0, isLeft ? 45f : -45f);
                arrow.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(arrowMat);
                SafeDestroyCollider(arrow);

                GameObject arrowWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrowWing.name = $"ArrowWing_{a}";
                arrowWing.transform.SetParent(board.transform, false);
                arrowWing.transform.localPosition = new Vector3(xOffset + (isLeft ? 0.18f : -0.18f), 0, -0.2f);
                arrowWing.transform.localScale = new Vector3(0.24f, 0.65f, 0.15f);
                arrowWing.transform.localRotation = Quaternion.Euler(0, 0, isLeft ? -45f : 45f);
                arrowWing.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(arrowMat);
                SafeDestroyCollider(arrowWing);
            }

            // Dynamic Point Light illuminating the sign
            GameObject sLight = new GameObject("SignLight");
            sLight.transform.SetParent(board.transform, false);
            sLight.transform.localPosition = new Vector3(0, 0, -0.8f);
            Light lit = sLight.AddComponent<Light>();
            lit.type = LightType.Point;
            lit.range = 12.0f;
            lit.intensity = 3.5f;
            lit.color = isLeft ? new Color(0.1f, 0.95f, 1.0f) : new Color(1.0f, 0.85f, 0.2f);

            // Two flickering torches on the sides of the board
            for (int tSide = -1; tSide <= 1; tSide += 2)
            {
                GameObject torchObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                torchObj.name = "SignTorch";
                torchObj.transform.SetParent(sign.transform, false);
                torchObj.transform.localPosition = new Vector3(tSide * 2.6f, 3.4f, 0.2f);
                torchObj.transform.localScale = new Vector3(0.1f, 0.45f, 0.1f);
                torchObj.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
                SafeDestroyCollider(torchObj);

                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "Flame";
                flame.transform.SetParent(torchObj.transform, false);
                flame.transform.localPosition = new Vector3(0, 0.5f, 0);
                flame.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
                flame.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchFlame");
                SafeDestroyCollider(flame);
            }
        }

        private void BuildRailwayTurnTracks(GameObject chunk, bool isLeft)
        {
            GameObject tracks = new GameObject("RailwayTurnTracks");
            tracks.transform.SetParent(chunk.transform, false);
            Material tieMat = MaterialFactory.GetMaterial("RailTie");
            Material railMat = MaterialFactory.GetMaterial("IronRail");

            // Sleepers entering the turn
            for (float z = 1.0f; z < 17.0f; z += 1.2f)
            {
                GameObject tie = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tie.transform.SetParent(tracks.transform, false);
                tie.transform.localPosition = new Vector3(0, 0.04f, z);
                tie.transform.localScale = new Vector3(3.8f, 0.12f, 0.38f);
                tie.GetComponent<MeshRenderer>().sharedMaterial = tieMat;
                SafeDestroyCollider(tie);
            }

            // Sleepers exiting the turn into the branch
            float side = isLeft ? -1f : 1f;
            for (float x = 2.0f; x < 18.0f; x += 1.2f)
            {
                GameObject tie = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tie.transform.SetParent(tracks.transform, false);
                tie.transform.localPosition = new Vector3(side * x, 0.04f, 18.0f);
                tie.transform.localScale = new Vector3(0.38f, 0.12f, 3.8f);
                tie.GetComponent<MeshRenderer>().sharedMaterial = tieMat;
                SafeDestroyCollider(tie);
            }

            // Rails entering and branching
            foreach (float rx in new float[] { -1.25f, 1.25f })
            {
                GameObject inRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                inRail.transform.SetParent(tracks.transform, false);
                inRail.transform.localPosition = new Vector3(rx, 0.16f, 8.5f);
                inRail.transform.localScale = new Vector3(0.12f, 0.16f, 17.0f);
                inRail.GetComponent<MeshRenderer>().sharedMaterial = railMat;
                SafeDestroyCollider(inRail);

                // Branch rail
                GameObject bRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bRail.transform.SetParent(tracks.transform, false);
                bRail.transform.localPosition = new Vector3(side * 10.0f, 0.16f, 18.0f + rx);
                bRail.transform.localScale = new Vector3(18.0f, 0.16f, 0.12f);
                bRail.GetComponent<MeshRenderer>().sharedMaterial = railMat;
                SafeDestroyCollider(bRail);
            }
        }

        private void CreateRuggedWall(GameObject parent, float side, string wallMat)
        {
            GameObject wallGroup = new GameObject(side < 0 ? "LeftRockWall" : "RightRockWall");
            wallGroup.transform.SetParent(parent.transform, false);

            float posX = side * (pathWidth * 0.5f + 0.6f);

            // Base main wall
            GameObject mainWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainWall.name = "WallBase";
            mainWall.transform.SetParent(wallGroup.transform, false);
            mainWall.transform.localPosition = new Vector3(posX, wallHeight * 0.5f, chunkLength * 0.5f);
            mainWall.transform.localScale = new Vector3(1.2f, wallHeight, chunkLength);
            mainWall.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);

            // Organic rocky facets & crevices along the wall
            int facets = 4;
            for (int i = 0; i < facets; i++)
            {
                float z = chunkLength * ((float)i / facets + 0.12f);
                GameObject facet = GameObject.CreatePrimitive(PrimitiveType.Cube);
                facet.name = $"RockFacet_{i}";
                facet.transform.SetParent(wallGroup.transform, false);
                facet.transform.localPosition = new Vector3(posX - side * Random.Range(0.1f, 0.4f), Random.Range(1.5f, wallHeight - 1f), z);
                facet.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(-25f, 25f), side * Random.Range(5f, 18f));
                facet.transform.localScale = new Vector3(1.4f, Random.Range(2.0f, 3.5f), Random.Range(3.0f, 5.5f));
                facet.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);
            }
        }

        private void SpawnStalactites(GameObject parent, string wallMat)
        {
            // Pointed stone cones hanging downward from ceiling
            int count = Random.Range(4, 7);
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-pathWidth * 0.42f, pathWidth * 0.42f);
                float z = Random.Range(2.0f, chunkLength - 2.0f);
                float length = Random.Range(1.4f, 2.6f);

                GameObject stalactite = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stalactite.name = "Stalactite";
                stalactite.transform.SetParent(parent.transform, false);
                stalactite.transform.localPosition = new Vector3(x, wallHeight - length * 0.5f, z);
                stalactite.transform.localScale = new Vector3(0.35f, length * 0.5f, 0.35f);
                stalactite.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);
                SafeDestroyCollider(stalactite);
            }
        }

        private void SpawnStalagmites(GameObject parent, string wallMat)
        {
            // Rising rock pillars along the sides of the path (decorative only - colliders removed)
            for (int i = 0; i < 3; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float x = side * (pathWidth * 0.5f - 0.5f);
                float z = chunkLength * (0.25f + i * 0.3f);
                float h = Random.Range(1.0f, 2.0f);

                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rock.name = "Stalagmite";
                rock.transform.SetParent(parent.transform, false);
                rock.transform.localPosition = new Vector3(x, h * 0.5f, z);
                rock.transform.localScale = new Vector3(0.5f, h * 0.5f, 0.5f);
                rock.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);
                SafeDestroyCollider(rock);
            }
        }

        private void CreateCaveArch(GameObject parent, string wallMat, float z)
        {
            GameObject arch = new GameObject("CaveArch");
            arch.transform.SetParent(parent.transform, false);
            arch.transform.localPosition = new Vector3(0, 0, z);

            // Left pillar (purely visual frame - no collider)
            GameObject lp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lp.name = "LeftPillar";
            lp.transform.SetParent(arch.transform, false);
            lp.transform.localPosition = new Vector3(-pathWidth * 0.5f + 0.3f, wallHeight * 0.5f, 0);
            lp.transform.localScale = new Vector3(1.0f, wallHeight, 1.2f);
            lp.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);
            SafeDestroyCollider(lp);

            // Right pillar (purely visual frame - no collider)
            GameObject rp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rp.name = "RightPillar";
            rp.transform.SetParent(arch.transform, false);
            rp.transform.localPosition = new Vector3(pathWidth * 0.5f - 0.3f, wallHeight * 0.5f, 0);
            rp.transform.localScale = new Vector3(1.0f, wallHeight, 1.2f);
            rp.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);
            SafeDestroyCollider(rp);

            // Arch top beam (purely visual portal frame - no collider so player / jetpack never hits it)
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "ArchBeam";
            beam.transform.SetParent(arch.transform, false);
            beam.transform.localPosition = new Vector3(0, wallHeight - 0.4f, 0);
            beam.transform.localScale = new Vector3(pathWidth, 1.0f, 1.3f);
            beam.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(wallMat);
            SafeDestroyCollider(beam);
        }

        // -------------------------------------------------------------
        // LEVEL SEGMENTS
        // -------------------------------------------------------------
        private void BuildSafeCorridor(GameObject chunk)
        {
            switch (currentLevel)
            {
                case 1:
                    BuildCaveShell(chunk, "CaveWall", "CaveFloor");
                    SpawnWallTorches(chunk);
                    SpawnCoinArc(chunk, new Vector3(0, 1.0f, chunkLength * 0.25f), 5);
                    SpawnCrystalCluster(chunk, new Vector3(2.4f, 0.4f, chunkLength * 0.6f), "TurquoiseCrystal");
                    break;

                case 2:
                    BuildCrystalCorridorSegment(chunk);
                    break;

                case 3:
                    BuildVaultCorridorSegment(chunk);
                    break;
            }
        }

        private void BuildCollapsingBridgeSegment(GameObject chunk)
        {
            // Level 1 Signature: Collapsing Stone Bridge (PPT Slide 9 & 10)
            CreateRuggedWall(chunk, -1, "CaveWall");
            CreateRuggedWall(chunk, 1, "CaveWall");
            CreateCaveArch(chunk, "CaveWall", chunkLength * 0.5f);
            CreateKillFloor(chunk, -6.0f);

            // Firm start/end platforms
            CreateSubFloor(chunk, 0, 4.0f, "CaveFloor");
            CreateSubFloor(chunk, chunkLength - 4.0f, 4.0f, "CaveFloor");

            // 3 lanes of stone bridge tiles across the chasm
            float[] laneX = { -2.0f, 0.0f, 2.0f };
            float startZ = 4.0f;
            float endZ = chunkLength - 4.0f;
            int rows = Mathf.FloorToInt((endZ - startZ) / 2.6f);

            for (int r = 0; r < rows; r++)
            {
                float z = startZ + r * 2.6f + 1.3f;
                int safeLane = Random.Range(0, 3);

                for (int l = 0; l < 3; l++)
                {
                    if (l == safeLane)
                    {
                        CreateBridgeTile(chunk, new Vector3(laneX[l], 0, z), false);
                        SpawnCoin(chunk, new Vector3(laneX[l], 1.0f, z));
                    }
                    else
                    {
                        if (Random.value < 0.65f)
                        {
                            CreateBridgeTile(chunk, new Vector3(laneX[l], 0, z), true);
                        }
                    }
                }
            }

            SpawnWallTorches(chunk);
        }

        private void BuildFallingRocksSegment(GameObject chunk)
        {
            // Level 2 Signature: Falling Rocks Cavern (PPT Slide 9)
            BuildCaveShell(chunk, "CaveWall_Crystal", "CaveFloor_Crystal");
            SpawnWallTorches(chunk);
            SpawnCrystalCluster(chunk, new Vector3(-2.4f, 0.4f, chunkLength * 0.3f), "EmeraldCrystal");
            SpawnCrystalCluster(chunk, new Vector3(2.4f, 0.4f, chunkLength * 0.7f), "TurquoiseCrystal");

            float[] lanes = { -2.0f, 0.0f, 2.0f };
            for (int i = 0; i < 2; i++)
            {
                float z = chunkLength * (0.35f + i * 0.35f);
                float x = lanes[Random.Range(0, lanes.Length)];

                GameObject rockHazard = new GameObject("FallingRockHazard");
                rockHazard.transform.SetParent(chunk.transform, false);
                rockHazard.transform.localPosition = new Vector3(x, 0, z);

                FallingRock fr = rockHazard.AddComponent<FallingRock>();
                fr.warningDuration = 0.8f;
                fr.fallGravity = 32.0f;

                GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                boulder.name = "RockBoulder";
                boulder.transform.SetParent(rockHazard.transform, false);
                boulder.transform.localPosition = new Vector3(0, 5.5f, 0);
                boulder.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
                boulder.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("SwingingStone");
                fr.rockMesh = boulder.transform;

                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                indicator.name = "GroundWarning";
                indicator.transform.SetParent(rockHazard.transform, false);
                indicator.transform.localPosition = new Vector3(0, 0.02f, 0);
                indicator.transform.localScale = new Vector3(1.8f, 0.01f, 1.8f);
                indicator.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("WarningIndicator");
                SafeDestroyCollider(indicator);
                indicator.SetActive(false);
                fr.groundWarningIndicator = indicator;

                GameObject trig = new GameObject("RockFallSensor");
                trig.transform.SetParent(rockHazard.transform, false);
                trig.transform.localPosition = new Vector3(0, 1.0f, -6.5f);
                BoxCollider bc = trig.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(6.0f, 3.0f, 4.0f);
                RockTriggerHelper rth = trig.AddComponent<RockTriggerHelper>();
                rth.rock = fr;
            }

            SpawnCoinArc(chunk, new Vector3(0, 1.0f, chunkLength * 0.5f), 4);
        }

        private void BuildPressurePlateSegment(GameObject chunk)
        {
            // Level 2 Signature: Hidden Pressure Plate & Thrusting Spikes (PPT Slide 9)
            BuildCaveShell(chunk, "CaveWall_Crystal", "CaveFloor_Crystal");
            SpawnWallTorches(chunk);
            SpawnCrystalCluster(chunk, new Vector3(2.5f, 0.4f, chunkLength * 0.5f), "TurquoiseCrystal");

            float[] lanes = { -2.0f, 0.0f, 2.0f };
            float trapZ = chunkLength * 0.5f;
            int trapLane = Random.Range(0, lanes.Length);

            for (int l = 0; l < lanes.Length; l++)
            {
                if (l == trapLane)
                {
                    GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plate.name = "PressurePlate";
                    plate.transform.SetParent(chunk.transform, false);
                    plate.transform.localPosition = new Vector3(lanes[l], 0.05f, trapZ - 2.5f);
                    plate.transform.localScale = new Vector3(1.6f, 0.08f, 1.6f);
                    plate.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("PressurePlate");

                    GameObject spikes = new GameObject("TrapSpikes");
                    spikes.transform.SetParent(chunk.transform, false);
                    spikes.transform.localPosition = new Vector3(lanes[l], 0, trapZ);
                    TrapSpikes ts = spikes.AddComponent<TrapSpikes>();

                    for (int s = -1; s <= 1; s++)
                    {
                        GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        spike.name = $"Spike_{s}";
                        spike.transform.SetParent(spikes.transform, false);
                        spike.transform.localPosition = new Vector3(s * 0.45f, 0.6f, 0);
                        spike.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
                        spike.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrapSpike");
                    }
                }
                else
                {
                    SpawnCoin(chunk, new Vector3(lanes[l], 1.0f, trapZ));
                }
            }
        }

        private void BuildSwingingPendulumsSegment(GameObject chunk)
        {
            // Level 3 Signature: Swinging Stone Pendulums (PPT Slide 9)
            BuildCaveShell(chunk, "CaveWall_Vault", "CaveFloor_Vault");
            SpawnWallTorches(chunk);

            for (int p = 0; p < 2; p++)
            {
                float z = chunkLength * (0.35f + p * 0.4f);

                GameObject pendulum = new GameObject($"SwingingPendulum_{p}");
                pendulum.transform.SetParent(chunk.transform, false);
                pendulum.transform.localPosition = new Vector3(0, 5.5f, z);

                SwingingStone ss = pendulum.AddComponent<SwingingStone>();
                ss.swingAngle = 55.0f;
                ss.swingSpeed = 2.4f;
                ss.phaseOffset = p * 1.5f;

                GameObject chain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                chain.name = "PendulumRod";
                chain.transform.SetParent(pendulum.transform, false);
                chain.transform.localPosition = new Vector3(0, -2.0f, 0);
                chain.transform.localScale = new Vector3(0.1f, 2.0f, 0.1f);
                chain.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
                SafeDestroyCollider(chain);

                GameObject stoneBob = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stoneBob.name = "StoneBob";
                stoneBob.transform.SetParent(pendulum.transform, false);
                stoneBob.transform.localPosition = new Vector3(0, -4.2f, 0);
                stoneBob.transform.localScale = new Vector3(1.8f, 0.7f, 0.6f);
                stoneBob.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("SwingingStone");
                ss.stoneBob = stoneBob.transform;
            }

            SpawnCoinArc(chunk, new Vector3(0, 1.0f, chunkLength * 0.5f), 3);
        }

        private void BuildCrystalCorridorSegment(GameObject chunk)
        {
            BuildCaveShell(chunk, "CaveWall_Crystal", "CaveFloor_Crystal");
            SpawnCrystalCluster(chunk, new Vector3(-2.4f, 0.4f, chunkLength * 0.25f), "TurquoiseCrystal");
            SpawnCrystalCluster(chunk, new Vector3(2.4f, 0.4f, chunkLength * 0.50f), "EmeraldCrystal");
            SpawnCrystalCluster(chunk, new Vector3(-2.4f, 0.4f, chunkLength * 0.75f), "RubyCrystal");
            SpawnCoinArc(chunk, new Vector3(0, 1.0f, chunkLength * 0.4f), 5);
        }

        private void BuildVaultCorridorSegment(GameObject chunk)
        {
            BuildCaveShell(chunk, "CaveWall_Vault", "CaveFloor_Vault");
            SpawnWallTorches(chunk);
            SpawnCrystalCluster(chunk, new Vector3(-2.4f, 0.4f, chunkLength * 0.3f), "RubyCrystal");
            SpawnCrystalCluster(chunk, new Vector3(2.4f, 0.4f, chunkLength * 0.7f), "RubyCrystal");
            SpawnCoinArc(chunk, new Vector3(0, 1.0f, chunkLength * 0.5f), 4);
        }

        // -------------------------------------------------------------
        // PPT GOAL: The Grand Golden Treasure Chamber Altar (Slide 4, 11, 15)
        // -------------------------------------------------------------
        private void BuildTreasureChamberAltar(GameObject chunk)
        {
            BuildCaveShell(chunk, "CaveWall_Vault", "CaveFloor_Vault");

            // Grand Golden Altar Platform
            GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            altar.name = "TreasureAltar";
            altar.transform.SetParent(chunk.transform, false);
            altar.transform.localPosition = new Vector3(0, 0.4f, chunkLength * 0.6f);
            altar.transform.localScale = new Vector3(4.5f, 0.8f, 4.5f);
            altar.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TreasureChestGold");

            // Legendary Golden Treasure Chest
            GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = "GoldenTreasureChest";
            chest.transform.SetParent(altar.transform, false);
            chest.transform.localPosition = new Vector3(0, 0.8f, 0);
            chest.transform.localScale = new Vector3(0.35f, 0.7f, 0.25f);
            chest.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TreasureChestGold");

            // Radiant golden treasure light
            GameObject treasureLight = new GameObject("TreasureLight");
            treasureLight.transform.SetParent(chest.transform, false);
            treasureLight.transform.localPosition = new Vector3(0, 0.5f, 0);
            Light tl = treasureLight.AddComponent<Light>();
            tl.type = LightType.Point;
            tl.color = new Color(1.0f, 0.85f, 0.3f);
            tl.range = 18.0f;
            tl.intensity = 4.0f;

            // Trigger Victory Zone
            BoxCollider bc = chest.GetComponent<BoxCollider>();
            if (bc != null) bc.isTrigger = true;
            chest.AddComponent<TreasureGoalTrigger>();
        }

        // -------------------------------------------------------------
        // HELPERS: Bridge Tiles, Torches, Crystals, Coins
        // -------------------------------------------------------------
        private void CreateSubFloor(GameObject parent, float localZ, float length, string mat)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(parent.transform, false);
            floor.transform.localPosition = new Vector3(0, -0.25f, localZ + length * 0.5f);
            floor.transform.localScale = new Vector3(pathWidth, 0.5f, length);
            floor.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(mat);
        }

        private void CreateKillFloor(GameObject parent, float yPos)
        {
            GameObject killObj = new GameObject("ChasmKillZone");
            killObj.transform.SetParent(parent.transform, false);
            killObj.transform.localPosition = new Vector3(0, yPos, chunkLength * 0.5f);
            BoxCollider bc = killObj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(pathWidth * 2f, 1.0f, chunkLength);
            killObj.AddComponent<KillZone>();
        }

        private void CreateBridgeTile(GameObject parent, Vector3 pos, bool isCollapsing)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = isCollapsing ? "CollapsingTile" : "FirmTile";
            tile.transform.SetParent(parent.transform, false);
            tile.transform.localPosition = pos;
            tile.transform.localScale = new Vector3(1.8f, 0.4f, 2.2f);
            tile.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("BridgeTile");

            if (isCollapsing)
            {
                CollapsingBridgeTile cbt = tile.AddComponent<CollapsingBridgeTile>();
                cbt.collapseDelay = 0.65f;
            }
        }

        private void SpawnWallTorches(GameObject parent)
        {
            float[] zOffsets = { chunkLength * 0.25f, chunkLength * 0.75f };
            for (int i = 0; i < zOffsets.Length; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                Vector3 pos = new Vector3(side * (pathWidth * 0.5f - 0.2f), 2.4f, zOffsets[i]);

                // Torch wooden sconce
                GameObject torch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                torch.name = "WallTorch";
                torch.transform.SetParent(parent.transform, false);
                torch.transform.localPosition = pos;
                torch.transform.localRotation = Quaternion.Euler(0, 0, side * -28f);
                torch.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
                torch.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
                SafeDestroyCollider(torch);

                // Blazing fire
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "Flame";
                flame.transform.SetParent(torch.transform, false);
                flame.transform.localPosition = new Vector3(0, 0.5f, 0);
                flame.transform.localScale = new Vector3(0.22f, 0.32f, 0.22f);
                flame.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchFlame");
                SafeDestroyCollider(flame);

                // Dynamic warm point light casting shadows
                GameObject lightObj = new GameObject("TorchPointLight");
                lightObj.transform.SetParent(flame.transform, false);
                lightObj.transform.localPosition = Vector3.zero;
                Light l = lightObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.68f, 0.25f);
                l.range = 14.0f;
                l.intensity = 2.2f;
            }
        }

        private void SpawnCoin(GameObject parent, Vector3 pos)
        {
            GameObject coinObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coinObj.name = "Coin";
            coinObj.transform.SetParent(parent.transform, false);
            coinObj.transform.localPosition = pos;
            coinObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            coinObj.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
            coinObj.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("Gold");

            Collider col = coinObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            coinObj.AddComponent<CollectibleCoin>();
        }

        private void SpawnCoinArc(GameObject parent, Vector3 startPos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float t = (count > 1) ? (float)i / (count - 1) : 0.5f;
                float arcY = Mathf.Sin(t * Mathf.PI) * 1.5f;
                Vector3 coinPos = startPos + new Vector3(0, arcY, i * 2.2f);
                SpawnCoin(parent, coinPos);
            }
        }

        private void SpawnCrystalCluster(GameObject parent, Vector3 pos, string matName)
        {
            GameObject cluster = new GameObject("CrystalCluster");
            cluster.transform.SetParent(parent.transform, false);
            cluster.transform.localPosition = pos;

            Color lightCol = Color.cyan;
            if (matName.Contains("Emerald")) lightCol = new Color(0.1f, 1.0f, 0.45f);
            else if (matName.Contains("Ruby")) lightCol = new Color(1.0f, 0.2f, 0.4f);

            // Glowing Crystal Point Light
            GameObject cl = new GameObject("CrystalGlowLight");
            cl.transform.SetParent(cluster.transform, false);
            cl.transform.localPosition = new Vector3(0, 0.6f, 0);
            Light l = cl.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = lightCol;
            l.range = 10.0f;
            l.intensity = 2.0f;

            for (int i = 0; i < 5; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shard.name = $"Shard_{i}";
                shard.transform.SetParent(cluster.transform, false);
                shard.transform.localPosition = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.2f, 0.55f), Random.Range(-0.35f, 0.35f));
                shard.transform.localRotation = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0, 360f), Random.Range(-25f, 25f));
                shard.transform.localScale = new Vector3(0.18f, Random.Range(0.5f, 1.0f), 0.18f);
                shard.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial(matName);
                SafeDestroyCollider(shard);
            }
        }

        private void SafeDestroyCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }
        }
    }

    public class TreasureGoalTrigger : MonoBehaviour
    {
        private bool triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                triggered = true;
                if (AudioManager.Instance != null && AudioManager.Instance.victoryThemeClip != null)
                {
                    AudioManager.Instance.PlayMusic(AudioManager.Instance.victoryThemeClip, false);
                }

                EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
                if (hud != null)
                {
                    hud.ShowVictory();
                }
            }
        }
    }
}
