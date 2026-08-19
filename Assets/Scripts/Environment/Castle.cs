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

        [Header("Impact Recoil")]
        public float recoilDistance = 0.35f; // Nudge distance when hit
        public float recoilRecoverySpeed = 10f; // Speed of spring recovery back to rest position

        private Vector3 originalLocalPos;
        private Vector3 recoilOffset;
        private bool isOriginalPosSaved = false;

        private void Start()
        {
            originalLocalPos = transform.localPosition;
            isOriginalPosSaved = true;

            if (GameManager.Instance != null)
            {
                if (ownerSide == PlayerSide.Player1) GameManager.Instance.player1Castle = this;
                else if (ownerSide == PlayerSide.Player2) GameManager.Instance.player2Castle = this;
            }

            InitializeCastle();
        }

        private void Update()
        {
            if (recoilOffset.sqrMagnitude > 0.0001f)
            {
                recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
                transform.localPosition = originalLocalPos + recoilOffset;
            }
            else if (isOriginalPosSaved && transform.localPosition != originalLocalPos)
            {
                recoilOffset = Vector3.zero;
                transform.localPosition = originalLocalPos;
            }
        }

        public void TriggerRecoil(Vector2 hitDirection)
        {
            if (!isOriginalPosSaved)
            {
                originalLocalPos = transform.localPosition;
                isOriginalPosSaved = true;
            }

            recoilOffset += (Vector3)(hitDirection.normalized * recoilDistance);
            recoilOffset = Vector3.ClampMagnitude(recoilOffset, recoilDistance * 1.5f);
        }

        public void InitializeCastle()
        {
            RefreshCastleHealth();
        }

        public void RefreshCastleHealth()
        {
            blocks.Clear();
            blocks.AddRange(GetComponentsInChildren<DestructibleBlock>());

            float maxHP = 0f;
            float currentHP = 0f;

            foreach (var block in blocks)
            {
                if (block != null)
                {
                    maxHP += block.maxHealth;
                    if (!block.IsDestroyed)
                    {
                        currentHP += block.currentHealth;
                    }

                    block.OnBlockDestroyed -= RecalculateCastleHealth;
                    block.OnBlockDestroyed += RecalculateCastleHealth;
                    block.OnDamageTaken -= HandleBlockDamageTaken;
                    block.OnDamageTaken += HandleBlockDamageTaken;
                }
            }

            maxCastleHealth = Mathf.Max(1f, maxHP);
            currentCastleHealth = currentHP;
            OnCastleHealthChanged?.Invoke(currentCastleHealth, maxCastleHealth);

            // Register soldiers sorted strictly Left-to-Right by X coordinate
            soldiers.Clear();
            Soldier[] foundSoldiers = GetComponentsInChildren<Soldier>();
            List<Soldier> sortedSoldiers = new List<Soldier>(foundSoldiers);
            sortedSoldiers.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            soldiers.AddRange(sortedSoldiers);

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

        private void HandleBlockDamageTaken(float dmg)
        {
            RecalculateCastleHealth();

            // Recoil away from opponent (Player 1 pushes right, Player 2 pushes left)
            Vector2 recoilDir = (ownerSide == PlayerSide.Player1) ? Vector2.right : Vector2.left;
            TriggerRecoil(recoilDir);
        }

        private void RecalculateCastleHealth()
        {
            FacadeGridBuilder facade = GetComponentInChildren<FacadeGridBuilder>();
            if (facade == null) facade = GetComponent<FacadeGridBuilder>();

            if (facade != null)
            {
                float fraction = facade.GetFacadeHealthFraction();
                currentCastleHealth = maxCastleHealth * fraction;
            }
            else
            {
                float currentHP = 0f;
                float maxHP = 0f;
                foreach (var block in blocks)
                {
                    if (block != null)
                    {
                        maxHP += block.maxHealth;
                        if (!block.IsDestroyed)
                        {
                            currentHP += block.currentHealth;
                        }
                    }
                }
                if (maxHP > 0f) maxCastleHealth = maxHP;
                currentCastleHealth = currentHP;
            }

            currentCastleHealth = Mathf.Max(0f, currentCastleHealth);
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
                if (block != null)
                {
                    block.OnBlockDestroyed -= RecalculateCastleHealth;
                    block.OnDamageTaken -= HandleBlockDamageTaken;
                }
            }
        }
    }
}
