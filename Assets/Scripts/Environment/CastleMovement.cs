using System;
using System.Collections.Generic;
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
        public float minimumCastleSeparation = 12f; // Minimum distance between castles to prevent touching/collision

        [Header("Fuel Settings")]
        public float maxFuel = 100f;
        public float currentFuel;
        public float fuelConsumptionRate = 25f; // Fuel consumed per second while moving

        [Header("Visual Feedback")]
        public Transform[] wheels;
        public float wheelRotationSpeed = 180f;

        [Header("Engine Movement Audio Config")]
        public AudioSource moveAudioSource; // Drag an existing AudioSource here, OR leave empty and assign moveClip below
        public AudioClip moveClip; // Drag tank accelerating/moving audio clip here
        [Range(0f, 1f)] public float maxMoveVolume = 0.8f; // Maximum volume when moving
        public float fadeSpeed = 4.0f; // Speed of smooth volume fade in/out (higher = faster fade)

        [Header("Vehicle Engine Juice & Dynamics")]
        public Transform chassisTransform; // Optional: Drag visual castle container transform (holds facade, interior, soldiers)
        public bool enableEngineJuice = true;
        public float idleRumbleAmount = 0.035f; // Upward idle engine vibration amplitude
        public float idleRumbleFrequency = 22f; // Engine idle chug frequency
        public float accelerationLeanAngle = 4.0f; // Chassis tilt angle when accelerating / moving
        public float drivingBobAmount = 0.08f; // Chassis vertical bounce when driving over ground
        public float drivingBobFrequency = 26f; // Tread/wheel bump frequency
        public float chassisDampingSpeed = 10f; // Smoothing speed for tilt & bobbing

        private bool isMovingThisFrame = false;
        private float currentMoveDir = 0f;
        private float currentLeanAngle = 0f;
        private float currentVerticalOffset = 0f;

        private Castle castle;
        private bool isMyTurn = false;

        public event Action<float, float> OnFuelChanged;

        private void Awake()
        {
            castle = GetComponent<Castle>();
            currentFuel = maxFuel;

            LockRotation();
        }

        private void FixedUpdate()
        {
            LockRotation();
        }

        private void LateUpdate()
        {
            UpdateEngineAudio();
            UpdateVehicleJuice();
            isMovingThisFrame = false;
        }

        private void LockRotation()
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.freezeRotation = true;
                rb.angularVelocity = 0f;
                rb.rotation = 0f;
            }
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
                HandleTurnChanged(TurnManager.Instance.activePlayer);
            }

            SetupUnifiedVisualContainer();
        }

        private void SetupUnifiedVisualContainer()
        {
            if (chassisTransform != null) return;

            // Check if a VisualContainer child already exists
            Transform existingContainer = transform.Find("VisualContainer");
            if (existingContainer != null)
            {
                chassisTransform = existingContainer;
                return;
            }

            // Create a new VisualContainer child at (0,0,0) to hold all visual elements together
            GameObject containerObj = new GameObject("VisualContainer");
            containerObj.transform.SetParent(transform, false);
            containerObj.transform.localPosition = Vector3.zero;
            containerObj.transform.localRotation = Quaternion.identity;
            containerObj.transform.localScale = Vector3.one;

            List<Transform> childrenToMove = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == containerObj.transform) continue;

                // Move visual children into container (Skip wheel anchors if assigned in wheels array)
                bool isWheel = false;
                if (wheels != null)
                {
                    foreach (var w in wheels)
                    {
                        if (w == child) { isWheel = true; break; }
                    }
                }

                if (!isWheel)
                {
                    childrenToMove.Add(child);
                }
            }

            foreach (var child in childrenToMove)
            {
                child.SetParent(containerObj.transform, true);
            }

            chassisTransform = containerObj.transform;
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
            if (castle.ownerSide == PlayerSide.Player2) return;

            float horizontalInput = uiInputDirection;

            if (Mathf.Abs(horizontalInput) < 0.01f)
            {
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

            float effectiveMinX = minX;
            float effectiveMaxX = maxX;

            if (castle != null && GameManager.Instance != null)
            {
                if (castle.ownerSide == PlayerSide.Player1 && GameManager.Instance.player2Castle != null)
                {
                    float maxAllowedX = GameManager.Instance.player2Castle.transform.position.x - minimumCastleSeparation;
                    effectiveMaxX = Mathf.Min(maxX, maxAllowedX);
                }
                else if (castle.ownerSide == PlayerSide.Player2 && GameManager.Instance.player1Castle != null)
                {
                    float minAllowedX = GameManager.Instance.player1Castle.transform.position.x + minimumCastleSeparation;
                    effectiveMinX = Mathf.Max(minX, minAllowedX);
                }
            }

            float moveAmount = direction * moveSpeed * Time.deltaTime;
            float newX = Mathf.Clamp(transform.position.x + moveAmount, effectiveMinX, effectiveMaxX);
            float actualDelta = newX - transform.position.x;

            if (Mathf.Abs(actualDelta) > 0.0001f)
            {
                isMovingThisFrame = true;
                currentMoveDir = Mathf.Sign(direction);

                Vector3 targetPos = new Vector3(newX, transform.position.y, transform.position.z);
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.MovePosition(targetPos);
                }
                else
                {
                    transform.position = targetPos;
                }

                float fuelUsed = fuelConsumptionRate * Time.deltaTime;
                currentFuel = Mathf.Max(0f, currentFuel - fuelUsed);
                OnFuelChanged?.Invoke(currentFuel, maxFuel);

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

        private void UpdateEngineAudio()
        {
            if (moveAudioSource == null && moveClip != null)
            {
                moveAudioSource = gameObject.AddComponent<AudioSource>();
                moveAudioSource.clip = moveClip;
                moveAudioSource.loop = true;
                moveAudioSource.playOnAwake = false;
                moveAudioSource.spatialBlend = 0.5f;
                moveAudioSource.volume = 0f;
            }

            if (moveAudioSource == null) return;

            float targetVolume = isMovingThisFrame ? maxMoveVolume : 0f;

            if (isMovingThisFrame && !moveAudioSource.isPlaying)
            {
                moveAudioSource.Play();
            }

            moveAudioSource.volume = Mathf.MoveTowards(moveAudioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);

            if (moveAudioSource.volume <= 0.001f && !isMovingThisFrame && moveAudioSource.isPlaying)
            {
                moveAudioSource.Stop();
            }
        }

        private void UpdateVehicleJuice()
        {
            if (!enableEngineJuice || chassisTransform == null) return;

            float targetLean = 0f;
            float targetYOffset = 0f;

            if (isMovingThisFrame)
            {
                // Torque pitch: Chassis tilts when accelerating / driving
                targetLean = -currentMoveDir * accelerationLeanAngle;

                // Upward-biased driving bounce (never drops below ground line 0.0)
                float drivingWave = (Mathf.Sin(Time.time * drivingBobFrequency) * 0.5f + 0.5f);
                targetYOffset = drivingWave * drivingBobAmount;
            }
            else
            {
                // Upward-biased engine idle chug (never drops below ground line 0.0)
                float idleWave = (Mathf.Sin(Time.time * idleRumbleFrequency) * 0.5f + 0.5f);
                targetYOffset = idleWave * idleRumbleAmount;

                targetLean = Mathf.Cos(Time.time * (idleRumbleFrequency * 0.4f)) * (idleRumbleAmount * 14f);
            }

            // Add slight ground clearance compensation when chassis tilts so bottom corners never dip into floor
            float tiltClearanceCompensation = Mathf.Abs(targetLean) * 0.015f;
            targetYOffset += tiltClearanceCompensation;

            // Smoothly interpolate chassis tilt and vertical offset
            currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLean, Time.deltaTime * chassisDampingSpeed);
            currentVerticalOffset = Mathf.Lerp(currentVerticalOffset, targetYOffset, Time.deltaTime * chassisDampingSpeed);

            // Apply visual transformation relative to local ground baseline
            chassisTransform.localRotation = Quaternion.Euler(0f, 0f, currentLeanAngle);
            chassisTransform.localPosition = new Vector3(0f, currentVerticalOffset, 0f);
        }
    }
}
