using UnityEngine;

namespace TreasureHunt
{
    public static class ProceduralMeshHelper
    {
        public static Mesh CreateCone(float radius = 0.5f, float height = 2.0f, int segments = 8, bool pointDown = false)
        {
            Mesh mesh = new Mesh { name = "ConeMesh" };

            int vertCount = segments + 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 3 + segments * 3];

            Vector3 tip = pointDown ? new Vector3(0, -height, 0) : new Vector3(0, height, 0);
            Vector3 baseCenter = Vector3.zero;

            vertices[0] = tip;
            normals[0] = pointDown ? Vector3.down : Vector3.up;
            uvs[0] = new Vector2(0.5f, 1f);

            vertices[1] = baseCenter;
            normals[1] = pointDown ? Vector3.up : Vector3.down;
            uvs[1] = new Vector2(0.5f, 0f);

            float angleStep = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                int idx = i + 2;
                vertices[idx] = new Vector3(x, 0, z);
                normals[idx] = new Vector3(x, 0.2f, z).normalized;
                uvs[idx] = new Vector2((float)i / segments, 0f);
            }

            int triIdx = 0;
            // Sides
            for (int i = 0; i < segments; i++)
            {
                int curr = i + 2;
                int next = ((i + 1) % segments) + 2;

                if (pointDown)
                {
                    triangles[triIdx++] = 0;
                    triangles[triIdx++] = next;
                    triangles[triIdx++] = curr;
                }
                else
                {
                    triangles[triIdx++] = 0;
                    triangles[triIdx++] = curr;
                    triangles[triIdx++] = next;
                }
            }

            // Base
            for (int i = 0; i < segments; i++)
            {
                int curr = i + 2;
                int next = ((i + 1) % segments) + 2;

                if (pointDown)
                {
                    triangles[triIdx++] = 1;
                    triangles[triIdx++] = curr;
                    triangles[triIdx++] = next;
                }
                else
                {
                    triangles[triIdx++] = 1;
                    triangles[triIdx++] = next;
                    triangles[triIdx++] = curr;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateCrystalMesh(float radius = 0.35f, float height = 1.4f, int facets = 6)
        {
            Mesh mesh = new Mesh { name = "CrystalMesh" };

            int vertCount = facets * 2 + 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[facets * 6 * 2];

            Vector3 topTip = new Vector3(0, height * 0.5f, 0);
            Vector3 botTip = new Vector3(0, -height * 0.5f, 0);

            vertices[0] = topTip;
            uvs[0] = new Vector2(0.5f, 1f);

            vertices[1] = botTip;
            uvs[1] = new Vector2(0.5f, 0f);

            float angleStep = Mathf.PI * 2f / facets;
            float midHeight = height * 0.15f;

            for (int i = 0; i < facets; i++)
            {
                float angle = i * angleStep;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                // Upper ring
                int uIdx = 2 + i;
                vertices[uIdx] = new Vector3(x, midHeight, z);
                uvs[uIdx] = new Vector2((float)i / facets, 0.6f);

                // Lower ring
                int lIdx = 2 + facets + i;
                vertices[lIdx] = new Vector3(x * 0.9f, -midHeight, z * 0.9f);
                uvs[lIdx] = new Vector2((float)i / facets, 0.4f);
            }

            int triIdx = 0;
            for (int i = 0; i < facets; i++)
            {
                int next = (i + 1) % facets;
                int uCurr = 2 + i;
                int uNext = 2 + next;
                int lCurr = 2 + facets + i;
                int lNext = 2 + facets + next;

                // Top pyramid cap
                triangles[triIdx++] = 0;
                triangles[triIdx++] = uCurr;
                triangles[triIdx++] = uNext;

                // Mid band 1
                triangles[triIdx++] = uCurr;
                triangles[triIdx++] = lCurr;
                triangles[triIdx++] = uNext;

                // Mid band 2
                triangles[triIdx++] = uNext;
                triangles[triIdx++] = lCurr;
                triangles[triIdx++] = lNext;

                // Bottom pyramid cap
                triangles[triIdx++] = 1;
                triangles[triIdx++] = lNext;
                triangles[triIdx++] = lCurr;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static GameObject CreateCrystalCluster(Transform parent, string crystalType = "TurquoiseCrystal", int count = 5)
        {
            GameObject cluster = new GameObject("CrystalCluster_" + crystalType);
            if (parent != null) cluster.transform.SetParent(parent, false);

            Material mat = MaterialFactory.GetMaterial(crystalType);
            Mesh crystalMesh = CreateCrystalMesh();

            for (int i = 0; i < count; i++)
            {
                GameObject crystal = new GameObject("Spike_" + i);
                crystal.transform.SetParent(cluster.transform, false);

                float angle = i * (360f / count) + Random.Range(-15f, 15f);
                float dist = Random.Range(0.1f, 0.4f);
                Vector3 pos = Quaternion.Euler(0, angle, 0) * Vector3.forward * dist;
                crystal.transform.localPosition = pos;

                float tilt = Random.Range(15f, 35f);
                crystal.transform.localRotation = Quaternion.Euler(Random.Range(-tilt, tilt), angle, Random.Range(-tilt, tilt));

                float scaleY = Random.Range(0.7f, 1.3f);
                float scaleXZ = Random.Range(0.6f, 1.1f);
                crystal.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);

                MeshFilter mf = crystal.AddComponent<MeshFilter>();
                mf.sharedMesh = crystalMesh;

                MeshRenderer mr = crystal.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
            }

            // Glow point light
            GameObject lightObj = new GameObject("CrystalLight");
            lightObj.transform.SetParent(cluster.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 0.6f, 0);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = mat.color;
            light.range = 5.0f;
            light.intensity = 2.2f;

            return cluster;
        }

        public static GameObject CreateTorch(Transform parent, Vector3 localPos)
        {
            GameObject torch = new GameObject("Torch");
            if (parent != null) torch.transform.SetParent(parent, false);
            torch.transform.localPosition = localPos;

            // Wood stick
            GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stick.name = "Stick";
            stick.transform.SetParent(torch.transform, false);
            stick.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
            stick.transform.localPosition = new Vector3(0, 0.5f, 0);
            stick.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchWood");
            Object.Destroy(stick.GetComponent<Collider>());

            // Metal sconce
            GameObject sconce = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sconce.name = "Sconce";
            sconce.transform.SetParent(torch.transform, false);
            sconce.transform.localScale = new Vector3(0.18f, 0.1f, 0.18f);
            sconce.transform.localPosition = new Vector3(0, 0.95f, 0);
            sconce.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TrapSpike");
            Object.Destroy(sconce.GetComponent<Collider>());

            // Flame head
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "FlameMesh";
            flame.transform.SetParent(torch.transform, false);
            flame.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
            flame.transform.localPosition = new Vector3(0, 1.15f, 0);
            flame.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.GetMaterial("TorchFlame");
            Object.Destroy(flame.GetComponent<Collider>());

            // Warm flickering light
            GameObject lightObj = new GameObject("TorchLight");
            lightObj.transform.SetParent(torch.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 1.3f, 0);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.65f, 0.25f);
            l.range = 8.5f;
            l.intensity = 2.8f;
            l.shadows = LightShadows.Soft;

            torch.AddComponent<TorchFlicker>();

            return torch;
        }
    }

    public class TorchFlicker : MonoBehaviour
    {
        private Light torchLight;
        private float baseIntensity = 2.8f;
        private float flickerSpeed = 12f;

        private void Start()
        {
            torchLight = GetComponentInChildren<Light>();
            if (torchLight != null) baseIntensity = torchLight.intensity;
        }

        private void Update()
        {
            if (torchLight == null) return;
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.0f);
            torchLight.intensity = baseIntensity * (0.85f + noise * 0.35f);
        }
    }
}
