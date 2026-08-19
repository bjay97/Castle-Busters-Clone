using System;
using UnityEngine;
using CastleBusters.Core;
using CastleBusters.Combat;

namespace CastleBusters.Units
{
    public enum SoldierType
    {
        HeavyMissile,
        ClusterMissile
    }

    public enum FireMode
    {
        SingleMissile,
        RapidSalvo
    }

    public enum CastleSlotPosition
    {
        UpperLeft,
        UpperRight,
        LowerLeft,
        LowerRight
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class Soldier : MonoBehaviour
    {
        [Header("Unit Properties")]
        public string soldierName = "Soldier";
        public PlayerSide ownerSide;
        public SoldierType unitType;
        public CastleSlotPosition slotPosition;

        [Header("Abilities & Fire Mode")]
        public FireMode fireMode = FireMode.SingleMissile;
        public GameObject projectilePrefab;
        public int salvoCount = 14; // 10 to 16 missiles sent at 0.1s intervals
        public float salvoInterval = 0.1f;
        public float salvoSpreadAngle = 6f;
        public float launchForceMultiplier = 12f;
        public bool hasFiredThisTurn = false;

        [Header("Health")]
        public float maxHealth = 100f;
        public float currentHealth;
        public bool IsDead => currentHealth <= 0;

        [Header("Collision Sensitivity")]
        public float minImpactForceToDamage = 3f;
        public float damageForceScale = 5f;

        public event Action<float, float> OnHealthChanged;
        public event Action OnSoldierDied;

        private void Start()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            EnsureModularVisual();

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
        }

        private void EnsureModularVisual()
        {
            ModularSoldierVisual visual = GetComponent<ModularSoldierVisual>();
            if (visual == null)
            {
                visual = gameObject.AddComponent<ModularSoldierVisual>();
                int index = (ownerSide == PlayerSide.Player1) ? 1 : 4;
                visual.characterNumber = index;
            }
            visual.BuildCharacterVisuals();
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }
        }

        private void HandleTurnChanged(PlayerSide activeSide)
        {
            if (activeSide == ownerSide)
            {
                hasFiredThisTurn = false;
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (IsDead)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"{soldierName} on {ownerSide} died!");
            OnSoldierDied?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckVictoryState();
            }

            // Visual disable / destruction
            gameObject.SetActive(false);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsDead) return;

            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce >= minImpactForceToDamage)
            {
                float damage = (impactForce - minImpactForceToDamage) * damageForceScale;
                TakeDamage(damage);
            }
        }
    }
}
