using UnityEngine;
using CastleBusters.Units;

namespace CastleBusters.Environment
{
    public class CastleSlotGrid : MonoBehaviour
    {
        [Header("Slot Anchors")]
        public Transform upperLeftSlot;
        public Transform upperRightSlot;
        public Transform lowerLeftSlot;
        public Transform lowerRightSlot;

        private void Awake()
        {
            AutoSetupSlots();
        }

        [ContextMenu("Setup 4 Slot Anchors")]
        public void AutoSetupSlots()
        {
            if (upperLeftSlot == null) upperLeftSlot = CreateOrFindSlot("UpperLeft_Slot", new Vector3(-1.5f, 1.0f, 0f));
            if (upperRightSlot == null) upperRightSlot = CreateOrFindSlot("UpperRight_Slot", new Vector3(1.5f, 1.0f, 0f));
            if (lowerLeftSlot == null) lowerLeftSlot = CreateOrFindSlot("LowerLeft_Slot", new Vector3(-1.5f, -0.8f, 0f));
            if (lowerRightSlot == null) lowerRightSlot = CreateOrFindSlot("LowerRight_Slot", new Vector3(1.5f, -0.8f, 0f));
        }

        private Transform CreateOrFindSlot(string slotName, Vector3 localPos)
        {
            Transform existing = transform.Find(slotName);
            if (existing != null) return existing;

            GameObject newSlot = new GameObject(slotName);
            newSlot.transform.SetParent(transform);
            newSlot.transform.localPosition = localPos;
            return newSlot.transform;
        }

        public Transform GetSlotTransform(CastleSlotPosition position)
        {
            switch (position)
            {
                case CastleSlotPosition.UpperLeft: return upperLeftSlot != null ? upperLeftSlot : transform;
                case CastleSlotPosition.UpperRight: return upperRightSlot != null ? upperRightSlot : transform;
                case CastleSlotPosition.LowerLeft: return lowerLeftSlot != null ? lowerLeftSlot : transform;
                case CastleSlotPosition.LowerRight: return lowerRightSlot != null ? lowerRightSlot : transform;
                default: return transform;
            }
        }
    }
}
