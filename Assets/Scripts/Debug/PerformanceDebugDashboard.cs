using System.Collections.Generic;
using System.IO;
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

        // Session Graph Storage
        private const int MAX_SAMPLES = 200;
        private List<float> fpsHistory = new List<float>();
        private List<float> frameMsHistory = new List<float>();
        private float sampleTimer = 0f;
        private const float SAMPLE_INTERVAL = 0.1f; // Sample FPS every 100ms

        private Rect windowRect = new Rect(20, 20, 380, 580);
        private Texture2D graphTexture;
        private string statusMessage = "";
        private float statusTimer = 0f;

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
            float currentFps = (deltaTime > 0f) ? (1.0f / deltaTime) : 60f;
            fps = Mathf.Lerp(fps, currentFps, Time.unscaledDeltaTime * 10f);

            resetStatsTimer += Time.unscaledDeltaTime;
            if (resetStatsTimer >= 0.5f)
            {
                if (currentFps < minFps) minFps = currentFps;
                if (currentFps > maxFps) maxFps = currentFps;
            }

            // Sample graph data periodically
            sampleTimer += Time.unscaledDeltaTime;
            if (sampleTimer >= SAMPLE_INTERVAL)
            {
                sampleTimer = 0f;
                RecordSample(currentFps, deltaTime * 1000f);
            }

            if (statusTimer > 0f)
            {
                statusTimer -= Time.unscaledDeltaTime;
            }
        }

        private void RecordSample(float sampleFps, float sampleMs)
        {
            fpsHistory.Add(sampleFps);
            frameMsHistory.Add(sampleMs);

            if (fpsHistory.Count > MAX_SAMPLES)
            {
                fpsHistory.RemoveAt(0);
                frameMsHistory.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
            if (!showDashboard)
            {
                // Draw lightweight mini FPS badge at top-left when dashboard window is closed
                GUI.color = Color.black;
                GUI.Label(new Rect(11, 11, 240, 25), $"FPS: {fps:F0} ({deltaTime * 1000f:F1}ms) [F3 Graph]");
                GUI.color = (fps >= 55f) ? Color.green : ((fps >= 30f) ? Color.yellow : Color.red);
                GUI.Label(new Rect(10, 10, 240, 25), $"FPS: {fps:F0} ({deltaTime * 1000f:F1}ms) [F3 Graph]");
                GUI.color = Color.white;
                return;
            }

            GUI.skin.box.fontSize = 12;
            windowRect = GUI.Window(9999, windowRect, DrawDashboardWindow, "⚡ PERFORMANCE DASHBOARD & SESSION GRAPH (F3)");
        }

        private void DrawDashboardWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 380, 25));

            GUILayout.Space(5);

            // --- 1. FPS & PERFORMANCE METRICS ---
            GUILayout.BeginVertical("box");
            float frameMs = deltaTime * 1000f;
            Color fpsColor = (fps >= 55f) ? Color.green : ((fps >= 30f) ? Color.yellow : Color.red);

            GUI.contentColor = fpsColor;
            GUILayout.Label($"<b>FPS:</b> {fps:F1}  ({frameMs:F1} ms/frame)", GUI.skin.label);
            GUI.contentColor = Color.white;

            GUILayout.Label($"Min FPS: {minFps:F0} | Max FPS: {maxFps:F0} | Samples: {fpsHistory.Count}", GUI.skin.label);
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // --- 2. LIVE SESSION PERFORMANCE GRAPH ---
            GUILayout.Label("<b>Session FPS Trend Graph:</b> (Green=60, Yellow=30)", GUI.skin.label);
            Rect graphRect = GUILayoutUtility.GetRect(350, 110);
            DrawGraphTexture(graphRect);

            GUILayout.Space(5);

            // --- 3. FEATURE PERFORMANCE TOGGLES ---
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

            GUILayout.Space(6);

            // --- 4. GRAPH EXPORT & DIAGNOSTIC ACTIONS ---
            GUILayout.Label("<b>Graph Export & Actions:</b>", GUI.skin.label);

            GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f);
            if (GUILayout.Button("💾 Save Performance Graph PNG (Overwrites Previous)", GUILayout.Height(28)))
            {
                ExportPerformanceGraphToPNG();
            }

            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUILayout.Button("🚫 DISABLE ALL FEATURES (Raw Baseline)", GUILayout.Height(24)))
            {
                SetAllFeatures(false);
            }

            GUI.backgroundColor = new Color(0.25f, 0.75f, 0.35f);
            if (GUILayout.Button("✅ ENABLE ALL FEATURES", GUILayout.Height(24)))
            {
                SetAllFeatures(true);
            }

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("🔄 Clear Graph & Reset Session", GUILayout.Height(22)))
            {
                ResetSessionData();
            }

            if (statusTimer > 0f)
            {
                GUI.contentColor = Color.cyan;
                GUILayout.Label(statusMessage, GUI.skin.label);
                GUI.contentColor = Color.white;
            }

            GUILayout.Space(2);
            GUILayout.Label("<color=#888888>Press [F3] or [~] to toggle graph overlay</color>", GUI.skin.label);
        }

        private void DrawGraphTexture(Rect rect)
        {
            int w = (int)rect.width;
            int h = (int)rect.height;

            if (graphTexture == null || graphTexture.width != w || graphTexture.height != h)
            {
                graphTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            }

            Color32[] pixels = new Color32[w * h];
            Color32 bgColor = new Color32(18, 22, 28, 240);
            Color32 grid60Color = new Color32(40, 160, 60, 220); // 60 FPS reference line
            Color32 grid30Color = new Color32(200, 180, 40, 220); // 30 FPS reference line

            // Fill background
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;

            // Draw horizontal reference lines for 60 FPS and 30 FPS
            int y60 = Mathf.Clamp(Mathf.RoundToInt((60f / 90f) * h), 0, h - 1);
            int y30 = Mathf.Clamp(Mathf.RoundToInt((30f / 90f) * h), 0, h - 1);

            for (int x = 0; x < w; x++)
            {
                pixels[y60 * w + x] = grid60Color;
                pixels[y30 * w + x] = grid30Color;
            }

            // Draw live FPS graph curve
            if (fpsHistory.Count > 1)
            {
                Color32 graphLineColor = new Color32(0, 220, 255, 255);
                Color32 dipColor = new Color32(255, 60, 60, 255);

                float xStep = (float)w / Mathf.Max(1, MAX_SAMPLES - 1);
                int count = fpsHistory.Count;

                for (int i = 0; i < count - 1; i++)
                {
                    int x1 = Mathf.Clamp(Mathf.RoundToInt(i * xStep), 0, w - 1);
                    int x2 = Mathf.Clamp(Mathf.RoundToInt((i + 1) * xStep), 0, w - 1);

                    float f1 = Mathf.Clamp(fpsHistory[i], 0f, 90f);
                    float f2 = Mathf.Clamp(fpsHistory[i + 1], 0f, 90f);

                    int py1 = Mathf.Clamp(Mathf.RoundToInt((f1 / 90f) * h), 0, h - 1);
                    int py2 = Mathf.Clamp(Mathf.RoundToInt((f2 / 90f) * h), 0, h - 1);

                    Color32 lineCol = (f2 < 30f) ? dipColor : graphLineColor;
                    DrawLineOnPixelArray(pixels, w, h, x1, py1, x2, py2, lineCol);
                }
            }

            graphTexture.SetPixels32(pixels);
            graphTexture.Apply(false);

            GUI.DrawTexture(rect, graphTexture);
        }

        private void DrawLineOnPixelArray(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                {
                    pixels[y0 * width + x0] = color;
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        public void ExportPerformanceGraphToPNG()
        {
            int w = 800;
            int h = 450;
            Texture2D exportTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            Color32 bgColor = new Color32(15, 18, 24, 255);
            Color32 grid60Color = new Color32(40, 180, 70, 255);
            Color32 grid30Color = new Color32(220, 180, 40, 255);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;

            int y60 = Mathf.Clamp(Mathf.RoundToInt((60f / 90f) * h), 0, h - 1);
            int y30 = Mathf.Clamp(Mathf.RoundToInt((30f / 90f) * h), 0, h - 1);

            for (int x = 0; x < w; x++)
            {
                pixels[y60 * w + x] = grid60Color;
                pixels[y30 * w + x] = grid30Color;
            }

            if (fpsHistory.Count > 1)
            {
                Color32 lineCol = new Color32(0, 220, 255, 255);
                float xStep = (float)w / Mathf.Max(1, fpsHistory.Count - 1);

                for (int i = 0; i < fpsHistory.Count - 1; i++)
                {
                    int x1 = Mathf.Clamp(Mathf.RoundToInt(i * xStep), 0, w - 1);
                    int x2 = Mathf.Clamp(Mathf.RoundToInt((i + 1) * xStep), 0, w - 1);

                    float f1 = Mathf.Clamp(fpsHistory[i], 0f, 90f);
                    float f2 = Mathf.Clamp(fpsHistory[i + 1], 0f, 90f);

                    int py1 = Mathf.Clamp(Mathf.RoundToInt((f1 / 90f) * h), 0, h - 1);
                    int py2 = Mathf.Clamp(Mathf.RoundToInt((f2 / 90f) * h), 0, h - 1);

                    DrawLineOnPixelArray(pixels, w, h, x1, py1, x2, py2, lineCol);
                }
            }

            exportTex.SetPixels32(pixels);
            exportTex.Apply(false);

            byte[] pngBytes = exportTex.EncodeToPNG();
            string path = Path.Combine(Application.dataPath, "PerformanceGraph_LatestSession.png");
            File.WriteAllBytes(path, pngBytes);

            statusMessage = $"💾 Saved: Assets/PerformanceGraph_LatestSession.png";
            statusTimer = 4.0f;
            Debug.Log($"[PerformanceDebugDashboard] Successfully exported session graph PNG to: {path}");
        }

        private void ResetSessionData()
        {
            fpsHistory.Clear();
            frameMsHistory.Clear();
            minFps = 999f;
            maxFps = 0f;
            resetStatsTimer = 0f;
            statusMessage = "🔄 Session graph reset!";
            statusTimer = 2.5f;
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
