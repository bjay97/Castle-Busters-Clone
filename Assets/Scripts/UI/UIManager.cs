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
        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        [Header("Turn & Action HUD (Supports Legacy Text or TextMeshPro)")]
        public Text turnText;
        public TextMeshProUGUI turnTextTMP;
        
        public Text actionCounterText;
        public TextMeshProUGUI actionCounterTextTMP;

        [Header("Round & Timer HUD (Supports Legacy Text, TextMeshPro, Image Fill or Slider)")]
        public GameObject roundAndTimerPanel;
        public GameObject timerProgressBarContainer;
        public Text roundText;
        public TextMeshProUGUI roundTextTMP;
        public Text timerText;
        public TextMeshProUGUI timerTextTMP;
        public Image timerProgressBarFill;
        public Slider timerProgressSlider;
        public RectTransform timerMaskRect;

        [Header("Castle Health Bars (Supports Legacy Text or TextMeshPro)")]
        public Image p1CastleHealthFill;
        public RectTransform p1HealthMaskRect; // Optional RectMask2D / RectTransform container for 9-Sliced Health Bar
        public Image p1DamageCatchUpFill; // White / Light Red Damage Catch-Up Fill Image
        public RectTransform p1DamageCatchUpMaskRect; // White / Light Red Damage Catch-Up Mask Rect
        public Text p1CastleHealthText;
        public TextMeshProUGUI p1CastleHealthTextTMP;

        public Image p2CastleHealthFill;
        public RectTransform p2HealthMaskRect; // Optional RectMask2D / RectTransform container for 9-Sliced Health Bar
        public Image p2DamageCatchUpFill; // White / Light Red Damage Catch-Up Fill Image
        public RectTransform p2DamageCatchUpMaskRect; // White / Light Red Damage Catch-Up Mask Rect
        public Text p2CastleHealthText;
        public TextMeshProUGUI p2CastleHealthTextTMP;

        [Header("Damage Trail Animation Config")]
        public float damageTrailPauseDuration = 0.25f; // Seconds before white damage trail begins shrinking
        public float damageTrailTweenDuration = 0.45f; // Duration of smooth shrink animation

        [Header("Castle Movement Fuel Gauge")]
        public Image fuelBarFill;
        public Text fuelText;
        public TextMeshProUGUI fuelTextTMP;

        [Header("Aiming Power & Angle HUD Panels")]
        public GameObject powerPanel; // Panel containing power text / percentage fill bar
        public GameObject anglePanel; // Panel containing trajectory angle text
        public Text powerText;
        public TextMeshProUGUI powerTextTMP;
        public Image powerFillImage; // Optional power percentage fill bar Image
        public Text angleText;
        public TextMeshProUGUI angleTextTMP;

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
        public Image soldier1HeadIcon; // Optional UI Image for Soldier 1 Head Icon
        public Button soldier2Btn;
        public Image soldier2HeadIcon; // Optional UI Image for Soldier 2 Head Icon

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
                TurnManager.Instance.OnRoundChanged += UpdateRoundUI;
                TurnManager.Instance.OnTurnTimerUpdated += UpdateTimerUI;

                // Explicitly refresh turn, action, round, and timer HUD on Start
                UpdateTurnUI(TurnManager.Instance.activePlayer);
                UpdateActionUI(TurnManager.Instance.actionsTakenThisTurn);
                UpdateRoundUI(TurnManager.Instance.currentRound, TurnManager.Instance.maxRounds);
                UpdateTimerUI(TurnManager.Instance.currentTurnTimeRemaining, TurnManager.Instance.turnDuration);
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
            EnsureRoundAndTimerUI();
        }

        private void EnsureRoundAndTimerUI()
        {
            if (roundText != null || roundTextTMP != null || timerText != null || timerTextTMP != null || timerProgressBarFill != null || timerProgressSlider != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject hudObj = new GameObject("RoundAndTimerHUD");
            hudObj.transform.SetParent(canvas.transform, false);

            roundAndTimerPanel = hudObj;

            RectTransform rect = hudObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f); // Top Center
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -15f);
            rect.sizeDelta = new Vector2(360f, 45f);

            // Round Text
            GameObject roundObj = new GameObject("RoundText");
            roundObj.transform.SetParent(hudObj.transform, false);
            RectTransform roundRect = roundObj.AddComponent<RectTransform>();
            roundRect.anchorMin = new Vector2(0f, 0f);
            roundRect.anchorMax = new Vector2(0.48f, 1f);
            roundRect.pivot = new Vector2(0.5f, 0.5f);
            roundRect.anchoredPosition = Vector2.zero;

            roundText = roundObj.AddComponent<Text>();
            roundText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (roundText.font == null) roundText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            roundText.fontSize = 20;
            roundText.fontStyle = FontStyle.Bold;
            roundText.color = Color.white;
            roundText.alignment = TextAnchor.MiddleCenter;
            roundText.raycastTarget = false;

            Outline rOutline = roundObj.AddComponent<Outline>();
            rOutline.effectColor = Color.black;
            rOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Timer Text
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(hudObj.transform, false);
            RectTransform timerRect = timerObj.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.52f, 0f);
            timerRect.anchorMax = new Vector2(1f, 1f);
            timerRect.pivot = new Vector2(0.5f, 0.5f);
            timerRect.anchoredPosition = Vector2.zero;

            timerText = timerObj.AddComponent<Text>();
            timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (timerText.font == null) timerText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            timerText.fontSize = 20;
            timerText.fontStyle = FontStyle.Bold;
            timerText.color = new Color(1.0f, 0.9f, 0.2f);
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.raycastTarget = false;

            Outline tOutline = timerObj.AddComponent<Outline>();
            tOutline.effectColor = Color.black;
            tOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void UpdateRoundUI(int current, int max)
        {
            EnsureRoundAndTimerUI();
            string msg = $"Round {current}/{max}";
            if (roundText != null) roundText.text = msg;
            if (roundTextTMP != null) roundTextTMP.text = msg;
        }

        private void UpdateTimerUI(float remaining, float total)
        {
            EnsureRoundAndTimerUI();
            int sec = Mathf.CeilToInt(remaining);
            string msg = $"{sec}s";

            Color textColor = (remaining <= 5f) ? new Color(1.0f, 0.25f, 0.25f) : new Color(1.0f, 0.9f, 0.2f);

            if (timerText != null)
            {
                timerText.text = msg;
                timerText.color = textColor;
            }
            if (timerTextTMP != null)
            {
                timerTextTMP.text = msg;
                timerTextTMP.color = textColor;
            }

            // --- Update Round Timer Progress Bar ---
            float fillFraction = (total > 0f) ? Mathf.Clamp01(remaining / total) : 0f;

            if (timerProgressBarFill != null)
            {
                if (timerProgressBarFill.type == Image.Type.Filled)
                {
                    timerProgressBarFill.fillAmount = fillFraction;
                }
                else
                {
                    // If image is not set to Filled type, adjust transform scale or anchor
                    Vector3 scale = timerProgressBarFill.transform.localScale;
                    scale.x = fillFraction;
                    timerProgressBarFill.transform.localScale = scale;
                }
            }

            if (timerProgressSlider != null)
            {
                timerProgressSlider.value = fillFraction;
            }

            if (timerMaskRect != null)
            {
                Vector2 anchorMax = timerMaskRect.anchorMax;
                anchorMax.x = fillFraction;
                timerMaskRect.anchorMax = anchorMax;
            }
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

        private void SetTimerUIVisibility(bool visible)
        {
            // Keep Round panel and Round text visible at all times
            if (roundAndTimerPanel != null) roundAndTimerPanel.SetActive(true);
            if (roundText != null) roundText.gameObject.SetActive(true);
            if (roundTextTMP != null) roundTextTMP.gameObject.SetActive(true);

            // Hide/Show ONLY Timer elements during enemy turn
            if (timerProgressBarContainer != null) timerProgressBarContainer.SetActive(visible);

            if (timerProgressBarFill != null)
            {
                timerProgressBarFill.gameObject.SetActive(visible);
                if (timerProgressBarFill.transform.parent != null && 
                    timerProgressBarFill.transform.parent.gameObject != roundAndTimerPanel && 
                    timerProgressBarFill.transform.parent.GetComponent<Canvas>() == null)
                {
                    timerProgressBarFill.transform.parent.gameObject.SetActive(visible);
                }
            }

            if (timerProgressSlider != null)
            {
                timerProgressSlider.gameObject.SetActive(visible);
                if (timerProgressSlider.transform.parent != null && 
                    timerProgressSlider.transform.parent.gameObject != roundAndTimerPanel && 
                    timerProgressSlider.transform.parent.GetComponent<Canvas>() == null)
                {
                    timerProgressSlider.transform.parent.gameObject.SetActive(visible);
                }
            }

            if (timerMaskRect != null)
            {
                timerMaskRect.gameObject.SetActive(visible);
                if (timerMaskRect.transform.parent != null && 
                    timerMaskRect.transform.parent.gameObject != roundAndTimerPanel && 
                    timerMaskRect.transform.parent.GetComponent<Canvas>() == null)
                {
                    timerMaskRect.transform.parent.gameObject.SetActive(visible);
                }
            }

            if (timerText != null) timerText.gameObject.SetActive(visible);
            if (timerTextTMP != null) timerTextTMP.gameObject.SetActive(visible);
        }

        private void UpdateTurnUI(PlayerSide side)
        {
            if (turnText != null) turnText.gameObject.SetActive(false);
            if (turnTextTMP != null) turnTextTMP.gameObject.SetActive(false);

            if (turnSequenceCoroutine != null) StopCoroutine(turnSequenceCoroutine);

            SetTimerUIVisibility(side == PlayerSide.Player1);

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

            // Auto-select first available soldier at start of selection phase
            if (p1Castle != null && p1Castle.soldiers.Count > 0)
            {
                foreach (var s in p1Castle.soldiers)
                {
                    if (s != null && !s.IsDead && !s.hasFiredThisTurn)
                    {
                        chosenSoldier = s;
                        break;
                    }
                }
                if (chosenSoldier == null) chosenSoldier = p1Castle.soldiers[0];
            }

            System.Action<Soldier, bool> applySoldierSelection = (s, focusCamera) =>
            {
                if (s == null) return;
                chosenSoldier = s;

                // Highlight active soldier in world space & dim unselected
                if (p1Castle != null)
                {
                    foreach (var sol in p1Castle.soldiers)
                    {
                        if (sol != null) sol.SetSelectionVisualState(sol == chosenSoldier);
                    }
                }

                // Update UI button highlight states (highlight active button, dim unselected)
                UpdateSoldierButtonHighlights(chosenSoldier, p1Castle);

                // Smoothly focus camera on chosen soldier if button was clicked
                if (focusCamera && CameraController.Instance != null)
                {
                    CameraController.Instance.FocusSoldier(chosenSoldier.transform);
                }

                // Enable aiming on launcher for chosen soldier
                if (launcher != null)
                {
                    launcher.EnableAimingForSoldier(chosenSoldier);
                }
            };

            if (p1Castle != null && p1Castle.soldiers.Count > 0)
            {
                // Ensure soldiers are sorted strictly Left-to-Right by X position
                p1Castle.soldiers.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

                if (soldier1Btn != null && p1Castle.soldiers.Count >= 1)
                {
                    Soldier s1 = p1Castle.soldiers[0];
                    Text t1 = soldier1Btn.GetComponentInChildren<Text>();
                    if (t1 != null) t1.text = $"{s1.soldierName} 1 (Left)";
                    TextMeshProUGUI t1TMP = soldier1Btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (t1TMP != null) t1TMP.text = $"{s1.soldierName} 1 (Left)";

                    bool is1Available = (s1 != null && !s1.IsDead && !s1.hasFiredThisTurn);
                    soldier1Btn.interactable = is1Available;

                    soldier1Btn.onClick.RemoveAllListeners();
                    if (is1Available)
                    {
                        soldier1Btn.onClick.AddListener(() => applySoldierSelection(s1, true));
                    }
                }

                if (soldier2Btn != null && p1Castle.soldiers.Count >= 2)
                {
                    Soldier s2 = p1Castle.soldiers[1];
                    Text t2 = soldier2Btn.GetComponentInChildren<Text>();
                    if (t2 != null) t2.text = $"{s2.soldierName} 2 (Right)";
                    TextMeshProUGUI t2TMP = soldier2Btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (t2TMP != null) t2TMP.text = $"{s2.soldierName} 2 (Right)";

                    bool is2Available = (s2 != null && !s2.IsDead && !s2.hasFiredThisTurn);
                    soldier2Btn.interactable = is2Available;

                    soldier2Btn.onClick.RemoveAllListeners();
                    if (is2Available)
                    {
                        soldier2Btn.onClick.AddListener(() => applySoldierSelection(s2, true));
                    }
                }
            }

            // Apply initial selection visuals WITHOUT zooming camera in (keep wide 10.23 SoldierSelection view at turn start!)
            if (chosenSoldier != null)
            {
                applySoldierSelection(chosenSoldier, false);
            }

            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetMode(CameraMode.SoldierSelection);
            }

            int startActionCount = (TurnManager.Instance != null) ? TurnManager.Instance.actionsTakenThisTurn : 0;

            // Keep panels active while selecting/switching soldiers; dynamically hide during aiming and restore on cancel!
            while (true)
            {
                // Exit selection phase ONLY when a missile is actually FIRED!
                if (TurnManager.Instance != null && TurnManager.Instance.actionsTakenThisTurn > startActionCount)
                {
                    break;
                }

                bool isDragging = (launcher != null && launcher.IsDragging);

                // Dynamically hide panels while dragging slingshot, restore them when not dragging
                if (soldierSelectPanel != null)
                {
                    bool showSelect = !isDragging;
                    if (soldierSelectPanel.activeSelf != showSelect) soldierSelectPanel.SetActive(showSelect);
                }

                if (movementPanel != null)
                {
                    bool showMove = !isDragging && (p1Movement != null && p1Movement.currentFuel > 0f);
                    if (movementPanel.activeSelf != showMove) movementPanel.SetActive(showMove);
                }

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
                    if (p1Vis != null && !isDragging) p1Vis.SetFacadeVisibility(false);
                }

                yield return null;
            }

            if (movementPanel != null) movementPanel.SetActive(false);
            if (soldierSelectPanel != null) soldierSelectPanel.SetActive(false);
            if (p1Movement != null) p1Movement.ReleaseMove();
        }

        private void UpdateSoldierButtonHighlights(Soldier activeSoldier, Castle p1Castle)
        {
            if (p1Castle == null || p1Castle.soldiers == null || p1Castle.soldiers.Count == 0) return;

            Button[] buttons = new Button[] { soldier1Btn, soldier2Btn };
            for (int i = 0; i < buttons.Length && i < p1Castle.soldiers.Count; i++)
            {
                Button btn = buttons[i];
                if (btn == null) continue;

                Soldier soldier = p1Castle.soldiers[i];
                bool isAvailable = (soldier != null && !soldier.IsDead && !soldier.hasFiredThisTurn);
                bool isSelected = isAvailable && (soldier == activeSoldier);

                btn.interactable = isAvailable;

                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    if (!isAvailable)
                    {
                        img.color = new Color(0.35f, 0.35f, 0.35f, 0.4f);
                    }
                    else if (isSelected)
                    {
                        img.color = Color.white;
                    }
                    else
                    {
                        img.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);
                    }
                }
                btn.transform.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;
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

            if (actionCounterText != null) actionCounterText.gameObject.SetActive(false);
            if (actionCounterTextTMP != null) actionCounterTextTMP.gameObject.SetActive(false);
            if (turnText != null) turnText.gameObject.SetActive(false);
            if (turnTextTMP != null) turnTextTMP.gameObject.SetActive(false);
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
            if (actionCounterText != null) actionCounterText.gameObject.SetActive(false);
            if (actionCounterTextTMP != null) actionCounterTextTMP.gameObject.SetActive(false);
        }

        private float p1MaskFullWidth = 0f;
        private float p2MaskFullWidth = 0f;
        private float p1CatchUpMaskFullWidth = 0f;
        private float p2CatchUpMaskFullWidth = 0f;

        private void InitializeMaskWidths()
        {
            if (p1HealthMaskRect != null && p1MaskFullWidth <= 0f)
            {
                p1MaskFullWidth = p1HealthMaskRect.rect.width;
            }
            if (p1DamageCatchUpMaskRect != null && p1CatchUpMaskFullWidth <= 0f)
            {
                p1CatchUpMaskFullWidth = p1DamageCatchUpMaskRect.rect.width;
            }
            if (p2HealthMaskRect != null && p2MaskFullWidth <= 0f)
            {
                p2MaskFullWidth = p2HealthMaskRect.rect.width;
            }
            if (p2DamageCatchUpMaskRect != null && p2CatchUpMaskFullWidth <= 0f)
            {
                p2CatchUpMaskFullWidth = p2DamageCatchUpMaskRect.rect.width;
            }
        }

        private float p1CurrentCatchUpFill = 1.0f;
        private float p2CurrentCatchUpFill = 1.0f;
        private bool p1HealthInitialized = false;
        private bool p2HealthInitialized = false;

        private Coroutine p1DamageTrailCoroutine;
        private Coroutine p2DamageTrailCoroutine;

        private void SetP1CatchUpFill(float value)
        {
            p1CurrentCatchUpFill = value;
            if (p1DamageCatchUpFill != null) p1DamageCatchUpFill.fillAmount = value;
            if (p1DamageCatchUpMaskRect != null)
            {
                InitializeMaskWidths();
                if (p1CatchUpMaskFullWidth > 0f)
                {
                    p1DamageCatchUpMaskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, p1CatchUpMaskFullWidth * value);
                }
            }
        }

        private void SetP2CatchUpFill(float value)
        {
            p2CurrentCatchUpFill = value;
            if (p2DamageCatchUpFill != null) p2DamageCatchUpFill.fillAmount = value;
            if (p2DamageCatchUpMaskRect != null)
            {
                InitializeMaskWidths();
                if (p2CatchUpMaskFullWidth > 0f)
                {
                    p2DamageCatchUpMaskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, p2CatchUpMaskFullWidth * value);
                }
            }
        }

        private void UpdateP1CastleHealth(float current, float max)
        {
            float fill = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
            int pct = Mathf.CeilToInt(fill * 100f);

            // Update main health bar fill immediately
            if (p1CastleHealthFill != null) p1CastleHealthFill.fillAmount = fill;
            if (p1HealthMaskRect != null)
            {
                InitializeMaskWidths();
                if (p1MaskFullWidth > 0f)
                {
                    p1HealthMaskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, p1MaskFullWidth * fill);
                }
            }

            // Animate white damage catch-up trail
            if (!p1HealthInitialized)
            {
                p1HealthInitialized = true;
                SetP1CatchUpFill(fill);
            }
            else if (p1DamageCatchUpFill != null || p1DamageCatchUpMaskRect != null)
            {
                if (p1DamageTrailCoroutine != null) StopCoroutine(p1DamageTrailCoroutine);
                p1DamageTrailCoroutine = StartCoroutine(AnimateP1DamageTrailRoutine(fill));
            }

            string msg = $"{pct}%";
            if (p1CastleHealthText != null) p1CastleHealthText.text = msg;
            if (p1CastleHealthTextTMP != null) p1CastleHealthTextTMP.text = msg;
        }

        private System.Collections.IEnumerator AnimateP1DamageTrailRoutine(float targetFill)
        {
            if (targetFill >= p1CurrentCatchUpFill)
            {
                SetP1CatchUpFill(targetFill);
                yield break;
            }

            float startFill = p1CurrentCatchUpFill;
            yield return new WaitForSeconds(damageTrailPauseDuration);

            float elapsed = 0f;
            while (elapsed < damageTrailTweenDuration)
            {
                elapsed += Time.deltaTime;
                float currentFill = Mathf.Lerp(startFill, targetFill, elapsed / damageTrailTweenDuration);
                SetP1CatchUpFill(currentFill);
                yield return null;
            }

            SetP1CatchUpFill(targetFill);
        }

        private void UpdateP2CastleHealth(float current, float max)
        {
            float fill = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
            int pct = Mathf.CeilToInt(fill * 100f);

            // Update main health bar fill immediately
            if (p2CastleHealthFill != null) p2CastleHealthFill.fillAmount = fill;
            if (p2HealthMaskRect != null)
            {
                InitializeMaskWidths();
                if (p2MaskFullWidth > 0f)
                {
                    p2HealthMaskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, p2MaskFullWidth * fill);
                }
            }

            // Animate white damage catch-up trail
            if (!p2HealthInitialized)
            {
                p2HealthInitialized = true;
                SetP2CatchUpFill(fill);
            }
            else if (p2DamageCatchUpFill != null || p2DamageCatchUpMaskRect != null)
            {
                if (p2DamageTrailCoroutine != null) StopCoroutine(p2DamageTrailCoroutine);
                p2DamageTrailCoroutine = StartCoroutine(AnimateP2DamageTrailRoutine(fill));
            }

            string msg = $"{pct}%";
            if (p2CastleHealthText != null) p2CastleHealthText.text = msg;
            if (p2CastleHealthTextTMP != null) p2CastleHealthTextTMP.text = msg;
        }

        private System.Collections.IEnumerator AnimateP2DamageTrailRoutine(float targetFill)
        {
            if (targetFill >= p2CurrentCatchUpFill)
            {
                SetP2CatchUpFill(targetFill);
                yield break;
            }

            float startFill = p2CurrentCatchUpFill;
            yield return new WaitForSeconds(damageTrailPauseDuration);

            float elapsed = 0f;
            while (elapsed < damageTrailTweenDuration)
            {
                elapsed += Time.deltaTime;
                float currentFill = Mathf.Lerp(startFill, targetFill, elapsed / damageTrailTweenDuration);
                SetP2CatchUpFill(currentFill);
                yield return null;
            }

            SetP2CatchUpFill(targetFill);
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

        public void UpdateAimingStatsUI(bool isAiming, float powerPercent, float angleDegrees)
        {
            if (powerPanel != null) powerPanel.SetActive(isAiming);
            if (anglePanel != null) anglePanel.SetActive(isAiming);

            if (!isAiming) return;

            int roundedPower = Mathf.Clamp(Mathf.RoundToInt(powerPercent), 0, 100);
            int roundedAngle = Mathf.RoundToInt(angleDegrees);

            string pwrString = $"{roundedPower}%";
            if (powerText != null) powerText.text = pwrString;
            if (powerTextTMP != null) powerTextTMP.text = pwrString;

            if (powerFillImage != null) powerFillImage.fillAmount = powerPercent / 100f;

            string angString = $"{roundedAngle}°";
            if (angleText != null) angleText.text = angString;
            if (angleTextTMP != null) angleTextTMP.text = angString;
        }
    }
}
