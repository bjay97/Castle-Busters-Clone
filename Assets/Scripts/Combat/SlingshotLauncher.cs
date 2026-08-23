using UnityEngine;
using CastleBusters.Core;
using CastleBusters.Units;
using CastleBusters.Environment;

namespace CastleBusters.Combat
{
    public class SlingshotLauncher : MonoBehaviour
    {
        [Header("Slingshot Config")]
        public float maxDragDistance = 3f;
        public float launchForceMultiplier = 12f;
        public TrajectoryPredictor trajectoryPredictor;

        [Header("Active Aiming State")]
        public Soldier activeSoldier;
        public bool isAimingAllowed = false;
        public bool IsDragging => isDragging;

        [Header("Aim Cancel Config & Customization")]
        public Vector3 cancelPositionOffset = new Vector3(0f, 2.3f, 0f); // Position of cancel button above soldier
        public float cancelZoneRadius = 1.3f; // Distance from cancel button center to trigger cancel state
        public GameObject customCancelUIPrefab; // Optional custom UI prefab (instantiated in world space)
        public RectTransform sceneCancelUIElement; // Optional Canvas UI element already in your UI hierarchy
        public Sprite customCancelSprite; // Optional custom sprite/icon for the cancel button badge

        [Header("Aim Cancel Visual Styling")]
        [Range(0f, 1f)] public float cancelNormalAlpha = 0.45f; // Semi-transparent when aiming
        [Range(0f, 1f)] public float cancelHoverAlpha = 1.0f; // Fully opaque when hovering cancel zone
        public Color cancelNormalColor = new Color(0.72f, 0.11f, 0.11f, 0.88f);
        public Color cancelHoverColor = new Color(1.0f, 0.08f, 0.18f, 1.0f);
        public string cancelNormalText = "✕ CANCEL";
        public string cancelHoverText = "RELEASE TO CANCEL";
        public float cancelNormalScale = 1.0f;
        public float cancelHoverScale = 1.3f;
        [Header("Aim Drag Handle UI Config")]
        public bool enablePointerDragHandle = true; // Enables cursor drag handle button while aiming
        public GameObject customPointerHandlePrefab; // Optional custom prefab for mouse cursor handle (instantiated in world space)
        public RectTransform scenePointerHandleUIElement; // Optional Canvas UI element already in your UI hierarchy
        public Sprite customPointerHandleSprite; // Optional custom sprite/icon for mouse drag handle
        public Color pointerHandleColor = new Color(0.38f, 0.38f, 0.42f, 0.88f); // Sleek grey button color
        public float pointerHandleScale = 1.0f; // Scale multiplier for drag handle button
        public string pointerSortingLayerName = "Default"; // Sorting layer name
        public int pointerSortingOrder = 500; // High sorting order (500) so pointer renders on top of castle interior and facade

        public event System.Action<bool, bool> OnAimCancelStateChanged; // Event fired when aiming state changes (isAiming, isHoveringCancel)
        public event System.Action<bool, float, float> OnAimingStatsChanged; // Event fired when aiming stats update (isAiming, powerPercent, angleDegrees)

        private GameObject cancelUIInstance;
        private CanvasGroup cancelCanvasGroup;
        private UnityEngine.UI.Image cancelBgImage;
        private UnityEngine.UI.Text cancelText;
        private bool isHoveringCancelZone = false;
        private Vector3 initialCancelUIScale = Vector3.one;

        private GameObject pointerHandleInstance;
        private Vector3 initialPointerHandleScale = Vector3.one;

        private bool isDragging = false;
        private Vector2 dragStartPosition;
        private Vector2 currentDragPosition;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (trajectoryPredictor == null) trajectoryPredictor = GetComponent<TrajectoryPredictor>();
            if (trajectoryPredictor == null) trajectoryPredictor = gameObject.AddComponent<TrajectoryPredictor>();

            FindAndSelectPlayer1Soldier();

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += AutoSelectSoldierForTurn;
            }
        }

        private void FindAndSelectPlayer1Soldier()
        {
            if (activeSoldier != null && !activeSoldier.IsDead && !activeSoldier.hasFiredThisTurn) return;

            Soldier[] soldiers = FindObjectsByType<Soldier>(FindObjectsSortMode.None);
            foreach (var s in soldiers)
            {
                if (s != null && s.ownerSide == PlayerSide.Player1 && !s.IsDead && !s.hasFiredThisTurn)
                {
                    activeSoldier = s;
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= AutoSelectSoldierForTurn;
            }
        }

        public void EnableAimingForSoldier(Soldier soldier)
        {
            if (soldier != null && !soldier.IsDead && !soldier.hasFiredThisTurn)
            {
                activeSoldier = soldier;
                isAimingAllowed = true;
            }
        }

        public void SelectSoldier(Soldier soldier)
        {
            EnableAimingForSoldier(soldier);
        }

        private void AutoSelectSoldierForTurn(PlayerSide activeSide)
        {
            activeSoldier = null;
            isAimingAllowed = false;

            Castle activeCastle = (activeSide == PlayerSide.Player1) 
                ? GameManager.Instance?.player1Castle 
                : GameManager.Instance?.player2Castle;

            if (activeCastle != null)
            {
                foreach (var soldier in activeCastle.soldiers)
                {
                    if (soldier != null && !soldier.IsDead && !soldier.hasFiredThisTurn)
                    {
                        activeSoldier = soldier;
                        break;
                    }
                }
            }

            // Scene search fallback for test scenes or missing Castle component
            if (activeSoldier == null)
            {
                Soldier[] soldiers = FindObjectsByType<Soldier>(FindObjectsSortMode.None);
                foreach (var s in soldiers)
                {
                    if (s != null && s.ownerSide == activeSide && !s.IsDead && !s.hasFiredThisTurn)
                    {
                        activeSoldier = s;
                        break;
                    }
                }
            }
        }

        private Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            }
#endif
            return Input.mousePosition;
        }

        private bool IsPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame;
            }
#endif
            return Input.GetMouseButtonDown(0);
        }

        private bool IsPointerIsPressed()
        {
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.press.isPressed;
            }
#endif
            return Input.GetMouseButton(0);
        }

        private bool IsPointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                return UnityEngine.InputSystem.Pointer.current.press.wasReleasedThisFrame;
            }
#endif
            return Input.GetMouseButtonUp(0);
        }

        private bool IsPointerOverUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return true;
            if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return true;
            return false;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            if (!isAimingAllowed) return;
            if (activeSoldier == null || activeSoldier.IsDead || activeSoldier.hasFiredThisTurn) return;

            if (mainCamera == null)
            {
                mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }
            if (mainCamera == null) return;

            Vector2 pointerScreenPos = GetPointerScreenPosition();
            Vector3 worldPos3D = mainCamera.ScreenToWorldPoint(new Vector3(pointerScreenPos.x, pointerScreenPos.y, -mainCamera.transform.position.z));
            Vector2 mouseWorldPos = (Vector2)worldPos3D;

            if (!isDragging && IsPointerPressedThisFrame())
            {
                // Ignore clicks over UI elements (e.g. Soldier Select Buttons, Movement Buttons)
                if (IsPointerOverUI())
                {
                    return;
                }

                float distToSoldier = Vector2.Distance(mouseWorldPos, activeSoldier.transform.position);
                if (distToSoldier < 4f)
                {
                    isDragging = true;
                    dragStartPosition = activeSoldier.transform.position;

                    // Hide Player 1's facade while actively aiming so player sees soldier and trajectory clearly
                    if (GameManager.Instance != null && GameManager.Instance.player1Castle != null)
                    {
                        CastleFacadeVisibility vis = GameManager.Instance.player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
                        if (vis != null) vis.SetAimingHideState(true);
                    }
                }
            }

            if (isDragging)
            {
                currentDragPosition = mouseWorldPos;
                Vector2 dragVector = dragStartPosition - currentDragPosition;

                // Check distance to Cancel Zone button hovering above soldier
                Vector3 cancelWorldPos = (Vector3)dragStartPosition + cancelPositionOffset;
                float distToCancel = Vector2.Distance(mouseWorldPos, cancelWorldPos);
                isHoveringCancelZone = (distToCancel <= cancelZoneRadius);

                UpdateCancelUIState(true, isHoveringCancelZone, dragStartPosition);
                UpdatePointerHandleUIState(true, mouseWorldPos);

                if (dragVector.magnitude > maxDragDistance)
                {
                    dragVector = dragVector.normalized * maxDragDistance;
                }

                float projSpeedMult = 1.0f;
                if (activeSoldier != null && activeSoldier.projectilePrefab != null)
                {
                    Projectile projComp = activeSoldier.projectilePrefab.GetComponent<Projectile>();
                    if (projComp != null)
                    {
                        projSpeedMult = projComp.speedMultiplier;
                    }
                }

                Vector2 launchVelocity = dragVector * launchForceMultiplier * projSpeedMult;

                // Dynamically update camera zoom & X-offset based on pull distance!
                float dragRatio = (maxDragDistance > 0f) ? (dragVector.magnitude / maxDragDistance) : 0f;
                if (CameraController.Instance != null)
                {
                    CameraController.Instance.UpdateDynamicAiming(dragRatio);
                }

                // Calculate real-time Power Percentage and Trajectory Angle
                float powerPercent = Mathf.Clamp01(dragRatio) * 100f;
                float launchAngle = Mathf.Atan2(launchVelocity.y, launchVelocity.x) * Mathf.Rad2Deg;

                OnAimingStatsChanged?.Invoke(true, powerPercent, launchAngle);
                if (CastleBusters.UI.UIManager.Instance != null)
                {
                    CastleBusters.UI.UIManager.Instance.UpdateAimingStatsUI(true, powerPercent, launchAngle);
                }

                // Show trajectory ONLY if NOT hovering over the Cancel Zone!
                if (!isHoveringCancelZone && trajectoryPredictor != null && dragVector.magnitude > 0.1f)
                {
                    trajectoryPredictor.ShowTrajectory(activeSoldier.transform.position, launchVelocity);
                }
                else if (isHoveringCancelZone && trajectoryPredictor != null)
                {
                    trajectoryPredictor.HideTrajectory();
                }

                if (IsPointerReleasedThisFrame())
                {
                    isDragging = false;
                    UpdateCancelUIState(false, false, dragStartPosition);
                    UpdatePointerHandleUIState(false, Vector2.zero);
                    if (trajectoryPredictor != null) trajectoryPredictor.HideTrajectory();

                    OnAimingStatsChanged?.Invoke(false, 0f, 0f);
                    if (CastleBusters.UI.UIManager.Instance != null)
                    {
                        CastleBusters.UI.UIManager.Instance.UpdateAimingStatsUI(false, 0f, 0f);
                    }

                    // Restore 100% full opaque facade when aim release occurs
                    if (GameManager.Instance != null && GameManager.Instance.player1Castle != null)
                    {
                        CastleFacadeVisibility vis = GameManager.Instance.player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
                        if (vis != null) vis.SetAimingHideState(false);
                    }

                    // If released over Cancel Zone -> Cancel aim completely (DO NOT FIRE!)
                    if (isHoveringCancelZone)
                    {
                        isHoveringCancelZone = false;
                        if (CameraController.Instance != null)
                        {
                            CameraController.Instance.SetMode(CameraMode.SoldierSelection);
                        }
                        return;
                    }

                    if (dragVector.magnitude > 0.3f)
                    {
                        FireProjectile(launchVelocity);
                    }
                }
            }
            else
            {
                UpdateCancelUIState(false, false, Vector2.zero);
                UpdatePointerHandleUIState(false, Vector2.zero);
                OnAimingStatsChanged?.Invoke(false, 0f, 0f);
                if (CastleBusters.UI.UIManager.Instance != null)
                {
                    CastleBusters.UI.UIManager.Instance.UpdateAimingStatsUI(false, 0f, 0f);
                }
            }
        }

        private void FireProjectile(Vector2 launchVelocity)
        {
            if (activeSoldier == null || activeSoldier.projectilePrefab == null) return;

            Soldier firingSoldier = activeSoldier;
            firingSoldier.hasFiredThisTurn = true;
            activeSoldier = null;
            isAimingAllowed = false;

            if (firingSoldier.fireMode == FireMode.RapidSalvo)
            {
                StartCoroutine(FireSalvoRoutine(firingSoldier, launchVelocity));
            }
            else
            {
                SpawnSingleMissile(firingSoldier, launchVelocity);
                if (TurnManager.Instance != null) TurnManager.Instance.RegisterActionFired();
            }
        }

        private void SpawnSingleMissile(Soldier soldier, Vector2 launchVelocity, bool trackCamera = true)
        {
            Vector3 spawnPos = soldier.transform.position + (Vector3)(launchVelocity.normalized * 0.8f);
            GameObject projObj = Instantiate(soldier.projectilePrefab, spawnPos, Quaternion.identity);

            Projectile projComp = projObj.GetComponent<Projectile>();
            if (projComp != null)
            {
                projComp.ownerSide = soldier.ownerSide;
            }

            Collider2D projCol = projObj.GetComponent<Collider2D>();
            if (projCol != null)
            {
                Castle ownCastle = soldier.GetComponentInParent<Castle>();
                if (ownCastle == null && GameManager.Instance != null)
                {
                    ownCastle = (soldier.ownerSide == PlayerSide.Player1) ? GameManager.Instance.player1Castle : GameManager.Instance.player2Castle;
                }
                if (ownCastle != null)
                {
                    Collider2D[] ownColliders = ownCastle.GetComponentsInChildren<Collider2D>();
                    foreach (var c in ownColliders)
                    {
                        if (c != null && c != projCol) Physics2D.IgnoreCollision(projCol, c);
                    }
                }
            }

            Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = launchVelocity;
            }

            // Smoothly track lead/midway player projectile with camera in real-time
            if (trackCamera && CameraController.Instance != null)
            {
                CameraController.Instance.FollowProjectile(projObj.transform);
            }
        }

        private System.Collections.IEnumerator FireSalvoRoutine(Soldier soldier, Vector2 baseLaunchVelocity)
        {
            int count = Mathf.Clamp(soldier.salvoCount, 4, 12);
            float interval = Mathf.Clamp(soldier.salvoInterval, 0.05f, 0.2f);
            float spread = soldier.salvoSpreadAngle;

            for (int i = 0; i < count; i++)
            {
                if (soldier == null) break;

                float baseAngle = Mathf.Atan2(baseLaunchVelocity.y, baseLaunchVelocity.x) * Mathf.Rad2Deg;
                float randomAngle = baseAngle + Random.Range(-spread, spread);
                float randomSpeedMult = Random.Range(0.92f, 1.08f);
                float speed = baseLaunchVelocity.magnitude * randomSpeedMult;

                Vector2 salvoVel = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad)) * speed;

                // Lock camera onto the 1st (i == 0) or midway (i == 3) projectile to lead the salvo barrage
                bool trackThisMissile = (i == 0 || i == 3);
                SpawnSingleMissile(soldier, salvoVel, trackThisMissile);

                float currentInterval = Mathf.Max(0.03f, interval + Random.Range(-0.02f, 0.02f));
                yield return new WaitForSeconds(currentInterval);
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RegisterActionFired();
            }
        }

        public void CancelAim()
        {
            isDragging = false;
            if (trajectoryPredictor != null) trajectoryPredictor.HideTrajectory();
            UpdateCancelUIState(false, false, Vector2.zero);
            UpdatePointerHandleUIState(false, Vector2.zero);
            OnAimingStatsChanged?.Invoke(false, 0f, 0f);
            if (CastleBusters.UI.UIManager.Instance != null)
            {
                CastleBusters.UI.UIManager.Instance.UpdateAimingStatsUI(false, 0f, 0f);
            }
            if (GameManager.Instance != null && GameManager.Instance.player1Castle != null)
            {
                CastleFacadeVisibility vis = GameManager.Instance.player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
                if (vis != null) vis.SetAimingHideState(false);
            }
        }

        private void EnsureCancelUIInitialized()
        {
            if (sceneCancelUIElement != null)
            {
                cancelUIInstance = sceneCancelUIElement.gameObject;
                cancelCanvasGroup = cancelUIInstance.GetComponent<CanvasGroup>();
                if (cancelCanvasGroup == null) cancelCanvasGroup = cancelUIInstance.AddComponent<CanvasGroup>();
                cancelBgImage = cancelUIInstance.GetComponent<UnityEngine.UI.Image>();
                if (cancelBgImage == null) cancelBgImage = cancelUIInstance.GetComponentInChildren<UnityEngine.UI.Image>();
                cancelText = cancelUIInstance.GetComponent<UnityEngine.UI.Text>();
                if (cancelText == null) cancelText = cancelUIInstance.GetComponentInChildren<UnityEngine.UI.Text>();
                return;
            }

            if (cancelUIInstance != null) return;

            if (customCancelUIPrefab != null)
            {
                cancelUIInstance = Instantiate(customCancelUIPrefab);
                initialCancelUIScale = customCancelUIPrefab.transform.localScale;
                if (initialCancelUIScale == Vector3.zero) initialCancelUIScale = Vector3.one;

                cancelCanvasGroup = cancelUIInstance.GetComponent<CanvasGroup>();
                if (cancelCanvasGroup == null) cancelCanvasGroup = cancelUIInstance.AddComponent<CanvasGroup>();

                // Ensure UI RectTransform prefabs have a WorldSpace Canvas so they render at full size
                if (cancelUIInstance.GetComponent<RectTransform>() != null && cancelUIInstance.GetComponent<Canvas>() == null && cancelUIInstance.GetComponentInParent<Canvas>() == null)
                {
                    Canvas c = cancelUIInstance.AddComponent<Canvas>();
                    c.renderMode = RenderMode.WorldSpace;
                    c.sortingOrder = 200;
                    cancelUIInstance.AddComponent<UnityEngine.UI.CanvasScaler>();
                    cancelUIInstance.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }

                cancelBgImage = cancelUIInstance.GetComponent<UnityEngine.UI.Image>();
                if (cancelBgImage == null) cancelBgImage = cancelUIInstance.GetComponentInChildren<UnityEngine.UI.Image>();
                cancelText = cancelUIInstance.GetComponent<UnityEngine.UI.Text>();
                if (cancelText == null) cancelText = cancelUIInstance.GetComponentInChildren<UnityEngine.UI.Text>();
                return;
            }

            // Procedurally create a World Space Cancel Badge Canvas hovering above the soldier
            GameObject canvasObj = new GameObject("AimCancelCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200; // Render above soldiers and castle facade

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(160f, 60f);
            canvasRT.localScale = new Vector3(0.012f, 0.012f, 1f);

            // Create circular background badge
            GameObject bgObj = new GameObject("CancelBG");
            bgObj.transform.SetParent(canvasObj.transform, false);

            cancelBgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            if (customCancelSprite != null) cancelBgImage.sprite = customCancelSprite;
            cancelBgImage.color = cancelNormalColor;

            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Create Cancel text label
            GameObject textObj = new GameObject("CancelText");
            textObj.transform.SetParent(bgObj.transform, false);

            cancelText = textObj.AddComponent<UnityEngine.UI.Text>();
            cancelText.text = cancelNormalText;
            cancelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cancelText.fontSize = 26;
            cancelText.fontStyle = FontStyle.Bold;
            cancelText.alignment = TextAnchor.MiddleCenter;
            cancelText.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            initialCancelUIScale = new Vector3(0.012f, 0.012f, 1f);
            cancelCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
            cancelUIInstance = canvasObj;
            cancelUIInstance.SetActive(false);
        }

        private void UpdateCancelUIState(bool show, bool isHovering, Vector3 soldierPos)
        {
            EnsureCancelUIInitialized();

            OnAimCancelStateChanged?.Invoke(show, isHovering);

            if (cancelUIInstance == null) return;

            cancelUIInstance.SetActive(show);

            if (show)
            {
                if (sceneCancelUIElement != null && mainCamera != null)
                {
                    // Screen Space UI positioning for scene canvas elements
                    Vector3 screenPos = mainCamera.WorldToScreenPoint(soldierPos + cancelPositionOffset);
                    sceneCancelUIElement.position = screenPos;
                }
                else
                {
                    // World Space UI positioning respecting custom prefab scale
                    cancelUIInstance.transform.position = soldierPos + cancelPositionOffset;

                    float multiplier = isHovering ? cancelHoverScale : cancelNormalScale;
                    Vector3 targetScale = new Vector3(initialCancelUIScale.x * multiplier, initialCancelUIScale.y * multiplier, initialCancelUIScale.z);

                    cancelUIInstance.transform.localScale = Vector3.Lerp(cancelUIInstance.transform.localScale, targetScale, Time.deltaTime * 15f);
                }

                // Smoothly fade transparency between semi-transparent aiming state and 100% solid hover state
                if (cancelCanvasGroup != null)
                {
                    float targetAlpha = isHovering ? cancelHoverAlpha : cancelNormalAlpha;
                    cancelCanvasGroup.alpha = Mathf.Lerp(cancelCanvasGroup.alpha, targetAlpha, Time.deltaTime * 15f);
                }

                if (cancelBgImage != null)
                {
                    Color targetColor = isHovering ? cancelHoverColor : cancelNormalColor;
                    if (customCancelSprite != null && cancelBgImage.sprite != customCancelSprite)
                    {
                        cancelBgImage.sprite = customCancelSprite;
                    }
                    cancelBgImage.color = Color.Lerp(cancelBgImage.color, targetColor, Time.deltaTime * 15f);
                }

                if (cancelText != null)
                {
                    string targetText = isHovering ? cancelHoverText : cancelNormalText;
                    cancelText.text = targetText;
                    cancelText.fontSize = isHovering ? 20 : 24;
                }
            }
        }

        private void EnsurePointerHandleUIInitialized()
        {
            if (scenePointerHandleUIElement != null)
            {
                pointerHandleInstance = scenePointerHandleUIElement.gameObject;
                return;
            }

            if (pointerHandleInstance != null) return;

            if (customPointerHandlePrefab != null)
            {
                pointerHandleInstance = Instantiate(customPointerHandlePrefab);
                initialPointerHandleScale = customPointerHandlePrefab.transform.localScale;
                if (initialPointerHandleScale == Vector3.zero) initialPointerHandleScale = Vector3.one;

                if (pointerHandleInstance.GetComponent<RectTransform>() != null && pointerHandleInstance.GetComponent<Canvas>() == null && pointerHandleInstance.GetComponentInParent<Canvas>() == null)
                {
                    Canvas c = pointerHandleInstance.AddComponent<Canvas>();
                    c.renderMode = RenderMode.WorldSpace;
                    c.sortingOrder = pointerSortingOrder;
                    pointerHandleInstance.AddComponent<UnityEngine.UI.CanvasScaler>();
                    pointerHandleInstance.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                ApplyPointerSortingOrder(pointerHandleInstance);
                pointerHandleInstance.SetActive(false);
                return;
            }

            // Procedurally create a World Space Grey Cursor Handle Disc Canvas
            GameObject canvasObj = new GameObject("AimPointerHandleCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 250; // Render above everything

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(70f, 70f);
            canvasRT.localScale = new Vector3(0.012f, 0.012f, 1f);

            GameObject bgObj = new GameObject("HandleBG");
            bgObj.transform.SetParent(canvasObj.transform, false);

            UnityEngine.UI.Image img = bgObj.AddComponent<UnityEngine.UI.Image>();
            if (customPointerHandleSprite != null) img.sprite = customPointerHandleSprite;
            img.color = pointerHandleColor;

            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            initialPointerHandleScale = new Vector3(0.012f, 0.012f, 1f);
            pointerHandleInstance = canvasObj;
            ApplyPointerSortingOrder(pointerHandleInstance);
            pointerHandleInstance.SetActive(false);
        }

        private void ApplyPointerSortingOrder(GameObject obj)
        {
            if (obj == null) return;

            Canvas[] canvases = obj.GetComponentsInChildren<Canvas>(true);
            foreach (var c in canvases)
            {
                c.overrideSorting = true;
                c.sortingOrder = pointerSortingOrder;
                if (!string.IsNullOrEmpty(pointerSortingLayerName)) c.sortingLayerName = pointerSortingLayerName;
            }

            SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                sr.sortingOrder = pointerSortingOrder;
                if (!string.IsNullOrEmpty(pointerSortingLayerName)) sr.sortingLayerName = pointerSortingLayerName;
            }
        }

        private void UpdatePointerHandleUIState(bool show, Vector3 mouseWorldPos)
        {
            if (!enablePointerDragHandle)
            {
                if (pointerHandleInstance != null) pointerHandleInstance.SetActive(false);
                return;
            }

            EnsurePointerHandleUIInitialized();
            if (pointerHandleInstance == null) return;

            pointerHandleInstance.SetActive(show);

            if (show)
            {
                if (scenePointerHandleUIElement != null)
                {
                    // Screen Space UI positioning for elements inside a Canvas UI
                    scenePointerHandleUIElement.position = GetPointerScreenPosition();
                }
                else
                {
                    // World Space UI positioning in 1:1 lockstep with cursor
                    pointerHandleInstance.transform.position = mouseWorldPos;

                    Vector3 targetScale = new Vector3(initialPointerHandleScale.x * pointerHandleScale, initialPointerHandleScale.y * pointerHandleScale, initialPointerHandleScale.z);
                    pointerHandleInstance.transform.localScale = targetScale;
                }
            }
        }
    }
}
