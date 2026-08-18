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
            if (GameManager.Instance != null)
            {
                if (ownerSide == PlayerSide.Player1) GameManager.Instance.player1Castle = this;
                else if (ownerSide == PlayerSide.Player2) GameManager.Instance.player2Castle = this;
            }

            InitializeCastle();
        }

        public void InitializeCastle()
        {
            RefreshCastleHealth();
        }

        public void RefreshCastleHealth()
        {
            blocks.Clear();
            blocks.AddRange(GetComponentsInChildren<DestructibleBlock>());

            int totalBlocks = blocks.Count;
            int intactBlocks = 0;

            foreach (var block in blocks)
            {
                if (block != null && !block.IsDestroyed)
                {
                    intactBlocks++;
                    block.OnBlockDestroyed -= HandleBlockDestroyed; // prevent duplicate
                    block.OnBlockDestroyed += HandleBlockDestroyed;
                }
            }

            maxCastleHealth = Mathf.Max(1f, totalBlocks);
            currentCastleHealth = intactBlocks;
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

        private void HandleBlockDestroyed()
        {
            if (IsDestroyed) return;

            // Recalculate intact blocks accurately
            int intact = 0;
            foreach (var block in blocks)
            {
                if (block != null && !block.IsDestroyed) intact++;
            }

            currentCastleHealth = intact;
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
            Soldier[] allSoldiers = FindObjectsByType<Soldier>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int totalOwnerSoldiers = 0;
            int aliveOwnerSoldiers = 0;

            foreach (var s in allSoldiers)
            {
                if (s != null && s.ownerSide == ownerSide)
                {
                    totalOwnerSoldiers++;
                    if (!s.IsDead && s.gameObject.activeInHierarchy)
                    {
                        aliveOwnerSoldiers++;
                    }
                }
            }

            return (totalOwnerSoldiers > 0 && aliveOwnerSoldiers == 0);
        }

        private void OnDestroy()
        {
            foreach (var block in blocks)
            {
                if (block != null) block.OnBlockDestroyed -= HandleBlockDestroyed;
            }
        }
    }
}
