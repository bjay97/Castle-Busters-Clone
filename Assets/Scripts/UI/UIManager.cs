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
        public RectTransform p1HealthMaskRect; // Optional RectMask2D / RectTransform container for 9-Sliced Health Bar
        public Text p1CastleHealthText;
        public TextMeshProUGUI p1CastleHealthTextTMP;

        public Image p2CastleHealthFill;
        public RectTransform p2HealthMaskRect; // Optional RectMask2D / RectTransform container for 9-Sliced Health Bar
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

        [Header("FPS Display (Top-Left)")]
        public Text fpsText;
        public TextMeshProUGUI fpsTextTMP;
        private float fpsAccumulator = 0f;
        private int fpsFrames = 0;
        private float fpsTimeLeft = 0.25f;

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
            EnsureFPSCounterUI();

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
            UpdateFPSCounter();
        }

        private void EnsureFPSCounterUI()
        {
            if (fpsText != null || fpsTextTMP != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject fpsObj = new GameObject("FPSCounterText");
            fpsObj.transform.SetParent(canvas.transform, false);

            RectTransform rect = fpsObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); // Top-Left
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(15f, -15f);
            rect.sizeDelta = new Vector2(220f, 40f);

            fpsText = fpsObj.AddComponent<Text>();
            fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fpsText.font == null) fpsText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            fpsText.fontSize = 18;
            fpsText.fontStyle = FontStyle.Bold;
            fpsText.color = new Color(0.2f, 1.0f, 0.4f, 1.0f); // Bright neon green
            fpsText.alignment = TextAnchor.UpperLeft;
            fpsText.raycastTarget = false;

            // Add shadow / outline effect for crisp contrast against any scene background
            Outline outline = fpsObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void UpdateFPSCounter()
        {
            EnsureFPSCounterUI();

            fpsAccumulator += Time.unscaledDeltaTime;
            fpsFrames++;
            fpsTimeLeft -= Time.unscaledDeltaTime;

            if (fpsTimeLeft <= 0.0f)
            {
                float fps = (fpsAccumulator > 0f) ? (fpsFrames / fpsAccumulator) : 0f;
                float ms = (fps > 0f) ? (1000.0f / fps) : 0f;

                string fpsString = $"FPS: {Mathf.RoundToInt(fps)} ({ms:F1} ms)";

                Color fpsColor = (fps >= 55f) ? new Color(0.2f, 1.0f, 0.4f) :
                                 (fps >= 30f) ? new Color(1.0f, 0.8f, 0.2f) :
                                                new Color(1.0f, 0.3f, 0.3f);

                if (fpsText != null)
                {
                    fpsText.text = fpsString;
                    fpsText.color = fpsColor;
                }

                if (fpsTextTMP != null)
                {
                    fpsTextTMP.text = fpsString;
                    fpsTextTMP.color = fpsColor;
                }

                fpsAccumulator = 0.0f;
                fpsFrames = 0;
                fpsTimeLeft = 0.25f;
            }
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
            CastleFacadeVisibility p1Vis = null;
            if (GameManager.Instance != null && GameManager.Instance.player1Castle != null)
            {
                p1Vis = GameManager.Instance.player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
            }

            if (CameraController.Instance != null) CameraController.Instance.SetMode(CameraMode.SoldierSelection);

            CastleMovement p1Movement = GameManager.Instance?.player1Castle?.GetComponent<CastleMovement>();
            if (p1Movement != null && p1Movement.currentFuel > 0f)
            {
                if (movementPanel != null) movementPanel.SetActive(true);
                SetupMovementButtons(p1Movement);
            }
            else
            {
                if (movementPanel != null) movementPanel.SetActive(false);
            }

            if (soldierSelectPanel != null) soldierSelectPanel.SetActive(true);

            Castle p1Castle = GameManager.Instance?.player1Castle;
            SlingshotLauncher launcher = FindFirstObjectByType<SlingshotLauncher>();

            Soldier chosenSoldier = null;

            if (p1Castle != null && p1Castle.soldiers.Count > 0)
            {
                // Ensure soldiers are sorted strictly Left-to-Right by X position
                p1Castle.soldiers.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

                // Reset all soldiers to bright visual state at turn start
                foreach (var s in p1Castle.soldiers)
                {
                    if (s != null) s.SetSelectionVisualState(true);
                }

                if (soldier1Btn != null && p1Castle.soldiers.Count >= 1)
                {
                    Soldier s1 = p1Castle.soldiers[0];
                    Text t1 = soldier1Btn.GetComponentInChildren<Text>();
                    if (t1 != null) t1.text = $"{s1.soldierName} 1 (Left)";
                    TextMeshProUGUI t1TMP = soldier1Btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (t1TMP != null) t1TMP.text = $"{s1.soldierName} 1 (Left)";

                    soldier1Btn.onClick.RemoveAllListeners();
                    soldier1Btn.onClick.AddListener(() => {
                        chosenSoldier = s1;
                    });
                }

                if (soldier2Btn != null && p1Castle.soldiers.Count >= 2)
                {
                    Soldier s2 = p1Castle.soldiers[1];
                    Text t2 = soldier2Btn.GetComponentInChildren<Text>();
                    if (t2 != null) t2.text = $"{s2.soldierName} 2 (Right)";
                    TextMeshProUGUI t2TMP = soldier2Btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (t2TMP != null) t2TMP.text = $"{s2.soldierName} 2 (Right)";

                    soldier2Btn.onClick.RemoveAllListeners();
                    soldier2Btn.onClick.AddListener(() => {
                        chosenSoldier = s2;
                    });
                }
            }

            // Wait until user selects a soldier via bottom-center buttons (or instantly if clicked while driving!)
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

            // Darken unselected soldier & keep active chosen soldier bright!
            if (p1Castle != null)
            {
                foreach (var s in p1Castle.soldiers)
                {
                    if (s != null)
                    {
                        s.SetSelectionVisualState(s == chosenSoldier);
                    }
                }
            }

            // Smoothly pan camera directly to the clicked active soldier
            if (CameraController.Instance != null && chosenSoldier != null)
            {
                CameraController.Instance.FocusSoldier(chosenSoldier.transform);
            }

            // Enable Trajectory Prediction & Aiming IMMEDIATELY after soldier selected!
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
            if (p1HealthMaskRect != null)
            {
                p1HealthMaskRect.anchorMax = new Vector2(fill, p1HealthMaskRect.anchorMax.y);
            }
            string msg = $"P1 Castle: {pct}%";
            if (p1CastleHealthText != null) p1CastleHealthText.text = msg;
            if (p1CastleHealthTextTMP != null) p1CastleHealthTextTMP.text = msg;
        }

        private void UpdateP2CastleHealth(float current, float max)
        {
            float fill = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
            int pct = Mathf.CeilToInt(fill * 100f);
            if (p2CastleHealthFill != null) p2CastleHealthFill.fillAmount = fill;
            if (p2HealthMaskRect != null)
            {
                p2HealthMaskRect.anchorMax = new Vector2(fill, p2HealthMaskRect.anchorMax.y);
            }
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
