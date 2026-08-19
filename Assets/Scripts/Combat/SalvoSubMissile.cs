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

        private static System.Collections.Generic.List<SalvoSubMissile> activeSubMissiles = new System.Collections.Generic.List<SalvoSubMissile>();

        protected override void Awake()
        {
            base.Awake();
            directDamageMultiplier = 5f;
            explosionRadius = 0.65f;
            splashDamage = 12f;
        }

        protected override void Start()
        {
            base.Start();

            // Ignore collisions with other active salvo sub-missiles using static list
            Collider2D myCol = GetComponent<Collider2D>();
            if (myCol != null)
            {
                for (int i = 0; i < activeSubMissiles.Count; i++)
                {
                    if (activeSubMissiles[i] != null && activeSubMissiles[i] != this)
                    {
                        Collider2D otherCol = activeSubMissiles[i].GetComponent<Collider2D>();
                        if (otherCol != null) Physics2D.IgnoreCollision(myCol, otherCol);
                    }
                }
            }

            activeSubMissiles.Add(this);
        }

        private void OnDestroy()
        {
            activeSubMissiles.Remove(this);
        }

        private Vector2 lastFlightDirection = Vector2.right;
        private float lastFlightSpeed = 12f;

        private void FixedUpdate()
        {
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                lastFlightDirection = rb.linearVelocity.normalized;
                lastFlightSpeed = rb.linearVelocity.magnitude;
            }
        }

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            DestructibleBlock block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block != null && !hasPunchedThrough)
            {
                // Roll chance to punch through outer facade into inner wall layers
                if (Random.value < punchThroughChance)
                {
                    hasPunchedThrough = true;

                    FacadeGridBuilder.CarveAllFacadesAt(collision.GetContact(0).point, explosionRadius);
                    block.TakeDamage(splashDamage);

                    // Convert collider to trigger so it glides straight through without physics collision bounce
                    Collider2D myCol = GetComponent<Collider2D>();
                    if (myCol != null)
                    {
                        myCol.isTrigger = true;
                    }

                    if (rb != null)
                    {
                        // Continue flying FORWARD in the exact pre-impact direction
                        rb.linearVelocity = lastFlightDirection * lastFlightSpeed;
                    }
                    return;
                }
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            // Normal impact explosion
            base.OnCollisionEnter2D(collision);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!hasPunchedThrough) return;

            DestructibleBlock block = other.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                block.TakeDamage(splashDamage);
                Explode();
            }
        }
    }
}
