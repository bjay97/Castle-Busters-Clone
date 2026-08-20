#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CastleBusters.EditorTools
{
    public static class CraterMaskTools
    {
        [MenuItem("Tools/Castle Busters/Enable Read-Write On All Crater Mask Textures")]
        public static void FixAllCraterTexturesReadWrite()
        {
            string[] searchFolders = new string[] { "Assets/Sprites/VFX/Craters", "Assets/Sprites/CraterMasks" };

            int fixedCount = 0;

            foreach (string folder in searchFolders)
            {
                if (Directory.Exists(folder))
                {
                    string[] files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                        if (importer != null)
                        {
                            bool needsUpdate = false;
                            if (!importer.isReadable)
                            {
                                importer.isReadable = true;
                                needsUpdate = true;
                            }
                            if (importer.textureType != TextureImporterType.Sprite)
                            {
                                importer.textureType = TextureImporterType.Sprite;
                                importer.spriteImportMode = SpriteImportMode.Single;
                                needsUpdate = true;
                            }

                            if (needsUpdate)
                            {
                                importer.SaveAndReimport();
                                fixedCount++;
                            }
                        }
                    }
                }
            }

            Debug.Log($"[CraterMaskTools] Processed crater texture folders. Updated Read/Write on {fixedCount} texture assets!");
        }
    }
}
#endif
