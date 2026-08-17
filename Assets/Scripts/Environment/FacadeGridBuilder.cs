using UnityEngine;

namespace CastleBusters.Environment
{
    public class FacadeGridBuilder : MonoBehaviour
    {
        [Header("Grid Dimensions")]
        public int columns = 12;
        public int rows = 8;
        public float chunkSize = 0.35f;

        [Header("Block Config")]
        public BlockMaterial materialType = BlockMaterial.Wood;
        public float blockHealth = 30f;
        public Sprite blockSprite;
        public Color blockColor = new Color(0.8f, 0.5f, 0.2f); // Wood / Stone color

        [Header("Auto Generate")]
        public bool generateOnStart = true;

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateFacadeGrid();
            }
        }

        [ContextMenu("Generate Facade Grid")]
        public void GenerateFacadeGrid()
        {
            // Clear existing children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            Vector2 origin = transform.position;
            float totalWidth = columns * chunkSize;
            float totalHeight = rows * chunkSize;

            Vector2 startPos = origin - new Vector2(totalWidth / 2f - chunkSize / 2f, totalHeight / 2f - chunkSize / 2f);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Vector2 pos = startPos + new Vector2(c * chunkSize, r * chunkSize);

                    GameObject chunk = new GameObject($"Chunk_{c}_{r}");
                    chunk.transform.position = pos;
                    chunk.transform.SetParent(transform);

                    // Add SpriteRenderer
                    SpriteRenderer sr = chunk.AddComponent<SpriteRenderer>();
                    if (blockSprite != null) sr.sprite = blockSprite;
                    sr.color = blockColor;
                    sr.sortingOrder = 2;

                    // Scale
                    chunk.transform.localScale = new Vector3(chunkSize, chunkSize, 1f);

                    // Add Physics & Colliders
                    Rigidbody2D rb = chunk.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.useFullKinematicContacts = true;

                    BoxCollider2D col = chunk.AddComponent<BoxCollider2D>();

                    // Add DestructibleBlock
                    DestructibleBlock block = chunk.AddComponent<DestructibleBlock>();
                    block.materialType = materialType;
                    block.maxHealth = blockHealth;
                    block.currentHealth = blockHealth;
                    block.isAttachedToFrame = true;
                    block.spawnDebrisOnDestroy = true;
                }
            }

            Debug.Log($"[FacadeGridBuilder] Generated facade wall of {columns * rows} micro-chunks.");
        }
    }
}
