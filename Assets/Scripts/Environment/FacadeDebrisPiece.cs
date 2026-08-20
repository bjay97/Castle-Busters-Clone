using UnityEngine;

namespace CastleBusters.Environment
{
    public class FacadeDebrisPiece : MonoBehaviour
    {
        public float lifetime = 1.3f;
        public float fadeStartTime = 0.5f;

        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private float timer = 0f;
        private Vector3 initialScale;
        private Color initialColor;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
        }

        public void Initialize(Color colorTint, Vector2 velocityImpulse, float angularSpin, Vector3 scale)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();

            initialColor = colorTint;
            initialScale = scale;
            transform.localScale = scale;

            if (sr != null)
            {
                sr.color = colorTint;
            }

            if (rb != null)
            {
                rb.linearVelocity = velocityImpulse;
                rb.angularVelocity = angularSpin;
                rb.gravityScale = 1.8f;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / lifetime);

            // Scale down smoothly
            transform.localScale = Vector3.Lerp(initialScale, initialScale * 0.2f, progress);

            // Fade alpha out during second half of lifetime
            if (timer >= fadeStartTime && sr != null)
            {
                float fadeProgress = (timer - fadeStartTime) / (lifetime - fadeStartTime);
                Color c = initialColor;
                c.a = Mathf.Lerp(initialColor.a, 0f, fadeProgress);
                sr.color = c;
            }

            if (timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
