using UnityEngine;

namespace TreasureHunt
{
    [ExecuteAlways]
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("Procedural Human Body Parts")]
        public Transform rootBone;
        public Transform hips;
        public Transform torso;
        public Transform neck;
        public Transform head;
        public Transform hat;

        [Header("Arms & Torch")]
        public Transform leftUpperArm;
        public Transform leftForearm;
        public Transform leftHand;

        public Transform rightUpperArm;
        public Transform rightForearm;
        public Transform rightHand;
        public Transform torchLightObj;
        public Light torchLight;

        [Header("Legs & Boots")]
        public Transform leftThigh;
        public Transform leftCalf;
        public Transform leftBoot;

        public Transform rightThigh;
        public Transform rightCalf;
        public Transform rightBoot;

        [Header("Female Explorer & Props")]
        public bool isFemale = true;
        public Transform ponytail;
        public GameObject backpackObj;
        public GameObject mapObj;

        [Header("Special Vehicles & Gear (Level 2 Jetpack & Level 3 Trolley)")]
        public GameObject trolleyObj;
        public Transform[] trolleyWheels = new Transform[4];
        public GameObject jetpackObj;
        public Light jetFlameLight;
        public Transform leftFlame;
        public Transform rightFlame;
        private bool isTrolleyActive = false;
        private bool isJetpackActive = false;

        private float runCycle = 0f;
        private Vector3 initialTorsoPos;
        private Quaternion initialTorsoRot;
        private float bankAngle = 0f;
        private bool isSliding = false;

        private void OnEnable()
        {
            isFemale = true;
            if (transform.Find("HumanRig") == null || mapObj == null || backpackObj == null)
            {
                BuildHumanExplorerModel();
            }
        }

        private void Awake()
        {
            isFemale = true;
            if (transform.Find("HumanRig") == null || mapObj == null || backpackObj == null)
            {
                BuildHumanExplorerModel();
            }
        }

        private void Start()
        {
            isFemale = true;
            if (transform.Find("HumanRig") == null || mapObj == null || backpackObj == null)
            {
                BuildHumanExplorerModel();
            }
        }

        public void BuildExplorerModel() => BuildHumanExplorerModel();

        [ContextMenu("Rebuild Human Explorer Model")]
        public void BuildHumanExplorerModel()
        {
            isFemale = true;
            // Remove any root mesh renderer/filter that may be showing an old cube
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null)
            {
                if (Application.isPlaying) Destroy(mf);
                else DestroyImmediate(mf);
            }

            // Clear any old child geometry from the player immediately
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            GameObject root = new GameObject("HumanRig");
            root.transform.SetParent(transform, false);
            rootBone = root.transform;

            // 1. Hips / Pelvis Pivot (unscaled 1:1 metric bone)
            GameObject hipsPivot = new GameObject("Hips");
            hipsPivot.transform.SetParent(rootBone, false);
            hipsPivot.transform.localPosition = new Vector3(0, 0.95f, 0);
            hips = hipsPivot.transform;

            // Hips visual mesh
            GameObject hipsMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hipsMesh.name = "HipsMesh";
            hipsMesh.transform.SetParent(hips, false);
            hipsMesh.transform.localPosition = Vector3.zero;
            hipsMesh.transform.localScale = new Vector3(0.32f, 0.16f, 0.22f);
            hipsMesh.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("GirlPants");
            SafeDestroyCollider(hipsMesh);

            // Leather Belt & Brass Buckle
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "Belt";
            belt.transform.SetParent(hips, false);
            belt.transform.localPosition = new Vector3(0, 0.07f, 0);
            belt.transform.localScale = new Vector3(0.34f, 0.05f, 0.24f);
            belt.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanBelt");
            SafeDestroyCollider(belt);

            GameObject buckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buckle.name = "Buckle";
            buckle.transform.SetParent(belt.transform, false);
            buckle.transform.localPosition = new Vector3(0, 0, 0.125f);
            buckle.transform.localScale = new Vector3(0.08f, 0.07f, 0.03f);
            buckle.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanBuckle");
            SafeDestroyCollider(buckle);

            // 2. Torso Pivot (unscaled 1:1 metric bone)
            GameObject torsoPivot = new GameObject("Torso");
            torsoPivot.transform.SetParent(hips, false);
            torsoPivot.transform.localPosition = new Vector3(0, 0.08f, 0);
            torso = torsoPivot.transform;
            initialTorsoPos = torso.localPosition;
            initialTorsoRot = torso.localRotation;

            // Torso visual mesh (Athletic Female Adventurer Shirt)
            GameObject torsoMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torsoMesh.name = "TorsoMesh";
            torsoMesh.transform.SetParent(torso, false);
            torsoMesh.transform.localPosition = new Vector3(0, 0.22f, 0);
            torsoMesh.transform.localScale = new Vector3(0.30f, 0.44f, 0.20f);
            torsoMesh.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("GirlShirt");
            SafeDestroyCollider(torsoMesh);

            // Leather Vest Overlay
            GameObject vest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vest.name = "LeatherVest";
            vest.transform.SetParent(torso, false);
            vest.transform.localPosition = new Vector3(0, 0.21f, 0);
            vest.transform.localScale = new Vector3(0.32f, 0.40f, 0.22f);
            vest.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanVest");
            SafeDestroyCollider(vest);

            // Detailed 3D Expedition Backpack (Bag on Back)
            AttachExpeditionBackpack(torso);

            // 3. Neck & Human Head Pivots
            GameObject neckPivot = new GameObject("Neck");
            neckPivot.transform.SetParent(torso, false);
            neckPivot.transform.localPosition = new Vector3(0, 0.44f, 0);
            neck = neckPivot.transform;

            GameObject neckMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neckMesh.name = "NeckMesh";
            neckMesh.transform.SetParent(neck, false);
            neckMesh.transform.localPosition = new Vector3(0, 0.03f, 0);
            neckMesh.transform.localScale = new Vector3(0.09f, 0.05f, 0.09f);
            neckMesh.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanSkin");
            SafeDestroyCollider(neckMesh);

            // Anatomical Head Pivot
            GameObject headPivot = new GameObject("Head");
            headPivot.transform.SetParent(neck, false);
            headPivot.transform.localPosition = new Vector3(0, 0.08f, 0.01f);
            head = headPivot.transform;

            GameObject headMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headMesh.name = "HeadMesh";
            headMesh.transform.SetParent(head, false);
            headMesh.transform.localPosition = new Vector3(0, 0.10f, 0);
            headMesh.transform.localScale = new Vector3(0.22f, 0.26f, 0.22f);
            headMesh.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanSkin");
            SafeDestroyCollider(headMesh);

            // Styled Brunette Hair Volume
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair";
            hair.transform.SetParent(head, false);
            hair.transform.localPosition = new Vector3(0, 0.14f, -0.02f);
            hair.transform.localScale = new Vector3(0.24f, 0.22f, 0.24f);
            hair.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("GirlHair");
            SafeDestroyCollider(hair);

            // Female Long Flowing Ponytail / Braid
            GameObject pTailRoot = new GameObject("PonytailPivot");
            pTailRoot.transform.SetParent(head, false);
            pTailRoot.transform.localPosition = new Vector3(0, 0.12f, -0.13f);
            ponytail = pTailRoot.transform;

            // Turquoise hair ribbon / tie
            GameObject hairTie = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hairTie.name = "HairTie";
            hairTie.transform.SetParent(ponytail, false);
            hairTie.transform.localPosition = Vector3.zero;
            hairTie.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            hairTie.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
            hairTie.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TurquoiseCrystal");
            SafeDestroyCollider(hairTie);

            // Upper flowing ponytail segment
            GameObject tail1 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail1.name = "PonytailSegment1";
            tail1.transform.SetParent(ponytail, false);
            tail1.transform.localPosition = new Vector3(0, -0.12f, -0.04f);
            tail1.transform.localRotation = Quaternion.Euler(20f, 0, 0);
            tail1.transform.localScale = new Vector3(0.06f, 0.14f, 0.06f);
            tail1.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("GirlHair");
            SafeDestroyCollider(tail1);

            // Lower flowing braid segment
            GameObject tail2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail2.name = "PonytailSegment2";
            tail2.transform.SetParent(tail1.transform, false);
            tail2.transform.localPosition = new Vector3(0, -0.22f, 0.02f);
            tail2.transform.localRotation = Quaternion.Euler(-15f, 0, 0);
            tail2.transform.localScale = new Vector3(0.85f, 0.90f, 0.85f);
            tail2.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("GirlHair");
            SafeDestroyCollider(tail2);

            // Human Eyes & Eyebrows
            CreateEye(head, "LeftEye", new Vector3(-0.06f, 0.12f, 0.10f));
            CreateEye(head, "RightEye", new Vector3(0.06f, 0.12f, 0.10f));

            CreateEyebrow(head, "LeftBrow", new Vector3(-0.06f, 0.16f, 0.105f), 12f);
            CreateEyebrow(head, "RightBrow", new Vector3(0.06f, 0.16f, 0.105f), -12f);

            // Human Nose
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(head, false);
            nose.transform.localPosition = new Vector3(0, 0.09f, 0.12f);
            nose.transform.localScale = new Vector3(0.03f, 0.06f, 0.04f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanSkin");
            SafeDestroyCollider(nose);

            // Human Mouth / Lips
            GameObject mouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouth.name = "Mouth";
            mouth.transform.SetParent(head, false);
            mouth.transform.localPosition = new Vector3(0, 0.04f, 0.11f);
            mouth.transform.localScale = new Vector3(0.06f, 0.02f, 0.02f);
            mouth.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanBoots");
            SafeDestroyCollider(mouth);

            // Human Ears
            CreateEar(head, "LeftEar", new Vector3(-0.115f, 0.10f, 0));
            CreateEar(head, "RightEar", new Vector3(0.115f, 0.10f, 0));

            // Explorer Fedora Hat
            GameObject hatBrim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatBrim.name = "HatBrim";
            hatBrim.transform.SetParent(head, false);
            hatBrim.transform.localPosition = new Vector3(0, 0.22f, 0.01f);
            hatBrim.transform.localScale = new Vector3(0.36f, 0.015f, 0.38f);
            hatBrim.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("ExplorerHat");
            SafeDestroyCollider(hatBrim);
            hat = hatBrim.transform;

            GameObject hatCrown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatCrown.name = "HatCrown";
            hatCrown.transform.SetParent(hatBrim.transform, false);
            hatCrown.transform.localPosition = new Vector3(0, 3.2f, 0);
            hatCrown.transform.localScale = new Vector3(0.68f, 3.2f, 0.72f);
            hatCrown.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("ExplorerHat");
            SafeDestroyCollider(hatCrown);

            GameObject hatRibbon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatRibbon.name = "HatRibbon";
            hatRibbon.transform.SetParent(hatBrim.transform, false);
            hatRibbon.transform.localPosition = new Vector3(0, 1.2f, 0);
            hatRibbon.transform.localScale = new Vector3(0.70f, 1.0f, 0.74f);
            hatRibbon.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TurquoiseCrystal");
            SafeDestroyCollider(hatRibbon);

            // 4. Articulated Left Arm (Holding Ancient Treasure Map!)
            leftUpperArm = CreateArmSegment(torso, "LeftUpperArm", new Vector3(-0.21f, 0.38f, 0), true);
            leftForearm = CreateArmSegment(leftUpperArm, "LeftForearm", new Vector3(0, -0.22f, 0), false);
            leftHand = CreateHand(leftForearm, new Vector3(0, -0.20f, 0));
            AttachAncientTreasureMap(leftHand);

            // 5. Articulated Right Arm (Holding Flaming Adventure Torch!)
            rightUpperArm = CreateArmSegment(torso, "RightUpperArm", new Vector3(0.21f, 0.38f, 0), true);
            rightForearm = CreateArmSegment(rightUpperArm, "RightForearm", new Vector3(0, -0.22f, 0), false);
            rightHand = CreateHand(rightForearm, new Vector3(0, -0.20f, 0));
            AttachFlamingTorch(rightHand);

            // 6. Articulated Legs (Thighs + Calves + Hiking Boots)
            leftThigh = CreateLegSegment(hips, "LeftThigh", new Vector3(-0.11f, -0.06f, 0), true);
            leftCalf = CreateLegSegment(leftThigh, "LeftCalf", new Vector3(0, -0.38f, 0), false);
            leftBoot = CreateBoot(leftCalf, new Vector3(0, -0.34f, 0.04f));

            rightThigh = CreateLegSegment(hips, "RightThigh", new Vector3(0.11f, -0.06f, 0), true);
            rightCalf = CreateLegSegment(rightThigh, "RightCalf", new Vector3(0, -0.38f, 0), false);
            rightBoot = CreateBoot(rightCalf, new Vector3(0, -0.34f, 0.04f));

            // 7. Special Vehicles & Gear: Mining Trolley & Rocket Jetpack
            BuildTrolleyModel(rootBone);
            BuildJetpackModel(torso);
        }

        private void BuildTrolleyModel(Transform parent)
        {
            trolleyObj = new GameObject("MiningTrolley");
            trolleyObj.transform.SetParent(parent, false);
            trolleyObj.transform.localPosition = new Vector3(0, -0.45f, 0);

            // Floor base
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "CartFloor";
            floor.transform.SetParent(trolleyObj.transform, false);
            floor.transform.localPosition = new Vector3(0, 0.12f, 0);
            floor.transform.localScale = new Vector3(1.5f, 0.12f, 1.7f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrolleyWood");
            SafeDestroyCollider(floor);

            // Left sideboard
            GameObject leftSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftSide.name = "LeftSide";
            leftSide.transform.SetParent(trolleyObj.transform, false);
            leftSide.transform.localPosition = new Vector3(-0.75f, 0.50f, 0);
            leftSide.transform.localScale = new Vector3(0.12f, 0.75f, 1.7f);
            leftSide.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrolleyWood");
            SafeDestroyCollider(leftSide);

            // Right sideboard
            GameObject rightSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightSide.name = "RightSide";
            rightSide.transform.SetParent(trolleyObj.transform, false);
            rightSide.transform.localPosition = new Vector3(0.75f, 0.50f, 0);
            rightSide.transform.localScale = new Vector3(0.12f, 0.75f, 1.7f);
            rightSide.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrolleyWood");
            SafeDestroyCollider(rightSide);

            // Front sideboard
            GameObject frontSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontSide.name = "FrontSide";
            frontSide.transform.SetParent(trolleyObj.transform, false);
            frontSide.transform.localPosition = new Vector3(0, 0.50f, 0.82f);
            frontSide.transform.localScale = new Vector3(1.5f, 0.75f, 0.12f);
            frontSide.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrolleyWood");
            SafeDestroyCollider(frontSide);

            // Back sideboard
            GameObject backSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backSide.name = "BackSide";
            backSide.transform.SetParent(trolleyObj.transform, false);
            backSide.transform.localPosition = new Vector3(0, 0.50f, -0.82f);
            backSide.transform.localScale = new Vector3(1.5f, 0.75f, 0.12f);
            backSide.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrolleyWood");
            SafeDestroyCollider(backSide);

            // Heavy Iron Rim
            GameObject ironRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ironRim.name = "IronRim";
            ironRim.transform.SetParent(trolleyObj.transform, false);
            ironRim.transform.localPosition = new Vector3(0, 0.88f, 0);
            ironRim.transform.localScale = new Vector3(1.64f, 0.10f, 1.82f);
            ironRim.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrolleyIron");
            SafeDestroyCollider(ironRim);

            // 4 Iron Flanged Wheels
            Vector3[] wheelPositions = new Vector3[]
            {
                new Vector3(-0.82f, 0.16f, 0.55f),
                new Vector3(0.82f, 0.16f, 0.55f),
                new Vector3(-0.82f, 0.16f, -0.55f),
                new Vector3(0.82f, 0.16f, -0.55f)
            };

            for (int w = 0; w < 4; w++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{w}";
                wheel.transform.SetParent(trolleyObj.transform, false);
                wheel.transform.localPosition = wheelPositions[w];
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                wheel.transform.localScale = new Vector3(0.42f, 0.08f, 0.42f);
                wheel.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrolleyIron");
                SafeDestroyCollider(wheel);
                trolleyWheels[w] = wheel.transform;
            }

            // Front Mining Headlight / Brass Lantern
            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lantern.name = "FrontLantern";
            lantern.transform.SetParent(trolleyObj.transform, false);
            lantern.transform.localPosition = new Vector3(0, 0.85f, 0.96f);
            lantern.transform.localScale = new Vector3(0.25f, 0.32f, 0.22f);
            lantern.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanBuckle");
            SafeDestroyCollider(lantern);

            GameObject lampLightObj = new GameObject("TrolleyHeadlight");
            lampLightObj.transform.SetParent(lantern.transform, false);
            lampLightObj.transform.localPosition = new Vector3(0, 0, 0.2f);
            Light headLight = lampLightObj.AddComponent<Light>();
            headLight.type = LightType.Spot;
            headLight.range = 25f;
            headLight.spotAngle = 65f;
            headLight.intensity = 3.5f;
            headLight.color = new Color(1.0f, 0.88f, 0.5f);

            trolleyObj.SetActive(false);
        }

        private void BuildJetpackModel(Transform parent)
        {
            jetpackObj = new GameObject("RocketJetpack");
            jetpackObj.transform.SetParent(parent, false);
            jetpackObj.transform.localPosition = new Vector3(0, 0.05f, -0.32f);

            // Harness plate
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            plate.transform.SetParent(jetpackObj.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localScale = new Vector3(0.45f, 0.55f, 0.12f);
            plate.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("JetpackMetal");
            SafeDestroyCollider(plate);

            // Twin rocket fuel cylinders
            for (int c = -1; c <= 1; c += 2)
            {
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.name = $"RocketCanister_{c}";
                cylinder.transform.SetParent(jetpackObj.transform, false);
                cylinder.transform.localPosition = new Vector3(c * 0.24f, 0, 0);
                cylinder.transform.localScale = new Vector3(0.16f, 0.45f, 0.16f);
                cylinder.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("JetpackMetal");
                SafeDestroyCollider(cylinder);

                // Nozzle
                GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nozzle.name = $"Nozzle_{c}";
                nozzle.transform.SetParent(cylinder.transform, false);
                nozzle.transform.localPosition = new Vector3(0, -1.05f, 0);
                nozzle.transform.localScale = new Vector3(1.15f, 0.25f, 1.15f);
                nozzle.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrolleyIron");
                SafeDestroyCollider(nozzle);

                // Flame cone
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                flame.name = $"Flame_{c}";
                flame.transform.SetParent(cylinder.transform, false);
                flame.transform.localPosition = new Vector3(0, -1.55f, 0);
                flame.transform.localScale = new Vector3(0.85f, 0.65f, 0.85f);
                flame.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("JetpackFlame");
                SafeDestroyCollider(flame);

                if (c == -1) leftFlame = flame.transform;
                else rightFlame = flame.transform;
            }

            GameObject flameLightObj = new GameObject("JetFlameLight");
            flameLightObj.transform.SetParent(jetpackObj.transform, false);
            flameLightObj.transform.localPosition = new Vector3(0, -0.6f, 0);
            jetFlameLight = flameLightObj.AddComponent<Light>();
            jetFlameLight.type = LightType.Point;
            jetFlameLight.range = 8.0f;
            jetFlameLight.intensity = 3.5f;
            jetFlameLight.color = new Color(1.0f, 0.55f, 0.15f);

            jetpackObj.SetActive(false);
        }

        public void SetTrolleyVisible(bool visible)
        {
            isTrolleyActive = visible;
            if (trolleyObj != null) trolleyObj.SetActive(visible);
            if (visible)
            {
                isJetpackActive = false;
                if (jetpackObj != null) jetpackObj.SetActive(false);
            }
        }

        public void SetJetpackVisible(bool visible)
        {
            isJetpackActive = visible;
            if (jetpackObj != null) jetpackObj.SetActive(visible);
            if (visible)
            {
                isTrolleyActive = false;
                if (trolleyObj != null) trolleyObj.SetActive(false);
                if (backpackObj != null) backpackObj.SetActive(false); // Conceal backpack during jetpack flight so it doesn't double-box!
            }
            else
            {
                if (backpackObj != null) backpackObj.SetActive(true); // Restore expedition backpack when on foot!
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

        private void CreateEye(Transform parent, string name, Vector3 pos)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = name;
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = pos;
            eye.transform.localScale = new Vector3(0.045f, 0.045f, 0.045f);
            eye.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanSclera");
            SafeDestroyCollider(eye);

            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = "Pupil";
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = new Vector3(0, 0, 0.42f);
            pupil.transform.localScale = new Vector3(0.55f, 0.55f, 0.45f);
            pupil.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanEyes");
            SafeDestroyCollider(pupil);
        }

        private void CreateEyebrow(Transform parent, string name, Vector3 pos, float tilt)
        {
            GameObject brow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brow.name = name;
            brow.transform.SetParent(parent, false);
            brow.transform.localPosition = pos;
            brow.transform.localRotation = Quaternion.Euler(0, 0, tilt);
            brow.transform.localScale = new Vector3(0.05f, 0.012f, 0.015f);
            brow.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("GirlHair");
            SafeDestroyCollider(brow);
        }

        private void CreateEar(Transform parent, string name, Vector3 pos)
        {
            GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ear.name = name;
            ear.transform.SetParent(parent, false);
            ear.transform.localPosition = pos;
            ear.transform.localScale = new Vector3(0.04f, 0.07f, 0.04f);
            ear.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanSkin");
            SafeDestroyCollider(ear);
        }

        private Transform CreateArmSegment(Transform parent, string name, Vector3 pos, bool isUpper)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pos;

            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh.name = name + "_Mesh";
            mesh.transform.SetParent(pivot.transform, false);
            mesh.transform.localPosition = new Vector3(0, isUpper ? -0.10f : -0.09f, 0);
            mesh.transform.localScale = isUpper ? new Vector3(0.09f, 0.12f, 0.09f) : new Vector3(0.08f, 0.12f, 0.08f);

            mesh.GetComponent<MeshRenderer>().sharedMaterial = isUpper ?
                MaterialFactory.GetMaterial("GirlShirt") :
                MaterialFactory.GetMaterial("HumanSkin");
            SafeDestroyCollider(mesh);

            return pivot.transform;
        }

        private Transform CreateHand(Transform parent, Vector3 pos)
        {
            GameObject pivot = new GameObject("HandPivot");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pos;

            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = "Hand";
            hand.transform.SetParent(pivot.transform, false);
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            hand.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanSkin");
            SafeDestroyCollider(hand);
            return pivot.transform;
        }

        private void AttachFlamingTorch(Transform hand)
        {
            GameObject torch = new GameObject("FlamingTorch");
            torch.transform.SetParent(hand, false);
            torch.transform.localPosition = new Vector3(0, 0.02f, 0.10f);
            torch.transform.localRotation = Quaternion.Euler(30f, 0, 0);

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(torch.transform, false);
            handle.transform.localPosition = Vector3.zero;
            handle.transform.localScale = new Vector3(0.04f, 0.24f, 0.04f);
            handle.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
            SafeDestroyCollider(handle);

            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "FlameHead";
            flame.transform.SetParent(torch.transform, false);
            flame.transform.localPosition = new Vector3(0, 0.26f, 0);
            flame.transform.localScale = new Vector3(0.12f, 0.18f, 0.12f);
            flame.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchFlame");
            SafeDestroyCollider(flame);

            GameObject lightObj = new GameObject("TorchLight");
            lightObj.transform.SetParent(flame.transform, false);
            lightObj.transform.localPosition = Vector3.zero;
            torchLight = lightObj.AddComponent<Light>();
            torchLight.type = LightType.Point;
            torchLight.color = new Color(1.0f, 0.65f, 0.25f);
            torchLight.range = 14.0f;
            torchLight.intensity = 2.8f;
            torchLightObj = lightObj.transform;
        }

        private void AttachAncientTreasureMap(Transform hand)
        {
            mapObj = new GameObject("AncientTreasureMap");
            mapObj.transform.SetParent(hand, false);
            mapObj.transform.localPosition = new Vector3(0, 0.02f, 0.10f);
            mapObj.transform.localRotation = Quaternion.Euler(25f, -20f, -10f);

            // Main rolled parchment scroll
            GameObject roll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roll.name = "ParchmentRoll";
            roll.transform.SetParent(mapObj.transform, false);
            roll.transform.localPosition = Vector3.zero;
            roll.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            roll.transform.localScale = new Vector3(0.05f, 0.22f, 0.05f);
            roll.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("MapParchment");
            SafeDestroyCollider(roll);

            // Wooden scroll knobs on ends
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                knob.name = $"ScrollKnob_{k}";
                knob.transform.SetParent(roll.transform, false);
                knob.transform.localPosition = new Vector3(0, k * 1.05f, 0);
                knob.transform.localScale = new Vector3(1.3f, 0.4f, 1.3f);
                knob.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
                SafeDestroyCollider(knob);
            }

            // Unfurled map sheet showing the ancient drawn route
            GameObject sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheet.name = "MapSheet";
            sheet.transform.SetParent(mapObj.transform, false);
            sheet.transform.localPosition = new Vector3(0.02f, 0.08f, 0.05f);
            sheet.transform.localRotation = Quaternion.Euler(15f, 0, 5f);
            sheet.transform.localScale = new Vector3(0.24f, 0.20f, 0.015f);
            sheet.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("MapParchment");
            SafeDestroyCollider(sheet);

            // Drawn trail markings on the map
            GameObject ink = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ink.name = "InkTrail";
            ink.transform.SetParent(sheet.transform, false);
            ink.transform.localPosition = new Vector3(0, 0, 0.55f);
            ink.transform.localScale = new Vector3(0.75f, 0.70f, 0.1f);
            ink.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("MapInk");
            SafeDestroyCollider(ink);

            // Crimson wax seal
            GameObject seal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seal.name = "WaxSeal";
            seal.transform.SetParent(sheet.transform, false);
            seal.transform.localPosition = new Vector3(0.28f, -0.28f, 0.58f);
            seal.transform.localScale = new Vector3(0.22f, 0.10f, 0.22f);
            seal.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("MapSeal");
            SafeDestroyCollider(seal);
        }

        private void AttachExpeditionBackpack(Transform parent)
        {
            backpackObj = new GameObject("ExpeditionBackpack");
            backpackObj.transform.SetParent(parent, false);
            backpackObj.transform.localPosition = new Vector3(0, 0.22f, -0.14f);

            // Main leather pack body
            GameObject pack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pack.name = "PackBody";
            pack.transform.SetParent(backpackObj.transform, false);
            pack.transform.localPosition = Vector3.zero;
            pack.transform.localScale = new Vector3(0.30f, 0.36f, 0.16f);
            pack.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("ExpeditionBag");
            SafeDestroyCollider(pack);

            // Top flap
            GameObject flap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flap.name = "PackFlap";
            flap.transform.SetParent(backpackObj.transform, false);
            flap.transform.localPosition = new Vector3(0, 0.16f, 0.01f);
            flap.transform.localRotation = Quaternion.Euler(14f, 0, 0);
            flap.transform.localScale = new Vector3(0.31f, 0.08f, 0.17f);
            flap.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("BagStrap");
            SafeDestroyCollider(flap);

            // Brass buckle
            GameObject buckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buckle.name = "FlapBuckle";
            buckle.transform.SetParent(flap.transform, false);
            buckle.transform.localPosition = new Vector3(0, -0.16f, -0.45f);
            buckle.transform.localScale = new Vector3(0.18f, 0.30f, 0.12f);
            buckle.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanBuckle");
            SafeDestroyCollider(buckle);

            // Bedroll / Sleeping roll strapped to bottom
            GameObject roll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roll.name = "Bedroll";
            roll.transform.SetParent(backpackObj.transform, false);
            roll.transform.localPosition = new Vector3(0, -0.20f, 0);
            roll.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            roll.transform.localScale = new Vector3(0.10f, 0.30f, 0.10f);
            roll.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
            SafeDestroyCollider(roll);
        }

        private Transform CreateLegSegment(Transform parent, string name, Vector3 pos, bool isThigh)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pos;

            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh.name = name + "_Mesh";
            mesh.transform.SetParent(pivot.transform, false);
            mesh.transform.localPosition = new Vector3(0, isThigh ? -0.18f : -0.16f, 0);
            mesh.transform.localScale = isThigh ? new Vector3(0.12f, 0.18f, 0.12f) : new Vector3(0.10f, 0.18f, 0.10f);
            mesh.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("GirlPants");
            SafeDestroyCollider(mesh);

            return pivot.transform;
        }

        private Transform CreateBoot(Transform parent, Vector3 pos)
        {
            GameObject pivot = new GameObject("BootPivot");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pos;

            GameObject boot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boot.name = "Boot";
            boot.transform.SetParent(pivot.transform, false);
            boot.transform.localPosition = new Vector3(0, -0.06f, 0.04f);
            boot.transform.localScale = new Vector3(0.12f, 0.12f, 0.22f);
            boot.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("HumanBoots");
            SafeDestroyCollider(boot);

            GameObject sole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sole.name = "BootSole";
            sole.transform.SetParent(pivot.transform, false);
            sole.transform.localPosition = new Vector3(0, -0.11f, 0.04f);
            sole.transform.localScale = new Vector3(0.13f, 0.03f, 0.23f);
            sole.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
            SafeDestroyCollider(sole);

            return pivot.transform;
        }

        public void Animate(float speed, bool isGrounded, bool isSprinting, bool isDead, float horizontalSteer = 0f, bool slide = false, bool flying = false, bool inTrolley = false)
        {
            isSliding = slide;

            if (inTrolley != isTrolleyActive) SetTrolleyVisible(inTrolley);
            if (flying != isJetpackActive) SetJetpackVisible(flying);

            if (torchLight != null)
            {
                torchLight.intensity = 2.5f + Mathf.Sin(Time.time * 18.0f) * 0.45f + Mathf.PerlinNoise(Time.time * 6.0f, 0f) * 0.35f;
            }

            if (isDead)
            {
                if (rootBone != null)
                {
                    rootBone.localRotation = Quaternion.Slerp(rootBone.localRotation, Quaternion.Euler(85f, 15f, 0), Time.deltaTime * 8f);
                    rootBone.localPosition = Vector3.Lerp(rootBone.localPosition, new Vector3(0, -0.85f, 0), Time.deltaTime * 8f);
                }
                return;
            }

            float targetBank = -horizontalSteer * 18.0f;
            bankAngle = Mathf.Lerp(bankAngle, targetBank, Time.deltaTime * 10f);

            // Dynamic Ponytail secondary motion
            if (ponytail != null)
            {
                float tailSway = Mathf.Sin(runCycle * 1.8f) * 12.0f;
                float flightDrag = isJetpackActive ? -28.0f : 0f;
                ponytail.localRotation = Quaternion.Euler(15.0f + flightDrag + Mathf.Abs(tailSway), tailSway * 0.4f, 0);
            }

            // =========================================================
            // 1. TROLLEY MODE (Level 3 Railcart Riding Pose & Motion)
            // =========================================================
            if (isTrolleyActive)
            {
                if (trolleyObj != null)
                {
                    // Tilt trolley into the railway curve
                    trolleyObj.transform.localRotation = Quaternion.Euler(0, 0, bankAngle * 0.85f);

                    // Spin all 4 wheels on rails
                    float wheelSpin = speed * 140f * Time.deltaTime;
                    foreach (var w in trolleyWheels)
                    {
                        if (w != null) w.Rotate(Vector3.up * wheelSpin, Space.Self);
                    }
                }

                if (isSliding)
                {
                    // Crouch down inside the cart bucket
                    if (hips != null) hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(0, 0.45f, 0), Time.deltaTime * 15f);
                    if (torso != null) torso.localRotation = Quaternion.Euler(45f, 0, bankAngle);
                }
                else
                {
                    // Stand gripping front iron rim of the cart
                    if (hips != null) hips.localPosition = new Vector3(0, 0.85f, 0);
                    if (torso != null) torso.localRotation = Quaternion.Euler(15.0f, 0, bankAngle);

                    if (leftUpperArm != null)
                    {
                        leftUpperArm.localRotation = Quaternion.Euler(55f, 12f, -8f);
                        if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-35f, 0, 0);
                    }
                    if (rightUpperArm != null)
                    {
                        rightUpperArm.localRotation = Quaternion.Euler(55f, -12f, 8f);
                        if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-35f, 0, 0);
                    }
                }

                // Legs stationary inside cart
                if (leftThigh != null) leftThigh.localRotation = Quaternion.identity;
                if (rightThigh != null) rightThigh.localRotation = Quaternion.identity;
                if (leftCalf != null) leftCalf.localRotation = Quaternion.identity;
                if (rightCalf != null) rightCalf.localRotation = Quaternion.identity;
                return;
            }

            // =========================================================
            // 2. JETPACK FLIGHT MODE (Level 2 Air Soaring Pose & Thruster Flames)
            // =========================================================
            if (isJetpackActive)
            {
                // Thruster flame cones dynamic pulsation
                float flamePulsate = 1.0f + Mathf.Sin(Time.time * 30.0f) * 0.35f;
                if (leftFlame != null) leftFlame.localScale = new Vector3(0.85f, 0.65f * flamePulsate, 0.85f);
                if (rightFlame != null) rightFlame.localScale = new Vector3(0.85f, 0.65f * flamePulsate, 0.85f);
                if (jetFlameLight != null) jetFlameLight.intensity = 3.5f + flamePulsate * 1.5f;

                // Aerodynamic flight posture
                if (hips != null) hips.localPosition = new Vector3(0, 0.95f, 0);
                if (torso != null) torso.localRotation = Quaternion.Euler(20.0f, 0, bankAngle);

                // Trailing legs together in the air
                if (leftThigh != null) leftThigh.localRotation = Quaternion.Euler(-18f, 0, -4f);
                if (rightThigh != null) rightThigh.localRotation = Quaternion.Euler(-18f, 0, 4f);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(12f, 0, 0);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(12f, 0, 0);

                // Left arm holds the Ancient Treasure Map forward to navigate the crystal skyway!
                if (leftUpperArm != null)
                {
                    leftUpperArm.localRotation = Quaternion.Euler(28f, 12f, -10f);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-35f, 0, 0);
                }
                // Right arm holds adventurer torch forward & slightly down (natural ergonomic posture)
                if (rightUpperArm != null)
                {
                    rightUpperArm.localRotation = Quaternion.Euler(22f, -12f, 10f);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-32f, 0, 0);
                }
                return;
            }

            // =========================================================
            // 3. GROUND RUNNING & SLIDING MODE
            // =========================================================
            if (isSliding)
            {
                if (hips != null) hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(0, 0.45f, 0), Time.deltaTime * 15f);
                if (torso != null) torso.localRotation = Quaternion.Euler(45f, 0, 0);
                if (leftThigh != null) leftThigh.localRotation = Quaternion.Euler(70f, 0, 0);
                if (rightThigh != null) rightThigh.localRotation = Quaternion.Euler(75f, 0, 0);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(-60f, 0, 0);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(-60f, 0, 0);
                return;
            }

            if (torso != null)
            {
                torso.localRotation = Quaternion.Euler(9.0f, 0, bankAngle);
            }

            if (!isGrounded)
            {
                if (leftThigh != null) leftThigh.localRotation = Quaternion.Euler(-28f, 0, 0);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(32f, 0, 0);
                if (rightThigh != null) rightThigh.localRotation = Quaternion.Euler(16f, 0, 0);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(20f, 0, 0);

                if (leftUpperArm != null)
                {
                    leftUpperArm.localRotation = Quaternion.Euler(28f, 12f, -10f);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-35f, 0, 0);
                }
                if (rightUpperArm != null)
                {
                    rightUpperArm.localRotation = Quaternion.Euler(18f, -10f, 10f);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-30f, 0, 0);
                }
                return;
            }

            float cadence = (speed > 8.0f) ? 14.5f : 10.0f;
            runCycle += Time.deltaTime * cadence;

            float legSwing = Mathf.Sin(runCycle) * 44.0f;
            float kneeBendLeft = Mathf.Clamp(Mathf.Cos(runCycle) * 60.0f, 0f, 75.0f);
            float kneeBendRight = Mathf.Clamp(-Mathf.Cos(runCycle) * 60.0f, 0f, 75.0f);

            if (leftThigh != null) leftThigh.localRotation = Quaternion.Euler(legSwing, 0, 0);
            if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(kneeBendLeft, 0, 0);

            if (rightThigh != null) rightThigh.localRotation = Quaternion.Euler(-legSwing, 0, 0);
            if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(kneeBendRight, 0, 0);

            float armSwing = Mathf.Cos(runCycle) * 38.0f;
            if (leftUpperArm != null)
            {
                // Left arm holds the Ancient Treasure Map forward, guiding the way!
                leftUpperArm.localRotation = Quaternion.Euler(30.0f + Mathf.Sin(runCycle) * 12.0f, 16f, -10f);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-38f, 0, 0);
            }

            if (rightUpperArm != null)
            {
                rightUpperArm.localRotation = Quaternion.Euler(armSwing * 0.7f + 15f, 0, 12f);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-35f, 0, 0);
            }

            float bounce = Mathf.Abs(Mathf.Sin(runCycle)) * 0.06f;
            if (hips != null)
            {
                hips.localPosition = new Vector3(0, 0.95f + bounce, 0);
            }
        }

        public void ResetPose()
        {
            SetTrolleyVisible(false);
            SetJetpackVisible(false);

            if (rootBone != null)
            {
                rootBone.localRotation = Quaternion.identity;
                rootBone.localPosition = Vector3.zero;
            }
            if (hips != null) hips.localPosition = new Vector3(0, 0.95f, 0);
            if (torso != null) torso.localRotation = Quaternion.identity;
            if (leftThigh != null) leftThigh.localRotation = Quaternion.identity;
            if (rightThigh != null) rightThigh.localRotation = Quaternion.identity;
            if (leftCalf != null) leftCalf.localRotation = Quaternion.identity;
            if (rightCalf != null) rightCalf.localRotation = Quaternion.identity;
            if (leftUpperArm != null) leftUpperArm.localRotation = Quaternion.identity;
            if (rightUpperArm != null) rightUpperArm.localRotation = Quaternion.identity;
            if (ponytail != null) ponytail.localRotation = Quaternion.Euler(15f, 0, 0);
            runCycle = 0f;
            bankAngle = 0f;
            isSliding = false;
        }
    }
}
