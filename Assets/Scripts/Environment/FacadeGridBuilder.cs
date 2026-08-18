using UnityEngine;

namespace CastleBusters.Environment
{
    public class FacadeGridBuilder : MonoBehaviour
    {
        [Header("Castle Artwork (Image to Slice)")]
        public Texture2D castleTexture;
        public Color fallbackColor = new Color(0.7f, 0.5f, 0.3f);

        [Header("Grid Dimensions (Dense & Tight)")]
        public int columns = 16;
        public int rows = 12;
        public float totalWidth = 6f;
        public float totalHeight = 4.5f;

        [Header("Block Attributes")]
        public BlockMaterial materialType = BlockMaterial.Wood;
        public float blockHealth = 25f;
        public bool generateOnStart = true;

        private void Start()
        {
            if (Application.isPlaying && generateOnStart && transform.childCount == 0)
            {
                GenerateSlicedCastleFacade();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && castleTexture != null && transform.childCount == 0)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && !Application.isPlaying && transform.childCount == 0)
                    {
                        GenerateSlicedCastleFacade();
                    }
                };
            }
        }
#endif

        [ContextMenu("Generate Sliced Castle Facade")]
        public void GenerateSlicedCastleFacade()
        {
            // Clear existing children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            float chunkWidth = totalWidth / columns;
            float chunkHeight = totalHeight / rows;

            Vector2 origin = transform.position;
            Vector2 startPos = origin - new Vector2(totalWidth / 2f - chunkWidth / 2f, totalHeight / 2f - chunkHeight / 2f);

            // Calculate Texture UV slices if texture is assigned
            Sprite[,] slicedSprites = null;
            if (castleTexture != null)
            {
                slicedSprites = SliceTextureIntoSprites(castleTexture, columns, rows);
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    // Tight position calculation (zero gaps)
                    Vector2 pos = startPos + new Vector2(c * chunkWidth, r * chunkHeight);

                    GameObject chunk = new GameObject($"Chunk_{c}_{r}");
                    chunk.transform.position = pos;
                    chunk.transform.SetParent(transform);

                    // Add SpriteRenderer
                    SpriteRenderer sr = chunk.AddComponent<SpriteRenderer>();

                    if (slicedSprites != null && slicedSprites[c, r] != null)
                    {
                        sr.sprite = slicedSprites[c, r];
                        // Match sprite size to chunk dimensions exactly
                        float spriteW = sr.sprite.bounds.size.x;
                        float spriteH = sr.sprite.bounds.size.y;
                        if (spriteW > 0 && spriteH > 0)
                        {
                            chunk.transform.localScale = new Vector3(chunkWidth / spriteW, chunkHeight / spriteH, 1f);
                        }
                    }
                    else
                    {
                        // Fallback square sprite
                        sr.sprite = CreateDefaultSquareSprite();
                        sr.color = fallbackColor;
                        chunk.transform.localScale = new Vector3(chunkWidth, chunkHeight, 1f);
                    }

                    sr.sortingOrder = 2;

                    // Add Kinematic Rigidbody 2D & BoxCollider 2D
                    Rigidbody2D rb = chunk.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.useFullKinematicContacts = true;

                    BoxCollider2D col = chunk.AddComponent<BoxCollider2D>();
                    col.size = Vector2.one; // Fit sprite bounds perfectly

                    // Add DestructibleBlock
                    DestructibleBlock block = chunk.AddComponent<DestructibleBlock>();
                    block.materialType = materialType;
                    block.maxHealth = blockHealth;
                    block.currentHealth = blockHealth;
                    block.isAttachedToFrame = true;
                    block.spawnDebrisOnDestroy = true;

                    int colIndex = c;
                    int rowIndex = r;
                    block.OnBlockDestroyed += () => CheckStructuralCollapse(colIndex, rowIndex);
                }
            }

            // Auto-refresh parent Castle health calculation
            Castle parentCastle = GetComponentInParent<Castle>();
            if (parentCastle != null)
            {
                parentCastle.RefreshCastleHealth();
            }

            Debug.Log($"[FacadeGridBuilder] Successfully generated tight castle facade with {columns * rows} sliced blocks!");
        }

        private bool isEvaluatingCollapse = false;

        public void CheckStructuralCollapse(int destroyedCol, int destroyedRow)
        {
            if (isEvaluatingCollapse || !Application.isPlaying) return;
            isEvaluatingCollapse = true;

            // Find all active blocks in grid
            DestructibleBlock[,] grid = new DestructibleBlock[columns, rows];
            DestructibleBlock[] allBlocks = GetComponentsInChildren<DestructibleBlock>();

            foreach (var b in allBlocks)
            {
                if (b != null && !b.IsDestroyed)
                {
                    // Match position to grid coords
                    int c = Mathf.Clamp(Mathf.FloorToInt((b.transform.localPosition.x + totalWidth / 2f) / (totalWidth / columns)), 0, columns - 1);
                    int r = Mathf.Clamp(Mathf.FloorToInt((b.transform.localPosition.y + totalHeight / 2f) / (totalHeight / rows)), 0, rows - 1);
                    grid[c, r] = b;
                }
            }

            // BFS from bottom row (r = 0) to find all ground-connected blocks
            bool[,] isAnchored = new bool[columns, rows];
            System.Collections.Generic.Queue<Vector2Int> queue = new System.Collections.Generic.Queue<Vector2Int>();

            for (int c = 0; c < columns; c++)
            {
                if (grid[c, 0] != null && !grid[c, 0].IsDestroyed)
                {
                    isAnchored[c, 0] = true;
                    queue.Enqueue(new Vector2Int(c, 0));
                }
            }

            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),  // Up
                new Vector2Int(0, -1), // Down
                new Vector2Int(1, 0),  // Right
                new Vector2Int(-1, 0)  // Left
            };

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                foreach (var dir in directions)
                {
                    int nc = current.x + dir.x;
                    int nr = current.y + dir.y;

                    if (nc >= 0 && nc < columns && nr >= 0 && nr < rows)
                    {
                        if (grid[nc, nr] != null && !grid[nc, nr].IsDestroyed && !isAnchored[nc, nr])
                        {
                            isAnchored[nc, nr] = true;
                            queue.Enqueue(new Vector2Int(nc, nr));
                        }
                    }
                }
            }

            // Collapse all unanchored floating blocks!
            bool collapsedAny = false;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    if (grid[c, r] != null && !grid[c, r].IsDestroyed && !isAnchored[c, r])
                    {
                        collapsedAny = true;
                        Destroy(grid[c, r].gameObject);
                    }
                }
            }

            if (collapsedAny)
            {
                Castle parentCastle = GetComponentInParent<Castle>();
                if (parentCastle != null) parentCastle.RefreshCastleHealth();
            }

            isEvaluatingCollapse = false;
        }

        private Sprite[,] SliceTextureIntoSprites(Texture2D tex, int cols, int rows)
        {
            Sprite[,] sprites = new Sprite[cols, rows];

            int texW = tex.width;
            int texH = tex.height;
            float sliceW = (float)texW / cols;
            float sliceH = (float)texH / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Rect rect = new Rect(c * sliceW, r * sliceH, sliceW, sliceH);
                    sprites[c, r] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                }
            }

            return sprites;
        }

        private Sprite CreateDefaultSquareSprite()
        {
            Texture2D t = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            t.SetPixels(colors);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
