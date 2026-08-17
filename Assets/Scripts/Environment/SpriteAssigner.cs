using UnityEngine;

namespace CastleBusters.Environment
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAssigner : MonoBehaviour
    {
        [Header("Drag ANY PNG or Texture Image Here")]
        public Texture2D imageTexture;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            ApplyTextureAsSprite();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyTextureAsSprite();
        }
#endif

        [ContextMenu("Apply Image Texture")]
        public void ApplyTextureAsSprite()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (imageTexture != null && spriteRenderer != null)
            {
                // Convert Texture2D into a Sprite dynamically
                Sprite newSprite = Sprite.Create(
                    imageTexture,
                    new Rect(0, 0, imageTexture.width, imageTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                spriteRenderer.sprite = newSprite;
            }
        }
    }
}
