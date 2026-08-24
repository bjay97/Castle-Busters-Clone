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
        public int salvoCount = 7; // Balanced 7 sub-missiles per salvo barrage
        public float salvoInterval = 0.1f;
        public float salvoSpreadAngle = 6f;
        public float launchForceMultiplier = 12f;
        public bool hasFiredThisTurn = false;

        [Header("Head Aiming Rotation Controls")]
        public Transform headTransform;         // Child head transform
        public float rotationSpeed = 20f;      // Interpolation smoothing speed
        public bool useInstantRotation = false; // Set to true to snap rotation instantly
        public float minAimAngle = -75f;        // Downward limit angle
        public float maxAimAngle = 85f;         // Upward limit angle

        [Header("Health")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public bool IsDead => currentHealth <= 0;

        [Header("Collision Sensitivity")]
        public float minImpactForceToDamage = 3f;
        public float damageForceScale = 5f;

        public event Action<float, float> OnHealthChanged;
        public event Action OnSoldierDied;

        private Quaternion initialHeadRot;
        private Quaternion targetHeadRot;
        private bool isAimingActive = false;

        private void Awake()
        {
            if (currentHealth <= 0f) currentHealth = maxHealth;

            // Auto-find head transform if unassigned in Inspector
            if (headTransform == null)
            {
                Transform foundHead = transform.Find("Head") ?? transform.Find("head");
                if (foundHead != null) headTransform = foundHead;
            }

            if (headTransform != null)
            {
                initialHeadRot = headTransform.localRotation;
                targetHeadRot = initialHeadRot;
            }
        }

        /// <summary>
        /// Smoothly rotates head towards aiming direction vector.
        /// </summary>
        public void AimAt(Vector2 aimDirection)
        {
            if (aimDirection.sqrMagnitude < 0.01f || hasFiredThisTurn) return;

            isAimingActive = true;

            // Calculate world angle towards launch velocity vector
            float worldAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

            // Clamp angle to realistic human aiming limits (-75 deg to +85 deg)
            worldAngle = Mathf.Clamp(worldAngle, minAimAngle, maxAimAngle);

            if (headTransform != null)
            {
                targetHeadRot = Quaternion.Euler(0f, 0f, worldAngle);
            }
        }

        /// <summary>
        /// Resets head back to default resting orientation.
        /// </summary>
        public void ResetAim()
        {
            isAimingActive = false;
        }

        private void LateUpdate()
        {
            if (headTransform == null) return;

            // Rotate towards target aiming angle during active drag
            if (isAimingActive && !hasFiredThisTurn)
            {
                headTransform.rotation = useInstantRotation 
                    ? targetHeadRot 
                    : Quaternion.Slerp(headTransform.rotation, targetHeadRot, Time.deltaTime * rotationSpeed);
            }
            else
            {
                // Smoothly return head back to default resting local rotation
                headTransform.localRotation = Quaternion.Slerp(headTransform.localRotation, initialHeadRot, Time.deltaTime * rotationSpeed);
            }
        }

        private void Start()
        {
            if (currentHealth <= 0f) currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
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
            SetSelectionVisualState(true);
        }

        public void SetSelectionVisualState(bool isSelected)
        {
            Color targetColor = isSelected ? Color.white : new Color(0.42f, 0.42f, 0.46f, 1.0f);
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    if (sr.gameObject.name.ToLower().Contains("shadow"))
                    {
                        sr.color = isSelected ? new Color(0f, 0f, 0f, 0.4f) : new Color(0f, 0f, 0f, 0.18f);
                    }
                    else
                    {
                        sr.color = targetColor;
                    }
                }
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
