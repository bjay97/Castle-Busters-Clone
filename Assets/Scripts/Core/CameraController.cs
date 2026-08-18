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
        public float movementZoom = 11.5f;       // Significantly zoomed out wide view while driving castle
        public float soldierSelectZoom = 5.2f;    // Slightly zoomed in during soldier select
        public float soldierFocusZoom = 3.8f;     // Smooth close-up pan on active soldier
        public float followTargetZoom = 8.5f;     // Significantly zoomed out wide view while tracking missiles
        public float overviewZoom = 13.0f;
        public float zoomSmoothTime = 0.3f;

        [Header("Smooth Damping Settings")]
        public float positionSmoothTime = 0.25f;
        public Vector3 cameraOffset = new Vector3(0f, 1.2f, -10f);

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

        public void SetMode(CameraMode mode)
        {
            currentMode = mode;
            if (mode != CameraMode.FollowTarget)
            {
                currentTarget = null;
            }
        }

        public void FollowProjectile(Transform projectileTransform)
        {
            currentTarget = projectileTransform;
            currentMode = CameraMode.FollowTarget;
        }

        public void FocusSoldier(Transform soldierTransform)
        {
            currentTarget = soldierTransform;
            currentMode = CameraMode.FocusSoldier;
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
                    targetZoom = castleFocusZoom;
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
                        targetPosition = currentTarget.position + cameraOffset;
                        targetZoom = soldierFocusZoom;
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
                        targetPosition = currentTarget.position + cameraOffset;
                        targetZoom = followTargetZoom;
                    }
                    else
                    {
                        // Target destroyed or lost -> revert to active player castle
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
