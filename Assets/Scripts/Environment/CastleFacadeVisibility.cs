using UnityEngine;
using CastleBusters.Core;

namespace CastleBusters.Environment
{
    public class CastleFacadeVisibility : MonoBehaviour
    {
        [Header("Castle Side")]
        public PlayerSide castleSide = PlayerSide.Player1;

        [Header("Alpha Settings")]
        [Range(0f, 1f)] public float ownTurnAlpha = 1.0f;   // Always 100% solid opaque picture
        [Range(0f, 1f)] public float enemyTargetAlpha = 1.0f; // 100% Opaque solid wall (Fog-of-War hides enemy interior)

        private Collider2D[] childColliders;
        private SpriteRenderer[] childRenderers;

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
            
            RefreshVisibility();
        }

        public void RefreshVisibility()
        {
            PlayerSide currentActive = (TurnManager.Instance != null) ? TurnManager.Instance.activePlayer : PlayerSide.Player1;
            HandleTurnChanged(currentActive);
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

            // Colliders disabled on active turn for outgoing missile launch
            SetFacadeCollidersEnabled(!isOurTurn);

            // Default Visual State: ALWAYS 100% Solid and Opaque for both castles
            SetFacadeVisibility(true);
        }

        public void SetAimingHideState(bool isAiming)
        {
            // Hide Player 1 facade ONLY while actively dragging the slingshot to aim
            if (castleSide == PlayerSide.Player1)
            {
                SetFacadeVisibility(!isAiming);
            }
        }

        public void SetFacadeCollidersEnabled(bool enableColliders)
        {
            DestructibleBlock[] blocks = GetComponentsInChildren<DestructibleBlock>();
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i] != null)
                {
                    Collider2D col = blocks[i].GetComponent<Collider2D>();
                    if (col != null) col.enabled = enableColliders;
                }
            }
        }

        public void SetFacadeVisibility(bool isVisible)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    // Keep grid block renderers disabled so dynamic masked facade is shown
                    DestructibleBlock block = renderers[i].GetComponent<DestructibleBlock>();
                    if (block != null)
                    {
                        renderers[i].enabled = false;
                    }
                    else
                    {
                        renderers[i].enabled = isVisible;
                        Color c = renderers[i].color;
                        renderers[i].color = new Color(c.r, c.g, c.b, 1.0f);
                    }
                }
            }
        }
    }
}
