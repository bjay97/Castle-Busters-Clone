using UnityEngine;

namespace CastleBusters.Environment
{
    [ExecuteAlways]
    public class TerrainTextureTiler : MonoBehaviour
    {
        [Header("Texture Asset & Material")]
        public Sprite groundSprite;              // Ground sprite asset
        public Texture2D groundTexture;          // Ground texture (Supports 1920x1080 parallax layers or seamless tiles)
        public Material customMaterial;          // Optional custom material
        public Color textureTint = Color.white;  // Color tint for the texture (default: Opaque White)

        [Header("Tiling & Scaling Controls")]
        public bool autoAspectUV = true;         // Auto-preserves 16:9 aspect ratio for 1920x1080 textures
        public Vector2 textureTiling = new Vector2(0.2f, 0.2f); // Tiling factor per world unit
        public float globalTextureScale = 1.0f;  // Master texture scale slider
        public Vector2 textureOffset = Vector2.zero; // Texture UV offset
        public int sortingOrder = 5;
        public string sortingLayerName = "Default";

        [Header("Mesh & Shape Options")]
        public float meshDepth = 6.0f;           // Depth for 2D filled ground if building from Edge/Polygon Collider

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            ApplyTiling();
        }

        private void OnEnable()
        {
            ApplyTiling();
        }

        private void OnValidate()
        {
            ApplyTiling();
        }

        [ContextMenu("Apply Terrain Texture Tiling")]
        public void ApplyTiling()
        {
            // Ensure texture Tint alpha is non-zero
            if (textureTint.a <= 0.01f) textureTint = Color.white;

            // Auto-extract Texture2D from groundSprite if assigned
            if (groundSprite != null && groundTexture == null)
            {
                groundTexture = groundSprite.texture;
            }

            // Ensure texture wrap mode is Repeat for seamless tiling
            if (groundTexture != null)
            {
                groundTexture.wrapMode = TextureWrapMode.Repeat;
            }

            // 1. Check for EdgeCollider2D or PolygonCollider2D to build 2D filled mesh
            EdgeCollider2D edgeCol = GetComponent<EdgeCollider2D>();
            PolygonCollider2D polyCol = GetComponent<PolygonCollider2D>();

            if (edgeCol != null && edgeCol.points != null && edgeCol.points.Length > 1)
            {
                BuildMeshFromPoints(edgeCol.points);
                return;
            }
            else if (polyCol != null && polyCol.points != null && polyCol.points.Length > 2)
            {
                BuildMeshFromPoints(polyCol.points);
                return;
            }

            // 2. Handle MeshFilter & MeshRenderer if present
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                UpdateMeshUVs(meshFilter.sharedMesh);
                SetupMaterial();
                return;
            }

            // 3. Handle SpriteRenderer component if attached
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (groundSprite != null) sr.sprite = groundSprite;

                // Create custom tiled material if standard SpriteDrawMode.Tiled doesn't tile due to Tight sprite mesh
                Material mat = new Material(Shader.Find("Sprites/Default"));
                if (groundTexture != null) mat.mainTexture = groundTexture;
                mat.color = textureTint;

                sr.sharedMaterial = mat;
                sr.sortingOrder = sortingOrder;
                if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;
                sr.color = textureTint;
                return;
            }

            // 4. Fallback: Any generic Renderer
            Renderer genRenderer = GetComponent<Renderer>();
            if (genRenderer != null)
            {
                Material mat = customMaterial != null ? customMaterial : new Material(Shader.Find("Sprites/Default"));
                if (groundTexture != null) mat.mainTexture = groundTexture;
                mat.color = textureTint;

                genRenderer.sharedMaterial = mat;
                genRenderer.sortingOrder = sortingOrder;
                if (!string.IsNullOrEmpty(sortingLayerName)) genRenderer.sortingLayerName = sortingLayerName;
            }
        }

        private void BuildMeshFromPoints(Vector2[] points)
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            int count = points.Length;
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[count * 2];
            Vector2[] uvs = new Vector2[count * 2];
            Color[] colors = new Color[count * 2];
            int[] triangles = new int[(count - 1) * 6];

            float aspectFactor = 1.0f;
            if (autoAspectUV && groundTexture != null && groundTexture.height > 0)
            {
                aspectFactor = (float)groundTexture.width / (float)groundTexture.height;
            }

            float effectiveTilingX = (textureTiling.x * globalTextureScale) / aspectFactor;
            float effectiveTilingY = textureTiling.y * globalTextureScale;

            for (int i = 0; i < count; i++)
            {
                float x = points[i].x;
                float topY = points[i].y;
                float bottomY = topY - meshDepth;

                vertices[i * 2] = new Vector3(x, topY, 0f);
                vertices[i * 2 + 1] = new Vector3(x, bottomY, 0f);

                uvs[i * 2] = new Vector2((x * effectiveTilingX) + textureOffset.x, (topY * effectiveTilingY) + textureOffset.y);
                uvs[i * 2 + 1] = new Vector2((x * effectiveTilingX) + textureOffset.x, (bottomY * effectiveTilingY) + textureOffset.y);

                colors[i * 2] = textureTint;
                colors[i * 2 + 1] = textureTint;
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
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
            SetupMaterial();
        }

        private void UpdateMeshUVs(Mesh mesh)
        {
            if (mesh == null) return;

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];

            float aspectFactor = 1.0f;
            if (autoAspectUV && groundTexture != null && groundTexture.height > 0)
            {
                aspectFactor = (float)groundTexture.width / (float)groundTexture.height;
            }

            float effectiveTilingX = (textureTiling.x * globalTextureScale) / aspectFactor;
            float effectiveTilingY = textureTiling.y * globalTextureScale;

            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = new Vector2((vertices[i].x * effectiveTilingX) + textureOffset.x, (vertices[i].y * effectiveTilingY) + textureOffset.y);
            }

            mesh.uv = uvs;
        }

        private void SetupMaterial()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) return;

            if (customMaterial != null)
            {
                meshRenderer.sharedMaterial = customMaterial;
            }
            else
            {
                Shader defaultShader = Shader.Find("Sprites/Default");
                if (defaultShader == null) defaultShader = Shader.Find("Unlit/Texture");

                Material mat = new Material(defaultShader);
                if (groundTexture != null)
                {
                    mat.mainTexture = groundTexture;
                }
                mat.color = textureTint;
                meshRenderer.sharedMaterial = mat;
            }

            meshRenderer.sortingOrder = sortingOrder;
            if (!string.IsNullOrEmpty(sortingLayerName)) meshRenderer.sortingLayerName = sortingLayerName;
        }
    }
}
