using System;
using UnityEngine;
using CastleBusters.Environment;

namespace CastleBusters.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Castles")]
        public Castle player1Castle;
        public Castle player2Castle;

        public GameState CurrentState { get; private set; } = GameState.Initialization;
        public bool IsGameOver { get; private set; } = false;

        public event Action<PlayerSide> OnGameOver;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            IsGameOver = false;
            CurrentState = GameState.IntroSequence;

            EnsureBackgroundManager();

            if (player1Castle != null) player1Castle.OnCastleDestroyed += CheckVictoryState;
            if (player2Castle != null) player2Castle.OnCastleDestroyed += CheckVictoryState;

            MatchIntroController introController = FindFirstObjectByType<MatchIntroController>();
            if (introController == null)
            {
                GameObject introObj = new GameObject("MatchIntroController");
                introController = introObj.AddComponent<MatchIntroController>();
            }

            introController.PlayIntroSequence(player1Castle, player2Castle, (startingPlayer) =>
            {
                CurrentState = GameState.PlayerTurn;
                if (TurnManager.Instance != null)
                {
                    TurnManager.Instance.StartTurn(startingPlayer);
                }
            });
        }

        private void EnsureBackgroundManager()
        {
            BackgroundManager bg = FindFirstObjectByType<BackgroundManager>();
            if (bg == null)
            {
                GameObject bgObj = new GameObject("BackgroundManager");
                bg = bgObj.AddComponent<BackgroundManager>();
            }
            bg.SetupBackgroundLayers();
        }

        public void CheckVictoryState()
        {
            if (IsGameOver) return;

            bool p1Lost = (player1Castle != null && (player1Castle.IsDestroyed || player1Castle.AreAllSoldiersDead()));
            bool p2Lost = (player2Castle != null && (player2Castle.IsDestroyed || player2Castle.AreAllSoldiersDead()));

            if (p1Lost && p2Lost)
            {
                EndGame(PlayerSide.Player1); // Draw or P1 default
            }
            else if (p1Lost)
            {
                EndGame(PlayerSide.Player2); // P2 Wins
            }
            else if (p2Lost)
            {
                EndGame(PlayerSide.Player1); // P1 Wins
            }
        }

        public void TriggerRoundLimitLoss()
        {
            if (IsGameOver) return;
            Debug.Log("GAME OVER! Max rounds reached (12 rounds). Player 1 lost.");
            EndGame(PlayerSide.Player2);
        }

        private void EndGame(PlayerSide winner)
        {
            IsGameOver = true;
            CurrentState = GameState.GameOver;
            Debug.Log($"GAME OVER! Winner: {winner}");
            OnGameOver?.Invoke(winner);
        }

        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
