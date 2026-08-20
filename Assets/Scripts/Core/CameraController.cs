using UnityEngine;
using CastleBusters.Core;
using CastleBusters.Units;

namespace CastleBusters.Core
{
    public enum CameraMode
    {
        Player1Castle,
        Player2Castle,
        DrivingCastle,
        SoldierSelection,
        FocusSoldier,
        FollowTarget,
        FullOverview
    }

    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Target Offsets & Anchors")]
        public Transform player1CastleTransform;
        public Transform player2CastleTransform;
        public Transform currentTarget;

        [Header("Camera Mode Settings")]
        public CameraMode currentMode = CameraMode.Player1Castle;

        [Header("Orthographic Zoom Settings")]
        public float castleFocusZoom = 6.0f;
        public float enemyTurnZoom = 10.5f;       // Constant zoomed out view during enemy turn (moving & firing)
        public float movementZoom = 11.0f;       // Significantly zoomed out wide view while driving castle
        public float soldierSelectZoom = 10.23f;  // Zoom during soldier select phase
        public float soldierFocusZoom = 10.23f;   // Matches soldierSelectZoom for smooth panning without jarring zooms
        public float followTargetZoom = 11.0f;    // Significantly zoomed out wide view while tracking missiles
        public float overviewZoom = 13.0f;
        public float zoomSmoothTime = 0.3f;

        [Header("Smooth Damping Settings")]
        public float positionSmoothTime = 0.25f;
        public Vector3 cameraOffset = new Vector3(0f, 1.2f, -10f);
        public Vector3 aimingOffset = new Vector3(3.8f, 1.0f, -10f); // Shifts camera +3.8 units right towards enemy castle while aiming

        [Header("Camera Arena Bounds")]
        public float minX = -16f;
        public float maxX = 16f;
        public float minY = -2f;
        public float maxY = 12f;

        private Camera cam;
        private Vector3 velocity = Vector3.zero;
        private float zoomVelocity = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();

            // Auto-override outdated scene values if old small zoom values were saved in Inspector
            if (soldierSelectZoom < 8.0f) soldierSelectZoom = 10.23f;
            if (soldierFocusZoom < 8.0f) soldierFocusZoom = 10.23f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            FindCastleTransforms();
            SetMode(CameraMode.Player1Castle);
        }

        public void FindCastleTransforms()
        {
            if (player1CastleTransform == null && GameManager.Instance != null && GameManager.Instance.player1Castle != null)
            {
                player1CastleTransform = GameManager.Instance.player1Castle.transform;
            }

            if (player2CastleTransform == null && GameManager.Instance != null && GameManager.Instance.player2Castle != null)
            {
                player2CastleTransform = GameManager.Instance.player2Castle.transform;
            }
        }

        [Header("Destruction View Hold")]
        public float destructionHoldDuration = 2.5f;
        private float holdTimer = 0f;
        private Vector3 lastTargetPos;

        public void SetMode(CameraMode mode)
        {
            currentMode = mode;
            if (mode != CameraMode.FollowTarget)
            {
                currentTarget = null;
                holdTimer = 0f;
            }
        }

        public void FollowProjectile(Transform projectileTransform)
        {
            currentTarget = projectileTransform;
            currentMode = CameraMode.FollowTarget;
            holdTimer = 0f;
        }

        public void FocusSoldier(Transform soldierTransform)
        {
            currentTarget = soldierTransform;
            currentMode = CameraMode.FocusSoldier;
            UpdateDynamicAiming(0f); // Default rest position
        }

        public void UpdateDynamicAiming(float dragRatio)
        {
            float safeRatio = Mathf.Clamp01(dragRatio);

            // Dynamically scale orthographic zoom based on pull strength (4.2 close range -> 8.5 far range)
            soldierFocusZoom = Mathf.Lerp(4.2f, 8.5f, safeRatio);

            // Dynamically shift camera X offset toward enemy castle based on pull strength (+2.0 close -> +5.5 far)
            aimingOffset.x = Mathf.Lerp(2.0f, 5.5f, safeRatio);
            aimingOffset.y = Mathf.Lerp(0.8f, 1.8f, safeRatio);
        }

        public void FocusCastle(PlayerSide side)
        {
            currentMode = (side == PlayerSide.Player1) ? CameraMode.Player1Castle : CameraMode.Player2Castle;
            currentTarget = null;
        }

        public void ShowFullOverview()
        {
            currentMode = CameraMode.FullOverview;
            currentTarget = null;
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null) return;

            FindCastleTransforms();

            Vector3 targetPosition = transform.position;
            float targetZoom = cam.orthographicSize;

            switch (currentMode)
            {
                case CameraMode.Player1Castle:
                    if (player1CastleTransform != null) targetPosition = player1CastleTransform.position + cameraOffset;
                    targetZoom = castleFocusZoom;
                    break;

                case CameraMode.Player2Castle:
                    if (player2CastleTransform != null) targetPosition = player2CastleTransform.position + cameraOffset;
                    targetZoom = enemyTurnZoom; // Constant zoomed-out view for enemy turn
                    break;

                case CameraMode.DrivingCastle:
                    if (player1CastleTransform != null) targetPosition = player1CastleTransform.position + cameraOffset;
                    targetZoom = movementZoom;
                    break;

                case CameraMode.SoldierSelection:
                    if (player1CastleTransform != null) targetPosition = player1CastleTransform.position + cameraOffset;
                    targetZoom = soldierSelectZoom;
                    break;

                case CameraMode.FocusSoldier:
                    if (currentTarget != null)
                    {
                        Soldier s = currentTarget.GetComponent<Soldier>();
                        if (s != null && s.ownerSide == PlayerSide.Player2)
                        {
                            // Do NOT zoom in on enemy soldiers! Keep constant zoomed-out view
                            targetPosition = currentTarget.position + cameraOffset;
                            targetZoom = enemyTurnZoom;
                        }
                        else
                        {
                            // Shift camera +3.8 units right towards enemy castle ONLY while actively aiming/dragging slingshot!
                            CastleBusters.Combat.SlingshotLauncher launcher = FindFirstObjectByType<CastleBusters.Combat.SlingshotLauncher>();
                            bool isDragging = (launcher != null && launcher.IsDragging);
                            Vector3 activeOffset = isDragging ? aimingOffset : cameraOffset;

                            targetPosition = currentTarget.position + activeOffset;
                            targetZoom = soldierFocusZoom;
                        }
                    }
                    else
                    {
                        if (player1CastleTransform != null) targetPosition = player1CastleTransform.position + cameraOffset;
                        targetZoom = soldierSelectZoom;
                    }
                    break;

                case CameraMode.FollowTarget:
                    if (currentTarget != null)
                    {
                        lastTargetPos = currentTarget.position;
                        targetPosition = currentTarget.position + cameraOffset;
                        targetZoom = followTargetZoom;
                        holdTimer = destructionHoldDuration;
                    }
                    else if (holdTimer > 0f)
                    {
                        // Hold camera at impact location for a few seconds to display facade destruction
                        holdTimer -= Time.deltaTime;
                        targetPosition = lastTargetPos + cameraOffset;
                        targetZoom = followTargetZoom;
                    }
                    else
                    {
                        // Hold duration finished -> revert to active player castle
                        PlayerSide active = TurnManager.Instance != null ? TurnManager.Instance.activePlayer : PlayerSide.Player1;
                        FocusCastle(active);
                    }
                    break;

                case CameraMode.FullOverview:
                    targetPosition = new Vector3(0f, 2.5f, -10f);
                    targetZoom = overviewZoom;
                    break;
            }

            // Clamp Target Camera Position within Arena Bounds
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            targetPosition.z = -10f; // Always keep Z camera distance

            // Smooth Position Transition
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, positionSmoothTime);

            // Smooth Zoom (Orthographic & Perspective support)
            if (cam.orthographic)
            {
                cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
            }
            else
            {
                // Perspective Camera Fallback: map orthographic size to Perspective Field of View
                float targetFOV = Mathf.Clamp(targetZoom * 6.5f, 20f, 85f);
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFOV, ref zoomVelocity, zoomSmoothTime);
            }
        }
    }
}
