using UnityEngine;
using CastleBusters.Core;

namespace CastleBusters.Units
{
    public class ModularSoldierVisual : MonoBehaviour
    {
        [Header("Character Model Config")]
        [Range(1, 8)]
        public int characterNumber = 4; // Select character_1 to character_8

        [Header("Idle Animation Settings")]
        public float idleBreatheSpeed = 2.5f;
        public float idleBreatheAmount = 0.035f;
        public float idleSwaySpeed = 1.8f;
        public float idleSwayAngle = 2.5f;

        private Transform headTransform;
        private Transform bodyTransform;
        private Transform weaponTransform;
        private Vector3 headInitialPos;
        private Vector3 bodyInitialPos;
        private Vector3 weaponInitialPos;

        private bool isBuilt = false;

        private void Start()
        {
            BuildCharacterVisuals();
        }

        public void BuildCharacterVisuals()
        {
            if (isBuilt && transform.childCount > 0) return;

            // Hide old single-sprite renderer on parent if present
            SpriteRenderer parentSr = GetComponent<SpriteRenderer>();
            if (parentSr != null) parentSr.enabled = false;

            // Create container
            GameObject rootContainer = new GameObject("ModularVisual_Root");
            rootContainer.transform.SetParent(transform);
            rootContainer.transform.localPosition = Vector3.zero;
            rootContainer.transform.localRotation = Quaternion.identity;

            // Determine flip direction based on side
            Soldier soldierComp = GetComponent<Soldier>();
            if (soldierComp != null && soldierComp.ownerSide == PlayerSide.Player2)
            {
                rootContainer.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                rootContainer.transform.localScale = Vector3.one;
            }

            string folderPath = $"Assets/Sprites/2D Minimal Characters/PNG/character/character_{characterNumber}/";
            string rootPath = "Assets/Sprites/2D Minimal Characters/PNG/character/";

            // Load Parts
            Sprite spriteHead = LoadSprite($"{folderPath}Head.png");
            Sprite spriteBody = LoadSprite($"{folderPath}Body.png");
            Sprite spriteLegLeft = LoadSprite($"{folderPath}Leg.png");
            Sprite spriteLegRight = LoadSprite($"{folderPath}Leg2.png");
            Sprite spriteCape = LoadSprite($"{folderPath}Cape.png");
            Sprite spriteMouth = LoadSprite($"{folderPath}Mouth.png");
            Sprite spriteWeapon = LoadSprite($"{folderPath}Weapon.png");
            Sprite spriteShadow = LoadSprite($"{rootPath}Shadow.png");

            int baseSortingOrder = 10;

            // 1. Shadow
            CreatePart(rootContainer, "Shadow", spriteShadow, new Vector3(0f, -0.42f, 0f), baseSortingOrder - 2, Vector3.one * 0.8f);

            // 2. Legs
            CreatePart(rootContainer, "LegLeft", spriteLegLeft, new Vector3(-0.12f, -0.28f, 0f), baseSortingOrder, Vector3.one);
            CreatePart(rootContainer, "LegRight", spriteLegRight, new Vector3(0.12f, -0.28f, 0f), baseSortingOrder, Vector3.one);

            // 3. Body & Cape
            if (spriteCape != null)
            {
                CreatePart(rootContainer, "Cape", spriteCape, new Vector3(-0.12f, 0.05f, 0f), baseSortingOrder + 1, Vector3.one);
            }
            GameObject bodyObj = CreatePart(rootContainer, "Body", spriteBody, new Vector3(0f, 0f, 0f), baseSortingOrder + 2, Vector3.one);
            bodyTransform = bodyObj.transform;
            bodyInitialPos = bodyTransform.localPosition;

            // 4. Head & Mouth
            GameObject headObj = CreatePart(rootContainer, "Head", spriteHead, new Vector3(0f, 0.32f, 0f), baseSortingOrder + 4, Vector3.one);
            headTransform = headObj.transform;
            headInitialPos = headTransform.localPosition;

            if (spriteMouth != null)
            {
                CreatePart(headObj, "Mouth", spriteMouth, new Vector3(0f, -0.06f, 0f), baseSortingOrder + 5, Vector3.one);
            }

            // 5. Weapon
            if (spriteWeapon != null)
            {
                GameObject weaponObj = CreatePart(rootContainer, "Weapon", spriteWeapon, new Vector3(0.24f, 0.02f, 0f), baseSortingOrder + 6, Vector3.one);
                weaponTransform = weaponObj.transform;
                weaponInitialPos = weaponTransform.localPosition;
            }

            isBuilt = true;
        }

        private GameObject CreatePart(GameObject parent, string name, Sprite sprite, Vector3 localPos, int sortingOrder, Vector3 scale)
        {
            if (sprite == null) return new GameObject(name);

            GameObject partObj = new GameObject(name);
            partObj.transform.SetParent(parent.transform);
            partObj.transform.localPosition = localPos;
            partObj.transform.localScale = scale;

            SpriteRenderer sr = partObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            return partObj;
        }

        private Sprite LoadSprite(string assetPath)
        {
            Sprite s = null;
#if UNITY_EDITOR
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif
            return s;
        }

        private void Update()
        {
            if (!isBuilt) return;

            // Procedural Idle Breathing & Swaying
            float breatheOffset = Mathf.Sin(Time.time * idleBreatheSpeed) * idleBreatheAmount;
            float swayAngle = Mathf.Sin(Time.time * idleSwaySpeed) * idleSwayAngle;

            if (headTransform != null)
            {
                headTransform.localPosition = headInitialPos + new Vector3(0f, breatheOffset, 0f);
                headTransform.localRotation = Quaternion.Euler(0f, 0f, swayAngle * 0.8f);
            }

            if (bodyTransform != null)
            {
                bodyTransform.localPosition = bodyInitialPos + new Vector3(0f, breatheOffset * 0.4f, 0f);
                bodyTransform.localRotation = Quaternion.Euler(0f, 0f, swayAngle * 0.3f);
            }

            if (weaponTransform != null)
            {
                weaponTransform.localPosition = weaponInitialPos + new Vector3(0f, breatheOffset * 0.6f, 0f);
                weaponTransform.localRotation = Quaternion.Euler(0f, 0f, -swayAngle * 1.2f);
            }
        }
    }
}
