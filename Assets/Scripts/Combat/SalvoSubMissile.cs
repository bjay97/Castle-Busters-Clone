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
            directDamageMultiplier = 5f;
            explosionRadius = 0.65f;
            splashDamage = 12f;
        }

        protected override void Start()
        {
            base.Start();

            // Ignore collisions with other salvo sub-missiles in mid-flight to prevent bouncing off each other
            Collider2D myCol = GetComponent<Collider2D>();
            if (myCol != null)
            {
                SalvoSubMissile[] activeSubMissiles = FindObjectsByType<SalvoSubMissile>(FindObjectsSortMode.None);
                foreach (var other in activeSubMissiles)
                {
                    if (other != null && other != this)
                    {
                        Collider2D otherCol = other.GetComponent<Collider2D>();
                        if (otherCol != null) Physics2D.IgnoreCollision(myCol, otherCol);
                    }
                }
            }
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
            // Instantly stop physics simulation to prevent any physical bounce response
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            DestructibleBlock block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block != null && !hasPunchedThrough)
            {
                // Roll chance to punch through outer facade into inner wall layers
                if (Random.value < punchThroughChance)
                {
                    hasPunchedThrough = true;

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
