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
