using UnityEngine;
using CastleBusters.Core;
using CastleBusters.Units;
using CastleBusters.Environment;

namespace CastleBusters.Combat
{
    public class SlingshotLauncher : MonoBehaviour
    {
        [Header("Slingshot Config")]
        public float maxDragDistance = 3f;
        public float launchForceMultiplier = 12f;
        public TrajectoryPredictor trajectoryPredictor;

        [Header("Active Aiming State")]
        public Soldier activeSoldier;

        private bool isDragging = false;
        private Vector2 dragStartPosition;
        private Vector2 currentDragPosition;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (trajectoryPredictor == null) trajectoryPredictor = GetComponent<TrajectoryPredictor>();

            if (activeSoldier == null)
            {
                activeSoldier = FindFirstObjectByType<Soldier>();
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += AutoSelectSoldierForTurn;
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= AutoSelectSoldierForTurn;
            }
        }

        public void SelectSoldier(Soldier soldier)
        {
            if (soldier != null && !soldier.IsDead && !soldier.hasFiredThisTurn)
            {
                activeSoldier = soldier;
            }
        }

        private void AutoSelectSoldierForTurn(PlayerSide activeSide)
        {
            activeSoldier = null;

            Castle activeCastle = (activeSide == PlayerSide.Player1) 
                ? GameManager.Instance?.player1Castle 
                : GameManager.Instance?.player2Castle;

            if (activeCastle != null)
            {
                foreach (var soldier in activeCastle.soldiers)
                {
                    if (soldier != null && !soldier.IsDead && !soldier.hasFiredThisTurn)
                    {
                        activeSoldier = soldier;
                        break;
                    }
                }
            }
        }

        private Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            }
#endif
            return Input.mousePosition;
        }

        private bool IsPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame;
            }
#endif
            return Input.GetMouseButtonDown(0);
        }

        private bool IsPointerIsPressed()
        {
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.press.isPressed;
            }
#endif
            return Input.GetMouseButton(0);
        }

        private bool IsPointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.press.wasReleasedThisFrame;
            }
#endif
            return Input.GetMouseButtonUp(0);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            if (activeSoldier == null || activeSoldier.IsDead || activeSoldier.hasFiredThisTurn) return;

            if (mainCamera == null)
            {
                mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }
            if (mainCamera == null) return;

            Vector2 pointerScreenPos = GetPointerScreenPosition();
            Vector3 worldPos3D = mainCamera.ScreenToWorldPoint(new Vector3(pointerScreenPos.x, pointerScreenPos.y, -mainCamera.transform.position.z));
            Vector2 mouseWorldPos = (Vector2)worldPos3D;

            if (IsPointerPressedThisFrame())
            {
                // Check if user clicked anywhere on screen or near active soldier
                float distToSoldier = Vector2.Distance(mouseWorldPos, activeSoldier.transform.position);
                if (distToSoldier < 6f || activeSoldier != null)
                {
                    isDragging = true;
                    dragStartPosition = activeSoldier.transform.position;
                }
            }

            if (isDragging)
            {
                currentDragPosition = mouseWorldPos;
                Vector2 dragVector = dragStartPosition - currentDragPosition;

                if (dragVector.magnitude > maxDragDistance)
                {
                    dragVector = dragVector.normalized * maxDragDistance;
                }

                Vector2 launchVelocity = dragVector * launchForceMultiplier;

                if (trajectoryPredictor != null)
                {
                    trajectoryPredictor.ShowTrajectory(activeSoldier.transform.position, launchVelocity);
                }

                if (IsPointerReleasedThisFrame() || !IsPointerIsPressed())
                {
                    isDragging = false;
                    if (trajectoryPredictor != null) trajectoryPredictor.HideTrajectory();

                    FireProjectile(launchVelocity);
                }
            }
        }

        private void FireProjectile(Vector2 launchVelocity)
        {
            if (activeSoldier == null || activeSoldier.projectilePrefab == null) return;

            GameObject projObj = Instantiate(activeSoldier.projectilePrefab, activeSoldier.transform.position, Quaternion.identity);
            Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = launchVelocity;
            }

            activeSoldier.hasFiredThisTurn = true;
            activeSoldier = null;

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RegisterActionFired();
            }
        }
    }
}
