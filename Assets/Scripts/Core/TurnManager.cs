using System;
using UnityEngine;

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

        [Header("Turn State")]
        public PlayerSide activePlayer = PlayerSide.Player1;
        public int actionsTakenThisTurn = 0;
        public const int MaxActionsPerTurn = 2;

        public event Action<PlayerSide> OnTurnChanged;
        public event Action<int> OnActionCountChanged;
        public event Action OnTurnSettled;

        private bool isWaitingForPhysics = false;
        private float settleTimer = 0f;
        public float settleDelay = 2.5f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartTurn(PlayerSide side)
        {
            activePlayer = side;
            actionsTakenThisTurn = 0;
            OnTurnChanged?.Invoke(activePlayer);
            OnActionCountChanged?.Invoke(actionsTakenThisTurn);
        }

        public void RegisterActionFired()
        {
            actionsTakenThisTurn++;
            OnActionCountChanged?.Invoke(actionsTakenThisTurn);
            StartWaitingForPhysicsSettle();
        }

        public void StartWaitingForPhysicsSettle()
        {
            isWaitingForPhysics = true;
            settleTimer = settleDelay;
        }

        private void Update()
        {
            if (!isWaitingForPhysics) return;

            settleTimer -= Time.deltaTime;
            if (settleTimer <= 0f)
            {
                isWaitingForPhysics = false;
                OnTurnSettled?.Invoke();
                EvaluateTurnProgress();
            }
        }

        private void EvaluateTurnProgress()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            if (actionsTakenThisTurn >= MaxActionsPerTurn)
            {
                // Switch turn to opposing player
                PlayerSide nextPlayer = (activePlayer == PlayerSide.Player1) ? PlayerSide.Player2 : PlayerSide.Player1;
                StartTurn(nextPlayer);
            }
            else
            {
                // Player still has actions remaining in current turn
                OnTurnChanged?.Invoke(activePlayer);
            }
        }
    }
}
