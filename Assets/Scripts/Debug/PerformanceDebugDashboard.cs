using UnityEngine;
using CastleBusters.Environment;
using CastleBusters.Combat;
using CastleBusters.Units;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CastleBusters.DebugTools
{
    public class PerformanceDebugDashboard : MonoBehaviour
    {
        public static PerformanceDebugDashboard Instance { get; private set; }

        [Header("Display Controls")]
        public bool showDashboard = false;

        private float deltaTime = 0.0f;
        private float fps = 0.0f;
        private float minFps = 999f;
        private float maxFps = 0f;
        private float resetStatsTimer = 0f;

        private Rect windowRect = new Rect(20, 20, 360, 440);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeDashboard()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("PerformanceDebugDashboard");
                obj.AddComponent<PerformanceDebugDashboard>();
                DontDestroyOnLoad(obj);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Toggle Dashboard visibility (Supports both New Input System & Legacy Input Manager)
            bool togglePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                togglePressed = Keyboard.current.f3Key.wasPressedThisFrame || Keyboard.current.backquoteKey.wasPressedThisFrame;
            }
#else
            try
            {
                togglePressed = Input.GetKeyDown(KeyCode.F3) || Input.GetKeyDown(KeyCode.BackQuote);
            }
            catch (System.Exception) { }
#endif

            if (togglePressed)
            {
                showDashboard = !showDashboard;
            }

            // Calculate smoothed FPS and frame times
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            float currentFps = 1.0f / deltaTime;
            fps = Mathf.Lerp(fps, currentFps, Time.unscaledDeltaTime * 10f);

            resetStatsTimer += Time.unscaledDeltaTime;
            if (resetStatsTimer >= 1.0f)
            {
                if (currentFps < minFps) minFps = currentFps;
                if (currentFps > maxFps) maxFps = currentFps;
            }
        }

        private void OnGUI()
        {
            if (!showDashboard)
            {
                // Draw lightweight mini FPS badge at top-left when dashboard window is closed
                GUI.color = Color.black;
                GUI.Label(new Rect(11, 11, 200, 25), $"FPS: {fps:F0} ({deltaTime * 1000f:F1}ms) [Press F3]");
                GUI.color = (fps >= 55f) ? Color.green : ((fps >= 30f) ? Color.yellow : Color.red);
                GUI.Label(new Rect(10, 10, 200, 25), $"FPS: {fps:F0} ({deltaTime * 1000f:F1}ms) [Press F3]");
                GUI.color = Color.white;
                return;
            }

            GUI.skin.box.fontSize = 12;
            windowRect = GUI.Window(9999, windowRect, DrawDashboardWindow, "⚡ PERFORMANCE DEBUG DASHBOARD (F3)");
        }

        private void DrawDashboardWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 360, 25));

            GUILayout.Space(5);

            // --- 1. FPS & PERFORMANCE METRICS ---
            GUILayout.BeginVertical("box");
            float frameMs = deltaTime * 1000f;
            Color fpsColor = (fps >= 55f) ? Color.green : ((fps >= 30f) ? Color.yellow : Color.red);

            GUI.contentColor = fpsColor;
            GUILayout.Label($"<b>FPS:</b> {fps:F1}  ({frameMs:F1} ms/frame)", GUI.skin.label);
            GUI.contentColor = Color.white;

            GUILayout.Label($"Min FPS: {minFps:F0} | Max FPS: {maxFps:F0}", GUI.skin.label);
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // --- 2. FEATURE PERFORMANCE TOGGLES ---
            GUILayout.Label("<b>Feature Performance Toggles:</b>", GUI.skin.label);

            GUILayout.BeginVertical("box");

            // Facade Carving / Destruction
            FacadeGridBuilder.enableFacadeCarving = GUILayout.Toggle(FacadeGridBuilder.enableFacadeCarving, " Enable Facade Carving / Destruction");

            // Debris Particles
            FacadeGridBuilder.globalEnableDebris = GUILayout.Toggle(FacadeGridBuilder.globalEnableDebris, " Enable Debris Chip Particles");

            // Missile Smoke Trails
            MissileSmokeTrail.globalEnableSmokeTrails = GUILayout.Toggle(MissileSmokeTrail.globalEnableSmokeTrails, " Enable Missile Smoke Trails");

            // Explosion Particle VFX
            Projectile.globalEnableExplosionVFX = GUILayout.Toggle(Projectile.globalEnableExplosionVFX, " Enable Explosion Particle VFX");

            // Castle Suspension Physics
            CastleSuspensionPhysics.enableSuspensionPhysics = GUILayout.Toggle(CastleSuspensionPhysics.enableSuspensionPhysics, " Enable Castle Suspension Physics");

            GUILayout.EndVertical();

            GUILayout.Space(8);

            // --- 3. QUICK PRESET BUTTONS ---
            GUILayout.Label("<b>Quick Diagnostic Actions:</b>", GUI.skin.label);

            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUILayout.Button("🚫 DISABLE ALL FEATURES (Raw Baseline)", GUILayout.Height(28)))
            {
                SetAllFeatures(false);
            }

            GUI.backgroundColor = new Color(0.25f, 0.75f, 0.35f);
            if (GUILayout.Button("✅ ENABLE ALL FEATURES", GUILayout.Height(28)))
            {
                SetAllFeatures(true);
            }

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("🧹 Clear Smoke Trail Pool", GUILayout.Height(24)))
            {
                MissileSmokeTrail.ClearPool();
            }

            if (GUILayout.Button("🔄 Reset FPS Min/Max Stats", GUILayout.Height(24)))
            {
                minFps = 999f;
                maxFps = 0f;
                resetStatsTimer = 0f;
            }

            GUILayout.Space(5);
            GUILayout.Label("<color=#888888>Press [F3] or [~] to toggle this window</color>", GUI.skin.label);
        }

        private void SetAllFeatures(bool enabledState)
        {
            FacadeGridBuilder.enableFacadeCarving = enabledState;
            FacadeGridBuilder.globalEnableDebris = enabledState;
            MissileSmokeTrail.globalEnableSmokeTrails = enabledState;
            Projectile.globalEnableExplosionVFX = enabledState;
            CastleSuspensionPhysics.enableSuspensionPhysics = enabledState;
        }
    }
}
