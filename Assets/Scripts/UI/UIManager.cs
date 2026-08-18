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

        [Header("Castle Health Bars (Supports Legacy Text or TextMeshPro)")]
        public Image p1CastleHealthFill;
        public Text p1CastleHealthText;
        public TextMeshProUGUI p1CastleHealthTextTMP;

        public Image p2CastleHealthFill;
        public Text p2CastleHealthText;
        public TextMeshProUGUI p2CastleHealthTextTMP;

        [Header("Castle Movement Fuel Gauge")]
        public Image fuelBarFill;
        public Text fuelText;
        public TextMeshProUGUI fuelTextTMP;

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
            }

            BindCastleHealthListeners();

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void Update()
        {
            // Auto bind listeners if castles registered after Start
            BindCastleHealthListeners();
        }

        private bool p1Bound = false;
        private bool p2Bound = false;
        private bool fuelBound = false;

        private void BindCastleHealthListeners()
        {
            if (!p1Bound && GameManager.Instance != null && GameManager.Instance.player1Castle != null)
            {
                p1Bound = true;
                GameManager.Instance.player1Castle.OnCastleHealthChanged += UpdateP1CastleHealth;
                UpdateP1CastleHealth(GameManager.Instance.player1Castle.currentCastleHealth, GameManager.Instance.player1Castle.maxCastleHealth);
            }

            if (!p2Bound && GameManager.Instance != null && GameManager.Instance.player2Castle != null)
            {
                p2Bound = true;
                GameManager.Instance.player2Castle.OnCastleHealthChanged += UpdateP2CastleHealth;
                UpdateP2CastleHealth(GameManager.Instance.player2Castle.currentCastleHealth, GameManager.Instance.player2Castle.maxCastleHealth);
            }

            if (!fuelBound && GameManager.Instance != null && GameManager.Instance.player1Castle != null)
            {
                CastleMovement movement = GameManager.Instance.player1Castle.GetComponent<CastleMovement>();
                if (movement != null)
                {
                    fuelBound = true;
                    movement.OnFuelChanged += UpdateFuelUI;
                    UpdateFuelUI(movement.currentFuel, movement.maxFuel);
                }
            }
        }

        private void UpdateFuelUI(float current, float max)
        {
            float fill = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
            int pct = Mathf.CeilToInt(fill * 100f);
            if (fuelBarFill != null) fuelBarFill.fillAmount = fill;
            string msg = $"Fuel: {pct}%";
            if (fuelText != null) fuelText.text = msg;
            if (fuelTextTMP != null) fuelTextTMP.text = msg;
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
            float fill = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
            int pct = Mathf.CeilToInt(fill * 100f);
            if (p1CastleHealthFill != null) p1CastleHealthFill.fillAmount = fill;
            string msg = $"P1 Castle: {pct}%";
            if (p1CastleHealthText != null) p1CastleHealthText.text = msg;
            if (p1CastleHealthTextTMP != null) p1CastleHealthTextTMP.text = msg;
        }

        private void UpdateP2CastleHealth(float current, float max)
        {
            float fill = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
            int pct = Mathf.CeilToInt(fill * 100f);
            if (p2CastleHealthFill != null) p2CastleHealthFill.fillAmount = fill;
            string msg = $"P2 Castle: {pct}%";
            if (p2CastleHealthText != null) p2CastleHealthText.text = msg;
            if (p2CastleHealthTextTMP != null) p2CastleHealthTextTMP.text = msg;
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
