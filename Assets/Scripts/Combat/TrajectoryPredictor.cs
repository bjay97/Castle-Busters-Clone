using UnityEngine;

namespace CastleBusters.Combat
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPredictor : MonoBehaviour
    {
        [Header("Trajectory Render Settings")]
        public int resolution = 30;
        public float timeStep = 0.08f;

        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.enabled = false;
        }

        public void ShowTrajectory(Vector2 startPosition, Vector2 launchVelocity, float gravityScale = 1f)
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

        public void HideTrajectory()
        {
            lineRenderer.enabled = false;
        }
    }
}
