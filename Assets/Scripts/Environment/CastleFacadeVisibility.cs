using UnityEngine;
using CastleBusters.Core;

namespace CastleBusters.Environment
{
    public class CastleFacadeVisibility : MonoBehaviour
    {
        [Header("Castle Side")]
        public PlayerSide castleSide = PlayerSide.Player1;

        [Header("Alpha Settings")]
        [Range(0f, 1f)] public float ownTurnAlpha = 0.35f;  // Semi-transparent so you see your own soldiers/interior
        [Range(0f, 1f)] public float enemyTargetAlpha = 1.0f; // 100% Opaque solid wall (Fog-of-War hides enemy interior)

        private Collider2D[] childColliders;
        private SpriteRenderer[] childRenderers;

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
                HandleTurnChanged(TurnManager.Instance.activePlayer);
            }
            else
            {
                HandleTurnChanged(PlayerSide.Player1);
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }
        }

        public void HandleTurnChanged(PlayerSide activePlayer)
        {
            bool isOurTurn = (activePlayer == castleSide);

            // 1. COLLISION LOGIC:
            // Disable colliders on our OWN castle facade during our turn so outgoing missiles have zero obstruction.
            // Enable colliders on our facade when we are the TARGET receiving enemy missiles.
            SetFacadeCollidersEnabled(!isOurTurn);

            // 2. VISUAL LOGIC (Fog-of-War):
            // Player's OWN castle facade can be semi-transparent on their turn so they see their own room.
            // ENEMY castle facade ALWAYS stays 100% OPAQUE (alpha = 1.0) so enemy interior is NEVER leaked!
            if (castleSide == PlayerSide.Player2)
            {
                // Enemy facade is ALWAYS 100% opaque (no peeking allowed!)
                SetFacadeAlpha(1.0f);
            }
            else
            {
                // Player's own castle: transparent on player's turn, opaque on enemy turn
                float targetAlpha = isOurTurn ? ownTurnAlpha : enemyTargetAlpha;
                SetFacadeAlpha(targetAlpha);
            }
        }

        public void SetFacadeCollidersEnabled(bool enableColliders)
        {
            if (childColliders == null || childColliders.Length == 0) childColliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < childColliders.Length; i++)
            {
                if (childColliders[i] != null)
                {
                    childColliders[i].enabled = enableColliders;
                }
            }
        }

        public void SetFacadeAlpha(float alpha)
        {
            if (childRenderers == null || childRenderers.Length == 0) childRenderers = GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < childRenderers.Length; i++)
            {
                if (childRenderers[i] != null)
                {
                    Color c = childRenderers[i].color;
                    childRenderers[i].color = new Color(c.r, c.g, c.b, alpha);
                }
            }
        }
    }
}
