using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// Barrel / power cell that detonates when shot, dealing radius damage to everything
    /// nearby - enemies AND the player. Projectile.CanDamage deliberately lets both sides
    /// damage these, so barrels are a shared hazard rather than a free player tool: standing
    /// next to one is a mistake regardless of who fired.
    /// </summary>
    public class ExplosiveObject : MonoBehaviour, IDamageable
    {
        [SerializeField] private string explosiveId;
        [SerializeField] private float maxHealth = 12f;
        [SerializeField] private float explosionRadius = 2.6f;
        [SerializeField] private float explosionDamage = 34f;
        [SerializeField] private float fuseSeconds = 0.35f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.9f, 0.45f, 0.15f);
        [SerializeField] private HitFlash hitFlash;

        private float currentHealth;
        private bool isFuseLit;
        private float fuseRemaining;
        private Color baseColor;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(explosiveId) &&
                SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(DetonatedFlag))
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

        private string DetonatedFlag => $"explosive_detonated_{explosiveId}";

        private void Update()
        {
            if (!isFuseLit)
            {
                return;
            }

            fuseRemaining -= Time.deltaTime;

            // Flash faster as the fuse runs down - the tell that gives the player time to move.
            float blinkRate = Mathf.Lerp(24f, 6f, Mathf.Clamp01(fuseRemaining / Mathf.Max(fuseSeconds, 0.01f)));
            bool bright = Mathf.Sin(Time.time * blinkRate) > 0f;
            hitFlash?.SetBaseColor(bright ? Color.Lerp(baseColor, Color.white, 0.75f) : baseColor);

            if (fuseRemaining <= 0f)
            {
                Detonate();
            }
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(float amount, Vector2 hitFromPosition)
        {
            if (isFuseLit)
            {
                return;
            }

            currentHealth -= amount;
            hitFlash?.Flash();

            if (currentHealth <= 0f)
            {
                isFuseLit = true;
                fuseRemaining = fuseSeconds;
            }
        }

        private void Detonate()
        {
            if (!string.IsNullOrEmpty(explosiveId))
            {
                SaveSystem.Instance?.SetFlag(DetonatedFlag);
            }

            CombatFeedback.Explosion(transform.position, explosionRadius, fallbackColor);

            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, explosionRadius))
            {
                var damageable = hit.GetComponent<IDamageable>();

                // Skip self - already mid-detonation - but chain into other explosives so a
                // cluster goes up together.
                if (damageable == null || ReferenceEquals(damageable, this))
                {
                    continue;
                }

                // Falloff: full damage at the centre, a third of it at the edge.
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                float falloff = Mathf.Lerp(1f, 0.33f, Mathf.Clamp01(distance / explosionRadius));
                damageable.TakeDamage(explosionDamage * falloff, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
