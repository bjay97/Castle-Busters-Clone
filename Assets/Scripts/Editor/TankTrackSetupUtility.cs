#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CastleBusters.Environment;

namespace CastleBusters.EditorTools
{
    public static class TankTrackSetupUtility
    {
        [MenuItem("Castle Busters/Setup Tank Tracks Sprite Sheet")]
        public static void SliceTankTrackSpriteSheet()
        {
            string path = "Assets/Sprites/Castle/TankTracks-Sheet.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                Debug.LogError($"[TankTrackSetupUtility] Could not find TextureImporter at path: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            int frameCount = 3;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogError($"[TankTrackSetupUtility] Could not load Texture2D at path: {path}");
                return;
            }

            int texWidth = tex.width;
            int texHeight = tex.height;
            float frameWidth = texWidth / (float)frameCount;

            SpriteMetaData[] metaData = new SpriteMetaData[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                SpriteMetaData smd = new SpriteMetaData();
                smd.name = $"TankTrack_Frame_{i}";
                smd.rect = new Rect(i * frameWidth, 0, frameWidth, texHeight);
                smd.pivot = new Vector2(0.5f, 0.5f);
                smd.alignment = (int)SpriteAlignment.Center;
                metaData[i] = smd;
            }

            importer.spritesheet = metaData;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Debug.Log($"[TankTrackSetupUtility] Successfully sliced {path} into {frameCount} sprite frames ({frameWidth}x{texHeight} each).");
        }

        [MenuItem("Castle Busters/Convert Castle Wheels to Tank Tracks")]
        public static void ConvertCastleWheelsToTankTracks()
        {
            SliceTankTrackSpriteSheet();

            Texture2D trackSheet = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Castle/TankTracks-Sheet.png");
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Castle/TankTracks-Sheet.png");

            Sprite[] trackFrames = new Sprite[3];
            int foundCount = 0;
            foreach (var asset in subAssets)
            {
                if (asset is Sprite s && foundCount < 3)
                {
                    trackFrames[foundCount++] = s;
                }
            }

            CastleMovement[] castles = Object.FindObjectsByType<CastleMovement>(FindObjectsSortMode.None);
            if (castles == null || castles.Length == 0)
            {
                Debug.LogWarning("[TankTrackSetupUtility] No CastleMovement instances found in the scene.");
                return;
            }

            int totalConverted = 0;

            foreach (var castle in castles)
            {
                Undo.RegisterCompleteObjectUndo(castle.gameObject, "Convert Wheels to Tank Tracks");

                Transform[] targets = castle.wheels;
                if (targets == null || targets.Length == 0)
                {
                    targets = new Transform[] {
                        castle.transform.Find("WheelFront"),
                        castle.transform.Find("WheelBack")
                    };
                }

                System.Collections.Generic.List<TankTrackAnimator> animators = new System.Collections.Generic.List<TankTrackAnimator>();

                foreach (var t in targets)
                {
                    if (t == null) continue;

                    TankTrackAnimator trackAnim = t.GetComponent<TankTrackAnimator>();
                    if (trackAnim == null)
                    {
                        trackAnim = Undo.AddComponent<TankTrackAnimator>(t.gameObject);
                    }

                    trackAnim.trackSheetTexture = trackSheet;
                    if (foundCount >= 3)
                    {
                        trackAnim.trackFrames = trackFrames;
                    }
                    trackAnim.InitializeFramesIfNeeded();

                    SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null && trackAnim.trackFrames != null && trackAnim.trackFrames.Length > 0)
                    {
                        sr.sprite = trackAnim.trackFrames[0];
                    }

                    animators.Add(trackAnim);
                    totalConverted++;
                }

                castle.trackAnimators = animators.ToArray();

                CastleSuspensionPhysics suspension = castle.GetComponent<CastleSuspensionPhysics>();
                if (suspension != null && animators.Count > 0)
                {
                    Undo.RegisterCompleteObjectUndo(suspension, "Update Suspension Track Anchors");
                    if (suspension.frontWheel == null && animators.Count > 0)
                        suspension.frontWheel = animators[0].transform;
                    if (suspension.backWheel == null && animators.Count > 1)
                        suspension.backWheel = animators[1].transform;
                }

                EditorUtility.SetDirty(castle.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[TankTrackSetupUtility] Converted {totalConverted} wheel transform(s) across {castles.Length} castle(s) to Tank Track Animators.");
        }
    }
}
#endif
