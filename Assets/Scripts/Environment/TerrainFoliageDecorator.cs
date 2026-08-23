using UnityEngine;
using System.Collections.Generic;

namespace CastleBusters.Environment
{
    public class TerrainFoliageDecorator : MonoBehaviour
    {
        [Header("Foliage Asset Lists")]
        public Sprite[] grassSprites;               // White-shaded grass sprites
        public Sprite[] bushSprites;                // White-shaded bush sprites
        public GameObject[] customFoliagePrefabs;   // Optional custom prefabs (for fine-tuned pivots, animators, etc.)

        [Header("Target World Size (Auto-Normalizes High-Res 1024x1024 Sprites)")]
        public bool autoNormalizeSpriteSize = true; // Auto-scales high-res textures to target world width in meters
        public float targetGrassWidth = 0.35f;      // Small 35cm grass tuft width in world meters
        public float targetBushWidth = 0.55f;       // Small 55cm bush width in world meters
        public float globalSizeMultiplier = 1.0f;  // Global master size multiplier

        [Header("Upright Growth & Alignment Controls")]
        public bool alignToTerrainSlope = false;    // Default False: Grass grows upright (0° vertical towards sky)
        [Range(0f, 1f)] public float slopeAlignmentFactor = 0.15f; // Blend factor if slope alignment is enabled
        public Vector3 spritePivotOffset = Vector3.zero; // Additional fine-tune offset for custom sprites/prefabs
        public float yOffset = 0.0f;                // Vertical offset relative to ground surface

        [Header("Density & Spacing")]
        public int totalFoliageCount = 120;         // Number of foliage elements to spawn along terrain width
        public float minSpacing = 0.05f;            // Low min spacing (0.05m) allows thick, dense, lush grass carpets

        [Header("Color Tinting (For White-Shaded Assets)")]
        public Color primaryGrassColor = new Color(0.42f, 0.75f, 0.22f, 1f); // Bright Grass Green
        public Color secondaryGrassColor = new Color(0.32f, 0.62f, 0.18f, 1f); // Slightly darker green
        public Color bushColor = new Color(0.28f, 0.55f, 0.16f, 1f); // Deep Bush Green
        [Range(0f, 0.4f)] public float colorVariation = 0.12f; // Subtle organic brightness/hue variation per foliage

        [Header("Organic Scaling & Flip")]
        public Vector2 scaleRange = new Vector2(0.8f, 1.25f);
        public bool randomFlipX = true;
        public int sortingOrder = 6;                // Renders on top of terrain top ground mesh (sortingOrder 5)
        public string sortingLayerName = "Default";

        [Header("Ambient Wind Sway Juice")]
        public bool enableWindSway = true;
        public float windSwaySpeed = 2.5f;
        public float windSwayAngle = 3.5f;

        [Header("Container & References")]
        public Transform foliageContainer;
        public UnevenTerrainGenerator terrainGenerator;

        private void Start()
        {
            if (terrainGenerator == null) terrainGenerator = GetComponent<UnevenTerrainGenerator>();
            DecorateTerrain();
        }

        [ContextMenu("Decorate Terrain Foliage")]
        public void DecorateTerrain()
        {
            ClearFoliage();

            if (terrainGenerator == null) terrainGenerator = GetComponent<UnevenTerrainGenerator>();
            EdgeCollider2D edgeCol = (terrainGenerator != null) ? terrainGenerator.GetComponent<EdgeCollider2D>() : GetComponent<EdgeCollider2D>();

            Vector2[] surfacePoints = null;
            if (edgeCol != null && edgeCol.points != null && edgeCol.points.Length > 1)
            {
                surfacePoints = edgeCol.points;
            }

            if (surfacePoints == null || surfacePoints.Length < 2) return;

            // Ensure foliageContainer is an independent root object with scale (1, 1, 1)
            if (foliageContainer == null)
            {
                GameObject cObj = GameObject.Find("TerrainFoliageContainer_Root");
                if (cObj == null)
                {
                    cObj = new GameObject("TerrainFoliageContainer_Root");
                }
                foliageContainer = cObj.transform;
            }
            foliageContainer.SetParent(null); // Keep at root level to prevent scale inheritance
            foliageContainer.position = Vector3.zero;
            foliageContainer.rotation = Quaternion.identity;
            foliageContainer.localScale = Vector3.one; // Strictly (1, 1, 1) in world space

            float minX = surfacePoints[0].x;
            float maxX = surfacePoints[surfacePoints.Length - 1].x;
            float totalWidth = maxX - minX;

            int count = Mathf.Clamp(totalFoliageCount, 5, 1000);
            float stepX = totalWidth / count;

            List<float> spawnXList = new List<float>();

            for (int i = 0; i < count; i++)
            {
                float targetX = minX + (i * stepX) + Random.Range(-stepX * 0.45f, stepX * 0.45f);
                targetX = Mathf.Clamp(targetX, minX + 0.1f, maxX - 0.1f);

                // Check min spacing if minSpacing > 0.01f
                if (minSpacing > 0.01f)
                {
                    bool tooClose = false;
                    foreach (float existingX in spawnXList)
                    {
                        if (Mathf.Abs(existingX - targetX) < minSpacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;
                }

                spawnXList.Add(targetX);
                SpawnFoliageAtX(targetX, surfacePoints);
            }
        }

        private void SpawnFoliageAtX(float targetX, Vector2[] surfacePoints)
        {
            // Sample Y position and slope tangent from surfacePoints
            Vector2 sampledPos = SampleTerrainSurface(targetX, surfacePoints, out float slopeAngle);
            Vector3 worldPos = transform.TransformPoint(new Vector3(sampledPos.x, sampledPos.y, 0f));

            GameObject spawnedObj = null;
            Sprite chosenSprite = null;
            bool isBush = false;

            // Pick randomly between prefabs, grass sprites, or bush sprites
            if (customFoliagePrefabs != null && customFoliagePrefabs.Length > 0 && Random.value < 0.3f)
            {
                GameObject prefab = customFoliagePrefabs[Random.Range(0, customFoliagePrefabs.Length)];
                if (prefab != null)
                {
                    spawnedObj = Instantiate(prefab, foliageContainer);
                    SpriteRenderer pSr = spawnedObj.GetComponent<SpriteRenderer>();
                    if (pSr == null) pSr = spawnedObj.GetComponentInChildren<SpriteRenderer>();
                    if (pSr != null) chosenSprite = pSr.sprite;
                }
            }

            if (spawnedObj == null && ((grassSprites != null && grassSprites.Length > 0) || (bushSprites != null && bushSprites.Length > 0)))
            {
                isBush = (bushSprites != null && bushSprites.Length > 0 && Random.value < 0.35f);
                Color chosenTint = primaryGrassColor;

                if (isBush)
                {
                    chosenSprite = bushSprites[Random.Range(0, bushSprites.Length)];
                    chosenTint = bushColor;
                }
                else if (grassSprites != null && grassSprites.Length > 0)
                {
                    chosenSprite = grassSprites[Random.Range(0, grassSprites.Length)];
                    chosenTint = Color.Lerp(primaryGrassColor, secondaryGrassColor, Random.value);
                }

                if (chosenSprite != null)
                {
                    spawnedObj = new GameObject(isBush ? "Bush" : "Grass");
                    spawnedObj.transform.SetParent(foliageContainer, false);

                    SpriteRenderer sr = spawnedObj.AddComponent<SpriteRenderer>();
                    sr.sprite = chosenSprite;
                    sr.sortingOrder = sortingOrder;
                    if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;

                    // Apply randomized organic color tint to white-shaded assets
                    float var = Random.Range(-colorVariation, colorVariation);
                    Color finalColor = new Color(
                        Mathf.Clamp01(chosenTint.r + var),
                        Mathf.Clamp01(chosenTint.g + var * 1.2f),
                        Mathf.Clamp01(chosenTint.b + var * 0.8f),
                        chosenTint.a
                    );
                    sr.color = finalColor;

                    if (randomFlipX) sr.flipX = (Random.value > 0.5f);
                }
            }

            if (spawnedObj == null) return;

            // Calculate auto-normalized scale for high-res 1024x1024 textures
            float baseScale = 1.0f;
            if (autoNormalizeSpriteSize && chosenSprite != null)
            {
                float nativeWidth = Mathf.Max(0.01f, chosenSprite.bounds.size.x);
                float desiredWidth = isBush ? targetBushWidth : targetGrassWidth;
                baseScale = desiredWidth / nativeWidth;
            }

            float finalScale = baseScale * globalSizeMultiplier * Random.Range(scaleRange.x, scaleRange.y);
            spawnedObj.transform.localScale = new Vector3(finalScale, finalScale, 1f);

            // Align bottom root edge of sprite flush with ground line
            if (chosenSprite != null)
            {
                float bottomOffsetInUnits = (chosenSprite.bounds.min.y) * finalScale;
                worldPos.y -= bottomOffsetInUnits;
            }
            worldPos += spritePivotOffset;
            worldPos.y += yOffset;

            spawnedObj.transform.position = worldPos;

            // Apply upright growth rotation (0° by default so grass stands straight up toward sky)
            float finalAngle = alignToTerrainSlope ? (slopeAngle * slopeAlignmentFactor) : 0f;
            spawnedObj.transform.rotation = Quaternion.Euler(0f, 0f, finalAngle);

            // Add wind sway effect
            if (enableWindSway)
            {
                FoliageWindSway sway = spawnedObj.AddComponent<FoliageWindSway>();
                sway.swaySpeed = windSwaySpeed + Random.Range(-0.5f, 0.5f);
                sway.swayAngle = windSwayAngle + Random.Range(-1f, 1f);
                sway.baseRotationZ = finalAngle;
                sway.phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private Vector2 SampleTerrainSurface(float targetX, Vector2[] points, out float slopeAngle)
        {
            slopeAngle = 0f;
            if (points == null || points.Length < 2) return Vector2.zero;

            for (int i = 0; i < points.Length - 1; i++)
            {
                if (targetX >= points[i].x && targetX <= points[i + 1].x)
                {
                    float t = (targetX - points[i].x) / (points[i + 1].x - points[i].x);
                    float y = Mathf.Lerp(points[i].y, points[i + 1].y, t);

                    Vector2 diff = points[i + 1] - points[i];
                    slopeAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                    return new Vector2(targetX, y);
                }
            }

            return new Vector2(targetX, points[0].y);
        }

        public void ClearFoliage()
        {
            if (foliageContainer == null)
            {
                GameObject cObj = GameObject.Find("TerrainFoliageContainer_Root");
                if (cObj != null) foliageContainer = cObj.transform;
            }

            if (foliageContainer != null)
            {
                int childCount = foliageContainer.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    GameObject child = foliageContainer.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }
        }
    }

    public class FoliageWindSway : MonoBehaviour
    {
        public float swaySpeed = 2.5f;
        public float swayAngle = 3.5f;
        public float baseRotationZ = 0f;
        public float phaseOffset = 0f;

        private void Update()
        {
            float z = baseRotationZ + Mathf.Sin((Time.time * swaySpeed) + phaseOffset) * swayAngle;
            transform.rotation = Quaternion.Euler(0f, 0f, z);
        }
    }
}
