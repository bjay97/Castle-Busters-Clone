using UnityEngine;
using CastleBusters.Environment;
using CastleBusters.Units;

namespace CastleBusters.Combat
{
    public class SalvoSubMissile : Projectile
    {
        [Header("Salvo Pepper Penetration Config")]
        [Range(0f, 1f)] public float punchThroughChance = 0.35f; // 35% chance for salvo missiles to penetrate deep into castle
        public float penetrationDepth = 1.2f; // Distance traveled inside the castle before exploding
        public float penetrationSpeed = 15f; // Penetration flight speed
        private bool hasPunchedThrough = false;

        private static System.Collections.Generic.List<SalvoSubMissile> activeSubMissiles = new System.Collections.Generic.List<SalvoSubMissile>();

        protected override void Awake()
        {
            base.Awake();
            if (debrisScaleMultiplier == 1.0f) debrisScaleMultiplier = 0.35f;
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
            if (!hasPunchedThrough && rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                lastFlightDirection = rb.linearVelocity.normalized;
                lastFlightSpeed = rb.linearVelocity.magnitude;
            }
        }

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            if (hasPunchedThrough) return;

            DestructibleBlock block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                // Roll chance to punch through outer facade into deeper inner wall layers
                if (Random.value < punchThroughChance)
                {
                    hasPunchedThrough = true;
                    StartCoroutine(PenetrateDeepRoutine(collision.GetContact(0).point, lastFlightDirection));
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

        private System.Collections.IEnumerator PenetrateDeepRoutine(Vector3 entryPoint, Vector2 flyDirection)
        {
            // 1. Carve small entry hole at outer wall facade
            FacadeGridBuilder.CarveAllFacadesAt(entryPoint, explosionRadius * 0.5f, customCraterShape);

            // 2. Disable colliders so missile glides into inner castle without bouncing
            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            foreach (var c in cols) if (c != null) c.enabled = false;

            // 3. Travel deeper into the castle wall
            float flyTime = penetrationDepth / Mathf.Max(5f, penetrationSpeed);
            float elapsedTime = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + (Vector3)(flyDirection.normalized * penetrationDepth);

            while (elapsedTime < flyTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / flyTime;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // 4. Detonate deep inside the castle, carving an isolated interior crater and damaging inner blocks!
            ExplodeAtPosition(transform.position);
        }
    }
}
