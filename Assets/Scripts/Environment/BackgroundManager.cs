using UnityEngine;

namespace CastleBusters.Environment
{
    public class BackgroundManager : MonoBehaviour
    {
        [Header("Background Config")]
        public string backgroundFolder = "Free 2D Cartoon Parallax Background/!_Moutain";
        public float worldWidthCoverage = 140f;  // Extra wide coverage for zoomed out camera views
        public float worldHeightScale = 36f;    // Extra height coverage for high altitude missile camera tracking
        public float centerYOffset = 8f;        // Vertical centering offset to cover high sky and low ground

        private void Start()
        {
            SetupBackgroundLayers();
        }

        public void SetupBackgroundLayers()
        {
            Transform existingContainer = transform.Find("BackgroundContainer");
            if (existingContainer != null)
            {
                DestroyImmediate(existingContainer.gameObject);
            }

            GameObject container = new GameObject("BackgroundContainer");
            container.transform.SetParent(transform);
            container.transform.position = Vector3.zero;

            string basePath = "Assets/Free 2D Cartoon Parallax Background/!_Moutain/";

            // 5 Layers from back to front: Layer_0 to Layer_4
            for (int layerIndex = 0; layerIndex <= 4; layerIndex++)
            {
                string layerName = $"Layer_{layerIndex}";
                Sprite layerSprite = null;

#if UNITY_EDITOR
                layerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}{layerName}.png");
#endif

                if (layerSprite == null)
                {
                    layerSprite = Resources.Load<Sprite>($"{layerName}");
                }

                if (layerSprite == null)
                {
                    Debug.LogWarning($"[BackgroundManager] Could not load sprite for {layerName}");
                    continue;
                }

                float zDepth = 15f - (layerIndex * 1.5f); // Z = 15 down to 9 (safely behind all game elements)
                int sortingOrder = -100 + (layerIndex * 10); // SortingOrder = -100 to -60

                float spriteWidth = layerSprite.bounds.size.x;
                float spriteHeight = layerSprite.bounds.size.y;

                float scaleY = worldHeightScale / spriteHeight;
                float scaleX = scaleY; // Preserve aspect ratio

                float actualScaledWidth = spriteWidth * scaleX;
                int tileCount = Mathf.CeilToInt(worldWidthCoverage / actualScaledWidth) + 2;
                float startX = -((tileCount - 1) * actualScaledWidth) / 2f;

                GameObject layerGroup = new GameObject($"Group_{layerName}");
                layerGroup.transform.SetParent(container.transform);
                layerGroup.transform.position = new Vector3(0f, 0f, zDepth);

                for (int t = 0; t < tileCount; t++)
                {
                    GameObject tileObj = new GameObject($"{layerName}_Tile_{t}");
                    tileObj.transform.SetParent(layerGroup.transform);
                    float posX = startX + (t * actualScaledWidth);
                    tileObj.transform.position = new Vector3(posX, centerYOffset, zDepth);
                    tileObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);

                    SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = layerSprite;
                    sr.sortingOrder = sortingOrder;
                }
            }
        }
    }
}
