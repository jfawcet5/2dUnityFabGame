using System;
using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// Objective prop: while any generator is still standing, every EnemyBase in the room is
    /// invulnerable. Turns "kill everything" into "find and break the thing keeping them
    /// alive first" while reusing the existing shooting and damage systems wholesale.
    ///
    /// EnemyBase asks the static AnyActive rather than holding a reference, because
    /// generators and enemies are placed independently per room and neither should need
    /// wiring to the other.
    /// </summary>
    public class ShieldGenerator : MonoBehaviour, IDamageable
    {
        [SerializeField] private string generatorId;
        [SerializeField] private float maxHealth = 55f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.4f, 0.9f, 0.85f);
        [SerializeField] private HitFlash hitFlash;

        public event Action Destroyed;

        private static int activeCount;

        /// <summary>True while at least one generator in the loaded scene is still alive.</summary>
        public static bool AnyActive => activeCount > 0;

        private float currentHealth;
        private bool counted;
        private Color baseColor;

        public bool GetIsDestroyed()
        {
            return !string.IsNullOrEmpty(generatorId) && SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(DestroyedFlag);
        }

        private void Awake()
        {
            if (!string.IsNullOrEmpty(generatorId) &&
                SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(DestroyedFlag))
            {
                Destroy(gameObject);
                return;
            }

            currentHealth = maxHealth;
            activeCount++;
            counted = true;

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

        private void OnDestroy()
        {
            // Balanced against the Awake increment (including the self-destroy path, which
            // returns before incrementing) so the count survives scene reloads intact.
            if (counted)
            {
                activeCount--;
                counted = false;
            }
        }

        private string DestroyedFlag => $"shield_generator_destroyed_{generatorId}";

        private void Update()
        {
            // Slow pulse so an active generator is visibly the thing powering the shields.
            if (hitFlash != null)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 3f);
                hitFlash.SetBaseColor(Color.Lerp(baseColor * 0.6f, baseColor, pulse));
            }
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(float amount, Vector2 hitFromPosition)
        {
            currentHealth -= amount;
            hitFlash?.Flash();

            if (currentHealth <= 0f)
            {
                if (!string.IsNullOrEmpty(generatorId))
                {
                    SaveSystem.Instance?.SetFlag(DestroyedFlag);
                }

                CombatFeedback.Death(transform.position, baseColor);
                Destroyed?.Invoke();
                Destroy(gameObject);
            }
        }

        public void TakeDamage(float amount, bool bypassInvulnerability = false)
        {
            throw new NotImplementedException();
        }
    }
}
