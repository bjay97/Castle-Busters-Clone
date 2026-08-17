using UnityEngine;
using UnityEngine.UI;
using CastleBusters.Core;
using CastleBusters.Environment;
using CastleBusters.Units;

namespace CastleBusters.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Turn & Action HUD")]
        public Text turnText;
        public Text actionCounterText;

        [Header("Castle Health Bars")]
        public Image p1CastleHealthFill;
        public Text p1CastleHealthText;
        public Image p2CastleHealthFill;
        public Text p2CastleHealthText;

        [Header("Game Over Overlay")]
        public GameObject gameOverPanel;
        public Text winnerText;
        public Button restartButton;

        private void Start()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += UpdateTurnUI;
                TurnManager.Instance.OnActionCountChanged += UpdateActionUI;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += HandleGameOver;

                if (GameManager.Instance.player1Castle != null)
                {
                    GameManager.Instance.player1Castle.OnCastleHealthChanged += UpdateP1CastleHealth;
                }
                if (GameManager.Instance.player2Castle != null)
                {
                    GameManager.Instance.player2Castle.OnCastleHealthChanged += UpdateP2CastleHealth;
                }
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= UpdateTurnUI;
                TurnManager.Instance.OnActionCountChanged -= UpdateActionUI;
            }
        }

        private void UpdateTurnUI(PlayerSide side)
        {
            if (turnText != null)
            {
                turnText.text = (side == PlayerSide.Player1) ? "Player 1's Turn (Left Castle)" : "Player 2's Turn (Right Castle)";
            }
        }

        private void UpdateActionUI(int actionsTaken)
        {
            if (actionCounterText != null)
            {
                int actionsRemaining = TurnManager.MaxActionsPerTurn - actionsTaken;
                actionCounterText.text = $"Shots Remaining: {actionsRemaining}/{TurnManager.MaxActionsPerTurn}";
            }
        }

        private void UpdateP1CastleHealth(float current, float max)
        {
            if (p1CastleHealthFill != null) p1CastleHealthFill.fillAmount = current / max;
            if (p1CastleHealthText != null) p1CastleHealthText.text = $"P1 Castle: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void UpdateP2CastleHealth(float current, float max)
        {
            if (p2CastleHealthFill != null) p2CastleHealthFill.fillAmount = current / max;
            if (p2CastleHealthText != null) p2CastleHealthText.text = $"P2 Castle: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void HandleGameOver(PlayerSide winner)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                if (winnerText != null)
                {
                    winnerText.text = $"{winner} VICTORY!";
                }
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
    }
}
