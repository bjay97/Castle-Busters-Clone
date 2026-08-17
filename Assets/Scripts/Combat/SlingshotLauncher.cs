using UnityEngine;
using CastleBusters.Core;
using CastleBusters.Units;

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
            mainCamera = Camera.main;
            if (trajectoryPredictor == null) trajectoryPredictor = GetComponent<TrajectoryPredictor>();

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

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            if (activeSoldier == null || activeSoldier.IsDead || activeSoldier.hasFiredThisTurn) return;

            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                // Check if user clicked near active soldier
                float distToSoldier = Vector2.Distance(mouseWorldPos, activeSoldier.transform.position);
                if (distToSoldier < 2f)
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

                if (Input.GetMouseButtonUp(0))
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
