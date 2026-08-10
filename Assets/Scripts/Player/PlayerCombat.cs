using System.Collections;
using BeyProject.Combat;
using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Player
{
    /// <summary>
    /// Mouse-aimed energy weapon driven by the player's current chip loadout
    /// (ChipManager.GetCurrentStats()). Added alongside PlayerController2D/PlayerInteractor
    /// on every room's Player GameObject - harmless where there are no enemies, avoiding a
    /// separate combat-only player variant. Gated by UIInputLock exactly like movement/
    /// interaction, so it freezes whenever Pause/Inventory/Dialogue/Fabrication are open.
    ///
    /// Fire is hold-to-repeat rather than click-per-shot: with a chip-driven fire rate, a
    /// click-only weapon would make the rate stat unfeelable.
    ///
    /// The equipped Processor's ItemDefinition.processorBehavior selects *how* fire input is
    /// interpreted (Standard hold-to-repeat, Burst, Scatter, Charge) - ChipStats remains the
    /// single place combat numbers are computed, this only changes when/how often the shared
    /// spend-resources-and-spawn-projectiles path (SpawnShots/SpendShot) gets called.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        private const float BaseProjectileSpeed = 8f;
        private const float BaseDamage = 10f;
        private const float BaseReloadSeconds = 1.2f;
        private const float BaseFireIntervalSeconds = 0.26f;
        private const float ProjectileSpreadDegrees = 12f;

        // Burst (ProcessorBehaviorType.Burst): a trigger pull fires this many rapid sub-shots,
        // each spending its own burst/energy, then forces a longer recovery than standard fire.
        private const int BurstSubShotCount = 3;
        private const float BurstSubShotIntervalSeconds = 0.05f;
        private const float BurstRecoverySeconds = 0.55f;

        // Scatter (ProcessorBehaviorType.Scatter): random cone instead of the fixed, precise
        // fan Parallel Processing/Cascade Processor already use - deliberately a different feel
        // (shotgun-like) rather than a re-skin of the existing multi-shot modules.
        private const float ScatterHalfAngleDegrees = 20f;

        // Charge (ProcessorBehaviorType.Charge): hold to charge, release to fire one shot whose
        // damage/size/cost scale with hold duration. These multipliers are layered on top of
        // ChipStats locally rather than added to the struct, since they're specific to this one
        // firing behavior rather than a general combat number.
        private const float ChargeMaxSeconds = 1.2f;
        private const float ChargeMinDamageMultiplier = 0.5f;
        private const float ChargeMaxDamageMultiplier = 2.2f;
        private const float ChargeMinSizeMultiplier = 0.8f;
        private const float ChargeMaxSizeMultiplier = 1.6f;
        private const float ChargeMinCostMultiplier = 0.6f;
        private const float ChargeMaxCostMultiplier = 1.8f;

        private static readonly Color BoltColor = new Color(0.3f, 0.85f, 1f);

        [SerializeField] private SpriteRenderer aimIndicatorRenderer;

        private float currentEnergy;
        private int currentBurst;
        private float reloadTimer;
        private float reloadDuration = BaseReloadSeconds;
        private float nextFireTime;
        private bool isReloading;
        private Transform aimIndicator;
        private ChipStats lastStats = ChipStats.Default;
        private ProcessorBehaviorType currentBehavior = ProcessorBehaviorType.Standard;

        private bool isBursting;
        private bool isCharging;
        private float chargeStartTime;
        private bool chargeHalfwayPulseFired;

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => lastStats.maxEnergy;
        public int CurrentBurst => currentBurst;
        public int BurstCapacity => lastStats.burstCapacity;
        public bool IsReloading => isReloading;
        public ChipStats CurrentStats => lastStats;

        /// <summary>0 while idle, 0-1 while charging a Charge-behavior shot.</summary>
        public float ChargeFraction => isCharging ? Mathf.Clamp01((Time.time - chargeStartTime) / ChargeMaxSeconds) : 0f;

        /// <summary>0 at the start of a reload, 1 when it completes. Drives the HUD's reload bar.</summary>
        public float ReloadProgress => isReloading && reloadDuration > 0f
            ? Mathf.Clamp01(1f - reloadTimer / reloadDuration)
            : 1f;

        private void Awake()
        {
            //var indicatorGO = new GameObject("AimIndicator", typeof(SpriteRenderer));
            //indicatorGO.transform.SetParent(transform, false);
            //indicatorGO.transform.localScale = new Vector3(0.4f, 0.15f, 1f);

            //SpriteRenderer renderer = indicatorGO.GetComponent<SpriteRenderer>();
            //renderer.sprite = PlaceholderSprite.SharedSquare();
            //renderer.color = new Color(1f, 0.9f, 0.3f);
            //renderer.sortingOrder = 6;

            //aimIndicator = indicatorGO.transform;
            //aimIndicatorRenderer = renderer;
        }

        private void Start()
        {
            lastStats = ChipManager.Instance != null ? ChipManager.Instance.GetCurrentStats() : ChipStats.Default;
            currentEnergy = lastStats.maxEnergy;
            currentBurst = lastStats.burstCapacity;
        }

        private void Update()
        {
            if (UIInputLock.IsBlocked)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                BeginReload();
            }

            Vector2 aimDirection = ComputeAimDirection();
            UpdateAimIndicator(aimDirection);

            lastStats = ChipManager.Instance != null ? ChipManager.Instance.GetCurrentStats() : ChipStats.Default;
            currentEnergy = Mathf.Min(currentEnergy, lastStats.maxEnergy);
            currentBurst = Mathf.Min(currentBurst, lastStats.burstCapacity);
            currentBehavior = ChipManager.Instance != null
                ? ChipManager.Instance.GetEquipped(ChipSlotType.Processor)?.processorBehavior ?? ProcessorBehaviorType.Standard
                : ProcessorBehaviorType.Standard;

            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0f)
                {
                    isReloading = false;
                    currentBurst = lastStats.burstCapacity;
                }
            }
            else
            {
                currentEnergy = Mathf.Min(lastStats.maxEnergy, currentEnergy + lastStats.energyRegenRate * Time.deltaTime);
            }

            if (currentBehavior == ProcessorBehaviorType.Charge)
            {
                UpdateChargeInput(aimDirection);
                return;
            }

            // A charge started under a different processor shouldn't carry over if swapped mid-hold.
            isCharging = false;

            if (!isReloading && !isBursting && Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                TryFire(aimDirection);
            }
        }

        private Vector2 ComputeAimDirection()
        {
            if (Camera.main == null)
            {
                return Vector2.up;
            }

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (Vector2)mouseWorld - (Vector2)transform.position;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        }

        private void UpdateAimIndicator(Vector2 aimDirection)
        {
            if (aimIndicator == null)
            {
                return;
            }

            aimIndicator.localPosition = (Vector3)(aimDirection * 0.6f);
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            aimIndicator.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (aimIndicatorRenderer != null)
            {
                // Dims while the weapon can't fire, so "why isn't it shooting" is answered
                // by the crosshair itself rather than only by the HUD in the corner.
                bool ready = !isReloading && !isBursting && currentBehavior != ProcessorBehaviorType.Charge
                    && currentEnergy >= lastStats.shotEnergyCost && currentBurst > 0;
                aimIndicatorRenderer.color = ready ? new Color(1f, 0.9f, 0.3f) : new Color(0.6f, 0.55f, 0.4f, 0.55f);
            }
        }

        private void TryFire(Vector2 aimDirection)
        {
            if (currentBurst <= 0 || currentEnergy < lastStats.shotEnergyCost)
            {
                return;
            }

            SpendShot();

            if (currentBehavior == ProcessorBehaviorType.Burst)
            {
                nextFireTime = Time.time + BurstRecoverySeconds / Mathf.Max(0.05f, lastStats.fireRateMultiplier);
                StartCoroutine(FireBurst(aimDirection));
                return;
            }

            nextFireTime = Time.time + BaseFireIntervalSeconds / Mathf.Max(0.05f, lastStats.fireRateMultiplier);

            bool randomSpread = currentBehavior == ProcessorBehaviorType.Scatter;
            SpawnShots(aimDirection, lastStats.projectileCount, BaseDamage * lastStats.damageMultiplier,
                lastStats.projectileSpeedMultiplier, lastStats.projectileSizeMultiplier, lastStats.homing, randomSpread);

            if (currentBurst <= 0)
            {
                BeginReload();
            }
        }

        /// <summary>
        /// Fires the remaining sub-shots of a burst. The opening shot's resources were already
        /// spent by TryFire before this started; each further sub-shot spends its own burst/
        /// energy exactly like a normal shot, so Cache/Battery modules stay just as meaningful
        /// under Burst as under Standard fire.
        /// </summary>
        private IEnumerator FireBurst(Vector2 aimDirection)
        {
            isBursting = true;

            SpawnShots(aimDirection, lastStats.projectileCount, BaseDamage * lastStats.damageMultiplier,
                lastStats.projectileSpeedMultiplier, lastStats.projectileSizeMultiplier, lastStats.homing, false);

            for (int i = 1; i < BurstSubShotCount; i++)
            {
                yield return new WaitForSeconds(BurstSubShotIntervalSeconds);

                if (UIInputLock.IsBlocked || currentBurst <= 0 || currentEnergy < lastStats.shotEnergyCost)
                {
                    break;
                }

                SpendShot();
                SpawnShots(ComputeAimDirection(), lastStats.projectileCount, BaseDamage * lastStats.damageMultiplier,
                    lastStats.projectileSpeedMultiplier, lastStats.projectileSizeMultiplier, lastStats.homing, false);
            }

            isBursting = false;

            if (currentBurst <= 0)
            {
                BeginReload();
            }
        }

        private void UpdateChargeInput(Vector2 aimDirection)
        {
            if (isReloading)
            {
                isCharging = false;
                return;
            }

            if (!isCharging && Input.GetMouseButtonDown(0) && currentBurst > 0
                && currentEnergy >= lastStats.shotEnergyCost * ChargeMinCostMultiplier)
            {
                isCharging = true;
                chargeStartTime = Time.time;
                chargeHalfwayPulseFired = false;
                CombatFeedback.Telegraph(transform.position, BoltColor, 0.6f);
            }

            if (!isCharging)
            {
                return;
            }

            float fraction = ChargeFraction;

            if (!chargeHalfwayPulseFired && fraction >= 0.6f)
            {
                chargeHalfwayPulseFired = true;
                CombatFeedback.Telegraph(transform.position, BoltColor, 1.1f);
            }

            if (Input.GetMouseButtonUp(0))
            {
                isCharging = false;
                FireCharged(aimDirection, fraction);
                return;
            }

            if (currentBurst <= 0 || currentEnergy < lastStats.shotEnergyCost * ChargeMinCostMultiplier)
            {
                // Resources drained out from under the charge (e.g. loadout swapped mid-hold) -
                // drop it silently rather than let the player release into a shot they can't afford.
                isCharging = false;
            }
        }

        private void FireCharged(Vector2 aimDirection, float chargeFraction)
        {
            float cost = lastStats.shotEnergyCost * Mathf.Lerp(ChargeMinCostMultiplier, ChargeMaxCostMultiplier, chargeFraction);
            if (currentBurst <= 0 || currentEnergy < cost)
            {
                return;
            }

            currentBurst--;
            currentEnergy -= cost;
            nextFireTime = Time.time + BaseFireIntervalSeconds / Mathf.Max(0.05f, lastStats.fireRateMultiplier);

            float damage = BaseDamage * lastStats.damageMultiplier
                * Mathf.Lerp(ChargeMinDamageMultiplier, ChargeMaxDamageMultiplier, chargeFraction);
            float size = lastStats.projectileSizeMultiplier
                * Mathf.Lerp(ChargeMinSizeMultiplier, ChargeMaxSizeMultiplier, chargeFraction);

            SpawnShots(aimDirection, lastStats.projectileCount, damage, lastStats.projectileSpeedMultiplier, size,
                lastStats.homing, false);

            if (currentBurst <= 0)
            {
                BeginReload();
            }
        }

        private void SpendShot()
        {
            currentBurst--;
            currentEnergy -= lastStats.shotEnergyCost;
        }

        /// <summary>External energy drain (e.g. a Thermal Leech's contact attack) - distinct
        /// from the weapon's own resource spending above, but clamped the same way.</summary>
        public void DrainEnergy(float amount)
        {
            currentEnergy = Mathf.Max(0f, currentEnergy - amount);
        }

        /// <summary>
        /// Overrides Start()'s fresh-full-resources initialization with values carried over
        /// from the previous room (see GameManager) - called one frame after this instance's
        /// own Start() has already run, so this intentionally has the final word.
        /// </summary>
        public void RestoreCombatState(float energy, int burst)
        {
            currentEnergy = Mathf.Clamp(energy, 0f, lastStats.maxEnergy);
            currentBurst = Mathf.Clamp(burst, 0, lastStats.burstCapacity);
        }

        private void BeginReload()
        {
            isReloading = true;
            reloadDuration = BaseReloadSeconds / Mathf.Max(0.05f, lastStats.reloadSpeedMultiplier);
            reloadTimer = reloadDuration;
        }

        /// <summary>
        /// The shared spend-nothing, spawn-only core every firing behavior funnels through, so
        /// ChipStats stays the one place damage/speed/size/homing are computed regardless of
        /// which behavior is dispatching the shot. randomSpread swaps the deterministic fan
        /// (Standard/Parallel/Cascade-style multi-shot) for Scatter's wide random cone.
        /// </summary>
        private void SpawnShots(Vector2 aimDirection, int count, float damage, float speedMultiplier,
            float sizeMultiplier, bool homing, bool randomSpread)
        {
            count = Mathf.Max(1, count);
            Vector3 spawnPosition = transform.position + (Vector3)(aimDirection * 0.6f);

            CombatFeedback.MuzzleFlash(spawnPosition, aimDirection, BoltColor, sizeMultiplier);

            float spreadStep = !randomSpread && count > 1 ? ProjectileSpreadDegrees : 0f;
            float startAngle = -(spreadStep * (count - 1)) / 2f;

            for (int i = 0; i < count; i++)
            {
                float angleOffset = randomSpread
                    ? Random.Range(-ScatterHalfAngleDegrees, ScatterHalfAngleDegrees)
                    : startAngle + spreadStep * i;

                Vector2 shotDirection = Quaternion.Euler(0f, 0f, angleOffset) * aimDirection;
                Projectile.Spawn(spawnPosition, shotDirection, BaseProjectileSpeed * speedMultiplier, damage,
                    isPlayerOwned: true, homing: homing, sizeMultiplier: sizeMultiplier, color: BoltColor);
            }
        }
    }
}
