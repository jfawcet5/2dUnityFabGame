using System;
using System.Collections;
using BeyProject.Core;
using BeyProject.Data;
using BeyProject.Overworld;
using BeyProject.Player;
using UnityEngine;

namespace BeyProject.Combat
{
    public enum EnemyType
    {
        Basic,
        Defensive,
        Fast,

        // Appended rather than inserted - ThermalLeech/Capacitor/Overclocker are read from
        // already-serialized scene data the moment they're placed, and reordering would
        // reclassify any existing enemyType values already saved in a scene.
        ThermalLeech,
        Capacitor,
        Overclocker
    }

    /// <summary>
    /// One component, branching on enemyType - same "flat fields + enum" convention already
    /// used by WorldAction, rather than a subclass per enemy type. The three original
    /// archetypes (Basic/Defensive/Fast) are untouched; three more reuse the same movement/
    /// damage plumbing to create different problems instead of just more HP:
    ///
    ///   ThermalLeech - reuses Basic's chase-and-hold, tuned (via a small preferredRange on
    ///                  the placed instance) to want full contact. Its actual attack is an
    ///                  energy drain on touch rather than a ranged bolt - pressures
    ///                  energy-hungry chip builds specifically.
    ///   Capacitor    - stationary. Takes damage normally, but every few hits it telegraphs
    ///                  and then discharges a radial burst before resetting - punishes rapid,
    ///                  low-damage spam (Scatter/Burst) more than sparse heavy hits
    ///                  (Focusing Algorithm/Charge).
    ///   Overclocker  - reuses Basic's movement, never attacks directly, and periodically
    ///                  pulses a buff aura that speeds up nearby enemies - a "kill this one
    ///                  first" priority target rather than a damage threat itself.
    ///
    /// Defeat persists via the same SaveSystem-flag/self-destroy-in-Awake idiom
    /// ItemPickup/Door already use.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        private const float ShieldCycleSeconds = 3.4f;
        private const float ShieldActiveSeconds = 1.3f;
        private const float ContactDamageIntervalSeconds = 0.75f;
        private const float KnockbackDamping = 9f;
        private const float DischargeWarningSeconds = 0.4f;

        // Whisker steering: probe straight ahead first (the common case, nothing in the way),
        // then sweep these offsets outward from straight-ahead until one comes back clear. Lets
        // an enemy slide along a wall or route around a corner/prop without a navmesh - distance
        // is comfortably more than one physics step's travel even during a Fast enemy's dash.
        private const float ObstacleProbeDistance = 0.9f;
        private static readonly float[] SteeringAngleOffsets = { 0f, 35f, -35f, 70f, -70f, 105f, -105f, 140f, -140f };

        // How long an enemy will keep trying to maneuver for a clear shot before settling for
        // shooting through destructible cover instead - cover can have a lot of health, so
        // grinding it down is a last resort, not the default, but an enemy that can never find
        // an angle (e.g. boxed in by cover) still needs to eventually do something.
        private const float CoverPatienceSeconds = 2.5f;

        private static readonly ContactFilter2D BlockingContactFilter = new ContactFilter2D { useTriggers = false };
        private static readonly RaycastHit2D[] ProbeHits = new RaycastHit2D[4];

        [SerializeField] private string enemyId;
        [SerializeField] private EnemyType enemyType = EnemyType.Basic;
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float contactDamage = 8f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private HitFlash hitFlash;

        [Tooltip("Optional - rolled fresh on death. Null means this enemy never drops anything, same as today.")]
        [SerializeField] private LootPool deathLootPool;

        [Header("Ranged attack (Basic / Defensive) - also reused by Capacitor's discharge")]
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

        [Header("Thermal Leech")]
        [SerializeField] private float energyDrainAmount = 18f;

        [Header("Capacitor")]
        [SerializeField] private int dischargeHitThreshold = 4;
        [SerializeField] private int dischargeProjectileCount = 8;

        [Header("Overclocker")]
        [SerializeField] private float buffRadius = 4f;
        [SerializeField] private float buffPulseIntervalSeconds = 3f;
        [SerializeField] private float buffDurationSeconds = 2.5f;
        [SerializeField] private float buffSpeedMultiplier = 1.5f;
        [SerializeField] private float buffAttackRateMultiplier = 1.6f;

        public event Action Defeated;

        /// <summary>Clear: nothing in the way, safe to shoot. BlockedByCover: a destructible
        /// CoverObject is in the way - worth shooting only as a patience-timeout fallback, since
        /// maneuvering for an angle is preferred (cover can have a lot of health). Blocked: a
        /// wall or indestructible obstacle - never worth shooting.</summary>
        private enum LineOfSight
        {
            Clear,
            BlockedByCover,
            Blocked
        }

        private enum DashPhase
        {
            Strafe,
            Telegraph,
            Dash,
            Recover
        }

        private float currentHealth;
        private Transform player;
        private Rigidbody2D body;
        private Collider2D selfCollider;
        private Vector2 desiredVelocity;
        private float lastContactDamageTime = float.NegativeInfinity;
        private Color baseColor;
        private Vector2 knockbackVelocity;
        private float nextAttackTime;
        private DashPhase dashPhase = DashPhase.Strafe;
        private float dashPhaseEndTime;
        private Vector2 dashDirection;
        private float strafeSign = 1f;

        private int hitsSinceDischarge;
        private bool isDischarging;

        private float coverBlockedSince = -1f;

        private float nextBuffPulseTime;
        private float speedBuffMultiplier = 1f;
        private float attackRateBuffMultiplier = 1f;
        private float buffExpiresAt = float.NegativeInfinity;

        public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        private void Awake()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.HasFlag($"enemy_defeated_{enemyId}"))
            {
                Destroy(gameObject);
                return;
            }

            currentHealth = maxHealth;
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            selfCollider = GetComponent<Collider2D>();

            strafeSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            nextAttackTime = Time.time + UnityEngine.Random.Range(0.4f, attackIntervalSeconds);
            nextBuffPulseTime = Time.time + UnityEngine.Random.Range(0.5f, buffPulseIntervalSeconds);

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
            desiredVelocity = Vector2.zero;

            ApplyKnockbackDecay();
            ApplyBuffDecay();

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
                case EnemyType.Capacitor:
                    // Stationary - all of its behavior lives in TakeDamage.
                    break;
                default:
                    // Basic, ThermalLeech, Overclocker all chase-and-hold the same way;
                    // ThermalLeech wants a much shorter preferredRange (full contact) and
                    // Overclocker/ThermalLeech both have canShoot off, tuned per placed instance.
                    TickBasic(direction, distance);
                    break;
            }

            if (enemyType == EnemyType.Overclocker)
            {
                TickOverclockerAura();
            }

            UpdateTint();
        }

        /// <summary>Closes to roughly preferredRange, then holds and shoots.</summary>
        private void TickBasic(Vector2 direction, float distance)
        {
            LineOfSight lineOfSight = GetLineOfSight(direction, distance);

            // A dead band around the preferred range keeps it from jittering in and out.
            if (distance > preferredRange + 0.5f)
            {
                Move(direction, moveSpeed);
            }
            else if (distance < preferredRange - 1.5f)
            {
                Move(-direction, moveSpeed * 0.6f);
            }
            else if (lineOfSight != LineOfSight.Clear)
            {
                // In good range but can't see the player - reposition for an angle instead of
                // standing still grinding down cover that might have a lot of health.
                Vector2 perpendicular = new Vector2(-direction.y, direction.x) * strafeSign;
                Move(perpendicular, moveSpeed * 0.6f);
            }

            TryShoot(direction, distance, lineOfSight);
        }

        /// <summary>Holds a stand-off range and strafes there; shield cycles independently.</summary>
        private void TickDefensive(Vector2 direction, float distance)
        {
            LineOfSight lineOfSight = GetLineOfSight(direction, distance);

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
                TryShoot(direction, distance, lineOfSight);
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

        /// <summary>Periodically buffs every other EnemyBase within buffRadius.</summary>
        private void TickOverclockerAura()
        {
            if (Time.time < nextBuffPulseTime)
            {
                return;
            }

            nextBuffPulseTime = Time.time + buffPulseIntervalSeconds;
            CombatFeedback.Telegraph(transform.position, new Color(1f, 0.5f, 0.9f), buffRadius * 0.5f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, buffRadius);
            foreach (Collider2D hit in hits)
            {
                EnemyBase other = hit.GetComponent<EnemyBase>();
                if (other != null && other != this)
                {
                    other.ApplyBuff(buffSpeedMultiplier, buffAttackRateMultiplier, buffDurationSeconds);
                }
            }
        }

        /// <summary>
        /// Applied by an Overclocker's aura pulse. Taking the max of current/incoming keeps a
        /// fresh weaker pulse from cutting a stronger one short; extending expiry refreshes
        /// uptime rather than compounding the multiplier under overlapping auras.
        /// </summary>
        public void ApplyBuff(float speedMultiplier, float attackRateMultiplier, float duration)
        {
            speedBuffMultiplier = Mathf.Max(speedBuffMultiplier, speedMultiplier);
            attackRateBuffMultiplier = Mathf.Max(attackRateBuffMultiplier, attackRateMultiplier);
            buffExpiresAt = Mathf.Max(buffExpiresAt, Time.time + duration);
        }

        private void ApplyBuffDecay()
        {
            if (Time.time > buffExpiresAt)
            {
                speedBuffMultiplier = 1f;
                attackRateBuffMultiplier = 1f;
            }
        }

        private void TryShoot(Vector2 direction, float distance, LineOfSight lineOfSight)
        {
            if (!canShoot || Time.time < nextAttackTime || distance > preferredRange + 3f)
            {
                return;
            }

            if (lineOfSight == LineOfSight.Blocked)
            {
                // A wall or indestructible obstacle - never worth shooting into. Not consumed,
                // so the enemy fires immediately once it maneuvers into the clear instead of
                // waiting out a wasted cooldown cycle.
                coverBlockedSince = -1f;
                return;
            }

            if (lineOfSight == LineOfSight.BlockedByCover)
            {
                if (coverBlockedSince < 0f)
                {
                    coverBlockedSince = Time.time;
                }

                // Prefer maneuvering for a clear shot over grinding down cover that might have a
                // lot of health - only fall back to shooting through it once repositioning has
                // had a real chance to find an angle and hasn't.
                if (Time.time - coverBlockedSince < CoverPatienceSeconds)
                {
                    return;
                }
            }
            else
            {
                coverBlockedSince = -1f;
            }

            nextAttackTime = Time.time + attackIntervalSeconds / Mathf.Max(0.05f, attackRateBuffMultiplier);

            Vector3 spawn = transform.position + (Vector3)(direction * 0.5f);
            CombatFeedback.MuzzleFlash(spawn, direction, new Color(1f, 0.6f, 0.4f), 0.8f);
            Projectile.Spawn(spawn, direction, projectileSpeed, projectileDamage,
                isPlayerOwned: false, homing: false, sizeMultiplier: 0.9f, color: new Color(1f, 0.55f, 0.35f));
        }

        /// <summary>
        /// Classifies the nearest solid (non-trigger) thing between here and the player - see
        /// the LineOfSight enum for what each outcome means to TryShoot/TickBasic.
        /// </summary>
        private LineOfSight GetLineOfSight(Vector2 direction, float distance)
        {
            int count = Physics2D.Raycast(transform.position, direction, BlockingContactFilter, ProbeHits, distance);

            RaycastHit2D? nearest = null;
            for (int i = 0; i < count; i++)
            {
                // The ray starts at transform.position, which sits inside this enemy's own
                // (now-solid) collider - without excluding it, every probe "hits" itself at
                // distance ~0 and nothing would ever read as clear.
                if (ProbeHits[i].collider == selfCollider)
                {
                    continue;
                }

                if (nearest == null || ProbeHits[i].distance < nearest.Value.distance)
                {
                    nearest = ProbeHits[i];
                }
            }

            if (nearest == null || nearest.Value.collider.GetComponent<PlayerHealth>() != null)
            {
                return LineOfSight.Clear;
            }

            CoverObject cover = nearest.Value.collider.GetComponent<CoverObject>();
            return cover != null && cover.Destructible ? LineOfSight.BlockedByCover : LineOfSight.Blocked;
        }

        /// <summary>
        /// Sets the velocity Tick*'s state machine wants this frame - actually applied to the
        /// Rigidbody2D in FixedUpdate, so movement goes through physics collision resolution
        /// (walls/props/other enemies) instead of teleporting through them like a raw
        /// transform.position write would.
        /// </summary>
        private void Move(Vector2 direction, float speed)
        {
            desiredVelocity = SteerAroundObstacles(direction.normalized) * speed * speedBuffMultiplier;
        }

        /// <summary>
        /// Whisker steering: if the desired direction is clear, use it unchanged (the common
        /// case). Otherwise sweep SteeringAngleOffsets until one probes clear, so the enemy
        /// slides along a wall/corner instead of shoving uselessly into it forever. Returns zero
        /// only when every candidate is blocked (boxed in on all sides) - holding position that
        /// frame rather than pushing into whatever's closest.
        /// </summary>
        private Vector2 SteerAroundObstacles(Vector2 desiredDirection)
        {
            if (desiredDirection.sqrMagnitude < 0.0001f)
            {
                return desiredDirection;
            }

            foreach (float angleOffset in SteeringAngleOffsets)
            {
                Vector2 candidate = Mathf.Approximately(angleOffset, 0f)
                    ? desiredDirection
                    : ((Vector2)(Quaternion.Euler(0f, 0f, angleOffset) * desiredDirection));

                if (!IsBlocked(candidate))
                {
                    return candidate;
                }
            }

            return Vector2.zero;
        }

        /// <summary>True if something solid other than the player sits within ObstacleProbeDistance
        /// along direction - the player itself must never register as an obstacle to steer away from.</summary>
        private bool IsBlocked(Vector2 direction)
        {
            int count = Physics2D.Raycast(transform.position, direction, BlockingContactFilter, ProbeHits, ObstacleProbeDistance);
            for (int i = 0; i < count; i++)
            {
                Collider2D hitCollider = ProbeHits[i].collider;
                if (hitCollider == selfCollider)
                {
                    continue;
                }

                if (hitCollider.GetComponent<PlayerHealth>() == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void FixedUpdate()
        {
            body.velocity = desiredVelocity + knockbackVelocity;
        }

        private void ApplyKnockbackDecay()
        {
            if (knockbackVelocity.sqrMagnitude < 0.0001f)
            {
                knockbackVelocity = Vector2.zero;
                return;
            }

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
            else if (Time.time < buffExpiresAt)
            {
                // Overclocker's buff reads as a warm magenta glow - distinct from every other
                // tint state so "this one is empowered" is legible at a glance.
                tint = Color.Lerp(baseColor, new Color(1f, 0.5f, 0.9f), 0.5f);
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

            if (enemyType == EnemyType.Capacitor && currentHealth > 0f)
            {
                RegisterCapacitorHit();
            }

            if (currentHealth <= 0f)
            {
                SaveSystem.Instance?.SetFlag($"enemy_defeated_{enemyId}");
                CombatFeedback.Death(transform.position, baseColor);
                TrySpawnDrop();
                Defeated?.Invoke();
                Destroy(gameObject);
            }
        }

        /// <summary>Rolled fresh on every death (no sticky cache - enemy_defeated_{enemyId}
        /// already guarantees this enemy can never die, and therefore roll, twice in one run).
        /// Most enemies leave deathLootPool unset and drop nothing, same as today.</summary>
        private void TrySpawnDrop()
        {
            if (deathLootPool == null)
            {
                return;
            }

            LootPoolEntry picked = deathLootPool.Roll();
            if (picked?.item == null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>("LevelDesign/ItemPickup");
            if (prefab == null)
            {
                return;
            }

            GameObject drop = Instantiate(prefab, transform.position, Quaternion.identity);
            drop.GetComponent<ItemPickup>()?.InitializeAsDrop(picked.item, picked.quantity);
        }

        /// <summary>
        /// Every dischargeHitThreshold hits, telegraphs and then discharges a radial burst -
        /// on top of, not instead of, normal health loss. Punishes rapid low-damage spam
        /// (more hits landed = more discharges) more than sparse heavy hits.
        /// </summary>
        private void RegisterCapacitorHit()
        {
            hitsSinceDischarge++;

            if (hitsSinceDischarge >= dischargeHitThreshold && !isDischarging)
            {
                hitsSinceDischarge = 0;
                StartCoroutine(DischargeAfterDelay());
            }
        }

        private IEnumerator DischargeAfterDelay()
        {
            isDischarging = true;
            CombatFeedback.Telegraph(transform.position, new Color(1f, 0.8f, 0.3f), 2f);

            yield return new WaitForSeconds(DischargeWarningSeconds);

            // A killing blow landed during the warning window - nothing left to discharge from.
            if (currentHealth > 0f)
            {
                CombatFeedback.Explosion(transform.position, 1.5f, baseColor);

                for (int i = 0; i < dischargeProjectileCount; i++)
                {
                    float angle = (360f / dischargeProjectileCount) * i;
                    Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
                    Projectile.Spawn(transform.position, dir, projectileSpeed, projectileDamage,
                        isPlayerOwned: false, homing: false, sizeMultiplier: 0.9f, color: new Color(1f, 0.8f, 0.3f));
                }
            }

            isDischarging = false;
        }

        /// <summary>
        /// Collision-based rather than trigger-based: the contact collider is now solid (so it
        /// physically stops at walls/props - see the RequireComponent<Rigidbody2D> change
        /// above), and a solid collider never fires OnTrigger* events.
        /// </summary>
        private void OnCollisionStay2D(Collision2D collision)
        {
            if (Time.time - lastContactDamageTime < ContactDamageIntervalSeconds)
            {
                return;
            }

            var playerHealth = collision.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                lastContactDamageTime = Time.time;
                playerHealth.TakeDamage(contactDamage, transform.position);

                if (enemyType == EnemyType.ThermalLeech)
                {
                    var playerCombat = collision.collider.GetComponent<PlayerCombat>();
                    playerCombat?.DrainEnergy(energyDrainAmount);
                }
            }
        }

        public bool GetIsDefeated()
        {
            return SaveSystem.Instance != null && SaveSystem.Instance.HasFlag($"enemy_defeated_{enemyId}");
        }

        public void TakeDamage(float amount, bool bypassInvulnerability = false)
        {
            throw new NotImplementedException();
        }
    }
}
