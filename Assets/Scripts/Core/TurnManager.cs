using System;
using UnityEngine;
using CastleBusters.Environment;
using CastleBusters.Units;

namespace CastleBusters.Core
{
    public enum GameState
    {
        Initialization,
        PlayerTurn,
        Aiming,
        ProjectileInFlight,
        PhysicsSettling,
        GameOver
    }

    public enum PlayerSide
    {
        Player1, // Left Castle
        Player2  // Right Castle
    }

    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Round & Timer Config")]
        public int currentRound = 1;
        public int maxRounds = 12;
        public float turnDuration = 30f;
        public float currentTurnTimeRemaining = 30f;
        public bool isTimerRunning = false;

        [Header("Turn State")]
        public PlayerSide activePlayer = PlayerSide.Player1;
        public int actionsTakenThisTurn = 0;
        public int maxActionsThisTurn = 1;
        public const int MaxActionsPerTurn = 2; // Kept for legacy compatibility if referenced

        public event Action<PlayerSide> OnTurnChanged;
        public event Action<int> OnActionCountChanged;
        public event Action<int, int> OnRoundChanged; // (currentRound, maxRounds)
        public event Action<float, float> OnTurnTimerUpdated; // (remainingSeconds, totalSeconds)
        public event Action OnTurnSettled;

        private bool isWaitingForPhysics = false;
        private float settleTimer = 0f;
        public float settleDelay = 3.2f;

        private bool isFirstTurnOfGame = true;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartTurn(PlayerSide side)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            // Increment round when turn cycles back to Player 1 (after initial game start)
            if (side == PlayerSide.Player1)
            {
                if (!isFirstTurnOfGame)
                {
                    currentRound++;
                }
                isFirstTurnOfGame = false;

                if (currentRound > maxRounds)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TriggerRoundLimitLoss();
                    }
                    return;
                }
            }

            activePlayer = side;
            actionsTakenThisTurn = 0;
            maxActionsThisTurn = GetAliveSoldierCountForSide(side);

            ResetSoldierFiredFlags(side);

            currentTurnTimeRemaining = turnDuration;
            isTimerRunning = true;

            if (CameraController.Instance != null) CameraController.Instance.FocusCastle(side);
            OnTurnChanged?.Invoke(activePlayer);
            OnActionCountChanged?.Invoke(actionsTakenThisTurn);
            OnRoundChanged?.Invoke(currentRound, maxRounds);
            OnTurnTimerUpdated?.Invoke(currentTurnTimeRemaining, turnDuration);
        }

        public int GetAliveSoldierCountForSide(PlayerSide side)
        {
            Castle castle = (side == PlayerSide.Player1) 
                ? GameManager.Instance?.player1Castle 
                : GameManager.Instance?.player2Castle;

            if (castle == null)
            {
                Castle[] foundCastles = FindObjectsByType<Castle>(FindObjectsSortMode.None);
                foreach (var c in foundCastles)
                {
                    if (c != null && c.ownerSide == side)
                    {
                        castle = c;
                        break;
                    }
                }
            }

            int count = 0;
            if (castle != null)
            {
                if (castle.soldiers == null || castle.soldiers.Count == 0)
                {
                    castle.RefreshCastleHealth();
                }

                foreach (var s in castle.soldiers)
                {
                    if (s != null && !s.IsDead) count++;
                }

                if (count == 0)
                {
                    Soldier[] childSoldiers = castle.GetComponentsInChildren<Soldier>(true);
                    foreach (var s in childSoldiers)
                    {
                        if (s != null && !s.IsDead) count++;
                    }
                }
            }

            if (count == 0)
            {
                Soldier[] soldiers = FindObjectsByType<Soldier>(FindObjectsSortMode.None);
                foreach (var s in soldiers)
                {
                    if (s != null && s.ownerSide == side && !s.IsDead) count++;
                }
            }

            return Mathf.Max(1, count);
        }

        public void ResetSoldierFiredFlags(PlayerSide side)
        {
            Soldier[] soldiers = FindObjectsByType<Soldier>(FindObjectsSortMode.None);
            foreach (var s in soldiers)
            {
                if (s != null && s.ownerSide == side)
                {
                    s.hasFiredThisTurn = false;
                }
            }
        }

        public void RegisterActionFired()
        {
            isTimerRunning = false;
            actionsTakenThisTurn++;
            OnActionCountChanged?.Invoke(actionsTakenThisTurn);
            StartWaitingForPhysicsSettle();
        }

        public void StartWaitingForPhysicsSettle()
        {
            isTimerRunning = false;
            isWaitingForPhysics = true;
            settleTimer = settleDelay;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            // Handle turn countdown timer
            if (isTimerRunning && !isWaitingForPhysics)
            {
                currentTurnTimeRemaining -= Time.deltaTime;
                if (currentTurnTimeRemaining < 0f) currentTurnTimeRemaining = 0f;
                OnTurnTimerUpdated?.Invoke(currentTurnTimeRemaining, turnDuration);

                if (currentTurnTimeRemaining <= 0f)
                {
                    // Time expired! Forfeit turn/attack
                    isTimerRunning = false;
                    Debug.Log($"Turn timer expired for {activePlayer}! Forfeiting turn.");
                    RegisterActionFired();
                }
            }

            // Handle physics settling countdown
            if (isWaitingForPhysics)
            {
                settleTimer -= Time.deltaTime;
                if (settleTimer <= 0f)
                {
                    isWaitingForPhysics = false;
                    OnTurnSettled?.Invoke();
                    EvaluateTurnProgress();
                }
            }
        }

        private void EvaluateTurnProgress()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            if (actionsTakenThisTurn >= maxActionsThisTurn)
            {
                // Switch turn to opposing player
                PlayerSide nextPlayer = (activePlayer == PlayerSide.Player1) ? PlayerSide.Player2 : PlayerSide.Player1;
                StartTurn(nextPlayer);
            }
            else
            {
                // Check if any unfired alive soldier remains
                bool hasUnfiredSoldier = false;
                Castle activeCastle = (activePlayer == PlayerSide.Player1) ? GameManager.Instance?.player1Castle : GameManager.Instance?.player2Castle;
                if (activeCastle != null)
                {
                    foreach (var s in activeCastle.soldiers)
                    {
                        if (s != null && !s.IsDead && !s.hasFiredThisTurn)
                        {
                            hasUnfiredSoldier = true;
                            break;
                        }
                    }
                }

                if (!hasUnfiredSoldier)
                {
                    PlayerSide nextPlayer = (activePlayer == PlayerSide.Player1) ? PlayerSide.Player2 : PlayerSide.Player1;
                    StartTurn(nextPlayer);
                }
                else
                {
                    // Player still has actions remaining in current turn: restart timer for remaining soldier
                    currentTurnTimeRemaining = turnDuration;
                    isTimerRunning = true;
                    OnTurnTimerUpdated?.Invoke(currentTurnTimeRemaining, turnDuration);
                    OnTurnChanged?.Invoke(activePlayer);
                }
            }
        }
    }
}
