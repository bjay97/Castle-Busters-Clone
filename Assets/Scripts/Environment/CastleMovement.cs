using System;
using UnityEngine;
using CastleBusters.Core;

namespace CastleBusters.Environment
{
    [RequireComponent(typeof(Castle))]
    public class CastleMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 3.5f;
        public float minX = -10f;
        public float maxX = -2f;

        [Header("Fuel Settings")]
        public float maxFuel = 100f;
        public float currentFuel;
        public float fuelConsumptionRate = 25f; // Fuel consumed per second while moving

        [Header("Visual Feedback")]
        public Transform[] wheels;
        public float wheelRotationSpeed = 180f;

        private Castle castle;
        private bool isMyTurn = false;

        public event Action<float, float> OnFuelChanged;

        private void Awake()
        {
            castle = GetComponent<Castle>();
            currentFuel = maxFuel;
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
                HandleTurnChanged(TurnManager.Instance.activePlayer);
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }
        }

        private PlayerSide lastActivePlayer = PlayerSide.Player2;

        private void HandleTurnChanged(PlayerSide activePlayer)
        {
            bool isNewRound = (lastActivePlayer != activePlayer && activePlayer == castle?.ownerSide);
            lastActivePlayer = activePlayer;

            isMyTurn = (castle != null && activePlayer == castle.ownerSide);

            if (isNewRound)
            {
                // Reset fuel ONLY at the start of a new round, NOT between shot 1 and shot 2!
                ResetFuel();
            }
        }

        public void ResetFuel()
        {
            currentFuel = maxFuel;
            OnFuelChanged?.Invoke(currentFuel, maxFuel);
        }

        private float uiInputDirection = 0f;

        public void PressMoveLeft() { uiInputDirection = -1f; }
        public void PressMoveRight() { uiInputDirection = 1f; }
        public void ReleaseMove() { uiInputDirection = 0f; }

        public bool IsActivelyMoving { get; private set; }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            if (!isMyTurn || castle == null) return;
            if (castle.ownerSide == PlayerSide.Player2) return; // Player 2 AI handled separately or by bot

            float horizontalInput = uiInputDirection;

            if (Mathf.Abs(horizontalInput) < 0.01f)
            {
                // Support Keyboard A/D fallback
#if ENABLE_INPUT_SYSTEM || UNITY_2020_1_OR_NEWER
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed)
                        horizontalInput = -1f;
                    else if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed)
                        horizontalInput = 1f;
                }
                else
                {
                    horizontalInput = Input.GetAxisRaw("Horizontal");
                }
#else
                horizontalInput = Input.GetAxisRaw("Horizontal");
#endif
            }

            IsActivelyMoving = (Mathf.Abs(horizontalInput) > 0.01f && currentFuel > 0f);

            if (IsActivelyMoving)
            {
                MoveCastle(horizontalInput);
            }
        }

        public void MoveCastle(float direction)
        {
            if (currentFuel <= 0f) return;

            float moveAmount = direction * moveSpeed * Time.deltaTime;
            float newX = Mathf.Clamp(transform.position.x + moveAmount, minX, maxX);
            float actualDelta = newX - transform.position.x;

            if (Mathf.Abs(actualDelta) > 0.0001f)
            {
                // Translate Castle
                transform.position = new Vector3(newX, transform.position.y, transform.position.z);

                // Consume Fuel
                float fuelUsed = fuelConsumptionRate * Time.deltaTime;
                currentFuel = Mathf.Max(0f, currentFuel - fuelUsed);
                OnFuelChanged?.Invoke(currentFuel, maxFuel);

                // Rotate Wheels visually
                RotateWheels(direction);
            }
        }

        private void RotateWheels(float direction)
        {
            if (wheels == null || wheels.Length == 0) return;

            float rotationAmount = -direction * wheelRotationSpeed * Time.deltaTime;
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                {
                    wheel.Rotate(0f, 0f, rotationAmount);
                }
            }
        }
    }
}
