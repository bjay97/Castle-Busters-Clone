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

        [Header("Speed & Physics Config")]
        public float speedMultiplier = 0.65f; // Tune flight speed per projectile type (e.g. 0.65f for slower clear flight trajectory)

        protected Rigidbody2D rb;
        private bool hasExploded = false;

        [Header("Visual Direction Config")]
        public bool rotateTowardsVelocity = true; // Rotate projectile transform to match ballistic flight arc
        public bool flipSpriteXOnLeftFlight = true; // Flip SpriteRenderer.flipX when traveling left (velocity.x < 0)

        protected SpriteRenderer spriteRenderer;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        protected virtual void Start()
        {
            Destroy(gameObject, maxLifetime);
        }

        protected virtual void Update()
        {
            UpdateFlightOrientation();
        }

        protected virtual void UpdateFlightOrientation()
        {
            if (hasExploded || rb == null) return;

            Vector2 vel = rb.linearVelocity;
            if (vel.sqrMagnitude > 0.05f)
            {
                bool isTravellingLeft = (vel.x < -0.05f);

                if (flipSpriteXOnLeftFlight && spriteRenderer != null)
                {
                    spriteRenderer.flipX = isTravellingLeft;
                }

                if (rotateTowardsVelocity)
                {
                    float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
                    if (flipSpriteXOnLeftFlight && isTravellingLeft)
                    {
                        angle += 180f;
                    }
                    transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }
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

        [Header("Explosion VFX")]
        public GameObject explosionVFXPrefab;

        protected virtual void Explode()
        {
            hasExploded = true;

            // Spawn explosion VFX at impact position
            SpawnExplosionVFX(transform.position);

            // Smooth facade impact carving with organic crater brush
            FacadeGridBuilder.CarveAllFacadesAt(transform.position, explosionRadius);

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
                if (hitRb != null && hit.GetComponent<Projectile>() == null)
                {
                    Vector2 dir = (hitRb.transform.position - transform.position).normalized;
                    hitRb.AddForce(dir * splashDamage * 5f, ForceMode2D.Impulse);
                }
            }

            Destroy(gameObject);
        }

        public void SpawnExplosionVFX(Vector3 position)
        {
            GameObject vfxPrefabToSpawn = explosionVFXPrefab;

            if (vfxPrefabToSpawn == null)
            {
                vfxPrefabToSpawn = Resources.Load<GameObject>("CFXR Explosion 1");
            }

#if UNITY_EDITOR
            if (vfxPrefabToSpawn == null)
            {
                vfxPrefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR Explosion 1.prefab");
            }
#endif

            if (vfxPrefabToSpawn != null)
            {
                GameObject vfxInstance = Instantiate(vfxPrefabToSpawn, position, Quaternion.identity);

                // Auto cleanup particle explosion after 3.5 seconds
                Destroy(vfxInstance, 3.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
