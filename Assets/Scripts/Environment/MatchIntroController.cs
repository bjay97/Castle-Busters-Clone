using System;
using System.Collections;
using UnityEngine;
using CastleBusters.Core;

namespace CastleBusters.Environment
{
    public class MatchIntroController : MonoBehaviour
    {
        [Header("Intro Movement Settings")]
        public float offscreenOffset = 10f; // Distance off-camera to start driving from
        public float driveSpeed = 4.0f; // Speed of intro movement

        [Header("Coin Toss Settings")]
        public bool showCoinTossLog = true;

        public event Action<PlayerSide> OnIntroComplete;

        public void PlayIntroSequence(Castle player1Castle, Castle player2Castle, Action<PlayerSide> callback)
        {
            StartCoroutine(IntroRoutine(player1Castle, player2Castle, callback));
        }

        private IEnumerator IntroRoutine(Castle player1Castle, Castle player2Castle, Action<PlayerSide> callback)
        {
            int arrivedCount = 0;

            // Prepare Player 1 Castle
            if (player1Castle != null)
            {
                CastleFacadeVisibility facade = player1Castle.GetComponentInChildren<CastleFacadeVisibility>();
                if (facade != null) facade.SetFacadeVisibility(true);

                CastleMovement move = player1Castle.GetComponent<CastleMovement>();
                if (move != null)
                {
                    float targetX = player1Castle.transform.position.x;
                    float startX = targetX - offscreenOffset;
                    move.StartIntroDrive(startX, targetX, driveSpeed, () => { arrivedCount++; });
                }
                else
                {
                    arrivedCount++;
                }
            }
            else
            {
                arrivedCount++;
            }

            // Prepare Player 2 Castle
            if (player2Castle != null)
            {
                CastleFacadeVisibility facade = player2Castle.GetComponentInChildren<CastleFacadeVisibility>();
                if (facade != null) facade.SetFacadeVisibility(true);

                CastleMovement move = player2Castle.GetComponent<CastleMovement>();
                if (move != null)
                {
                    float targetX = player2Castle.transform.position.x;
                    float startX = targetX + offscreenOffset;
                    move.StartIntroDrive(startX, targetX, driveSpeed, () => { arrivedCount++; });
                }
                else
                {
                    arrivedCount++;
                }
            }
            else
            {
                arrivedCount++;
            }

            // Wait until both castles reach target positions
            while (arrivedCount < 2)
            {
                yield return null;
            }

            // Small settle pause after parking
            yield return new WaitForSeconds(0.4f);

            // Perform Random Coin Toss to decide who takes the first shot
            PlayerSide startingPlayer = (UnityEngine.Random.value < 0.5f) ? PlayerSide.Player1 : PlayerSide.Player2;

            if (showCoinTossLog)
            {
                Debug.Log($"[MATCH INTRO] Coin toss complete! Winner of first turn: {startingPlayer}");
            }

            OnIntroComplete?.Invoke(startingPlayer);
            callback?.Invoke(startingPlayer);
        }
    }
}
