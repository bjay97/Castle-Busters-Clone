using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CastleBusters.Core;
using CastleBusters.Environment;
using CastleBusters.Units;

namespace CastleBusters.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Turn & Action HUD (Supports Legacy Text or TextMeshPro)")]
        public Text turnText;
        public TextMeshProUGUI turnTextTMP;
        
        public Text actionCounterText;
        public TextMeshProUGUI actionCounterTextTMP;

        [Header("Castle Health Bars")]
        public Image p1CastleHealthFill;
        public Text p1CastleHealthText;
        public Image p2CastleHealthFill;
        public Text p2CastleHealthText;

        [Header("Game Over Overlay")]
        public GameObject gameOverPanel;
        public Text winnerText;
        public TextMeshProUGUI winnerTextTMP;
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
            string msg = (side == PlayerSide.Player1) ? "Player 1's Turn (Your Turn)" : "Player 2's Turn (AI Bot Thinking...)";
            if (turnText != null) turnText.text = msg;
            if (turnTextTMP != null) turnTextTMP.text = msg;
        }

        private void UpdateActionUI(int actionsTaken)
        {
            int actionsRemaining = TurnManager.MaxActionsPerTurn - actionsTaken;
            string msg = $"Shots Remaining: {actionsRemaining}/{TurnManager.MaxActionsPerTurn}";
            if (actionCounterText != null) actionCounterText.text = msg;
            if (actionCounterTextTMP != null) actionCounterTextTMP.text = msg;
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
                string msg = $"{winner} VICTORY!";
                if (winnerText != null) winnerText.text = msg;
                if (winnerTextTMP != null) winnerTextTMP.text = msg;
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
