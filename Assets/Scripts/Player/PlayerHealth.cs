using BeyProject.Combat;
using BeyProject.Core;
using BeyProject.Overworld;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Player
{
    /// <summary>
    /// Player's combat health. On death: brief message, respawn at StartRoom with health
    /// reset. No permanent loss of components/inventory - a checkpoint respawn, not a run
    /// reset, matching the "no permanent meta upgrades" scope restriction for this milestone.
    /// Brief invulnerability after each hit keeps contact damage and overlapping hazards from
    /// draining the whole bar in a single frame.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        private const float InvulnerabilitySeconds = 0.45f;
        private const float KnockbackForce = 5.5f;
        private const float KnockbackDamping = 7f;

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private HitFlash hitFlash;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>Rises to 1 when hit and decays back to 0 - drives the HUD's damage flash.</summary>
        public float RecentDamagePulse { get; private set; }

        /// <summary>
        /// Decaying knockback velocity, added on top of movement by PlayerController2D.
        /// It has to be additive rather than a Rigidbody2D impulse: PlayerController2D
        /// assigns body.velocity outright every FixedUpdate, so an impulse would be erased
        /// before it ever moved the player.
        /// </summary>
        public Vector2 KnockbackVelocity { get; private set; }

        private bool isDying;
        private float invulnerableUntil;
        private Rigidbody2D body;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            body = GetComponent<Rigidbody2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (hitFlash == null)
            {
                hitFlash = GetComponent<HitFlash>();
            }

            if (hitFlash != null && spriteRenderer != null)
            {
                hitFlash.SetBaseColor(spriteRenderer.color);
            }
        }

        private void Update()
        {
            if (RecentDamagePulse > 0f)
            {
                RecentDamagePulse = Mathf.Max(0f, RecentDamagePulse - Time.deltaTime * 2.5f);
            }

            if (KnockbackVelocity.sqrMagnitude > 0.0001f)
            {
                KnockbackVelocity = Vector2.Lerp(KnockbackVelocity, Vector2.zero, KnockbackDamping * Time.deltaTime);
            }
            else
            {
                KnockbackVelocity = Vector2.zero;
            }
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(float amount, Vector2 hitFromPosition)
        {
            if (isDying || IsInvulnerable)
            {
                return;
            }

            CurrentHealth -= amount;
            invulnerableUntil = Time.time + InvulnerabilitySeconds;
            RecentDamagePulse = 1f;

            hitFlash?.Flash(0.14f);
            CombatFeedback.Impact(transform.position, new Color(1f, 0.4f, 0.4f));
            CameraFollow2D.RequestShake(0.18f, 0.12f);

            ApplyKnockback(hitFromPosition);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>Restores health - used by Repair Stations and on respawn.</summary>
        public void Heal(float amount)
        {
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        /// <summary>
        /// Sets health directly to a specific value with none of TakeDamage's side effects
        /// (invulnerability window, knockback, hit feedback) - for carrying the previous
        /// room's health into a freshly-loaded scene's Player instance, not for combat.
        /// </summary>
        public void RestoreHealth(float value)
        {
            CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);
        }

        private void ApplyKnockback(Vector2 hitFromPosition)
        {
            Vector2 away = (Vector2)transform.position - hitFromPosition;
            if (away.sqrMagnitude < 0.0001f)
            {
                return;
            }

            KnockbackVelocity = away.normalized * KnockbackForce;
        }

        private void Die()
        {
            isDying = true;
            CombatFeedback.Death(transform.position, new Color(0.4f, 0.7f, 1f));
            CameraFollow2D.RequestShake(0.4f, 0.3f);

            if (DialogueUI.Instance != null)
            {
                DialogueUI.Instance.Show("System", new[] { "Systems disabled - returning to start." }, OnDeathMessageComplete);
            }
            else
            {
                OnDeathMessageComplete();
            }
        }

        private void OnDeathMessageComplete()
        {
            CurrentHealth = maxHealth;
            isDying = false;
            invulnerableUntil = Time.time + InvulnerabilitySeconds;
            GameManager.Instance?.TravelToRoom("MainLobbyScene", "main_lobby_start", transform.position);
        }

        public void TakeDamage(float amount, bool bypassInvulnerability = false)
        {
            if (isDying)
            {
                return;
            }

            CurrentHealth -= amount;
            RecentDamagePulse = 1f;

            hitFlash?.Flash(0.14f);
            CombatFeedback.Impact(transform.position, new Color(1f, 0.4f, 0.4f));
            CameraFollow2D.RequestShake(0.18f, 0.12f);

            ApplyKnockback(transform.position);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }
    }
}
