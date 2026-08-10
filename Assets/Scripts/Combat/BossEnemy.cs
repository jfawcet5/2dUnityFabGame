using System.Collections;
using BeyProject.Core;
using BeyProject.Overworld;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// "Thermal Runaway Core" - a three-phase encounter rather than one health pool with a
    /// ring attack. Its own component, not an EnemyBase variant, because it's driven by a
    /// scripted attack coroutine instead of a chase loop.
    ///
    /// Phase 1 (100-66%): slow ring bursts. Teaches the pattern - find the gap, walk through.
    /// Phase 2 ( 66-33%): spiral bursts plus aimed spreads, and the arena hazards come online.
    ///                    Standing still stops working; the floor is now part of the fight.
    /// Phase 3 ( 33-0% ): faster, denser attacks and a support enemy pair.
    ///
    /// After each attack it overheats: a fixed vulnerability window where it stops firing and
    /// takes double damage. That window - not raw DPS - is what makes different chip builds
    /// resolve the fight differently. A Focusing Algorithm build empties it in a few windows;
    /// a Parallel Processing build needs most of them but survives the misses better.
    ///
    /// Same defeat-flag/self-destroy-in-Awake persistence idiom as EnemyBase/ItemPickup/Door.
    /// </summary>
    public class BossEnemy : MonoBehaviour, IDamageable
    {
        private const float VulnerableDamageMultiplier = 2f;
        private const float VulnerableSeconds = 1.8f;

        [SerializeField] private string bossId;
        [SerializeField] private float maxHealth = 260f;
        [SerializeField] private float attackIntervalSeconds = 2.5f;
        [SerializeField] private int projectilesPerBurst = 8;
        [SerializeField] private float projectileSpeed = 3.5f;
        [SerializeField] private float projectileDamage = 10f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private HitFlash hitFlash;

        [Header("Phase 2+ arena hazards - activated on phase transition")]
        [SerializeField] private GameObject[] phaseTwoHazards;
        [SerializeField] private GameObject[] phaseThreeHazards;

        [Header("Phase 3 support enemies - activated on phase transition")]
        [SerializeField] private GameObject[] phaseThreeSupportEnemies;

        [Header("Return trip after victory")]
        [SerializeField] private string returnSceneName = "Lobby";
        [SerializeField] private string returnSpawnPointId = "lobby_from_startroom";
        [SerializeField] private Vector2 returnFallbackPosition;

        /// <summary>The live boss, or null - lets CombatHUD show a boss bar without wiring.</summary>
        public static BossEnemy Active { get; private set; }

        public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        public bool IsVulnerable => Time.time < vulnerableUntil;
        public int Phase { get; private set; } = 1;
        public string DisplayName => "Thermal Runaway Core";

        [SerializeField]
        private float currentHealth;
        private bool isDefeated;
        private float vulnerableUntil;
        private float spiralAngle;
        private Color baseColor;
        private Transform player;

        private void Awake()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.HasFlag($"boss_defeated_{bossId}"))
            {
                Destroy(gameObject);
                return;
            }

            currentHealth = maxHealth;
            Active = this;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            if (hitFlash == null)
            {
                hitFlash = GetComponent<HitFlash>();
            }

            hitFlash?.SetBaseColor(baseColor);

            // Hazards and adds stay dormant until their phase - the arena starts clean so
            // phase 2 reads as an escalation rather than as the room having always been lethal.
            SetGroupActive(phaseTwoHazards, false);
            SetGroupActive(phaseThreeHazards, false);
            SetGroupActive(phaseThreeSupportEnemies, false);
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Start()
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.transform;
            }

            StartCoroutine(AttackLoop());
        }

        private void Update()
        {
            if (hitFlash == null)
            {
                return;
            }

            if (IsVulnerable)
            {
                // Bright, pulsing, unmistakable - the window has to be readable at a glance
                // or the whole encounter reads as an unresponsive health sponge.
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 14f);
                hitFlash.SetBaseColor(Color.Lerp(baseColor, new Color(1f, 0.95f, 0.6f), 0.4f + 0.4f * pulse));
            }
            else
            {
                // Runs visibly hotter each phase.
                float heat = (Phase - 1) * 0.22f;
                hitFlash.SetBaseColor(Color.Lerp(baseColor, new Color(1f, 0.4f, 0.15f), heat));
            }
        }

        private IEnumerator AttackLoop()
        {
            while (!isDefeated)
            {
                yield return new WaitForSeconds(CurrentAttackInterval);
                if (isDefeated)
                {
                    yield break;
                }

                yield return StartCoroutine(PerformPhaseAttack());
                if (isDefeated)
                {
                    yield break;
                }

                // Overheat: the punish window that follows every attack.
                vulnerableUntil = Time.time + VulnerableSeconds;
                CombatFeedback.Telegraph(transform.position, new Color(1f, 0.95f, 0.6f), 3f);
                yield return new WaitForSeconds(VulnerableSeconds);
            }
        }

        private float CurrentAttackInterval => Phase switch
        {
            3 => attackIntervalSeconds * 0.55f,
            2 => attackIntervalSeconds * 0.78f,
            _ => attackIntervalSeconds
        };

        private IEnumerator PerformPhaseAttack()
        {
            switch (Phase)
            {
                case 3:
                    FireRingBurst(projectilesPerBurst + 6, 0f);
                    yield return new WaitForSeconds(0.35f);
                    FireAimedSpread(5, 26f);
                    yield return new WaitForSeconds(0.35f);
                    FireSpiral(10);
                    break;

                case 2:
                    FireSpiral(8);
                    yield return new WaitForSeconds(0.4f);
                    FireAimedSpread(3, 18f);
                    break;

                default:
                    FireRingBurst(projectilesPerBurst, 0f);
                    break;
            }
        }

        private void FireRingBurst(int count, float angleOffsetDegrees)
        {
            CameraFollow2D.RequestShake(0.14f, 0.07f);

            for (int i = 0; i < count; i++)
            {
                float angle = ((360f / count) * i + angleOffsetDegrees) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnBossProjectile(direction, projectileSpeed);
            }
        }

        /// <summary>Ring fired at a rotating offset, so successive bursts sweep the arena.</summary>
        private void FireSpiral(int count)
        {
            spiralAngle += 23f;
            FireRingBurst(count, spiralAngle);
        }

        /// <summary>Aimed fan - punishes standing directly opposite the core.</summary>
        private void FireAimedSpread(int count, float spreadDegrees)
        {
            if (player == null)
            {
                return;
            }

            Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            float start = -(spreadDegrees * (count - 1)) / 2f;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, start + spreadDegrees * i) * toPlayer;
                SpawnBossProjectile(direction, projectileSpeed * 1.35f);
            }
        }

        private void SpawnBossProjectile(Vector2 direction, float speed)
        {
            Vector3 spawn = transform.position + (Vector3)(direction * 0.9f);
            Projectile.Spawn(spawn, direction, speed, projectileDamage,
                isPlayerOwned: false, homing: false, sizeMultiplier: 1.2f, color: new Color(0.9f, 0.35f, 0.2f));
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(float amount, Vector2 hitFromPosition)
        {
            if (isDefeated)
            {
                return;
            }

            float actual = IsVulnerable ? amount * VulnerableDamageMultiplier : amount;
            currentHealth -= actual;
            hitFlash?.Flash();

            UpdatePhase();

            if (currentHealth <= 0f)
            {
                Defeat();
            }
        }

        private void UpdatePhase()
        {
            int target = HealthFraction <= 0.33f ? 3 : HealthFraction <= 0.66f ? 2 : 1;
            if (target <= Phase)
            {
                return;
            }

            Phase = target;

            CombatFeedback.Explosion(transform.position, 2.2f, new Color(1f, 0.5f, 0.2f));
            CameraFollow2D.RequestShake(0.5f, 0.3f);

            // Ending the vulnerability window on transition stops a big burst from carrying
            // its double-damage bonus across a phase boundary for free.
            vulnerableUntil = 0f;

            if (Phase >= 2)
            {
                SetGroupActive(phaseTwoHazards, true);
                RoomTitleUI.Instance?.Show("Thermal Runaway - Containment Failing");
            }

            if (Phase >= 3)
            {
                SetGroupActive(phaseThreeHazards, true);
                SetGroupActive(phaseThreeSupportEnemies, true);
                RoomTitleUI.Instance?.Show("Thermal Runaway - Critical");
            }
        }

        private void Defeat()
        {
            isDefeated = true;
            StopAllCoroutines();
            SaveSystem.Instance?.SetFlag($"boss_defeated_{bossId}");

            SetGroupActive(phaseTwoHazards, false);
            SetGroupActive(phaseThreeHazards, false);

            CombatFeedback.Explosion(transform.position, 4f, new Color(1f, 0.6f, 0.2f));
            CameraFollow2D.RequestShake(0.8f, 0.4f);

            DialogueUI.Instance?.Show(DisplayName,
                new[] { "The core destabilizes and powers down.", "You built a chip, and it changed how you play." },
                OnVictoryDialogueComplete);
        }

        private void OnVictoryDialogueComplete()
        {
            GameManager.Instance?.TravelToRoom(returnSceneName, returnSpawnPointId, returnFallbackPosition);
            Destroy(gameObject);
        }

        private static void SetGroupActive(GameObject[] group, bool active)
        {
            if (group == null)
            {
                return;
            }

            foreach (GameObject go in group)
            {
                if (go != null)
                {
                    go.SetActive(active);
                }
            }
        }

        public void TakeDamage(float amount, bool bypassInvulnerability = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
