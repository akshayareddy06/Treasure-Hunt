using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunt
{
    public static class MaterialFactory
    {
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static Texture2D rockTexture;
        private static Texture2D floorStoneTexture;

        public static Material GetMaterial(string type)
        {
            if (Cache.TryGetValue(type, out Material mat) && mat != null)
                return mat;

            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
            if (standardShader == null) standardShader = Shader.Find("Diffuse");

            mat = new Material(standardShader) { name = "Mat_" + type };

            switch (type)
            {
                case "CaveWall":
                    // Dark subterranean slate rock with stone grain
                    mat.color = new Color(0.18f, 0.20f, 0.22f);
                    mat.mainTexture = GetRockTexture();
                    SetRoughness(mat, 0.85f, 0.05f);
                    break;

                case "CaveWall_Crystal":
                    // Mineral-rich crystal cave wall with slight blue sheen
                    mat.color = new Color(0.14f, 0.22f, 0.26f);
                    mat.mainTexture = GetRockTexture();
                    SetRoughness(mat, 0.70f, 0.15f);
                    SetEmission(mat, new Color(0.0f, 0.15f, 0.2f), 0.4f);
                    break;

                case "CaveWall_Vault":
                    // Grand treasure vault ancient carved granite with gold veins
                    mat.color = new Color(0.24f, 0.20f, 0.16f);
                    mat.mainTexture = GetRockTexture();
                    SetRoughness(mat, 0.75f, 0.2f);
                    SetEmission(mat, new Color(0.25f, 0.18f, 0.05f), 0.35f);
                    break;

                case "CaveFloor":
                    // Rugged cavern flagstone path
                    mat.color = new Color(0.26f, 0.28f, 0.30f);
                    mat.mainTexture = GetFloorTexture();
                    SetRoughness(mat, 0.75f, 0.1f);
                    break;

                case "CaveFloor_Crystal":
                    mat.color = new Color(0.20f, 0.28f, 0.32f);
                    mat.mainTexture = GetFloorTexture();
                    SetRoughness(mat, 0.65f, 0.15f);
                    break;

                case "CaveFloor_Vault":
                    mat.color = new Color(0.32f, 0.26f, 0.20f);
                    mat.mainTexture = GetFloorTexture();
                    SetRoughness(mat, 0.70f, 0.25f);
                    break;

                case "BridgeTile":
                    mat.color = new Color(0.38f, 0.32f, 0.26f);
                    mat.mainTexture = GetFloorTexture();
                    SetRoughness(mat, 0.85f, 0.05f);
                    break;

                case "BridgeWarning":
                    mat.color = new Color(0.85f, 0.20f, 0.10f);
                    mat.mainTexture = GetFloorTexture();
                    SetRoughness(mat, 0.5f, 0.1f);
                    SetEmission(mat, new Color(1.0f, 0.25f, 0.10f), 2.2f);
                    break;

                case "TurquoiseCrystal":
                    mat.color = new Color(0.0f, 0.95f, 0.88f, 0.92f);
                    SetRoughness(mat, 0.1f, 0.25f);
                    SetEmission(mat, new Color(0.0f, 1.0f, 0.9f), 3.2f);
                    break;

                case "EmeraldCrystal":
                    mat.color = new Color(0.05f, 0.95f, 0.45f, 0.92f);
                    SetRoughness(mat, 0.1f, 0.25f);
                    SetEmission(mat, new Color(0.05f, 1.0f, 0.45f), 3.2f);
                    break;

                case "RubyCrystal":
                    mat.color = new Color(0.98f, 0.15f, 0.35f, 0.92f);
                    SetRoughness(mat, 0.1f, 0.25f);
                    SetEmission(mat, new Color(1.0f, 0.15f, 0.35f), 3.2f);
                    break;

                case "TorchWood":
                    mat.color = new Color(0.24f, 0.15f, 0.08f);
                    SetRoughness(mat, 0.9f, 0.0f);
                    break;

                case "TorchFlame":
                    mat.color = new Color(1.0f, 0.55f, 0.10f);
                    SetRoughness(mat, 0.1f, 0.0f);
                    SetEmission(mat, new Color(1.0f, 0.60f, 0.10f), 4.0f);
                    break;

                case "Gold":
                    mat.color = new Color(1.0f, 0.82f, 0.18f);
                    SetRoughness(mat, 0.25f, 0.9f);
                    SetEmission(mat, new Color(0.45f, 0.35f, 0.05f), 0.6f);
                    break;

                case "TreasureChestWood":
                    mat.color = new Color(0.35f, 0.18f, 0.10f);
                    SetRoughness(mat, 0.75f, 0.05f);
                    break;

                case "TreasureChestGold":
                    mat.color = new Color(1.0f, 0.82f, 0.18f);
                    SetRoughness(mat, 0.2f, 0.95f);
                    SetEmission(mat, new Color(0.5f, 0.4f, 0.1f), 0.8f);
                    break;

                case "HumanSkin":
                    mat.color = new Color(0.92f, 0.74f, 0.60f); // Warm natural human skin
                    SetRoughness(mat, 0.65f, 0.0f);
                    break;

                case "HumanHair":
                    mat.color = new Color(0.16f, 0.10f, 0.06f); // Brunette dark hair
                    SetRoughness(mat, 0.85f, 0.0f);
                    break;

                case "HumanShirt":
                    mat.color = new Color(0.85f, 0.80f, 0.70f); // Safari khaki shirt
                    SetRoughness(mat, 0.9f, 0.0f);
                    break;

                case "HumanVest":
                    mat.color = new Color(0.45f, 0.28f, 0.14f); // Explorer leather vest
                    SetRoughness(mat, 0.8f, 0.05f);
                    break;

                case "HumanBelt":
                    mat.color = new Color(0.14f, 0.09f, 0.06f); // Dark leather belt
                    SetRoughness(mat, 0.7f, 0.1f);
                    break;

                case "HumanBuckle":
                    mat.color = new Color(0.96f, 0.84f, 0.25f); // Polished brass buckle
                    SetRoughness(mat, 0.3f, 0.9f);
                    break;

                case "HumanBoots":
                    mat.color = new Color(0.20f, 0.12f, 0.06f); // Rugged hiking boots
                    SetRoughness(mat, 0.85f, 0.1f);
                    break;

                case "HumanEyes":
                    mat.color = new Color(0.12f, 0.18f, 0.28f);
                    SetRoughness(mat, 0.1f, 0.0f);
                    break;

                case "HumanSclera":
                    mat.color = new Color(0.96f, 0.96f, 0.96f);
                    SetRoughness(mat, 0.2f, 0.0f);
                    break;

                case "ExplorerHat":
                    mat.color = new Color(0.36f, 0.24f, 0.14f);
                    SetRoughness(mat, 0.9f, 0.0f);
                    break;

                case "ExplorerPants":
                    mat.color = new Color(0.26f, 0.26f, 0.26f);
                    SetRoughness(mat, 0.85f, 0.0f);
                    break;

                case "WarningIndicator":
                    mat.color = new Color(1.0f, 0.15f, 0.15f, 0.65f);
                    SetRoughness(mat, 0.2f, 0.0f);
                    SetEmission(mat, new Color(1.0f, 0.15f, 0.15f), 2.5f);
                    break;

                case "PressurePlate":
                    mat.color = new Color(0.40f, 0.35f, 0.30f);
                    SetRoughness(mat, 0.85f, 0.2f);
                    break;

                case "TrapSpike":
                    mat.color = new Color(0.55f, 0.58f, 0.62f);
                    SetRoughness(mat, 0.3f, 0.85f);
                    break;

                case "SwingingStone":
                    mat.color = new Color(0.25f, 0.23f, 0.21f);
                    mat.mainTexture = GetRockTexture();
                    SetRoughness(mat, 0.85f, 0.1f);
                    break;

                case "IronRail":
                    mat.color = new Color(0.65f, 0.68f, 0.72f);
                    SetRoughness(mat, 0.35f, 0.95f);
                    break;

                case "RailTie":
                    mat.color = new Color(0.28f, 0.18f, 0.10f);
                    SetRoughness(mat, 0.85f, 0.05f);
                    break;

                case "TrolleyWood":
                    mat.color = new Color(0.36f, 0.22f, 0.14f);
                    SetRoughness(mat, 0.80f, 0.05f);
                    break;

                case "TrolleyIron":
                    mat.color = new Color(0.22f, 0.24f, 0.26f);
                    SetRoughness(mat, 0.40f, 0.85f);
                    break;

                case "JetpackMetal":
                    mat.color = new Color(0.72f, 0.75f, 0.80f);
                    SetRoughness(mat, 0.25f, 0.90f);
                    break;

                case "JetpackFlame":
                    mat.color = new Color(1.0f, 0.55f, 0.15f);
                    SetRoughness(mat, 0.1f, 0.0f);
                    SetEmission(mat, new Color(1.0f, 0.65f, 0.15f), 4.5f);
                    break;

                case "SignWood":
                    mat.color = new Color(0.24f, 0.14f, 0.08f);
                    mat.mainTexture = GetFloorTexture();
                    SetRoughness(mat, 0.85f, 0.05f);
                    break;

                case "SignArrow":
                    mat.color = new Color(1.0f, 0.85f, 0.1f);
                    SetRoughness(mat, 0.1f, 0.1f);
                    SetEmission(mat, new Color(1.0f, 0.85f, 0.1f), 3.8f);
                    break;

                case "SignArrowCyan":
                    mat.color = new Color(0.1f, 0.95f, 1.0f);
                    SetRoughness(mat, 0.1f, 0.1f);
                    SetEmission(mat, new Color(0.1f, 0.95f, 1.0f), 3.8f);
                    break;

                case "HazardStripe":
                    mat.color = new Color(0.95f, 0.65f, 0.1f);
                    SetRoughness(mat, 0.5f, 0.1f);
                    SetEmission(mat, new Color(0.85f, 0.45f, 0.05f), 1.2f);
                    break;

                case "MapParchment":
                    mat.color = new Color(0.93f, 0.86f, 0.72f);
                    SetRoughness(mat, 0.90f, 0.0f);
                    break;

                case "MapInk":
                    mat.color = new Color(0.32f, 0.20f, 0.12f);
                    SetRoughness(mat, 0.85f, 0.0f);
                    break;

                case "MapSeal":
                    mat.color = new Color(0.85f, 0.15f, 0.15f);
                    SetRoughness(mat, 0.35f, 0.1f);
                    SetEmission(mat, new Color(0.6f, 0.05f, 0.05f), 0.5f);
                    break;

                case "ExpeditionBag":
                    mat.color = new Color(0.34f, 0.18f, 0.10f);
                    SetRoughness(mat, 0.75f, 0.05f);
                    break;

                case "BagStrap":
                    mat.color = new Color(0.22f, 0.12f, 0.06f);
                    SetRoughness(mat, 0.85f, 0.0f);
                    break;

                case "GirlHair":
                    mat.color = new Color(0.18f, 0.11f, 0.07f);
                    SetRoughness(mat, 0.45f, 0.05f);
                    break;

                case "GirlShirt":
                    mat.color = new Color(0.24f, 0.45f, 0.40f);
                    SetRoughness(mat, 0.85f, 0.02f);
                    break;

                case "GirlPants":
                    mat.color = new Color(0.30f, 0.26f, 0.22f);
                    SetRoughness(mat, 0.85f, 0.02f);
                    break;

                default:
                    mat.color = Color.white;
                    break;
            }

            Cache[type] = mat;
            return mat;
        }

        private static Texture2D GetRockTexture()
        {
            if (rockTexture != null) return rockTexture;
            rockTexture = GenerateNoiseTexture(new Color(0.25f, 0.27f, 0.30f), new Color(0.12f, 0.13f, 0.15f), 0.06f);
            return rockTexture;
        }

        private static Texture2D GetFloorTexture()
        {
            if (floorStoneTexture != null) return floorStoneTexture;
            floorStoneTexture = GenerateNoiseTexture(new Color(0.32f, 0.34f, 0.36f), new Color(0.18f, 0.20f, 0.22f), 0.08f);
            return floorStoneTexture;
        }

        private static Texture2D GenerateNoiseTexture(Color lightColor, Color darkColor, float freq)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * freq, y * freq);
                    float n2 = Mathf.PerlinNoise(x * freq * 2.5f + 12f, y * freq * 2.5f + 12f) * 0.5f;
                    float v = Mathf.Clamp01((n1 + n2) / 1.5f);
                    Color col = Color.Lerp(darkColor, lightColor, v);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply(true, true);
            return tex;
        }

        private static void SetRoughness(Material mat, float smoothness, float metallic)
        {
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        }

        private static void SetEmission(Material mat, Color emissionColor, float intensity)
        {
            Color finalColor = emissionColor * intensity;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", finalColor);
            }
        }
    }
}
