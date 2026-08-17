using System;
using System.Collections.Generic;
using UnityEngine;
using CastleBusters.Core;
using CastleBusters.Units;

namespace CastleBusters.Environment
{
    public class Castle : MonoBehaviour
    {
        [Header("Castle Settings")]
        public PlayerSide ownerSide;
        public CastleSlotGrid slotGrid;
        public List<Soldier> soldiers = new List<Soldier>();
        public List<DestructibleBlock> blocks = new List<DestructibleBlock>();

        [Header("Castle Health")]
        public float maxCastleHealth = 1000f;
        public float currentCastleHealth;
        public bool IsDestroyed => currentCastleHealth <= 0f;

        public event Action<float, float> OnCastleHealthChanged;
        public event Action OnCastleDestroyed;

        private void Start()
        {
            InitializeCastle();
        }

        public void InitializeCastle()
        {
            blocks.Clear();
            blocks.AddRange(GetComponentsInChildren<DestructibleBlock>());

            float calculatedMaxHealth = 0f;
            foreach (var block in blocks)
            {
                if (block != null)
                {
                    calculatedMaxHealth += block.maxHealth;
                    block.OnDamageTaken += HandleBlockDamage;
                }
            }

            if (calculatedMaxHealth > 0f) maxCastleHealth = calculatedMaxHealth;
            currentCastleHealth = maxCastleHealth;
            OnCastleHealthChanged?.Invoke(currentCastleHealth, maxCastleHealth);

            // Register soldiers
            soldiers.Clear();
            soldiers.AddRange(GetComponentsInChildren<Soldier>());
            foreach (var soldier in soldiers)
            {
                if (soldier != null)
                {
                    soldier.ownerSide = ownerSide;
                    // Position soldier in grid if slot matches
                    if (slotGrid != null)
                    {
                        Transform slotXform = slotGrid.GetSlotTransform(soldier.slotPosition);
                        if (slotXform != null)
                        {
                            soldier.transform.position = slotXform.position;
                        }
                    }
                }
            }
        }

        private void HandleBlockDamage(float damage)
        {
            if (IsDestroyed) return;

            currentCastleHealth = Mathf.Max(0f, currentCastleHealth - damage);
            OnCastleHealthChanged?.Invoke(currentCastleHealth, maxCastleHealth);

            if (IsDestroyed)
            {
                OnCastleDestroyed?.Invoke();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckVictoryState();
                }
            }
        }

        public bool AreAllSoldiersDead()
        {
            foreach (var s in soldiers)
            {
                if (s != null && !s.IsDead) return false;
            }
            return true;
        }

        private void OnDestroy()
        {
            foreach (var block in blocks)
            {
                if (block != null) block.OnDamageTaken -= HandleBlockDamage;
            }
        }
    }
}
