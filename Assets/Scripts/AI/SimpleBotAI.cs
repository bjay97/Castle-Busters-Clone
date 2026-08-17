using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CastleBusters.Core;
using CastleBusters.Units;
using CastleBusters.Environment;

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
                // Calculate randomized trajectory angle & force towards Player 1
                float randomAngle = Random.Range(minLaunchAngle, maxLaunchAngle) * Mathf.Deg2Rad;
                float randomForce = Random.Range(minLaunchForce, maxLaunchForce);

                Vector2 launchVelocity = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomForce;

                // Spawn projectile with offset
                Vector3 spawnPos = aiSoldier.transform.position + (Vector3)(launchVelocity.normalized * 0.8f);
                GameObject projObj = Instantiate(aiSoldier.projectilePrefab, spawnPos, Quaternion.identity);

                // Prevent self-damage
                Collider2D projCol = projObj.GetComponent<Collider2D>();
                Collider2D soldierCol = aiSoldier.GetComponent<Collider2D>();
                if (projCol != null && soldierCol != null)
                {
                    Physics2D.IgnoreCollision(projCol, soldierCol);
                }

                Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = launchVelocity;
                }

                aiSoldier.hasFiredThisTurn = true;

                Debug.Log($"[SimpleBotAI] AI launched {aiSoldier.soldierName}'s missile with velocity {launchVelocity}");

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
