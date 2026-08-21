using UnityEngine;

namespace CastleBusters.Environment
{
    [ExecuteAlways]
    [RequireComponent(typeof(EdgeCollider2D), typeof(MeshFilter), typeof(MeshRenderer))]
    public class GroundMeshFromCollider : MonoBehaviour
    {
        [Header("Mesh Styling")]
        public float groundDepth = 5f;
        public Color topGroundColor = new Color(0.45f, 0.75f, 0.25f); // Grass Green top
        public Color bottomGroundColor = new Color(0.4f, 0.25f, 0.15f); // Dirt Brown bottom
        public int sortingOrder = 5;

        private EdgeCollider2D edgeCollider;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void OnEnable()
        {
            GenerateMeshFromCollider();
        }

        private void Update()
        {
            // Auto update in edit mode if collider points changed
            if (!Application.isPlaying)
            {
                GenerateMeshFromCollider();
            }
        }

        private void OnValidate()
        {
            GenerateMeshFromCollider();
        }

        [ContextMenu("Rebuild Mesh From Collider")]
        public void GenerateMeshFromCollider()
        {
            edgeCollider = GetComponent<EdgeCollider2D>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (edgeCollider == null || edgeCollider.points == null || edgeCollider.points.Length < 2)
                return;

            // Safely remove SpriteRenderer if present on the same object
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (Application.isPlaying) Destroy(sr);
                else DestroyImmediate(sr);
            }

            Vector2[] points = edgeCollider.points;
            int count = points.Length;

            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[count * 2];
            Color[] colors = new Color[count * 2];
            int[] triangles = new int[(count - 1) * 6];

            for (int i = 0; i < count; i++)
            {
                float x = points[i].x;
                float topY = points[i].y;
                float bottomY = topY - groundDepth;

                vertices[i * 2] = new Vector3(x, topY, 0f);           // Top edge vertex
                vertices[i * 2 + 1] = new Vector3(x, bottomY, 0f);   // Bottom depth vertex

                colors[i * 2] = topGroundColor;
                colors[i * 2 + 1] = bottomGroundColor;
            }

            int triIndex = 0;
            for (int i = 0; i < count - 1; i++)
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

            if (meshFilter != null)
            {
                meshFilter.mesh = mesh;
            }

            if (meshRenderer != null)
            {
                if (meshRenderer.sharedMaterial == null)
                {
                    meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                }
                meshRenderer.sortingOrder = sortingOrder;
            }
        }
    }
}
