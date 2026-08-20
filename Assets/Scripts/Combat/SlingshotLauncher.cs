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
        public bool isAimingAllowed = false;
        public bool IsDragging => isDragging;

        private bool isDragging = false;
        private Vector2 dragStartPosition;
        private Vector2 currentDragPosition;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (trajectoryPredictor == null) trajectoryPredictor = GetComponent<TrajectoryPredictor>();
            if (trajectoryPredictor == null) trajectoryPredictor = gameObject.AddComponent<TrajectoryPredictor>();

            FindAndSelectPlayer1Soldier();

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += AutoSelectSoldierForTurn;
            }
        }

        private void FindAndSelectPlayer1Soldier()
        {
            if (activeSoldier != null && !activeSoldier.IsDead && !activeSoldier.hasFiredThisTurn) return;

            Soldier[] soldiers = FindObjectsByType<Soldier>(FindObjectsSortMode.None);
            foreach (var s in soldiers)
            {
                if (s != null && s.ownerSide == PlayerSide.Player1 && !s.IsDead && !s.hasFiredThisTurn)
                {
                    activeSoldier = s;
                    return;
                }
            }

            // Fallback for P1 soldier taking 2nd action
            foreach (var s in soldiers)
            {
                if (s != null && s.ownerSide == PlayerSide.Player1 && !s.IsDead)
                {
                    s.hasFiredThisTurn = false;
                    activeSoldier = s;
                    return;
                }
            }

            // General fallback
            if (soldiers.Length > 0 && activeSoldier == null)
            {
                activeSoldier = soldiers[0];
                activeSoldier.hasFiredThisTurn = false;
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= AutoSelectSoldierForTurn;
            }
        }

        public void EnableAimingForSoldier(Soldier soldier)
        {
            if (soldier != null && !soldier.IsDead && !soldier.hasFiredThisTurn)
            {
                activeSoldier = soldier;
                isAimingAllowed = true;
            }
        }

        public void SelectSoldier(Soldier soldier)
        {
            EnableAimingForSoldier(soldier);
        }

        private void AutoSelectSoldierForTurn(PlayerSide activeSide)
        {
            activeSoldier = null;
            isAimingAllowed = false;

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

            // Scene search fallback for test scenes or missing Castle component
            if (activeSoldier == null)
            {
                Soldier[] soldiers = FindObjectsByType<Soldier>(FindObjectsSortMode.None);
                foreach (var s in soldiers)
                {
                    if (s != null && s.ownerSide == activeSide && !s.IsDead && !s.hasFiredThisTurn)
                    {
                        activeSoldier = s;
                        break;
                    }
                }

                // If only 1 soldier is alive on this side, reset flag so they can take the 2nd shot of the turn
                if (activeSoldier == null)
                {
                    foreach (var s in soldiers)
                    {
                        if (s != null && s.ownerSide == activeSide && !s.IsDead)
                        {
                            s.hasFiredThisTurn = false;
                            activeSoldier = s;
                            break;
                        }
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

        private bool IsPointerOverUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return true;
            if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return true;
            return false;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            if (!isAimingAllowed) return;
            if (activeSoldier == null || activeSoldier.IsDead || activeSoldier.hasFiredThisTurn) return;

            if (mainCamera == null)
            {
                mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }
            if (mainCamera == null) return;

            Vector2 pointerScreenPos = GetPointerScreenPosition();
            Vector3 worldPos3D = mainCamera.ScreenToWorldPoint(new Vector3(pointerScreenPos.x, pointerScreenPos.y, -mainCamera.transform.position.z));
            Vector2 mouseWorldPos = (Vector2)worldPos3D;

            if (!isDragging && IsPointerPressedThisFrame())
            {
                // Ignore clicks over UI elements (e.g. Soldier Select Buttons, Movement Buttons)
                if (IsPointerOverUI())
                {
                    return;
                }

                float distToSoldier = Vector2.Distance(mouseWorldPos, activeSoldier.transform.position);
                if (distToSoldier < 4f)
                {
                    isDragging = true;
                    dragStartPosition = activeSoldier.transform.position;

                    // Hide Player 1's facade while actively aiming so player sees soldier and trajectory clearly
                    if (GameManager.Instance != null && GameManager.Instance.player1Castle != null)
                    {
                        CastleFacadeVisibility vis = GameManager.Instance.player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
                        if (vis != null) vis.SetAimingHideState(true);
                    }
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

                float projSpeedMult = 1.0f;
                if (activeSoldier != null && activeSoldier.projectilePrefab != null)
                {
                    Projectile projComp = activeSoldier.projectilePrefab.GetComponent<Projectile>();
                    if (projComp != null)
                    {
                        projSpeedMult = projComp.speedMultiplier;
                    }
                }

                Vector2 launchVelocity = dragVector * launchForceMultiplier * projSpeedMult;

                // Dynamically update camera zoom & X-offset based on pull distance!
                float dragRatio = (maxDragDistance > 0f) ? (dragVector.magnitude / maxDragDistance) : 0f;
                if (CameraController.Instance != null)
                {
                    CameraController.Instance.UpdateDynamicAiming(dragRatio);
                }

                if (trajectoryPredictor != null && dragVector.magnitude > 0.1f)
                {
                    trajectoryPredictor.ShowTrajectory(activeSoldier.transform.position, launchVelocity);
                }

                if (IsPointerReleasedThisFrame())
                {
                    isDragging = false;
                    if (trajectoryPredictor != null) trajectoryPredictor.HideTrajectory();

                    // Restore 100% full opaque facade when aim release occurs
                    if (GameManager.Instance != null && GameManager.Instance.player1Castle != null)
                    {
                        CastleFacadeVisibility vis = GameManager.Instance.player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
                        if (vis != null) vis.SetAimingHideState(false);
                    }

                    if (dragVector.magnitude > 0.3f)
                    {
                        FireProjectile(launchVelocity);
                    }
                }
            }
        }

        private void FireProjectile(Vector2 launchVelocity)
        {
            if (activeSoldier == null || activeSoldier.projectilePrefab == null) return;

            Soldier firingSoldier = activeSoldier;
            firingSoldier.hasFiredThisTurn = true;
            activeSoldier = null;
            isAimingAllowed = false;

            if (firingSoldier.fireMode == FireMode.RapidSalvo)
            {
                StartCoroutine(FireSalvoRoutine(firingSoldier, launchVelocity));
            }
            else
            {
                SpawnSingleMissile(firingSoldier, launchVelocity);
                if (TurnManager.Instance != null) TurnManager.Instance.RegisterActionFired();
            }
        }

        private void SpawnSingleMissile(Soldier soldier, Vector2 launchVelocity, bool trackCamera = true)
        {
            Vector3 spawnPos = soldier.transform.position + (Vector3)(launchVelocity.normalized * 0.8f);
            GameObject projObj = Instantiate(soldier.projectilePrefab, spawnPos, Quaternion.identity);

            Projectile projComp = projObj.GetComponent<Projectile>();
            if (projComp != null)
            {
                projComp.ownerSide = soldier.ownerSide;
            }

            Collider2D projCol = projObj.GetComponent<Collider2D>();
            Collider2D soldierCol = soldier.GetComponent<Collider2D>();
            if (projCol != null && soldierCol != null)
            {
                Physics2D.IgnoreCollision(projCol, soldierCol);
            }

            Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = launchVelocity;
            }

            // Smoothly track lead/midway player projectile with camera in real-time
            if (trackCamera && CameraController.Instance != null)
            {
                CameraController.Instance.FollowProjectile(projObj.transform);
            }
        }

        private System.Collections.IEnumerator FireSalvoRoutine(Soldier soldier, Vector2 baseLaunchVelocity)
        {
            int count = Mathf.Clamp(soldier.salvoCount, 10, 16);
            float interval = Mathf.Clamp(soldier.salvoInterval, 0.05f, 0.2f);
            float spread = soldier.salvoSpreadAngle;

            for (int i = 0; i < count; i++)
            {
                if (soldier == null) break;

                float baseAngle = Mathf.Atan2(baseLaunchVelocity.y, baseLaunchVelocity.x) * Mathf.Rad2Deg;
                float randomAngle = baseAngle + Random.Range(-spread, spread);
                float randomSpeedMult = Random.Range(0.92f, 1.08f);
                float speed = baseLaunchVelocity.magnitude * randomSpeedMult;

                Vector2 salvoVel = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad)) * speed;

                // Lock camera onto the 1st (i == 0) or midway (i == 3) projectile to lead the salvo barrage
                bool trackThisMissile = (i == 0 || i == 3);
                SpawnSingleMissile(soldier, salvoVel, trackThisMissile);

                float currentInterval = Mathf.Max(0.03f, interval + Random.Range(-0.02f, 0.02f));
                yield return new WaitForSeconds(currentInterval);
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RegisterActionFired();
            }
        }
    }
}
