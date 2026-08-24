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

        [Header("Crater Mask Settings (Facade Mask Pool Fallback)")]
        public UnityEngine.Object[] craterMaskPool; // Drag Sprite or PNG Texture2D crater masks here
        public bool useRandomMaskRotation = true;
        public bool useRandomMaskFlip = true;

        [Header("Scorch & Smoke Residue Settings")]
        public bool enableScorchMarks = true;
        public float scorchRadiusMultiplier = 1.35f; // Scorch ring extends 1.35x beyond crater radius
        [Range(0f, 1f)]
        public float maxScorchDarkening = 0.30f; // Default subtle soot darkening (30% darker)

        [Header("Debris & Falling Particle Config")]
        public bool enableDebrisParticles = true;
        public int debrisCountPerImpact = 8; // Number of debris chips spawned per impact
        public Sprite[] debrisChipSprites; // Pre-loaded debris chip sprites (auto-generates default if empty)

        [Header("Facade Destruction Audio Config")]
        public AudioClip[] facadeDestructionSounds; // Array of facade crumble/destruction SFX clips
        [Range(0f, 1f)] public float facadeDestructionVolume = 0.85f;
        public Vector2 pitchRandomRange = new Vector2(0.85f, 1.15f); // Random pitch shift for acoustic variety

        [Header("Performance Debug Controls")]
        public static bool enableFacadeCarving = true;
        public static bool globalEnableDebris = true;

        [Header("Castle Interior Sync")]
        public SpriteRenderer castleInteriorRenderer; // Optional Castle Interior renderer to auto-align 1:1 with facade visual

        [Header("Facade Layering & Sorting")]
        public int facadeSortingOrder = 20; // Default 20 (higher than soldier body parts 10-16) so facade covers soldiers
        public string facadeSortingLayerName = "Default";

        private Texture2D dynamicFacadeTexture;
        private Color32[] rawFacadePixels;
        private SpriteRenderer fullFacadeRenderer;
        private Sprite fullFacadeSprite;

        private bool isTextureDirty = false;
        private bool needsCleanup = false;

        private void LateUpdate()
        {
            if (isTextureDirty && dynamicFacadeTexture != null && rawFacadePixels != null)
            {
                if (needsCleanup)
                {
                    CleanupFloatingTextureSectionsInternal();
                    needsCleanup = false;
                }
                dynamicFacadeTexture.SetPixels32(rawFacadePixels);
                dynamicFacadeTexture.Apply(false);
                isTextureDirty = false;
            }
        }

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

            RefreshChunkColliders();
        }

        [Header("In-Game Debug Visualizer")]
        public bool showDebugColliderOverlay = true;

        public void RefreshChunkColliders()
        {
            if (columns <= 0 || rows <= 0) return;

            DestructibleBlock[] blocks = GetComponentsInChildren<DestructibleBlock>(true);
            foreach (var b in blocks)
            {
                if (b == null) continue;

                Vector3 localPos = transform.InverseTransformPoint(b.transform.position);
                int c = Mathf.Clamp(Mathf.FloorToInt((localPos.x + totalWidth / 2f) / totalWidth * columns), 0, columns - 1);
                int r = Mathf.Clamp(Mathf.FloorToInt((localPos.y + totalHeight / 2f) / totalHeight * rows), 0, rows - 1);

                bool isSolid = GetChunkSolidBounds(c, r, out Vector2 offset, out Vector2 size);
                if (!isSolid)
                {
                    // Instantly purge empty-air chunk GameObjects from the scene!
                    if (Application.isPlaying) Destroy(b.gameObject);
                    else DestroyImmediate(b.gameObject);
                    continue;
                }

                BoxCollider2D boxCol = b.GetComponent<BoxCollider2D>();
                if (boxCol != null)
                {
                    boxCol.offset = offset;
                    boxCol.size = size;
                }

                // In-Game Debug Collider Painting
                SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
                if (sr != null && sr != fullFacadeRenderer)
                {
                    if (showDebugColliderOverlay)
                    {
                        sr.enabled = true;
                        sr.sortingOrder = 999; // Draw on top of everything in-game!
                        sr.color = new Color(0f, 1f, 0f, 0.40f); // Bright Transparent Green for active colliders
                        sr.transform.localPosition = localPos + (Vector3)offset;
                        sr.transform.localScale = new Vector3((totalWidth / columns) * size.x, (totalHeight / rows) * size.y, 1f);
                    }
                    else
                    {
                        sr.enabled = false;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugColliderOverlay) return;

            DestructibleBlock[] blocks = GetComponentsInChildren<DestructibleBlock>(true);
            foreach (var b in blocks)
            {
                if (b == null) continue;
                BoxCollider2D boxCol = b.GetComponent<BoxCollider2D>();
                if (boxCol != null && boxCol.enabled)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
                    Vector3 center = b.transform.position + (Vector3)boxCol.offset;
                    Vector3 size = new Vector3(b.transform.lossyScale.x * boxCol.size.x, b.transform.lossyScale.y * boxCol.size.y, 0.1f);
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fullFacadeRenderer != null)
            {
                if (!string.IsNullOrEmpty(facadeSortingLayerName)) fullFacadeRenderer.sortingLayerName = facadeSortingLayerName;
                fullFacadeRenderer.sortingOrder = facadeSortingOrder;
            }

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

                    bool isSolidChunk = GetChunkSolidBounds(c, r, out Vector2 offset, out Vector2 size);
                    if (!isSolidChunk)
                    {
                        DestroyImmediate(chunk);
                        continue; // Skip creating empty-air chunk GameObjects entirely!
                    }

                    BoxCollider2D col = chunk.AddComponent<BoxCollider2D>();
                    col.offset = offset;
                    col.size = size;

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

            rawFacadePixels = dynamicFacadeTexture.GetPixels32();

            GameObject fullObj = new GameObject("FullFacadeVisual");
            fullObj.transform.SetParent(transform);
            fullObj.transform.localPosition = Vector3.zero;
            fullObj.transform.localRotation = Quaternion.identity;

            fullFacadeRenderer = fullObj.AddComponent<SpriteRenderer>();
            fullFacadeSprite = Sprite.Create(dynamicFacadeTexture, new Rect(0, 0, dynamicFacadeTexture.width, dynamicFacadeTexture.height), new Vector2(0.5f, 0.5f), 100f);
            fullFacadeRenderer.sprite = fullFacadeSprite;

            float spriteW = fullFacadeSprite.bounds.size.x;
            float spriteH = fullFacadeSprite.bounds.size.y;

            // Automatically sync totalWidth and totalHeight to native sprite artwork dimensions if using texture
            if (castleTexture != null)
            {
                totalWidth = spriteW;
                totalHeight = spriteH;
            }

            fullObj.transform.localScale = Vector3.one;
            if (!string.IsNullOrEmpty(facadeSortingLayerName)) fullFacadeRenderer.sortingLayerName = facadeSortingLayerName;
            fullFacadeRenderer.sortingOrder = facadeSortingOrder;

            // Auto-align & scale optional Castle Interior SpriteRenderer to match 1:1 with facade visual bounds
            if (castleInteriorRenderer != null)
            {
                castleInteriorRenderer.transform.position = fullObj.transform.position;
                castleInteriorRenderer.transform.rotation = fullObj.transform.rotation;
                if (castleInteriorRenderer.sprite != null)
                {
                    float intW = castleInteriorRenderer.sprite.bounds.size.x;
                    float intH = castleInteriorRenderer.sprite.bounds.size.y;
                    if (intW > 0f && intH > 0f)
                    {
                        castleInteriorRenderer.transform.localScale = new Vector3(totalWidth / intW, totalHeight / intH, 1f);
                    }
                }
            }

            CalculateInitialSolidPixels();
        }

        public bool GetChunkSolidBounds(int colIndex, int rowIndex, out Vector2 offset, out Vector2 size)
        {
            offset = Vector2.zero;
            size = Vector2.one;

            Texture2D tex = (dynamicFacadeTexture != null) ? dynamicFacadeTexture : castleTexture;
            if (tex == null) return true;

            int texWidth = tex.width;
            int texHeight = tex.height;

            int startX = Mathf.FloorToInt((float)colIndex / columns * texWidth);
            int endX = Mathf.FloorToInt((float)(colIndex + 1) / columns * texWidth);
            int startY = Mathf.FloorToInt((float)rowIndex / rows * texHeight);
            int endY = Mathf.FloorToInt((float)(rowIndex + 1) / rows * texHeight);

            startX = Mathf.Clamp(startX, 0, texWidth - 1);
            endX = Mathf.Clamp(endX, startX + 1, texWidth);
            startY = Mathf.Clamp(startY, 0, texHeight - 1);
            endY = Mathf.Clamp(endY, startY + 1, texHeight);

            int width = endX - startX;
            int height = endY - startY;
            if (width <= 0 || height <= 0) return true;

            Color[] pixels = tex.GetPixels(startX, startY, width, height);

            int minX = width, maxX = -1, minY = height, maxY = -1;
            int solidCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a > 0.35f)
                    {
                        solidCount++;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // Require at least 4 solid pixels to create an active collider
            if (solidCount < 4 || maxX < minX || maxY < minY)
            {
                return false; // Transparent empty air -> disable collider!
            }

            // Calculate tight normalized offset and size within this chunk (0.0 to 1.0)
            float normMinX = (float)minX / width;
            float normMaxX = (float)(maxX + 1) / width;
            float normMinY = (float)minY / height;
            float normMaxY = (float)(maxY + 1) / height;

            float normW = normMaxX - normMinX;
            float normH = normMaxY - normMinY;
            float normCenterX = (normMinX + normMaxX) * 0.5f - 0.5f;
            float normCenterY = (normMinY + normMaxY) * 0.5f - 0.5f;

            size = new Vector2(normW, normH);
            offset = new Vector2(normCenterX, normCenterY);
            return true;
        }

        public bool IsChunkSolid(int colIndex, int rowIndex)
        {
            return GetChunkSolidBounds(colIndex, rowIndex, out _, out _);
        }

        private int initialSolidPixelCount = 0;

        private void CalculateInitialSolidPixels()
        {
            if (dynamicFacadeTexture == null) return;
            Color32[] pixels = dynamicFacadeTexture.GetPixels32();
            int count = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 15) count++;
            }
            initialSolidPixelCount = Mathf.Max(1, count);
        }

        public float GetFacadeHealthFraction()
        {
            if (dynamicFacadeTexture == null || initialSolidPixelCount <= 0) return 1f;

            Color32[] pixels = dynamicFacadeTexture.GetPixels32();
            int currentCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 15) currentCount++;
            }
            return Mathf.Clamp01((float)currentCount / initialSolidPixelCount);
        }

        public static void CarveAllFacadesAt(Vector2 worldPos, float radius, UnityEngine.Object customShape = null, bool allowRandomRotation = true, float scorchDarkeningOverride = -1f, float debrisScaleMultiplier = 1.0f, Sprite[] customDebrisSprites = null)
        {
            FacadeGridBuilder[] builders = FindObjectsByType<FacadeGridBuilder>(FindObjectsSortMode.None);
            foreach (var builder in builders)
            {
                if (builder != null)
                {
                    builder.CarveFacadeImpact(worldPos, radius, customShape, allowRandomRotation, scorchDarkeningOverride, debrisScaleMultiplier, customDebrisSprites);
                }
            }
        }

        public void CarveFacadeImpact(Vector2 worldPos, float radius, UnityEngine.Object customShape = null, bool allowRandomRotation = true, float scorchDarkeningOverride = -1f, float debrisScaleMultiplier = 1.0f, Sprite[] customDebrisSprites = null)
        {
            if (!enableFacadeCarving) return;

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

            // Pre-sample the original facade artwork color at the hit location before carving cutouts
            Color originalHitColor = dynamicFacadeTexture.GetPixel(cx, cy);

            // Step 1: Check for custom shape assigned to projectile
            UnityEngine.Object selectedMask = customShape;

            // Step 2: Fall back to facade's craterMaskPool if projectile has no custom shape
            if (selectedMask == null && craterMaskPool != null && craterMaskPool.Length > 0)
            {
                System.Collections.Generic.List<UnityEngine.Object> pool = new System.Collections.Generic.List<UnityEngine.Object>();
                foreach (var obj in craterMaskPool) if (obj != null) pool.Add(obj);
                if (pool.Count > 0)
                {
                    selectedMask = pool[Random.Range(0, pool.Count)];
                }
            }

            bool maskCarvedSuccessfully = false;

            // Step 3: Carve with selected mask shape
            if (selectedMask != null)
            {
                maskCarvedSuccessfully = TryCarveWithMaskObject(selectedMask, cx, cy, avgR, texW, texH, allowRandomRotation, scorchDarkeningOverride);
            }

            // Step 4: Fall back to existing procedural noise crater math if no shape was assigned or if mask sampling failed
            if (!maskCarvedSuccessfully)
            {
                CarveProceduralNoiseCrater(cx, cy, avgR, texW, texH, scorchDarkeningOverride);
            }

            // Note: DestructibleBlock damage within blast radius is handled by Projectile.ExplodeAtPosition using configured splashDamage.

            // Perform automatic pixel flood-fill cleanup for any isolated floating texture & physics sections
            CleanupFloatingTextureSections();

            // Spawn color-tinted facade debris chips
            if (enableDebrisParticles && globalEnableDebris)
            {
                SpawnImpactDebris(worldPos, originalHitColor, radius, debrisScaleMultiplier, customDebrisSprites);
            }

            // Play randomized facade destruction SFX
            PlayFacadeDestructionSFX(worldPos);
        }

        private static Sprite[] cachedDebrisChipPool;

        private static Sprite[] GetOrCreateDefaultDebrisChipPool()
        {
            if (cachedDebrisChipPool != null && cachedDebrisChipPool.Length > 0) return cachedDebrisChipPool;

            int size = 8;
            cachedDebrisChipPool = new Sprite[5];

            // Shape 0: Sharp Triangle Splinter
            Texture2D tex0 = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] c0 = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    c0[y * size + x] = (x + y <= size - 1) ? Color.white : Color.clear;
            tex0.SetPixels(c0); tex0.Apply();
            cachedDebrisChipPool[0] = Sprite.Create(tex0, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);

            // Shape 1: Sharp Diamond Shard
            Texture2D tex1 = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] c1 = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    c1[y * size + x] = (Mathf.Abs(x - 3.5f) + Mathf.Abs(y - 3.5f) <= 3.5f) ? Color.white : Color.clear;
            tex1.SetPixels(c1); tex1.Apply();
            cachedDebrisChipPool[1] = Sprite.Create(tex1, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);

            // Shape 2: Thin Vertical Wood Needle
            Texture2D tex2 = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] c2 = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    c2[y * size + x] = (x >= 3 && x <= 4 && y >= 1 && y <= 6) ? Color.white : Color.clear;
            tex2.SetPixels(c2); tex2.Apply();
            cachedDebrisChipPool[2] = Sprite.Create(tex2, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);

            // Shape 3: Asymmetric Stone Chunk
            Texture2D tex3 = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] c3 = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    c3[y * size + x] = (x >= 1 && x <= 6 && y >= 2 && y <= 5 && !(x == 6 && y == 5)) ? Color.white : Color.clear;
            tex3.SetPixels(c3); tex3.Apply();
            cachedDebrisChipPool[3] = Sprite.Create(tex3, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);

            // Shape 4: Tiny Pebble Dot
            Texture2D tex4 = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] c4 = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    c4[y * size + x] = (Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f)) <= 2.2f) ? Color.white : Color.clear;
            tex4.SetPixels(c4); tex4.Apply();
            cachedDebrisChipPool[4] = Sprite.Create(tex4, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);

            return cachedDebrisChipPool;
        }

        private void SpawnImpactDebris(Vector2 worldPos, Color sampledColor, float radius, float debrisScaleMultiplier = 1.0f, Sprite[] customDebrisSprites = null)
        {
            if (sampledColor.a < 0.1f)
            {
                sampledColor = new Color(0.65f, 0.55f, 0.45f, 1f); // Fall back to natural wood/stone tan
            }

            int count = Mathf.Clamp(debrisCountPerImpact, 1, 25);

            // Priority 1: Custom missile debris sprites -> Priority 2: Facade debrisChipSprites -> Priority 3: Fallback chip pool
            Sprite[] chipPool = (customDebrisSprites != null && customDebrisSprites.Length > 0) 
                ? customDebrisSprites 
                : ((debrisChipSprites != null && debrisChipSprites.Length > 0) ? debrisChipSprites : GetOrCreateDefaultDebrisChipPool());

            for (int i = 0; i < count; i++)
            {
                if (FacadeDebrisPiece.activeDebrisCount >= FacadeDebrisPiece.maxActiveDebrisCount) break;

                Sprite chipSprite = chipPool[Random.Range(0, chipPool.Length)];

                GameObject pieceObj = new GameObject("FacadeDebris_Piece");
                pieceObj.transform.position = (Vector3)worldPos + new Vector3(Random.Range(-radius * 0.35f, radius * 0.35f), Random.Range(-radius * 0.35f, radius * 0.35f), 0f);

                SpriteRenderer sr = pieceObj.AddComponent<SpriteRenderer>();
                sr.sprite = chipSprite;
                sr.sortingLayerName = !string.IsNullOrEmpty(facadeSortingLayerName) ? facadeSortingLayerName : "Default";
                sr.sortingOrder = 150; // Render above explosion VFX (100) and facade (20)

                Rigidbody2D pieceRb = pieceObj.AddComponent<Rigidbody2D>();

                // Slightly vary color shade for natural organic look
                float shadeFactor = Random.Range(0.80f, 1.20f);
                Color pieceColor = new Color(
                    Mathf.Clamp01(sampledColor.r * shadeFactor),
                    Mathf.Clamp01(sampledColor.g * shadeFactor),
                    Mathf.Clamp01(sampledColor.b * shadeFactor),
                    1f
                );

                Vector2 randomDirection = (Random.insideUnitCircle.normalized + Vector2.up * 0.7f).normalized;
                Vector2 velocityImpulse = randomDirection * Random.Range(3.5f, 8.5f);
                float spin = Random.Range(-400f, 400f);
                float baseScaleVal = (Random.value < 0.30f) ? Random.Range(1.30f, 2.00f) : Random.Range(0.50f, 1.10f);
                float scaleVal = baseScaleVal * Mathf.Clamp(debrisScaleMultiplier, 0.1f, 3.0f);
                Vector3 scale = new Vector3(scaleVal, scaleVal, 1f);

                FacadeDebrisPiece pieceComponent = pieceObj.AddComponent<FacadeDebrisPiece>();
                pieceComponent.Initialize(pieceColor, velocityImpulse, spin, scale);
            }

            RefreshChunkColliders();
        }

        private void PlayFacadeDestructionSFX(Vector2 worldPos)
        {
            if (facadeDestructionSounds == null || facadeDestructionSounds.Length == 0) return;

            // Pick a random clip from the facade destruction pool
            AudioClip chosenClip = facadeDestructionSounds[Random.Range(0, facadeDestructionSounds.Length)];
            if (chosenClip == null) return;

            // Create temporary spatial AudioSource object at impact point
            GameObject sfxObj = new GameObject("TempFacadeDestructionSFX");
            sfxObj.transform.position = (Vector3)worldPos;

            AudioSource audioSource = sfxObj.AddComponent<AudioSource>();
            audioSource.clip = chosenClip;
            audioSource.volume = facadeDestructionVolume;
            audioSource.pitch = Random.Range(pitchRandomRange.x, pitchRandomRange.y);
            audioSource.spatialBlend = 0.5f; // Balanced 2D/3D spatial audio
            audioSource.Play();

            // Destroy temp audio object after clip finishes playing
            float duration = chosenClip.length / Mathf.Max(0.1f, audioSource.pitch);
            Destroy(sfxObj, duration);
        }

        private bool GetMaskTextureAndRect(UnityEngine.Object maskObj, out Texture2D maskTex, out Rect maskRect, out string maskName)
        {
            maskTex = null;
            maskRect = Rect.zero;
            maskName = maskObj != null ? maskObj.name : "Null";

            if (maskObj is Sprite sprite && sprite != null)
            {
                maskTex = sprite.texture;
                maskRect = sprite.textureRect;
                return maskTex != null;
            }
            else if (maskObj is Texture2D tex && tex != null)
            {
                maskTex = tex;
                maskRect = new Rect(0, 0, tex.width, tex.height);
                return true;
            }

            return false;
        }

        private bool TryCarveWithMaskObject(UnityEngine.Object maskObj, int cx, int cy, float avgR, int texW, int texH, bool allowRandomRotation = true, float scorchDarkeningOverride = -1f)
        {
            if (!GetMaskTextureAndRect(maskObj, out Texture2D maskTex, out Rect maskRect, out string maskName))
            {
                return false;
            }

            // Test if mask texture is readable
            try
            {
                maskTex.GetPixel(Mathf.FloorToInt(maskRect.x), Mathf.FloorToInt(maskRect.y));
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"[FacadeGridBuilder] Crater mask '{maskName}' texture is not readable! Ensure Read/Write is enabled in Texture Import Settings. Falling back to procedural noise.");
                return false;
            }

            float effectiveDarkening = (scorchDarkeningOverride >= 0f) ? Mathf.Clamp01(scorchDarkeningOverride) : maxScorchDarkening;

            float rot = (useRandomMaskRotation && allowRandomRotation) ? Random.Range(0f, Mathf.PI * 2f) : 0f;
            float cosR = Mathf.Cos(rot);
            float sinR = Mathf.Sin(rot);
            float flipX = (useRandomMaskFlip && Random.value > 0.5f) ? -1f : 1f;
            float flipY = (useRandomMaskFlip && Random.value > 0.5f) ? -1f : 1f;

            // Expand bounding box if scorch marks are enabled to cover outer soot ring
            float scorchR = enableScorchMarks ? avgR * Mathf.Max(1.05f, scorchRadiusMultiplier) : avgR;

            int minX = Mathf.Clamp(Mathf.FloorToInt(cx - scorchR * 1.5f), 0, texW - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(cx + scorchR * 1.5f), 0, texW - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(cy - scorchR * 1.5f), 0, texH - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(cy + scorchR * 1.5f), 0, texH - 1);

            bool modified = false;

            if (rawFacadePixels == null || rawFacadePixels.Length != texW * texH)
            {
                rawFacadePixels = dynamicFacadeTexture.GetPixels32();
            }

            for (int y = minY; y <= maxY; y++)
            {
                int rowOffset = y * texW;
                for (int x = minX; x <= maxX; x++)
                {
                    int pIdx = rowOffset + x;
                    Color32 targetCol = rawFacadePixels[pIdx];
                    if (targetCol.a <= 5) continue;

                    float dx = (x - cx);
                    float dy = (y - cy);

                    // Pass 1: Outer Scorch Ring (Darken original RGB colors using shape-matched mask contour)
                    if (enableScorchMarks && scorchR > avgR)
                    {
                        float rdxScorch = (dx * cosR - dy * sinR) / scorchR * flipX;
                        float rdyScorch = (dx * sinR + dy * cosR) / scorchR * flipY;

                        float scorchU = (rdxScorch + 1f) * 0.5f;
                        float scorchV = (rdyScorch + 1f) * 0.5f;

                        if (scorchU >= 0f && scorchU <= 1f && scorchV >= 0f && scorchV <= 1f)
                        {
                            int smx = Mathf.Clamp(Mathf.FloorToInt(maskRect.x + scorchU * maskRect.width), (int)maskRect.xMin, (int)maskRect.xMax - 1);
                            int smy = Mathf.Clamp(Mathf.FloorToInt(maskRect.y + scorchV * maskRect.height), (int)maskRect.yMin, (int)maskRect.yMax - 1);

                            Color sCol = maskTex.GetPixel(smx, smy);
                            float sAlpha = sCol.a;
                            if (sAlpha <= 0.01f && (sCol.r + sCol.g + sCol.b) > 1.5f) sAlpha = (sCol.r + sCol.g + sCol.b) / 3f;

                            if (sAlpha > 0.05f)
                            {
                                float burnFactor = sAlpha * effectiveDarkening;
                                float darkMult = Mathf.Clamp01(1f - burnFactor);

                                targetCol.r = (byte)(targetCol.r * darkMult);
                                targetCol.g = (byte)(targetCol.g * darkMult);
                                targetCol.b = (byte)(targetCol.b * darkMult);

                                rawFacadePixels[pIdx] = targetCol;
                                modified = true;
                            }
                        }
                    }

                    // Pass 2: Inner Core Cutout (Carve alpha = 0 for blast hole)
                    float rdx = (dx * cosR - dy * sinR) / avgR * flipX;
                    float rdy = (dx * sinR + dy * cosR) / avgR * flipY;

                    float maskU = (rdx + 1f) * 0.5f;
                    float maskV = (rdy + 1f) * 0.5f;

                    if (maskU >= 0f && maskU <= 1f && maskV >= 0f && maskV <= 1f)
                    {
                        int mx = Mathf.Clamp(Mathf.FloorToInt(maskRect.x + maskU * maskRect.width), (int)maskRect.xMin, (int)maskRect.xMax - 1);
                        int my = Mathf.Clamp(Mathf.FloorToInt(maskRect.y + maskV * maskRect.height), (int)maskRect.yMin, (int)maskRect.yMax - 1);

                        Color maskCol = maskTex.GetPixel(mx, my);
                        float maskAlpha = maskCol.a;
                        if (maskAlpha <= 0.01f && (maskCol.r + maskCol.g + maskCol.b) > 1.5f) maskAlpha = (maskCol.r + maskCol.g + maskCol.b) / 3f;

                        if (maskAlpha > 0.05f)
                        {
                            if (maskAlpha >= 0.25f)
                            {
                                targetCol.a = 0;
                            }
                            else
                            {
                                float cutoutFactor = (maskAlpha - 0.05f) / 0.20f;
                                byte newAlpha = (byte)(targetCol.a * (1f - cutoutFactor));
                                if (newAlpha < targetCol.a) targetCol.a = newAlpha;
                            }
                            rawFacadePixels[pIdx] = targetCol;
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                isTextureDirty = true;
            }

            return true;
        }

        private void CarveProceduralNoiseCrater(int cx, int cy, float avgR, int texW, int texH, float scorchDarkeningOverride = -1f)
        {
            float seed = Random.Range(0f, 1000f);
            float aspectX = Random.Range(0.85f, 1.25f);
            float aspectY = Random.Range(0.85f, 1.25f);
            float rot = Random.Range(0f, Mathf.PI * 2f);
            float cosR = Mathf.Cos(rot);
            float sinR = Mathf.Sin(rot);

            float effectiveDarkening = (scorchDarkeningOverride >= 0f) ? Mathf.Clamp01(scorchDarkeningOverride) : maxScorchDarkening;
            float scorchR = enableScorchMarks ? avgR * Mathf.Max(1.05f, scorchRadiusMultiplier) : avgR;

            int minX = Mathf.Clamp(Mathf.FloorToInt(cx - scorchR * 1.5f), 0, texW - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(cx + scorchR * 1.5f), 0, texW - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(cy - scorchR * 1.5f), 0, texH - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(cy + scorchR * 1.5f), 0, texH - 1);

            bool modified = false;

            if (rawFacadePixels == null || rawFacadePixels.Length != texW * texH)
            {
                rawFacadePixels = dynamicFacadeTexture.GetPixels32();
            }

            for (int y = minY; y <= maxY; y++)
            {
                int rowOffset = y * texW;
                for (int x = minX; x <= maxX; x++)
                {
                    int pIdx = rowOffset + x;
                    Color32 col = rawFacadePixels[pIdx];
                    if (col.a <= 5) continue;

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
                    float outerScorchRadius = scorchR * (1.0f + noise);

                    // Scorch darkening in outer ring
                    if (enableScorchMarks && dist > craterRadius && dist <= outerScorchRadius)
                    {
                        float distFactor = 1f - ((dist - craterRadius) / Mathf.Max(0.01f, outerScorchRadius - craterRadius));
                        float burnFactor = Mathf.Clamp01(distFactor) * effectiveDarkening;
                        float darkMult = Mathf.Clamp01(1f - burnFactor);

                        col.r = (byte)(col.r * darkMult);
                        col.g = (byte)(col.g * darkMult);
                        col.b = (byte)(col.b * darkMult);

                        rawFacadePixels[pIdx] = col;
                        modified = true;
                    }

                    // Cutout in inner blast core
                    if (dist <= craterRadius)
                    {
                        if (dist <= craterRadius - 1.5f)
                        {
                            col.a = 0;
                        }
                        else
                        {
                            float edgeFactor = (dist - (craterRadius - 1.5f)) / 1.5f;
                            byte newAlpha = (byte)(col.a * Mathf.Clamp01(edgeFactor));
                            if (newAlpha < col.a) col.a = newAlpha;
                        }
                        rawFacadePixels[pIdx] = col;
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                isTextureDirty = true;
            }
        }

        public void CleanupFloatingTextureSections()
        {
            needsCleanup = true;
            isTextureDirty = true;
        }

        private void CleanupFloatingTextureSectionsInternal()
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
                        int rowOffset = py * texW;
                        for (int px = startPx; px <= endPx && !cellHasContent; px += 2)
                        {
                            if (rawFacadePixels[rowOffset + px].a > 12)
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
                            int rowOffset = py * texW;
                            for (int px = minX; px <= maxX; px++)
                            {
                                int pIdx = rowOffset + px;
                                if (rawFacadePixels[pIdx].a > 0)
                                {
                                    rawFacadePixels[pIdx].a = 0;
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
                isTextureDirty = true;
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

