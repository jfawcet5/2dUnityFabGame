using UnityEngine;

namespace BeyProject.Player
{
    /// <summary>
    /// Drives the player's SpriteRenderer from a 4-direction x 4-frame walk sheet, reading
    /// PlayerController2D's existing public Facing/MoveInput instead of duplicating movement
    /// or input logic. Purely additive - PlayerController2D is untouched, and HitFlash still
    /// owns renderer.color exclusively, so the two never fight over the same property.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        private const float SecondsPerFrame = 0.12f;
        private const float MovingThresholdSqr = 0.01f;

        [SerializeField] private Sprite[] downFrames;
        [SerializeField] private Sprite[] upFrames;
        [SerializeField] private Sprite[] leftFrames;
        [SerializeField] private Sprite[] rightFrames;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private PlayerController2D controller;
        private FacingDirection lastFacing = FacingDirection.Down;
        private float frameTimer;
        private int frameIndex;

        private void Awake()
        {
            controller = GetComponent<PlayerController2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (controller == null || spriteRenderer == null)
            {
                return;
            }

            FacingDirection facing = controller.Facing;
            if (facing != lastFacing)
            {
                lastFacing = facing;
                frameIndex = 0;
                frameTimer = 0f;
            }

            bool isMoving = controller.MoveInput.sqrMagnitude > MovingThresholdSqr;
            if (isMoving)
            {
                frameTimer += Time.deltaTime;
                while (frameTimer >= SecondsPerFrame)
                {
                    frameTimer -= SecondsPerFrame;
                    frameIndex = (frameIndex + 1) % 4;
                }
            }
            else
            {
                frameIndex = 0;
                frameTimer = 0f;
            }

            Sprite[] frames = FramesFor(facing);
            if (frames != null && frameIndex < frames.Length && frames[frameIndex] != null)
            {
                spriteRenderer.sprite = frames[frameIndex];
            }
        }

        private Sprite[] FramesFor(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.Up: return upFrames;
                case FacingDirection.Left: return leftFrames;
                case FacingDirection.Right: return rightFrames;
                default: return downFrames;
            }
        }
    }
}
