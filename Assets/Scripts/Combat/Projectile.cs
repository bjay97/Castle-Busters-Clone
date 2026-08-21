using UnityEngine;
using CastleBusters.Environment;
using CastleBusters.Units;
using CastleBusters.Core;

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

        [Header("Friendly Fire Config")]
        public PlayerSide ownerSide = PlayerSide.Player1; // Firing player side
        public bool allowFriendlyFire = false; // Disabled by default to prevent self/friendly damage and facade carving

        [Header("Custom Crater Shape Override (Drag ANY Sprite or PNG Texture2D here)")]
        public UnityEngine.Object customCraterShape; // Custom crater shape for this projectile
        public bool useRandomRotationForShape = true; // Randomly rotate shape for visual variety
        [Range(-1f, 1f)]
        public float customScorchDarkening = -1f; // -1 to use Facade default; 0 to 1 to override soot darkness

        [Header("Crater Sizing & Damage Coupling Mode")]
        public bool useMaskNativeSize = false; // True: Radius & damage derived from Crater Mask shape asset dimensions; False: Uses explicit explosionRadius
        public float craterScaleMultiplier = 1.0f; // Scale multiplier applied to mask asset size when useMaskNativeSize is true
        public float damagePerCraterAreaUnit = 40f; // Damage factor per surface area unit when useMaskNativeSize is true

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

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 50; // Flying projectile renders above facade (20) and soldiers (10-16)
            }
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

            // Check if collision is with a friendly castle block
            DestructibleBlock block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                Castle blockCastle = block.GetComponentInParent<Castle>();
                if (blockCastle != null && !allowFriendlyFire && blockCastle.ownerSide == ownerSide)
                {
                    Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
                    return; // Ignore friendly castle blocks!
                }
            }

            // Check if collision is with a friendly soldier
            Soldier soldier = collision.gameObject.GetComponent<Soldier>();
            if (soldier != null)
            {
                if (!allowFriendlyFire && soldier.ownerSide == ownerSide)
                {
                    Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
                    return; // Ignore friendly soldiers!
                }
            }

            float impactSpeed = collision.relativeVelocity.magnitude;
            
            // Direct hit damage to enemy DestructibleBlock or enemy Soldier
            if (block != null)
            {
                block.TakeDamage(impactSpeed * directDamageMultiplier);
            }

            if (soldier != null)
            {
                soldier.TakeDamage(impactSpeed * directDamageMultiplier * 0.8f);
            }

            // Always center explosion on exact surface contact point of enemy target or terrain!
            Vector2 contactPoint = (collision.contactCount > 0) ? collision.GetContact(0).point : (Vector2)transform.position;
            ExplodeAtPosition(contactPoint);
        }

        [Header("Explosion VFX")]
        public GameObject explosionVFXPrefab;

        public void GetEffectiveExplosionRadiusAndDamage(out float effectiveRadius, out float effectiveDamage)
        {
            effectiveRadius = explosionRadius;
            effectiveDamage = splashDamage;

            if (useMaskNativeSize && customCraterShape != null)
            {
                float maskWorldWidth = 1.5f; // Fallback width

                if (customCraterShape is Sprite spr && spr != null)
                {
                    maskWorldWidth = spr.bounds.size.x * craterScaleMultiplier;
                }
                else if (customCraterShape is Texture2D tex && tex != null)
                {
                    // Default 100 pixels per unit if raw texture
                    maskWorldWidth = (tex.width / 100f) * craterScaleMultiplier;
                }

                effectiveRadius = Mathf.Max(0.01f, maskWorldWidth * 0.5f);
                float craterArea = Mathf.PI * effectiveRadius * effectiveRadius;
                effectiveDamage = Mathf.Max(0f, craterArea * damagePerCraterAreaUnit);
            }
            else
            {
                effectiveRadius = Mathf.Max(0.01f, explosionRadius);
                effectiveDamage = Mathf.Max(0f, splashDamage);
            }
        }

        protected virtual void Explode()
        {
            ExplodeAtPosition(transform.position);
        }

        public virtual void ExplodeAtPosition(Vector3 impactPoint)
        {
            hasExploded = true;

            GetEffectiveExplosionRadiusAndDamage(out float radius, out float damage);

            // Stop missile smoke trail emission
            MissileSmokeTrail trail = GetComponent<MissileSmokeTrail>();
            if (trail == null) trail = GetComponentInChildren<MissileSmokeTrail>();
            if (trail != null) trail.StopEmitting();

            // Spawn explosion VFX at impact position
            SpawnExplosionVFX(impactPoint);

            // Carve facade ONLY for enemy castles (skip friendly castle if friendly fire disabled)
            FacadeGridBuilder[] builders = FindObjectsByType<FacadeGridBuilder>(FindObjectsSortMode.None);
            foreach (var builder in builders)
            {
                if (builder != null)
                {
                    Castle parentCastle = builder.GetComponentInParent<Castle>();
                    if (parentCastle != null && !allowFriendlyFire && parentCastle.ownerSide == ownerSide)
                    {
                        continue; // Skip carving friendly castle facade!
                    }
                    builder.CarveFacadeImpact(impactPoint, radius, customCraterShape, useRandomRotationForShape, customScorchDarkening);
                }
            }

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(impactPoint, radius);
            foreach (var hit in hitColliders)
            {
                if (hit.gameObject == gameObject) continue;

                DestructibleBlock block = hit.GetComponent<DestructibleBlock>();
                if (block != null)
                {
                    Castle blockCastle = block.GetComponentInParent<Castle>();
                    if (blockCastle == null || allowFriendlyFire || blockCastle.ownerSide != ownerSide)
                    {
                        block.TakeDamage(damage);
                    }
                }

                Soldier soldier = hit.GetComponent<Soldier>();
                if (soldier != null)
                {
                    if (allowFriendlyFire || soldier.ownerSide != ownerSide)
                    {
                        soldier.TakeDamage(damage * 0.5f);
                    }
                }

                Rigidbody2D hitRb = hit.GetComponent<Rigidbody2D>();
                if (hitRb != null && hit.GetComponent<Projectile>() == null)
                {
                    Vector2 dir = (hitRb.transform.position - impactPoint).normalized;
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

                // Force all particle renderers and child renderers to sortingOrder = 100 so explosions render ON TOP of castle facade (20)
                int topSortingOrder = 100;
                Renderer[] renderers = vfxInstance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r != null)
                    {
                        r.sortingOrder = topSortingOrder;
                    }
                }

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
