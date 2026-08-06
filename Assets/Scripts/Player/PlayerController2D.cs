using BeyProject.Core;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Player
{
    public enum FacingDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6.5f;
        private float currentSpeed;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.2f, 0.5f, 0.95f);
        [SerializeField] private float footstepDistancePerStep = 0.5f;

        public FacingDirection Facing { get; private set; } = FacingDirection.Down;
        public Vector2 MoveInput { get; private set; }

        private Rigidbody2D body;
        private PlayerHealth health;
        private Vector3 lastFootstepPosition;
        private float footstepDistanceAccumulated;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<PlayerHealth>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = PlaceholderSprite.CreateSquare(fallbackColor);
            }

            lastFootstepPosition = transform.position;
        }

        private void Update()
        {
            if (UIInputLock.IsBlocked)
            {
                MoveInput = Vector2.zero;
                return;
            }

            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            MoveInput = new Vector2(x, y).normalized;

            currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

            // Chip modules can trade mobility for firepower (and vice versa), so movement has
            // to read the loadout too - otherwise a "heavy" module would only be heavy on paper.
            if (ChipManager.Instance != null)
            {
                currentSpeed *= ChipManager.Instance.GetCurrentStats().moveSpeedMultiplier;
            }

            UpdateFacing(MoveInput);
            UpdateFootsteps();
        }

        private void FixedUpdate()
        {
            // Knockback is added on top rather than assigned, so being shot shoves the player
            // even while they're holding a movement key.
            Vector2 knockback = health != null ? health.KnockbackVelocity : Vector2.zero;
            body.velocity = MoveInput * currentSpeed + knockback;
        }

        private void UpdateFacing(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
            {
                return;
            }

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                Facing = input.x > 0f ? FacingDirection.Right : FacingDirection.Left;
            }
            else
            {
                Facing = input.y > 0f ? FacingDirection.Up : FacingDirection.Down;
            }
        }

        private void UpdateFootsteps()
        {
            float moved = Vector3.Distance(transform.position, lastFootstepPosition);
            lastFootstepPosition = transform.position;
            footstepDistanceAccumulated += moved;

            while (footstepDistanceAccumulated >= footstepDistancePerStep)
            {
                footstepDistanceAccumulated -= footstepDistancePerStep;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFootstepIfSet();
                }
            }
        }
    }
}
