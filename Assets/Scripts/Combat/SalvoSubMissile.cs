using UnityEngine;
using CastleBusters.Environment;
using CastleBusters.Units;

namespace CastleBusters.Combat
{
    public class SalvoSubMissile : Projectile
    {
        [Header("Salvo Pepper Penetration")]
        [Range(0f, 1f)] public float punchThroughChance = 0.25f; // 25% chance to punch through outer wall
        private bool hasPunchedThrough = false;

        protected override void Awake()
        {
            base.Awake();
            // Lower damage & radius profile for salvo sub-missiles
            directDamageMultiplier = 5f;
            explosionRadius = 0.65f;
            splashDamage = 12f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            DestructibleBlock block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block != null && !hasPunchedThrough)
            {
                // Roll chance to punch through outer facade into inner wall layers
                if (Random.value < punchThroughChance)
                {
                    hasPunchedThrough = true;
                    block.TakeDamage(splashDamage);

                    // Ignore collision with this block and continue flying deeper into the wall grid!
                    Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider);
                    return;
                }
            }

            // Normal impact explosion
            base.OnCollisionEnter2D(collision);
        }
    }
}
