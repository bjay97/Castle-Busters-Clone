using UnityEngine;

namespace CastleBusters.Environment
{
    /// <summary>
    /// Handles 3-frame tank track sprite animation for castles.
    /// Cycles through sprite frames when moving, pauses frame updates when stationary,
    /// and provides live edit-mode previewing in the Unity Scene View.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class TankTrackAnimator : MonoBehaviour
    {
        [Header("Frame References")]
        [Tooltip("3-frame sprite sequence for the tank track. If empty, will auto-slice from sheet texture.")]
        public Sprite[] trackFrames;

        [Tooltip("Source texture sheet (7200x1788 or similar) containing 3 horizontal frames.")]
        public Texture2D trackSheetTexture;

        [Header("Animation Settings")]
        public float animationFps = 12f;
        public bool lockLocalRotation = false; // Set to false to allow CastleSuspensionPhysics cliff pitch articulation

        [Header("Editor Preview")]
        [Range(0, 2)]
        [Tooltip("Select frame index (0, 1, or 2) to preview directly in the Unity Scene view.")]
        public int previewFrameIndex = 0;

        private SpriteRenderer spriteRenderer;
        private int currentFrameIndex = 0;
        private float frameTimer = 0f;
        private bool isMoving = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            InitializeFramesIfNeeded();
            ApplyCurrentFrame();
        }

        private void Start()
        {
            InitializeFramesIfNeeded();
            ApplyCurrentFrame();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            InitializeFramesIfNeeded();
            if (!Application.isPlaying)
            {
                int maxIdx = (trackFrames != null && trackFrames.Length > 0) ? trackFrames.Length - 1 : 0;
                currentFrameIndex = Mathf.Clamp(previewFrameIndex, 0, maxIdx);
                ApplyCurrentFrame();
            }
        }

        /// <summary>
        /// Automatically slices trackSheetTexture into 3 equal horizontal frames if trackFrames is empty.
        /// </summary>
        public void InitializeFramesIfNeeded()
        {
            if (trackFrames != null && trackFrames.Length >= 3 && trackFrames[0] != null)
                return;

            if (trackSheetTexture == null)
            {
                trackSheetTexture = Resources.Load<Texture2D>("Castle/TankTracks-Sheet");
                if (trackSheetTexture == null)
                {
                    trackSheetTexture = Resources.Load<Texture2D>("TankTracks-Sheet");
                }
            }

            if (trackSheetTexture != null)
            {
                int frameCount = 3;
                float frameWidth = trackSheetTexture.width / (float)frameCount;
                float frameHeight = trackSheetTexture.height;

                trackFrames = new Sprite[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    Rect rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
                    Vector2 pivot = new Vector2(0.5f, 0.5f);
                    trackFrames[i] = Sprite.Create(trackSheetTexture, rect, pivot, 100f);
                    trackFrames[i].name = $"{trackSheetTexture.name}_Frame_{i}";
                }
            }
        }

        private void Update()
        {
            if (lockLocalRotation)
            {
                transform.localRotation = Quaternion.identity;
            }

            if (!Application.isPlaying)
            {
                InitializeFramesIfNeeded();
                int maxIdx = (trackFrames != null && trackFrames.Length > 0) ? trackFrames.Length - 1 : 0;
                currentFrameIndex = Mathf.Clamp(previewFrameIndex, 0, maxIdx);
                ApplyCurrentFrame();
                return;
            }

            if (!isMoving || trackFrames == null || trackFrames.Length == 0) return;

            frameTimer += Time.deltaTime;
            float frameInterval = 1f / Mathf.Max(1f, animationFps);

            if (frameTimer >= frameInterval)
            {
                frameTimer -= frameInterval;
                currentFrameIndex = (currentFrameIndex + 1) % trackFrames.Length;
                ApplyCurrentFrame();
            }
        }

        /// <summary>
        /// Call to update movement status. Updates animation cycling.
        /// </summary>
        public void SetMoving(bool moving)
        {
            if (isMoving == moving) return;

            isMoving = moving;
            if (!isMoving)
            {
                frameTimer = 0f;
            }
        }

        private void ApplyCurrentFrame()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null && trackFrames != null && trackFrames.Length > 0)
            {
                int safeIndex = Mathf.Clamp(currentFrameIndex, 0, trackFrames.Length - 1);
                if (trackFrames[safeIndex] != null)
                {
                    spriteRenderer.sprite = trackFrames[safeIndex];
                }
            }
        }
    }
}
