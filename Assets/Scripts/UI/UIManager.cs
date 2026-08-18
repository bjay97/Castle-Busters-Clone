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
            // Auto-ensure EventSystem exists so UI button clicks always work
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            EnsureNonBlockingPanels();

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

            if (CameraController.Instance != null) CameraController.Instance.SetMode(CameraMode.Player1Castle);

            yield return new WaitForSeconds(2.5f);

            // 2. PHASE 2: DRIVING PHASE
            // Keep facade VISIBLE while driving (as requested!)
            if (p1Vis != null) p1Vis.SetFacadeVisibility(true);

            // Zoom out camera somewhat while moving castle (as requested!)
            if (CameraController.Instance != null) CameraController.Instance.SetMode(CameraMode.DrivingCastle);

            if (movementPanel != null) movementPanel.SetActive(true);

            CastleMovement p1Movement = GameManager.Instance?.player1Castle?.GetComponent<CastleMovement>();
            SetupMovementButtons(p1Movement);

            bool doneMoving = false;
            if (doneMovingBtn != null)
            {
                doneMovingBtn.onClick.RemoveAllListeners();
                doneMovingBtn.onClick.AddListener(() => doneMoving = true);
            }

            float driveTimer = 0f;
            while (!doneMoving && driveTimer < 8.0f)
            {
                driveTimer += Time.deltaTime;
                yield return null;
            }

            if (movementPanel != null) movementPanel.SetActive(false);
            if (p1Movement != null) p1Movement.ReleaseMove();

            // 3. PHASE 3: SOLDIER SELECTION PHASE
            if (p1Vis != null) p1Vis.SetFacadeVisibility(false);
            if (CameraController.Instance != null) CameraController.Instance.SetMode(CameraMode.SoldierSelection);
            if (soldierSelectPanel != null) soldierSelectPanel.SetActive(true);

            // Keep Movement Panel active alongside Soldier Selection if fuel is remaining!
            if (p1Movement != null && p1Movement.currentFuel > 0f)
            {
                if (movementPanel != null) movementPanel.SetActive(true);
                SetupMovementButtons(p1Movement);
            }

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
                // Dynamic driving state during soldier selection!
                if (p1Movement != null && p1Movement.IsActivelyMoving)
                {
                    // Zoom out to DrivingCastle mode and show solid facade while driving!
                    if (CameraController.Instance != null) CameraController.Instance.SetMode(CameraMode.DrivingCastle);
                    if (p1Vis != null) p1Vis.SetFacadeVisibility(true);
                }
                else
                {
                    // Zoom back in to SoldierSelection mode and clear facade when stopped!
                    if (CameraController.Instance != null && CameraController.Instance.currentMode == CameraMode.DrivingCastle)
                    {
                        CameraController.Instance.SetMode(CameraMode.SoldierSelection);
                    }
                    if (p1Vis != null) p1Vis.SetFacadeVisibility(false);
                }

                // Auto hide movement panel if fuel empties during soldier selection
                if (p1Movement != null && p1Movement.currentFuel <= 0f && movementPanel != null)
                {
                    movementPanel.SetActive(false);
                }

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

            if (movementPanel != null) movementPanel.SetActive(false);
            if (soldierSelectPanel != null) soldierSelectPanel.SetActive(false);
            if (p1Movement != null) p1Movement.ReleaseMove();

            // Smoothly pan camera directly to the clicked active soldier (as requested!)
            if (CameraController.Instance != null && chosenSoldier != null)
            {
                CameraController.Instance.FocusSoldier(chosenSoldier.transform);
            }

            // 4. PHASE 4: Enable Trajectory Prediction & Aiming ONLY after soldier selected!
            if (launcher != null && chosenSoldier != null)
            {
                launcher.EnableAimingForSoldier(chosenSoldier);
            }
        }

        private void EnsureNonBlockingPanels()
        {
            if (movementPanel != null)
            {
                Image img = movementPanel.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }

            if (soldierSelectPanel != null)
            {
                Image img = soldierSelectPanel.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
        }

        private void SetupMovementButtons(CastleMovement movement)
        {
            if (movement == null) return;

            EnsureNonBlockingPanels();

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

                var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exit.callback.AddListener((data) => movement.ReleaseMove());
                trigger.triggers.Add(exit);
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

                var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exit.callback.AddListener((data) => movement.ReleaseMove());
                trigger.triggers.Add(exit);
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
