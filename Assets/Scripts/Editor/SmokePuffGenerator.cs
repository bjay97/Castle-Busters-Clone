#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CastleBusters.EditorTools
{
    public static class SmokePuffGenerator
    {
        [MenuItem("Tools/Castle Busters/Generate Smoke Puff Sprite")]
        public static void GenerateSmokePuffSprite()
        {
            string dir = "Assets/Sprites/VFX";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string filePath = Path.Combine(dir, "SmokePuff.png");
            CreateAndSaveSmokePuff(filePath);

            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.isReadable = true;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Debug.Log($"[SmokePuffGenerator] Successfully generated smoke puff sprite at '{filePath}' with Read/Write enabled!");
        }

        private static void CreateAndSaveSmokePuff(string fullPath)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            float center = size * 0.5f;
            float maxRadius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Organic multi-layered cloud puff shape
                    float noise = Mathf.Sin(angle * 4f) * 0.12f + Mathf.Cos(angle * 7f) * 0.08f;
                    float r = maxRadius * (1f + noise);

                    float normDist = Mathf.Clamp01(dist / r);
                    float alpha = Mathf.SmoothStep(1f, 0f, normDist);

                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);
        }
    }
}
#endif
