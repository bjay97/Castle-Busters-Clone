using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CastleBusters.Core;
using CastleBusters.Units;
using CastleBusters.Environment;
using CastleBusters.Combat;

namespace CastleBusters.AI
{
    public class SimpleBotAI : MonoBehaviour
    {
        [Header("AI Config")]
        public PlayerSide aiSide = PlayerSide.Player2;
        public float turnThinkDelay = 1.2f;

        [Header("Aiming Randomization")]
        public float minLaunchAngle = 135f; // Pointing left towards Player 1 castle
        public float maxLaunchAngle = 160f;
        public float minLaunchForce = 10f;
        public float maxLaunchForce = 16f;

        private bool isExecutingTurn = false;

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }
        }

        private void HandleTurnChanged(PlayerSide activeSide)
        {
            if (activeSide == aiSide && !isExecutingTurn)
            {
                StartCoroutine(ExecuteAITurnRoutine());
            }
        }

        private IEnumerator ExecuteAITurnRoutine()
        {
            isExecutingTurn = true;

            // Wait a moment so the AI action feels natural
            yield return new WaitForSeconds(turnThinkDelay);

            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                isExecutingTurn = false;
                yield break;
            }

            // Find available AI soldier
            Soldier aiSoldier = FindAvailableAISoldier();

            if (aiSoldier != null && aiSoldier.projectilePrefab != null)
            {
                // Optionally drive AI castle slightly to reposition before aiming
                Castle aiCastle = (aiSide == PlayerSide.Player1) ? GameManager.Instance?.player1Castle : GameManager.Instance?.player2Castle;
                if (aiCastle != null)
                {
                    CastleMovement aiMovement = aiCastle.GetComponent<CastleMovement>();
                    if (aiMovement != null && Random.value < 0.65f && aiMovement.currentFuel > 20f)
                    {
                        float driveDir = (Random.value < 0.5f) ? -1f : 1f;
                        float driveTime = Random.Range(0.6f, 1.2f);
                        float elapsed = 0f;
                        while (elapsed < driveTime && aiMovement.currentFuel > 0f)
                        {
                            elapsed += Time.deltaTime;
                            aiMovement.MoveCastle(driveDir);
                            yield return null;
                        }
                        yield return new WaitForSeconds(0.3f);
                    }
                }

                float randomAngle = Random.Range(minLaunchAngle, maxLaunchAngle) * Mathf.Deg2Rad;
                float randomForce = Random.Range(minLaunchForce, maxLaunchForce);
                Vector2 launchVelocity = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomForce;

                aiSoldier.hasFiredThisTurn = true;

                if (aiSoldier.fireMode == FireMode.RapidSalvo)
                {
                    yield return StartCoroutine(FireAISalvoRoutine(aiSoldier, launchVelocity));
                }
                else
                {
                    SpawnAISingleMissile(aiSoldier, launchVelocity);
                }

                Debug.Log($"[SimpleBotAI] AI launched {aiSoldier.soldierName}'s attack.");

                if (TurnManager.Instance != null)
                {
                    TurnManager.Instance.RegisterActionFired();
                }
            }
            else
            {
                Debug.LogWarning("[SimpleBotAI] No available AI soldier found to fire!");
            }

            isExecutingTurn = false;
        }

        private void SpawnAISingleMissile(Soldier soldier, Vector2 launchVelocity, bool trackCamera = true)
        {
            Vector3 spawnPos = soldier.transform.position + (Vector3)(launchVelocity.normalized * 0.8f);
            GameObject projObj = Instantiate(soldier.projectilePrefab, spawnPos, Quaternion.identity);

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

            // Smoothly track lead/midway AI enemy projectile with camera in real-time
            if (trackCamera && CameraController.Instance != null)
            {
                CameraController.Instance.FollowProjectile(projObj.transform);
            }
        }

        private IEnumerator FireAISalvoRoutine(Soldier soldier, Vector2 baseLaunchVelocity)
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

                bool trackThisMissile = (i == 0 || i == 3);
                SpawnAISingleMissile(soldier, salvoVel, trackThisMissile);

                float currentInterval = Mathf.Max(0.03f, interval + Random.Range(-0.02f, 0.02f));
                yield return new WaitForSeconds(currentInterval);
            }
        }

        private Soldier FindAvailableAISoldier()
        {
            Castle aiCastle = (aiSide == PlayerSide.Player1) 
                ? GameManager.Instance?.player1Castle 
                : GameManager.Instance?.player2Castle;

            if (aiCastle != null)
            {
                foreach (var s in aiCastle.soldiers)
                {
                    if (s != null && !s.IsDead && !s.hasFiredThisTurn) return s;
                }
            }

            // Scene search fallback
            Soldier[] allSoldiers = FindObjectsByType<Soldier>(FindObjectsSortMode.None);
            foreach (var s in allSoldiers)
            {
                if (s != null && s.ownerSide == aiSide && !s.IsDead && !s.hasFiredThisTurn)
                {
                    return s;
                }
            }

            // If only 1 AI soldier is alive, reset flag so they can take the 2nd shot of the turn
            foreach (var s in allSoldiers)
            {
                if (s != null && s.ownerSide == aiSide && !s.IsDead)
                {
                    s.hasFiredThisTurn = false;
                    return s;
                }
            }

            return null;
        }
    }
}
