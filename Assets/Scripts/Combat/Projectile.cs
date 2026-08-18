using UnityEngine;
using CastleBusters.Environment;
using CastleBusters.Units;

namespace CastleBusters.Combat
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Damage Profile")]
        public float directDamageMultiplier = 15f;
        public float explosionRadius = 1.5f;
        public float splashDamage = 40f;
        public float maxLifetime = 8f;

        protected Rigidbody2D rb;
        private bool hasExploded = false;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        protected virtual void Start()
        {
            Destroy(gameObject, maxLifetime);
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (hasExploded) return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            
            // Direct hit damage to DestructibleBlock or Soldier
            DestructibleBlock block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                block.TakeDamage(impactSpeed * directDamageMultiplier);
            }

            Soldier soldier = collision.gameObject.GetComponent<Soldier>();
            if (soldier != null)
            {
                soldier.TakeDamage(impactSpeed * directDamageMultiplier * 0.8f);
            }

            // Explosion splash
            Explode();
        }

        protected virtual void Explode()
        {
            hasExploded = true;

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var hit in hitColliders)
            {
                if (hit.gameObject == gameObject) continue;

                DestructibleBlock block = hit.GetComponent<DestructibleBlock>();
                if (block != null)
                {
                    block.TakeDamage(splashDamage);
                }

                Soldier soldier = hit.GetComponent<Soldier>();
                if (soldier != null)
                {
                    soldier.TakeDamage(splashDamage * 0.5f);
                }

                Rigidbody2D hitRb = hit.GetComponent<Rigidbody2D>();
                if (hitRb != null)
                {
                    Vector2 dir = (hitRb.transform.position - transform.position).normalized;
                    hitRb.AddForce(dir * splashDamage * 5f, ForceMode2D.Impulse);
                }
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
