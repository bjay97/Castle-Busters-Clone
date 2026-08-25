using UnityEngine;

namespace CastleBusters.Environment
{
    public class CastleSuspensionPhysics : MonoBehaviour
    {
        [Header("Wheel Anchors")]
        public Transform frontWheel;
        public Transform backWheel;

        [Header("Suspension Settings")]
        public static bool enableSuspensionPhysics = true;
        public LayerMask groundLayer;
        public float raycastDistance = 2.5f;
        public float suspensionRestHeight = 0.5f;
        public float maxDroop = 0.4f;        // Max downward droop over valleys
        public float maxCompression = 0.3f;  // Max upward compression over peaks
        public float suspensionDamping = 12f;
        public float tiltDamping = 8f;

        private Vector3 frontWheelBaseLocalPos;
        private Vector3 backWheelBaseLocalPos;

        private void Awake()
        {
            if (frontWheel == null || backWheel == null)
            {
                var tracks = GetComponentsInChildren<TankTrackAnimator>();
                if (tracks != null && tracks.Length > 0)
                {
                    if (frontWheel == null) frontWheel = tracks[0].transform;
                    if (backWheel == null) backWheel = tracks[tracks.Length > 1 ? 1 : 0].transform;
                }
            }

            if (frontWheel != null) frontWheelBaseLocalPos = frontWheel.localPosition;
            if (backWheel != null) backWheelBaseLocalPos = backWheel.localPosition;
        }

        private void FixedUpdate()
        {
            if (!enableSuspensionPhysics) return;
            UpdateSuspensionAndTilt();
        }

        private void UpdateSuspensionAndTilt()
        {
            if (frontWheel == null || backWheel == null) return;

            // 1. Raycast ground height for Front Wheel
            Vector2 frontOrigin = frontWheel.position + Vector3.up * 1.0f;
            RaycastHit2D frontHit = Physics2D.Raycast(frontOrigin, Vector2.down, raycastDistance, groundLayer);

            // 2. Raycast ground height for Back Wheel
            Vector2 backOrigin = backWheel.position + Vector3.up * 1.0f;
            RaycastHit2D backHit = Physics2D.Raycast(backOrigin, Vector2.down, raycastDistance, groundLayer);

            Vector3 frontGroundPoint = frontHit.collider != null ? (Vector3)frontHit.point : frontWheel.position;
            Vector3 backGroundPoint = backHit.collider != null ? (Vector3)backHit.point : backWheel.position;

            // --- A. CHASSIS TILT CALCULATION ---
            // Calculate ground slope angle between front and back wheel ground contact points
            Vector2 terrainVector = (frontGroundPoint - backGroundPoint);
            if (terrainVector.sqrMagnitude > 0.01f)
            {
                float targetAngle = Mathf.Atan2(terrainVector.y, terrainVector.x) * Mathf.Rad2Deg;
                float currentAngle = transform.eulerAngles.z;
                float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * tiltDamping);
                transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
            }

            // --- B. WHEEL DROOP & COMPRESSION ARTICULATION ---
            UpdateWheelSuspension(frontWheel, frontWheelBaseLocalPos, frontHit);
            UpdateWheelSuspension(backWheel, backWheelBaseLocalPos, backHit);
        }

        private void UpdateWheelSuspension(Transform wheel, Vector3 baseLocalPos, RaycastHit2D hit)
        {
            if (hit.collider != null)
            {
                // Calculate ground distance relative to wheel parent chassis
                float groundDistance = hit.distance - 1.0f; // Offset raycast origin
                float targetYOffset = Mathf.Clamp(-groundDistance, -maxDroop, maxCompression);

                Vector3 targetLocalPos = baseLocalPos + new Vector3(0f, targetYOffset, 0f);
                wheel.localPosition = Vector3.Lerp(wheel.localPosition, targetLocalPos, Time.deltaTime * suspensionDamping);
            }
            else
            {
                // Droop fully downwards if over a deep valley/air
                Vector3 targetLocalPos = baseLocalPos - new Vector3(0f, maxDroop, 0f);
                wheel.localPosition = Vector3.Lerp(wheel.localPosition, targetLocalPos, Time.deltaTime * suspensionDamping);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (frontWheel != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(frontWheel.position + Vector3.up * 1.0f, Vector3.down * raycastDistance);
            }
            if (backWheel != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(backWheel.position + Vector3.up * 1.0f, Vector3.down * raycastDistance);
            }
        }
    }
}
