using UnityEngine;

namespace CastleBusters.Combat
{
    public class ClusterProjectile : Projectile
    {
        [Header("Cluster Settings")]
        public GameObject subProjectilePrefab;
        public int subProjectileCount = 4;
        public float splitDelay = 0.8f;
        public float scatterSpreadAngle = 45f;
        public float subProjectileSpeed = 8f;

        private float timer = 0f;
        private bool hasSplit = false;

        protected override void Start()
        {
            base.Start();
            timer = splitDelay;
        }

        private void Update()
        {
            if (hasSplit) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                SplitIntoClusters();
            }
        }

        private void SplitIntoClusters()
        {
            hasSplit = true;

            if (subProjectilePrefab != null)
            {
                Vector2 currentVel = rb.linearVelocity;
                float baseAngle = Mathf.Atan2(currentVel.y, currentVel.x) * Mathf.Rad2Deg;

                float startAngle = baseAngle - (scatterSpreadAngle / 2f);
                float stepAngle = scatterSpreadAngle / (subProjectileCount - 1);

                for (int i = 0; i < subProjectileCount; i++)
                {
                    float angle = startAngle + (stepAngle * i);
                    Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                    GameObject sub = Instantiate(subProjectilePrefab, transform.position, Quaternion.identity);
                    Rigidbody2D subRb = sub.GetComponent<Rigidbody2D>();
                    if (subRb != null)
                    {
                        subRb.linearVelocity = dir * (currentVel.magnitude * 0.7f + subProjectileSpeed);
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}
