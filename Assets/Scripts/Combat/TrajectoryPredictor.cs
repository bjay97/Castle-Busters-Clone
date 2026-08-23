using UnityEngine;

namespace CastleBusters.Combat
{
    public class TrajectoryPredictor : MonoBehaviour
    {
        [Header("Trajectory Mode Selection")]
        public bool useDirectionalPointer = true; // True: Use custom decorative UI pointer; False: Classic parabola LineRenderer

        [Header("Decorative Directional Pointer Config")]
        public GameObject customDirectionalPrefab; // Custom UI or World prefab for decorative pointer line
        public Sprite customDirectionalSprite; // Optional custom sprite/icon for pointer line
        public Color pointerColor = new Color(1f, 0.85f, 0.2f, 0.9f); // Sleek gold decorative line color
        public bool preserveAspectRatio = true; // True: Maintains exact native aspect ratio; False: Stretches X axis
        public float minPointerScale = 0.8f; // Scale multiplier when slightly pulled (with aspect ratio preserved)
        public float maxPointerScale = 1.5f; // Scale multiplier when fully pulled (with aspect ratio preserved)
        public float minPointerLength = 1.0f; // Stretch X length when preserveAspectRatio is false
        public float maxPointerLength = 4.5f; // Stretch X length when preserveAspectRatio is false
        public string sortingLayerName = "Default";
        public int sortingOrder = 500; // High sorting order so decorative pointer renders on top of castle interior/soldiers

        [Header("Classic Parabola Settings (Fallback)")]
        public int resolution = 30;
        public float timeStep = 0.08f;

        private LineRenderer lineRenderer;
        private GameObject pointerInstance;
        private UnityEngine.UI.Image pointerImage;
        private Vector3 initialPointerScale = Vector3.one;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
                if (lineRenderer.material == null || lineRenderer.material.name.Contains("Default"))
                {
                    lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                }
                lineRenderer.startWidth = 0.15f;
                lineRenderer.endWidth = 0.05f;
                lineRenderer.startColor = Color.yellow;
                lineRenderer.endColor = new Color(1f, 1f, 1f, 0.2f);
                lineRenderer.sortingOrder = sortingOrder;
            }
        }

        private void EnsurePointerInstanceInitialized()
        {
            if (pointerInstance != null) return;

            if (customDirectionalPrefab != null)
            {
                pointerInstance = Instantiate(customDirectionalPrefab);
                initialPointerScale = customDirectionalPrefab.transform.localScale;
                if (initialPointerScale == Vector3.zero) initialPointerScale = Vector3.one;

                if (pointerInstance.GetComponent<RectTransform>() != null && pointerInstance.GetComponent<Canvas>() == null && pointerInstance.GetComponentInParent<Canvas>() == null)
                {
                    Canvas c = pointerInstance.AddComponent<Canvas>();
                    c.renderMode = RenderMode.WorldSpace;
                    c.sortingOrder = sortingOrder;
                    pointerInstance.AddComponent<UnityEngine.UI.CanvasScaler>();
                    pointerInstance.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                ApplySortingOrder(pointerInstance);
                pointerInstance.SetActive(false);
                return;
            }

            // Procedurally create a World Space Decorative Pointer Line Canvas pivoting from launch point
            GameObject canvasObj = new GameObject("DecorativeTrajectoryPointerCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = sortingOrder;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(180f, 24f);
            canvasRT.pivot = new Vector2(0f, 0.5f); // Pivot at start of trajectory line
            canvasRT.localScale = new Vector3(0.012f, 0.012f, 1f);

            GameObject lineObj = new GameObject("PointerLineImage");
            lineObj.transform.SetParent(canvasObj.transform, false);

            pointerImage = lineObj.AddComponent<UnityEngine.UI.Image>();
            if (customDirectionalSprite != null) pointerImage.sprite = customDirectionalSprite;
            pointerImage.color = pointerColor;

            RectTransform lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.anchorMin = Vector2.zero;
            lineRT.anchorMax = Vector2.one;
            lineRT.sizeDelta = Vector2.zero;

            initialPointerScale = new Vector3(0.012f, 0.012f, 1f);
            pointerInstance = canvasObj;
            pointerInstance.SetActive(false);
        }

        private void ApplySortingOrder(GameObject obj)
        {
            if (obj == null) return;

            Canvas[] canvases = obj.GetComponentsInChildren<Canvas>(true);
            foreach (var c in canvases)
            {
                c.overrideSorting = true;
                c.sortingOrder = sortingOrder;
                if (!string.IsNullOrEmpty(sortingLayerName)) c.sortingLayerName = sortingLayerName;
            }

            SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                sr.sortingOrder = sortingOrder;
                if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;
            }
        }

        public void ShowTrajectory(Vector2 startPosition, Vector2 launchVelocity, float gravityScale = 1f)
        {
            if (useDirectionalPointer)
            {
                if (lineRenderer != null) lineRenderer.enabled = false;

                EnsurePointerInstanceInitialized();
                if (pointerInstance != null)
                {
                    pointerInstance.SetActive(true);
                    pointerInstance.transform.position = startPosition;

                    // Calculate direction angle pointing towards launch trajectory
                    float angle = Mathf.Atan2(launchVelocity.y, launchVelocity.x) * Mathf.Rad2Deg;
                    pointerInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                    // Dynamic scaling proportional to pull distance / launch speed
                    float speedRatio = Mathf.Clamp01(launchVelocity.magnitude / 35f);

                    if (preserveAspectRatio)
                    {
                        // Uniform scaling that retains exact native aspect ratio without distortion
                        float scaleMultiplier = Mathf.Lerp(minPointerScale, maxPointerScale, speedRatio);
                        pointerInstance.transform.localScale = initialPointerScale * scaleMultiplier;
                    }
                    else
                    {
                        // Non-uniform X-axis stretch
                        float targetLength = Mathf.Lerp(minPointerLength, maxPointerLength, speedRatio);
                        Vector3 targetScale = new Vector3(initialPointerScale.x * targetLength, initialPointerScale.y, initialPointerScale.z);
                        pointerInstance.transform.localScale = targetScale;
                    }
                }
            }
            else
            {
                if (pointerInstance != null) pointerInstance.SetActive(false);

                if (lineRenderer != null)
                {
                    lineRenderer.enabled = true;
                    lineRenderer.positionCount = resolution;

                    Vector2 gravity = Physics2D.gravity * gravityScale;

                    for (int i = 0; i < resolution; i++)
                    {
                        float t = i * timeStep;
                        Vector2 point = startPosition + launchVelocity * t + 0.5f * gravity * (t * t);
                        lineRenderer.SetPosition(i, new Vector3(point.x, point.y, 0f));
                    }
                }
            }
        }

        public void HideTrajectory()
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            if (pointerInstance != null) pointerInstance.SetActive(false);
        }
    }
}
