using System.Collections;
using UnityEngine;

namespace CastleBusters.Combat
{
    public class MissileSmokeTrail : MonoBehaviour
    {
        [Header("Smoke Visual Config")]
        public Sprite smokeSprite;
        public Color startColor = new Color(0.85f, 0.85f, 0.85f, 0.7f);
        public Color endColor = new Color(0.9f, 0.9f, 0.9f, 0f);

        [Header("Trail Emission Config")]
        public float spawnInterval = 0.04f; // Seconds between puff spawns
        public float minDistanceBetweenPuffs = 0.12f; // Minimum distance moved before spawning next puff
        public Vector3 spawnOffset = Vector3.zero; // Offset relative to missile center

        [Header("Puff Scale & Animation")]
        public Vector3 startScale = new Vector3(0.35f, 0.35f, 1f);
        public Vector3 endScale = new Vector3(1.25f, 1.25f, 1f);
        public float puffLifetime = 0.75f;
        public bool useRandomRotation = true;
        public bool useRandomDrift = true;
        public float driftSpeed = 0.2f;

        [Header("Sorting Order")]
        public int sortingOrder = 45; // Renders right behind missile (50)

        private bool isEmitting = true;
        private Vector3 lastSpawnPosition;
        private float spawnTimer = 0f;
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            lastSpawnPosition = transform.position;
        }

        private void Update()
        {
            if (!isEmitting) return;

            spawnTimer += Time.deltaTime;

            // Only spawn when missile is moving
            bool isMoving = (rb == null || rb.linearVelocity.sqrMagnitude > 0.1f);
            float distFromLast = Vector3.Distance(transform.position, lastSpawnPosition);

            if (isMoving && (spawnTimer >= spawnInterval || distFromLast >= minDistanceBetweenPuffs))
            {
                SpawnSmokePuff();
                spawnTimer = 0f;
                lastSpawnPosition = transform.position;
            }
        }

        public void StopEmitting()
        {
            isEmitting = false;
        }

        private void SpawnSmokePuff()
        {
            Vector3 spawnPos = transform.TransformPoint(spawnOffset);

            GameObject puffObj = new GameObject("SmokePuff");
            puffObj.transform.position = spawnPos;

            if (useRandomRotation)
            {
                puffObj.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }

            SpriteRenderer sr = puffObj.AddComponent<SpriteRenderer>();
            sr.sprite = smokeSprite != null ? smokeSprite : CreateDefaultSmokePuffSprite();
            sr.color = startColor;
            sr.sortingOrder = sortingOrder;

            Vector2 driftDir = useRandomDrift ? Random.insideUnitCircle.normalized * driftSpeed : Vector2.zero;

            StartCoroutine(AnimateSmokePuff(puffObj, sr, driftDir));
        }

        private IEnumerator AnimateSmokePuff(GameObject puffObj, SpriteRenderer sr, Vector2 driftDir)
        {
            float elapsed = 0f;

            while (elapsed < puffLifetime && puffObj != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / puffLifetime);

                // Smooth ease-out growth curve
                float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

                puffObj.transform.localScale = Vector3.Lerp(startScale, endScale, easeT);
                puffObj.transform.position += (Vector3)(driftDir * Time.deltaTime);

                if (sr != null)
                {
                    sr.color = Color.Lerp(startColor, endColor, t);
                }

                yield return null;
            }

            if (puffObj != null)
            {
                Destroy(puffObj);
            }
        }

        private static Sprite cachedDefaultSmokeSprite;

        private static Sprite CreateDefaultSmokePuffSprite()
        {
            if (cachedDefaultSmokeSprite != null) return cachedDefaultSmokeSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            float center = size * 0.5f;
            float maxRadius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float normDist = Mathf.Clamp01(dist / maxRadius);

                    // Soft radial falloff for smooth smoke cloud puff
                    float alpha = Mathf.SmoothStep(1f, 0f, normDist);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            cachedDefaultSmokeSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return cachedDefaultSmokeSprite;
        }
    }
}
