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

        private Texture2D dynamicFacadeTexture;
        private SpriteRenderer fullFacadeRenderer;
        private Sprite fullFacadeSprite;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                EnsureFacadeVisualInitialized();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                EnsureFacadeVisualInitialized();
            }
        }

        public void EnsureFacadeVisualInitialized()
        {
            if (dynamicFacadeTexture == null)
            {
                Transform existingFullObj = transform.Find("FullFacadeVisual");
                if (existingFullObj != null)
                {
                    DestroyImmediate(existingFullObj.gameObject);
                }

                CreateFullDynamicFacadeVisual();
            }

            // Disable all underlying tile SpriteRenderers so full dynamic mask facade visual is shown
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in childRenderers)
            {
                if (sr != null && sr != fullFacadeRenderer)
                {
                    sr.enabled = false;
                }
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

            // Create Dynamic Masked Facade Texture & Renderer
            CreateFullDynamicFacadeVisual();

            float chunkWidth = totalWidth / columns;
            float chunkHeight = totalHeight / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    // Tight local position calculation relative to parent facade transform
                    Vector3 localPos = new Vector3(
                        -totalWidth / 2f + chunkWidth / 2f + (c * chunkWidth),
                        -totalHeight / 2f + chunkHeight / 2f + (r * chunkHeight),
                        0f
                    );

                    GameObject chunk = new GameObject($"Chunk_{c}_{r}");
                    chunk.transform.SetParent(transform);
                    chunk.transform.localPosition = localPos;

                    // Invisible SpriteRenderer for underlying physics grid (visual handled by full dynamic texture facade)
                    SpriteRenderer sr = chunk.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateDefaultSquareSprite();
                    sr.color = fallbackColor;
                    sr.enabled = false; // Hide individual tiles so dynamic masked texture is visible
                    chunk.transform.localScale = new Vector3(chunkWidth, chunkHeight, 1f);

                    BoxCollider2D col = chunk.AddComponent<BoxCollider2D>();
                    col.size = Vector2.one;

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

            Castle parentCastle = GetComponentInParent<Castle>();
            if (parentCastle != null) parentCastle.RefreshCastleHealth();

            CastleFacadeVisibility visibility = GetComponent<CastleFacadeVisibility>();
            if (visibility == null) visibility = GetComponentInParent<CastleFacadeVisibility>();
            if (visibility != null) visibility.RefreshVisibility();

            Debug.Log($"[FacadeGridBuilder] Successfully generated smooth masked castle facade with {columns * rows} grid colliders!");
        }

        private void CreateFullDynamicFacadeVisual()
        {
            if (castleTexture == null)
            {
                SpriteRenderer[] childSRs = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in childSRs)
                {
                    if (sr != null && sr.sprite != null && sr.sprite.texture != null)
                    {
                        castleTexture = sr.sprite.texture;
                        break;
                    }
                }
            }

            if (castleTexture != null)
            {
                RenderTexture rt = RenderTexture.GetTemporary(castleTexture.width, castleTexture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(castleTexture, rt);
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                dynamicFacadeTexture = new Texture2D(castleTexture.width, castleTexture.height, TextureFormat.RGBA32, false);
                dynamicFacadeTexture.ReadPixels(new Rect(0, 0, castleTexture.width, castleTexture.height), 0, 0);
                dynamicFacadeTexture.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
            else
            {
                int w = 512;
                int h = 384;
                dynamicFacadeTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                Color[] colors = new Color[w * h];
                for (int i = 0; i < colors.Length; i++) colors[i] = fallbackColor;
                dynamicFacadeTexture.SetPixels(colors);
                dynamicFacadeTexture.Apply();
            }

            GameObject fullObj = new GameObject("FullFacadeVisual");
            fullObj.transform.SetParent(transform);
            fullObj.transform.localPosition = Vector3.zero;
            fullObj.transform.localRotation = Quaternion.identity;

            fullFacadeRenderer = fullObj.AddComponent<SpriteRenderer>();
            fullFacadeSprite = Sprite.Create(dynamicFacadeTexture, new Rect(0, 0, dynamicFacadeTexture.width, dynamicFacadeTexture.height), new Vector2(0.5f, 0.5f), 100f);
            fullFacadeRenderer.sprite = fullFacadeSprite;

            float spriteW = fullFacadeSprite.bounds.size.x;
            float spriteH = fullFacadeSprite.bounds.size.y;
            fullObj.transform.localScale = new Vector3(totalWidth / spriteW, totalHeight / spriteH, 1f);
            fullFacadeRenderer.sortingOrder = 3;
        }

        public static void CarveAllFacadesAt(Vector2 worldPos, float radius)
        {
            FacadeGridBuilder[] builders = FindObjectsByType<FacadeGridBuilder>(FindObjectsSortMode.None);
            foreach (var builder in builders)
            {
                if (builder != null)
                {
                    builder.CarveFacadeImpact(worldPos, radius);
                }
            }
        }

        public void CarveFacadeImpact(Vector2 worldPos, float radius)
        {
            if (dynamicFacadeTexture == null)
            {
                EnsureFacadeVisualInitialized();
            }
            if (dynamicFacadeTexture == null) return;

            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            // Bounds check
            if (Mathf.Abs(localPos.x) > (totalWidth / 2f + radius) || Mathf.Abs(localPos.y) > (totalHeight / 2f + radius))
            {
                return;
            }

            float u = (localPos.x + totalWidth / 2f) / totalWidth;
            float v = (localPos.y + totalHeight / 2f) / totalHeight;

            int texW = dynamicFacadeTexture.width;
            int texH = dynamicFacadeTexture.height;
            int cx = Mathf.Clamp(Mathf.RoundToInt(u * texW), 0, texW - 1);
            int cy = Mathf.Clamp(Mathf.RoundToInt(v * texH), 0, texH - 1);

            float rx = (radius / totalWidth) * texW;
            float ry = (radius / totalHeight) * texH;
            float avgR = (rx + ry) * 0.5f;

            // Ensure impact crater aligns with solid facade artwork pixels if hit landed on transparent outer margin
            if (dynamicFacadeTexture.GetPixel(cx, cy).a < 0.1f)
            {
                Vector2 dirToCenter = new Vector2(texW * 0.5f - cx, texH * 0.5f - cy).normalized;
                int maxSteps = Mathf.RoundToInt(avgR * 1.8f);
                for (int step = 1; step <= maxSteps; step++)
                {
                    int testX = Mathf.Clamp(cx + Mathf.RoundToInt(dirToCenter.x * step), 0, texW - 1);
                    int testY = Mathf.Clamp(cy + Mathf.RoundToInt(dirToCenter.y * step), 0, texH - 1);
                    if (dynamicFacadeTexture.GetPixel(testX, testY).a > 0.1f)
                    {
                        cx = testX;
                        cy = testY;
                        break;
                    }
                }
            }

            float seed = Random.Range(0f, 1000f);
            float aspectX = Random.Range(0.85f, 1.25f);
            float aspectY = Random.Range(0.85f, 1.25f);
            float rot = Random.Range(0f, Mathf.PI * 2f);
            float cosR = Mathf.Cos(rot);
            float sinR = Mathf.Sin(rot);

            int minX = Mathf.Clamp(Mathf.FloorToInt(cx - avgR * 1.5f), 0, texW - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(cx + avgR * 1.5f), 0, texW - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(cy - avgR * 1.5f), 0, texH - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(cy + avgR * 1.5f), 0, texH - 1);

            bool modified = false;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - cx);
                    float dy = (y - cy);

                    float rdx = (dx * cosR - dy * sinR) / aspectX;
                    float rdy = (dx * sinR + dy * cosR) / aspectY;

                    float dist = Mathf.Sqrt(rdx * rdx + rdy * rdy);
                    float angle = Mathf.Atan2(rdy, rdx);

                    float noise = Mathf.Sin(angle * 4f + seed) * 0.18f
                                + Mathf.Cos(angle * 7f + seed * 1.5f) * 0.12f
                                + Mathf.Sin(angle * 12f + seed * 3f) * 0.06f;

                    float craterRadius = avgR * (1.0f + noise);

                    if (dist <= craterRadius)
                    {
                        Color col = dynamicFacadeTexture.GetPixel(x, y);
                        if (col.a > 0f)
                        {
                            if (dist <= craterRadius - 1.5f)
                            {
                                col.a = 0f;
                            }
                            else
                            {
                                float edgeFactor = (dist - (craterRadius - 1.5f)) / 1.5f;
                                col.a = Mathf.Min(col.a, Mathf.Clamp01(edgeFactor));
                            }
                            dynamicFacadeTexture.SetPixel(x, y, col);
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                dynamicFacadeTexture.Apply();
            }

            // Damage underlying grid blocks within blast radius
            DestructibleBlock[] blocks = GetComponentsInChildren<DestructibleBlock>();
            foreach (var b in blocks)
            {
                if (b != null && !b.IsDestroyed)
                {
                    float distToBlock = Vector2.Distance(worldPos, b.transform.position);
                    if (distToBlock <= radius * 1.2f)
                    {
                        b.TakeDamage(blockHealth * 2f); // ensure destruction inside crater
                    }
                }
            }

            // Perform automatic pixel flood-fill cleanup for any isolated floating texture & physics sections
            CleanupFloatingTextureSections();
        }

        public void CleanupFloatingTextureSections()
        {
            if (dynamicFacadeTexture == null) return;

            int texW = dynamicFacadeTexture.width;
            int texH = dynamicFacadeTexture.height;

            // Downsampled 64x48 cell grid for fast BFS connectivity and speckle cleanup
            int gridW = 64;
            int gridH = 48;
            float stepX = (float)texW / gridW;
            float stepY = (float)texH / gridH;

            bool[,] hasContent = new bool[gridW, gridH];

            // 1. Full pixel occupancy check per cell to catch all pixel fragments & speckles
            for (int gy = 0; gy < gridH; gy++)
            {
                int startPy = Mathf.Clamp(Mathf.FloorToInt(gy * stepY), 0, texH - 1);
                int endPy = Mathf.Clamp(Mathf.CeilToInt((gy + 1) * stepY), 0, texH - 1);

                for (int gx = 0; gx < gridW; gx++)
                {
                    int startPx = Mathf.Clamp(Mathf.FloorToInt(gx * stepX), 0, texW - 1);
                    int endPx = Mathf.Clamp(Mathf.CeilToInt((gx + 1) * stepX), 0, texW - 1);

                    bool cellHasContent = false;
                    for (int py = startPy; py <= endPy && !cellHasContent; py += 2)
                    {
                        for (int px = startPx; px <= endPx && !cellHasContent; px += 2)
                        {
                            if (dynamicFacadeTexture.GetPixel(px, py).a > 0.05f)
                            {
                                cellHasContent = true;
                            }
                        }
                    }
                    hasContent[gx, gy] = cellHasContent;
                }
            }

            // 2. BFS from bottom row (gy = 0) to find all ground-anchored cells
            bool[,] isAnchored = new bool[gridW, gridH];
            System.Collections.Generic.Queue<Vector2Int> queue = new System.Collections.Generic.Queue<Vector2Int>();

            for (int gx = 0; gx < gridW; gx++)
            {
                if (hasContent[gx, 0])
                {
                    isAnchored[gx, 0] = true;
                    queue.Enqueue(new Vector2Int(gx, 0));
                }
            }

            Vector2Int[] dirs = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0)
            };

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();
                foreach (var dir in dirs)
                {
                    int nx = curr.x + dir.x;
                    int ny = curr.y + dir.y;
                    if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH)
                    {
                        if (hasContent[nx, ny] && !isAnchored[nx, ny])
                        {
                            isAnchored[nx, ny] = true;
                            queue.Enqueue(new Vector2Int(nx, ny));
                        }
                    }
                }
            }

            // 3. Remove tiny floating speckle clusters (< 4 connected cells) even if anchored
            bool[,] visited = new bool[gridW, gridH];
            for (int gy = 0; gy < gridH; gy++)
            {
                for (int gx = 0; gx < gridW; gx++)
                {
                    if (hasContent[gx, gy] && !visited[gx, gy])
                    {
                        System.Collections.Generic.List<Vector2Int> cluster = new System.Collections.Generic.List<Vector2Int>();
                        System.Collections.Generic.Queue<Vector2Int> cQueue = new System.Collections.Generic.Queue<Vector2Int>();

                        visited[gx, gy] = true;
                        cQueue.Enqueue(new Vector2Int(gx, gy));

                        while (cQueue.Count > 0)
                        {
                            Vector2Int cCurr = cQueue.Dequeue();
                            cluster.Add(cCurr);

                            foreach (var dir in dirs)
                            {
                                int nx = cCurr.x + dir.x;
                                int ny = cCurr.y + dir.y;
                                if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH)
                                {
                                    if (hasContent[nx, ny] && !visited[nx, ny])
                                    {
                                        visited[nx, ny] = true;
                                        cQueue.Enqueue(new Vector2Int(nx, ny));
                                    }
                                }
                            }
                        }

                        // If isolated cluster is tiny fragment (< 4 cells), mark for removal
                        if (cluster.Count < 4)
                        {
                            foreach (var cell in cluster)
                            {
                                isAnchored[cell.x, cell.y] = false;
                            }
                        }
                    }
                }
            }

            // 4. Clear all pixels & destroy underlying blocks for unanchored/speckle cells
            bool clearedAny = false;
            for (int gy = 0; gy < gridH; gy++)
            {
                for (int gx = 0; gx < gridW; gx++)
                {
                    if (hasContent[gx, gy] && !isAnchored[gx, gy])
                    {
                        int minX = Mathf.Clamp(Mathf.FloorToInt(gx * stepX), 0, texW - 1);
                        int maxX = Mathf.Clamp(Mathf.CeilToInt((gx + 1) * stepX), 0, texW - 1);
                        int minY = Mathf.Clamp(Mathf.FloorToInt(gy * stepY), 0, texH - 1);
                        int maxY = Mathf.Clamp(Mathf.CeilToInt((gy + 1) * stepY), 0, texH - 1);

                        for (int py = minY; py <= maxY; py++)
                        {
                            for (int px = minX; px <= maxX; px++)
                            {
                                Color c = dynamicFacadeTexture.GetPixel(px, py);
                                if (c.a > 0f)
                                {
                                    c.a = 0f;
                                    dynamicFacadeTexture.SetPixel(px, py, c);
                                    clearedAny = true;
                                }
                            }
                        }

                        // Clear matching physics blocks in this unanchored cell space
                        float localX = ((float)gx / gridW) * totalWidth - totalWidth / 2f;
                        float localY = ((float)gy / gridH) * totalHeight - totalHeight / 2f;
                        float cellW = totalWidth / gridW;
                        float cellH = totalHeight / gridH;

                        Vector2 cellCenterLocal = new Vector2(localX + cellW / 2f, localY + cellH / 2f);
                        Vector2 cellWorldPos = transform.TransformPoint(cellCenterLocal);

                        Collider2D[] cols = Physics2D.OverlapBoxAll(cellWorldPos, new Vector2(cellW * 1.3f, cellH * 1.3f), 0f);
                        foreach (var col in cols)
                        {
                            if (col != null && col.transform.IsChildOf(transform))
                            {
                                DestructibleBlock b = col.GetComponent<DestructibleBlock>();
                                if (b != null && !b.IsDestroyed)
                                {
                                    Destroy(b.gameObject);
                                }
                            }
                        }
                    }
                }
            }

            if (clearedAny)
            {
                dynamicFacadeTexture.Apply();

                Castle parentCastle = GetComponentInParent<Castle>();
                if (parentCastle != null) parentCastle.RefreshCastleHealth();
            }
        }

        private bool isEvaluatingCollapse = false;

        public void CheckStructuralCollapse(int destroyedCol, int destroyedRow)
        {
            if (isEvaluatingCollapse || !Application.isPlaying) return;
            isEvaluatingCollapse = true;

            float chunkW = totalWidth / columns;
            float chunkH = totalHeight / rows;

            DestructibleBlock[] allBlocks = GetComponentsInChildren<DestructibleBlock>();
            if (allBlocks == null || allBlocks.Length == 0)
            {
                isEvaluatingCollapse = false;
                return;
            }

            DestructibleBlock[,] grid = new DestructibleBlock[columns, rows];
            foreach (var b in allBlocks)
            {
                if (b != null && !b.IsDestroyed && b.gameObject.activeInHierarchy)
                {
                    float localX = b.transform.localPosition.x + totalWidth / 2f;
                    float localY = b.transform.localPosition.y + totalHeight / 2f;

                    int c = Mathf.Clamp(Mathf.FloorToInt(localX / chunkW), 0, columns - 1);
                    int r = Mathf.Clamp(Mathf.FloorToInt(localY / chunkH), 0, rows - 1);
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

            // Collect all unanchored floating blocks to destroy in a single batch
            System.Collections.Generic.List<GameObject> toDestroy = new System.Collections.Generic.List<GameObject>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    if (grid[c, r] != null && !grid[c, r].IsDestroyed && !isAnchored[c, r])
                    {
                        // Carve out unanchored block area from facade texture
                        CarveRectangleLocal(c * chunkW - totalWidth / 2f, r * chunkH - totalHeight / 2f, chunkW, chunkH);

                        // Unsubscribe listener so Destroy doesn't trigger recursive checks
                        grid[c, r].OnBlockDestroyed -= () => CheckStructuralCollapse(c, r);
                        toDestroy.Add(grid[c, r].gameObject);
                    }
                }
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                if (toDestroy[i] != null) Destroy(toDestroy[i]);
            }

            if (toDestroy.Count > 0)
            {
                Castle parentCastle = GetComponentInParent<Castle>();
                if (parentCastle != null) parentCastle.RefreshCastleHealth();
            }

            isEvaluatingCollapse = false;
        }

        private void CarveRectangleLocal(float localX, float localY, float width, float height)
        {
            if (dynamicFacadeTexture == null) return;

            int texW = dynamicFacadeTexture.width;
            int texH = dynamicFacadeTexture.height;

            float minU = (localX + totalWidth / 2f) / totalWidth;
            float maxU = (localX + width + totalWidth / 2f) / totalWidth;
            float minV = (localY + totalHeight / 2f) / totalHeight;
            float maxV = (localY + height + totalHeight / 2f) / totalHeight;

            int startX = Mathf.Clamp(Mathf.FloorToInt(minU * texW), 0, texW - 1);
            int endX = Mathf.Clamp(Mathf.CeilToInt(maxU * texW), 0, texW - 1);
            int startY = Mathf.Clamp(Mathf.FloorToInt(minV * texH), 0, texH - 1);
            int endY = Mathf.Clamp(Mathf.CeilToInt(maxV * texH), 0, texH - 1);

            bool modified = false;
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    Color col = dynamicFacadeTexture.GetPixel(x, y);
                    if (col.a > 0f)
                    {
                        col.a = 0f;
                        dynamicFacadeTexture.SetPixel(x, y, col);
                        modified = true;
                    }
                }
            }

            if (modified) dynamicFacadeTexture.Apply();
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

