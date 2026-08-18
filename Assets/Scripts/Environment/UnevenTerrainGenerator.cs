using UnityEngine;

namespace CastleBusters.Environment
{
    [RequireComponent(typeof(EdgeCollider2D))]
    public class UnevenTerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Curve Settings")]
        public float terrainWidth = 30f;
        public int pointCount = 60;
        public float peakHeight = 0.6f;      // Height of hills and valleys
        public float waveFrequency = 0.5f;   // Frequency of peaks

        [Header("Visual Ground Rendering")]
        public bool generateLineVisual = true;
        public Color groundLineColor = new Color(0.4f, 0.25f, 0.15f);

        private EdgeCollider2D edgeCollider;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            GenerateUnevenGround();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && !Application.isPlaying) GenerateUnevenGround();
                };
            }
        }
#endif

        [ContextMenu("Generate Peaks & Valleys")]
        public void GenerateUnevenGround()
        {
            edgeCollider = GetComponent<EdgeCollider2D>();
            Vector2[] points = new Vector2[pointCount];

            float startX = -terrainWidth / 2f;
            float stepX = terrainWidth / (pointCount - 1);

            for (int i = 0; i < pointCount; i++)
            {
                float x = startX + (i * stepX);
                // Multi-frequency wave for natural peaks and valleys
                float y = Mathf.Sin(x * waveFrequency) * peakHeight 
                        + Mathf.Cos(x * waveFrequency * 2.3f) * (peakHeight * 0.4f);

                points[i] = new Vector2(x, y);
            }

            edgeCollider.points = points;

            // Generate visual ground line matching collider peaks
            if (generateLineVisual)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

                lineRenderer.positionCount = pointCount;
                lineRenderer.startWidth = 0.25f;
                lineRenderer.endWidth = 0.25f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = groundLineColor;
                lineRenderer.endColor = groundLineColor;

                for (int i = 0; i < pointCount; i++)
                {
                    lineRenderer.SetPosition(i, transform.TransformPoint(points[i]));
                }
            }
        }
    }
}
