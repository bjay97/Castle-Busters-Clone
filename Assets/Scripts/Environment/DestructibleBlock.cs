using System;
using UnityEngine;

namespace CastleBusters.Environment
{
    public enum BlockMaterial
    {
        Wood,
        Stone
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class DestructibleBlock : MonoBehaviour
    {
        [Header("Block Attributes")]
        public BlockMaterial materialType = BlockMaterial.Wood;
        public float maxHealth = 100f;
        public float currentHealth;
        public float damageThreshold = 2.5f;

        [Header("Visual Feedback")]
        public SpriteRenderer spriteRenderer;
        public Sprite damagedSprite;

        public event Action<float> OnDamageTaken;
        public event Action OnBlockDestroyed;

        public bool IsDestroyed => currentHealth <= 0f;

        [Header("Destruction Behavior")]
        public bool isAttachedToFrame = true;
        public bool spawnDebrisOnDestroy = true;

        private Rigidbody2D rb;

        private void Start()
        {
            currentHealth = maxHealth;
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (isAttachedToFrame && rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            currentHealth -= damage;
            OnDamageTaken?.Invoke(damage);

            if (currentHealth <= maxHealth * 0.5f && damagedSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = damagedSprite;
            }

            if (currentHealth <= 0f)
            {
                DestroyBlock();
            }
        }

        private void DestroyBlock()
        {
            OnBlockDestroyed?.Invoke();

            if (spawnDebrisOnDestroy && rb != null)
            {
                // Detach from castle frame and convert into a short-lived physics debris piece
                transform.SetParent(null);
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.useFullKinematicContacts = false;
                
                // Add random impulse pop & spin
                Vector2 randomImpulse = new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(2f, 5f));
                rb.AddForce(randomImpulse, ForceMode2D.Impulse);
                rb.AddTorque(UnityEngine.Random.Range(-10f, 10f), ForceMode2D.Impulse);

                // Fade/Destroy after 1.5s
                Destroy(gameObject, 1.5f);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsDestroyed) return;

            float relativeSpeed = collision.relativeVelocity.magnitude;
            if (relativeSpeed >= damageThreshold)
            {
                float damageMultiplier = (materialType == BlockMaterial.Wood) ? 12f : 8f;
                float damage = (relativeSpeed - damageThreshold) * damageMultiplier;
                TakeDamage(damage);
            }
        }
    }
}
