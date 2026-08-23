using UnityEngine;

namespace CastleBusters.Environment
{
    [RequireComponent(typeof(EdgeCollider2D), typeof(MeshFilter), typeof(MeshRenderer))]
    public class UnevenTerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Wave Controls (Tight Battlefield Scope)")]
        public float terrainWidth = 18f;     // Tight battlefield arena size
        public int pointCount = 120;
        public float peakHeight = 0.35f;     // Realistic micro-peaks & valleys height
        public float waveFrequency = 1.8f;   // High frequency = mini-peaks MUCH narrower than castle chassis!
        public float groundDepth = 5f;       // Depth of ground fill below peaks

        [Header("Visual Styling")]
        public Color topGroundColor = new Color(0.45f, 0.75f, 0.25f); // Grass Green top
        public Color bottomGroundColor = new Color(0.4f, 0.25f, 0.15f); // Dirt Brown bottom
        public int sortingOrder = 5;

        private EdgeCollider2D edgeCollider;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            GenerateUnevenGround();
        }

        [ContextMenu("Generate Peaks & Valleys Mesh")]
        public void GenerateUnevenGround()
        {
            int safePointCount = Mathf.Clamp(pointCount, 10, 300);
            float safeWidth = Mathf.Max(1f, terrainWidth);

            // Unity does not allow SpriteRenderer and MeshRenderer on the same GameObject.
            // Safely remove SpriteRenderer if present.
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (Application.isPlaying) Destroy(sr);
                else DestroyImmediate(sr);
            }

            edgeCollider = GetComponent<EdgeCollider2D>();
            if (edgeCollider == null) edgeCollider = gameObject.AddComponent<EdgeCollider2D>();

            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            Vector2[] points = new Vector2[safePointCount];
            float startX = -safeWidth / 2f;
            float stepX = safeWidth / (safePointCount - 1);

            // 1. Calculate tight micro-wave height points for mini peaks & valleys
            for (int i = 0; i < safePointCount; i++)
            {
                float x = startX + (i * stepX);
                float y = Mathf.Sin(x * waveFrequency) * peakHeight 
                        + Mathf.Cos(x * waveFrequency * 2.7f) * (peakHeight * 0.5f);

                points[i] = new Vector2(x, y);
            }

            // 2. Assign Edge Collider
            edgeCollider.points = points;

            // 3. Build 2D Filled Mesh for terrain peaks and valleys
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[safePointCount * 2];
            Color[] colors = new Color[safePointCount * 2];
            int[] triangles = new int[(safePointCount - 1) * 6];

            for (int i = 0; i < safePointCount; i++)
            {
                float x = points[i].x;
                float topY = points[i].y;
                float bottomY = topY - groundDepth;

                vertices[i * 2] = new Vector3(x, topY, 0f);           // Top peak vertex
                vertices[i * 2 + 1] = new Vector3(x, bottomY, 0f);   // Bottom ground vertex

                colors[i * 2] = topGroundColor;
                colors[i * 2 + 1] = bottomGroundColor;
            }

            int triIndex = 0;
            for (int i = 0; i < safePointCount - 1; i++)
            {
                int topL = i * 2;
                int botL = i * 2 + 1;
                int topR = (i + 1) * 2;
                int botR = (i + 1) * 2 + 1;

                triangles[triIndex++] = topL;
                triangles[triIndex++] = topR;
                triangles[triIndex++] = botL;

                triangles[triIndex++] = botL;
                triangles[triIndex++] = topR;
                triangles[triIndex++] = botR;
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;

            // Material & Sorting Order setup
            Material mat = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.sharedMaterial = mat;
            meshRenderer.sortingOrder = sortingOrder;

            // Automatically decorate top surface contour with grass & bushes if decorator present
            GetComponent<TerrainFoliageDecorator>()?.DecorateTerrain();
        }
    }
}
