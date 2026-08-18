using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CastleBusters.Core;
using CastleBusters.Environment;
using CastleBusters.Units;
using CastleBusters.Combat;

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

        [Header("Match Loading Overlay")]
        public GameObject loadingPanel;
        public CanvasGroup loadingCanvasGroup;
        public Text loadingText;
        public TextMeshProUGUI loadingTextTMP;
        public float loadingDisplayDuration = 1.0f;

        [Header("Bottom-Left Movement Controls")]
        public GameObject movementPanel;
        public Button moveLeftBtn;
        public Button moveRightBtn;
        public Button doneMovingBtn;

        [Header("Bottom-Center Soldier Select Controls")]
        public GameObject soldierSelectPanel;
        public Button soldier1Btn;
        public Button soldier2Btn;

        [Header("Game Over Overlay")]
        public GameObject gameOverPanel;
        public Text winnerText;
        public TextMeshProUGUI winnerTextTMP;
        public Button restartButton;

        private void Start()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                StartCoroutine(HideLoadingScreenRoutine());
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += UpdateTurnUI;
                TurnManager.Instance.OnActionCountChanged += UpdateActionUI;

                // Explicitly refresh turn and action HUD on Start
                UpdateTurnUI(TurnManager.Instance.activePlayer);
                UpdateActionUI(TurnManager.Instance.actionsTakenThisTurn);
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

        private System.Collections.IEnumerator HideLoadingScreenRoutine()
        {
            string msg = "ASSEMBLING FORTRESSES...";
            if (loadingText != null) loadingText.text = msg;
            if (loadingTextTMP != null) loadingTextTMP.text = msg;

            // Allow 1.0s for grid slicing, collider creation, and physics settling
            yield return new WaitForSeconds(loadingDisplayDuration);

            // Fade out smoothly if CanvasGroup is attached specifically to loadingPanel
            if (loadingCanvasGroup != null && loadingPanel != null && (loadingCanvasGroup.gameObject == loadingPanel || loadingCanvasGroup.transform.IsChildOf(loadingPanel.transform)))
            {
                float fadeTime = 0.4f;
                float elapsed = 0f;
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                    yield return null;
                }
            }

            if (loadingPanel != null) loadingPanel.SetActive(false);

            // Notify player turn and refresh HUD elements
            if (TurnManager.Instance != null)
            {
                UpdateTurnUI(TurnManager.Instance.activePlayer);
                UpdateActionUI(TurnManager.Instance.actionsTakenThisTurn);
            }

            BindCastleHealthListeners();
        }

        private Coroutine turnSequenceCoroutine;

        private void UpdateTurnUI(PlayerSide side)
        {
            string msg = (side == PlayerSide.Player1) ? "Player 1's Turn (Your Turn)" : "Player 2's Turn (AI Bot Thinking...)";
            if (turnText != null) turnText.text = msg;
            if (turnTextTMP != null) turnTextTMP.text = msg;

            if (turnSequenceCoroutine != null) StopCoroutine(turnSequenceCoroutine);

            if (side == PlayerSide.Player1)
            {
                turnSequenceCoroutine = StartCoroutine(Player1TurnSequenceRoutine());
            }
            else
            {
                // Hide panels on Player 2 turn
                if (movementPanel != null) movementPanel.SetActive(false);
                if (soldierSelectPanel != null) soldierSelectPanel.SetActive(false);

                // Show solid facades during AI turn
                if (GameManager.Instance != null && GameManager.Instance.player1Castle != null)
                {
                    CastleFacadeVisibility vis = GameManager.Instance.player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
                    if (vis != null) vis.SetFacadeVisibility(true);
                }
            }
        }

        private System.Collections.IEnumerator Player1TurnSequenceRoutine()
        {
            // 1. PHASE 1: Initial 2.5 seconds opaque view
            if (movementPanel != null) movementPanel.SetActive(false);
            if (soldierSelectPanel != null) soldierSelectPanel.SetActive(false);

            CastleFacadeVisibility p1Vis = null;
            if (GameManager.Instance != null && GameManager.Instance.player1Castle != null)
            {
                p1Vis = GameManager.Instance.player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
                if (p1Vis != null) p1Vis.SetFacadeVisibility(true); // 100% OPAQUE at start of turn
            }

            yield return new WaitForSeconds(2.5f);

            // 2. PHASE 2: Clear up facade & Show Bottom-Left Movement Controls
            if (p1Vis != null) p1Vis.SetFacadeVisibility(false); // Clear facade so room is visible

            if (movementPanel != null) movementPanel.SetActive(true);

            // Setup Movement Button listeners
            CastleMovement p1Movement = GameManager.Instance?.player1Castle?.GetComponent<CastleMovement>();
            SetupMovementButtons(p1Movement);

            // Wait until user clicks "Done Moving" or moves & stops
            bool doneMoving = false;
            if (doneMovingBtn != null)
            {
                doneMovingBtn.onClick.RemoveAllListeners();
                doneMovingBtn.onClick.AddListener(() => doneMoving = true);
            }

            // Allow driving phase for up to 8 seconds or until Done Moving clicked
            float driveTimer = 0f;
            while (!doneMoving && driveTimer < 8.0f)
            {
                driveTimer += Time.deltaTime;
                yield return null;
            }

            if (movementPanel != null) movementPanel.SetActive(false);
            if (p1Movement != null) p1Movement.ReleaseMove();

            // 3. PHASE 3: Show Bottom-Center Soldier Selection Controls
            if (soldierSelectPanel != null) soldierSelectPanel.SetActive(true);

            Castle p1Castle = GameManager.Instance?.player1Castle;
            SlingshotLauncher launcher = FindFirstObjectByType<SlingshotLauncher>();

            Soldier chosenSoldier = null;

            if (p1Castle != null && p1Castle.soldiers.Count > 0)
            {
                if (soldier1Btn != null && p1Castle.soldiers.Count >= 1)
                {
                    soldier1Btn.onClick.RemoveAllListeners();
                    soldier1Btn.onClick.AddListener(() => {
                        chosenSoldier = p1Castle.soldiers[0];
                    });
                }

                if (soldier2Btn != null && p1Castle.soldiers.Count >= 2)
                {
                    soldier2Btn.onClick.RemoveAllListeners();
                    soldier2Btn.onClick.AddListener(() => {
                        chosenSoldier = p1Castle.soldiers[1];
                    });
                }
            }

            // Wait until user selects a soldier via bottom-center buttons
            while (chosenSoldier == null)
            {
                // Fallback auto-select if buttons not wired in scene
                if (soldierSelectPanel == null || (!soldier1Btn && !soldier2Btn))
                {
                    if (p1Castle != null && p1Castle.soldiers.Count > 0)
                    {
                        foreach (var s in p1Castle.soldiers)
                        {
                            if (s != null && !s.IsDead && !s.hasFiredThisTurn) { chosenSoldier = s; break; }
                        }
                    }
                    if (chosenSoldier == null && p1Castle != null && p1Castle.soldiers.Count > 0) chosenSoldier = p1Castle.soldiers[0];
                }
                yield return null;
            }

            if (soldierSelectPanel != null) soldierSelectPanel.SetActive(false);

            // 4. PHASE 4: Enable Trajectory Prediction & Aiming ONLY after soldier selected!
            if (launcher != null && chosenSoldier != null)
            {
                launcher.EnableAimingForSoldier(chosenSoldier);
            }
        }

        private void SetupMovementButtons(CastleMovement movement)
        {
            if (movement == null) return;

            if (moveLeftBtn != null)
            {
                UnityEngine.EventSystems.EventTrigger trigger = moveLeftBtn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null) trigger = moveLeftBtn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                trigger.triggers.Clear();

                var down = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
                down.callback.AddListener((data) => movement.PressMoveLeft());
                trigger.triggers.Add(down);

                var up = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
                up.callback.AddListener((data) => movement.ReleaseMove());
                trigger.triggers.Add(up);
            }

            if (moveRightBtn != null)
            {
                UnityEngine.EventSystems.EventTrigger trigger = moveRightBtn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null) trigger = moveRightBtn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                trigger.triggers.Clear();

                var down = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
                down.callback.AddListener((data) => movement.PressMoveRight());
                trigger.triggers.Add(down);

                var up = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
                up.callback.AddListener((data) => movement.ReleaseMove());
                trigger.triggers.Add(up);
            }
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
