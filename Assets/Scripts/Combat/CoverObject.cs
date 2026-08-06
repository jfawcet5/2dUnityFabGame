using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// Solid prop that blocks movement and projectiles for both sides - Projectile dies on
    /// any non-trigger collider, so cover needs no special-casing there. Optionally
    /// destructible: set destructible + maxHealth and it becomes a IDamageable that can be
    /// shot away, which is what turns a static room into one where positioning decays over
    /// the fight. Destruction persists through the same SaveSystem-flag/self-destroy-in-Awake
    /// idiom as EnemyBase/ItemPickup/Door.
    /// </summary>
    public class CoverObject : MonoBehaviour, IDamageable
    {
        [SerializeField] private string coverId;
        [SerializeField] private bool destructible = true;
        [SerializeField] private float maxHealth = 40f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] private HitFlash hitFlash;

        private float currentHealth;
        private Color baseColor;

        private void Awake()
        {
            if (destructible && !string.IsNullOrEmpty(coverId) &&
                SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(DestroyedFlag))
            {
                Destroy(gameObject);
                return;
            }

            currentHealth = maxHealth;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                if (spriteRenderer.sprite == null)
                {
                    spriteRenderer.sprite = PlaceholderSprite.CreateSquare(fallbackColor);
                }

                baseColor = spriteRenderer.color;
            }

            if (hitFlash == null)
            {
                hitFlash = GetComponent<HitFlash>();
            }

            hitFlash?.SetBaseColor(baseColor);
        }

        private string DestroyedFlag => $"cover_destroyed_{coverId}";

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(float amount, Vector2 hitFromPosition)
        {
            if (!destructible)
            {
                // Indestructible cover still eats the shot (Projectile stops on the solid
                // collider) - it just doesn't degrade.
                return;
            }

            currentHealth -= amount;
            hitFlash?.Flash();

            // Darken as it degrades so damaged cover reads as "about to go" before it does.
            if (spriteRenderer != null && maxHealth > 0f)
            {
                float wear = Mathf.Clamp01(currentHealth / maxHealth);
                Color worn = Color.Lerp(baseColor * 0.45f, baseColor, wear);
                worn.a = baseColor.a;
                hitFlash?.SetBaseColor(worn);
                if (hitFlash == null)
                {
                    spriteRenderer.color = worn;
                }
            }

            if (currentHealth <= 0f)
            {
                if (!string.IsNullOrEmpty(coverId))
                {
                    SaveSystem.Instance?.SetFlag(DestroyedFlag);
                }

                CombatFeedback.Death(transform.position, baseColor);
                Destroy(gameObject);
            }
        }
    }
}
