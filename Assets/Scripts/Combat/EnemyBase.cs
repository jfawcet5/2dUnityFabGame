using System;
using BeyProject.Core;
using BeyProject.Player;
using UnityEngine;

namespace BeyProject.Combat
{
    public enum EnemyType
    {
        Basic,
        Defensive,
        Fast
    }

    /// <summary>
    /// One component, branching on enemyType - same "flat fields + enum" convention already
    /// used by WorldAction, rather than a subclass per enemy type. The three archetypes are
    /// kept (Phase 4 says improve them, not replace them) but each now has a distinct
    /// decision loop instead of three speeds of "walk at the player":
    ///
    ///   Basic     - closes to a mid range and lobs slow aimed bolts. The baseline threat;
    ///               punishes standing still, ignorable if you keep moving.
    ///   Defensive - holds a preferred stand-off range, backing away if crowded, and cycles a
    ///               telegraphed shield that halves incoming damage. Rewards burst damage
    ///               timed to the gap, punishes steady chip damage.
    ///   Fast      - strafes in a circle, then telegraphs and dashes through the player.
    ///               Rewards tracking and repositioning, punishes tunnel vision.
    ///
    /// Defeat persists via the same SaveSystem-flag/self-destroy-in-Awake idiom
    /// ItemPickup/Door already use.
    /// </summary>
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        private const float ShieldCycleSeconds = 3.4f;
        private const float ShieldActiveSeconds = 1.3f;
        private const float ContactDamageIntervalSeconds = 0.75f;
        private const float KnockbackDamping = 9f;

        [SerializeField] private string enemyId;
        [SerializeField] private EnemyType enemyType = EnemyType.Basic;
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float contactDamage = 8f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private HitFlash hitFlash;

        [Header("Ranged attack (Basic / Defensive)")]
        [SerializeField] private bool canShoot = true;
        [SerializeField] private float preferredRange = 4.5f;
        [SerializeField] private float attackIntervalSeconds = 2.2f;
        [SerializeField] private float projectileSpeed = 4.5f;
        [SerializeField] private float projectileDamage = 7f;

        [Header("Dash (Fast)")]
        [SerializeField] private float dashSpeedMultiplier = 3.2f;
        [SerializeField] private float dashTelegraphSeconds = 0.45f;
        [SerializeField] private float dashSeconds = 0.4f;
        [SerializeField] private float dashRecoverSeconds = 0.8f;

        public event Action Defeated;

        private enum DashPhase
        {
            Strafe,
            Telegraph,
            Dash,
            Recover
        }

        private float currentHealth;
        private Transform player;
        private float lastContactDamageTime = float.NegativeInfinity;
        private Color baseColor;
        private Vector2 knockbackVelocity;
        private float nextAttackTime;
        private DashPhase dashPhase = DashPhase.Strafe;
        private float dashPhaseEndTime;
        private Vector2 dashDirection;
        private float strafeSign = 1f;

        public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        private void Awake()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.HasFlag($"enemy_defeated_{enemyId}"))
            {
                Destroy(gameObject);
                return;
            }

            currentHealth = maxHealth;
            strafeSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            nextAttackTime = Time.time + UnityEngine.Random.Range(0.4f, attackIntervalSeconds);

            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            if (hitFlash == null)
            {
                hitFlash = GetComponent<HitFlash>();
            }

            hitFlash?.SetBaseColor(baseColor);
        }

        private void Start()
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.transform;
            }
        }

        private void Update()
        {
            ApplyKnockbackDecay();

            if (player == null)
            {
                return;
            }

            Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
            float distance = toPlayer.magnitude;
            if (distance < 0.0001f)
            {
                return;
            }

            Vector2 direction = toPlayer / distance;

            switch (enemyType)
            {
                case EnemyType.Fast:
                    TickFast(direction, distance);
                    break;
                case EnemyType.Defensive:
                    TickDefensive(direction, distance);
                    break;
                default:
                    TickBasic(direction, distance);
                    break;
            }

            UpdateTint();
        }

        /// <summary>Closes to roughly preferredRange, then holds and shoots.</summary>
        private void TickBasic(Vector2 direction, float distance)
        {
            // A dead band around the preferred range keeps it from jittering in and out.
            if (distance > preferredRange + 0.5f)
            {
                Move(direction, moveSpeed);
            }
            else if (distance < preferredRange - 1.5f)
            {
                Move(-direction, moveSpeed * 0.6f);
            }

            TryShoot(direction, distance);
        }

        /// <summary>Holds a stand-off range and strafes there; shield cycles independently.</summary>
        private void TickDefensive(Vector2 direction, float distance)
        {
            if (distance > preferredRange + 0.5f)
            {
                Move(direction, moveSpeed);
            }
            else if (distance < preferredRange - 1f)
            {
                Move(-direction, moveSpeed);
            }
            else
            {
                Vector2 perpendicular = new Vector2(-direction.y, direction.x) * strafeSign;
                Move(perpendicular, moveSpeed * 0.5f);
            }

            // Only shoot when unshielded - the shield window is the price of its safety, and
            // that alternation is what makes it read as a rhythm rather than a health sponge.
            if (!IsShielded)
            {
                TryShoot(direction, distance);
            }
        }

        /// <summary>Circle-strafe -> telegraph -> commit to a dash -> recover, on a loop.</summary>
        private void TickFast(Vector2 direction, float distance)
        {
            switch (dashPhase)
            {
                case DashPhase.Strafe:
                {
                    // Orbit inward: perpendicular for the circling, a little inward pull so it
                    // still closes distance instead of orbiting forever at range.
                    Vector2 perpendicular = new Vector2(-direction.y, direction.x) * strafeSign;
                    Vector2 orbit = (perpendicular + direction * 0.55f).normalized;
                    Move(orbit, moveSpeed);

                    if (distance < preferredRange + 2f && Time.time >= dashPhaseEndTime)
                    {
                        dashPhase = DashPhase.Telegraph;
                        dashPhaseEndTime = Time.time + dashTelegraphSeconds;
                        dashDirection = direction;
                        CombatFeedback.Telegraph(transform.position, new Color(1f, 0.95f, 0.4f), 1.5f);
                    }
                    break;
                }

                case DashPhase.Telegraph:
                {
                    // Winds up in place, tracking loosely so the dash isn't trivially dodged
                    // by standing still, but committed enough to be dodged by moving.
                    dashDirection = Vector2.Lerp(dashDirection, direction, Time.deltaTime * 3f).normalized;
                    if (Time.time >= dashPhaseEndTime)
                    {
                        dashPhase = DashPhase.Dash;
                        dashPhaseEndTime = Time.time + dashSeconds;
                    }
                    break;
                }

                case DashPhase.Dash:
                {
                    Move(dashDirection, moveSpeed * dashSpeedMultiplier);
                    if (Time.time >= dashPhaseEndTime)
                    {
                        dashPhase = DashPhase.Recover;
                        dashPhaseEndTime = Time.time + dashRecoverSeconds;
                        strafeSign = -strafeSign;
                    }
                    break;
                }

                default:
                {
                    if (Time.time >= dashPhaseEndTime)
                    {
                        dashPhase = DashPhase.Strafe;
                        dashPhaseEndTime = Time.time + UnityEngine.Random.Range(0.6f, 1.4f);
                    }
                    break;
                }
            }
        }

        private void TryShoot(Vector2 direction, float distance)
        {
            if (!canShoot || Time.time < nextAttackTime || distance > preferredRange + 3f)
            {
                return;
            }

            nextAttackTime = Time.time + attackIntervalSeconds;

            Vector3 spawn = transform.position + (Vector3)(direction * 0.5f);
            CombatFeedback.MuzzleFlash(spawn, direction, new Color(1f, 0.6f, 0.4f), 0.8f);
            Projectile.Spawn(spawn, direction, projectileSpeed, projectileDamage,
                isPlayerOwned: false, homing: false, sizeMultiplier: 0.9f, color: new Color(1f, 0.55f, 0.35f));
        }

        private void Move(Vector2 direction, float speed)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        private void ApplyKnockbackDecay()
        {
            if (knockbackVelocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.position += (Vector3)(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, KnockbackDamping * Time.deltaTime);
        }

        private void UpdateTint()
        {
            if (hitFlash == null)
            {
                return;
            }

            Color tint = baseColor;

            if (IsShielded)
            {
                tint = Color.Lerp(baseColor, new Color(0.8f, 0.95f, 1f), 0.55f);
            }
            else if (enemyType == EnemyType.Fast && dashPhase == DashPhase.Telegraph)
            {
                tint = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.4f), 0.7f);
            }
            else if (IsInvulnerable)
            {
                // Shield-generator protection reads as a dull, drained tint - visibly
                // different from the Defensive enemy's own bright shield window.
                tint = Color.Lerp(baseColor, new Color(0.35f, 0.4f, 0.45f), 0.6f);
            }

            hitFlash.SetBaseColor(tint);
        }

        private bool IsShielded => enemyType == EnemyType.Defensive && (Time.time % ShieldCycleSeconds) < ShieldActiveSeconds;

        /// <summary>Shield generators make every enemy in the room immune until they're down.</summary>
        private bool IsInvulnerable => ShieldGenerator.AnyActive;

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(float amount, Vector2 hitFromPosition)
        {
            if (IsInvulnerable)
            {
                hitFlash?.Flash(0.05f);
                CombatFeedback.Impact(transform.position, new Color(0.5f, 0.85f, 0.9f));
                return;
            }

            float actual = IsShielded ? amount * 0.5f : amount;
            currentHealth -= actual;

            hitFlash?.Flash();

            Vector2 away = (Vector2)transform.position - hitFromPosition;
            if (away.sqrMagnitude > 0.0001f)
            {
                // Scaled by damage so a Focusing Algorithm slug visibly shoves, while a
                // Parallel Processing pellet only nudges.
                knockbackVelocity += away.normalized * Mathf.Clamp(actual * 0.35f, 1f, 7f);
            }

            if (currentHealth <= 0f)
            {
                SaveSystem.Instance?.SetFlag($"enemy_defeated_{enemyId}");
                CombatFeedback.Death(transform.position, baseColor);
                Defeated?.Invoke();
                Destroy(gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time - lastContactDamageTime < ContactDamageIntervalSeconds)
            {
                return;
            }

            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                lastContactDamageTime = Time.time;
                playerHealth.TakeDamage(contactDamage, transform.position);
            }
        }

        public bool GetIsDefeated()
        {
            return SaveSystem.Instance != null && SaveSystem.Instance.HasFlag($"enemy_defeated_{enemyId}");
        }
    }
}
