using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// Owns its SpriteRenderer's color outright: callers push a "resting" tint via
    /// SetBaseColor and request a white flash via Flash. Applied in LateUpdate so it always
    /// wins over anything writing color in Update (EnemyBase's shield tint, for instance) -
    /// two systems fighting over the same renderer.color was the alternative, and it loses
    /// flashes at random depending on script execution order.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        private const float DefaultFlashSeconds = 0.09f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = Color.white;

        private Color baseColor = Color.white;
        private float flashTimer;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }
        }

        public void SetBaseColor(Color color)
        {
            baseColor = color;
        }

        public void Flash(float seconds = DefaultFlashSeconds)
        {
            flashTimer = Mathf.Max(flashTimer, seconds);
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                spriteRenderer.color = flashColor;
            }
            else
            {
                spriteRenderer.color = baseColor;
            }
        }
    }
}
