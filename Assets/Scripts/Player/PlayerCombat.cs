using BeyProject.Combat;
using BeyProject.Core;
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
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        private const float BaseProjectileSpeed = 8f;
        private const float BaseDamage = 10f;
        private const float BaseReloadSeconds = 1.2f;
        private const float BaseFireIntervalSeconds = 0.26f;
        private const float ProjectileSpreadDegrees = 12f;

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

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => lastStats.maxEnergy;
        public int CurrentBurst => currentBurst;
        public int BurstCapacity => lastStats.burstCapacity;
        public bool IsReloading => isReloading;
        public ChipStats CurrentStats => lastStats;

        /// <summary>0 at the start of a reload, 1 when it completes. Drives the HUD's reload bar.</summary>
        public float ReloadProgress => isReloading && reloadDuration > 0f
            ? Mathf.Clamp01(1f - reloadTimer / reloadDuration)
            : 1f;

        private void Awake()
        {
            var indicatorGO = new GameObject("AimIndicator", typeof(SpriteRenderer));
            indicatorGO.transform.SetParent(transform, false);
            indicatorGO.transform.localScale = new Vector3(0.4f, 0.15f, 1f);

            SpriteRenderer renderer = indicatorGO.GetComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSprite.SharedSquare();
            renderer.color = new Color(1f, 0.9f, 0.3f);
            renderer.sortingOrder = 6;

            aimIndicator = indicatorGO.transform;
            aimIndicatorRenderer = renderer;
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

            Vector2 aimDirection = ComputeAimDirection();
            UpdateAimIndicator(aimDirection);

            lastStats = ChipManager.Instance != null ? ChipManager.Instance.GetCurrentStats() : ChipStats.Default;
            currentEnergy = Mathf.Min(currentEnergy, lastStats.maxEnergy);
            currentBurst = Mathf.Min(currentBurst, lastStats.burstCapacity);

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

            if (!isReloading && Input.GetMouseButton(0) && Time.time >= nextFireTime)
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
                bool ready = !isReloading && currentEnergy >= lastStats.shotEnergyCost && currentBurst > 0;
                aimIndicatorRenderer.color = ready ? new Color(1f, 0.9f, 0.3f) : new Color(0.6f, 0.55f, 0.4f, 0.55f);
            }
        }

        private void TryFire(Vector2 aimDirection)
        {
            if (currentBurst <= 0 || currentEnergy < lastStats.shotEnergyCost)
            {
                return;
            }

            currentBurst--;
            currentEnergy -= lastStats.shotEnergyCost;
            nextFireTime = Time.time + BaseFireIntervalSeconds / Mathf.Max(0.05f, lastStats.fireRateMultiplier);

            int count = Mathf.Max(1, lastStats.projectileCount);
            float spreadStep = count > 1 ? ProjectileSpreadDegrees : 0f;
            float startAngle = -(spreadStep * (count - 1)) / 2f;
            Vector3 spawnPosition = transform.position + (Vector3)(aimDirection * 0.6f);

            CombatFeedback.MuzzleFlash(spawnPosition, aimDirection, BoltColor, lastStats.projectileSizeMultiplier);

            for (int i = 0; i < count; i++)
            {
                float angleOffset = startAngle + spreadStep * i;
                Vector2 shotDirection = Quaternion.Euler(0f, 0f, angleOffset) * aimDirection;
                Projectile.Spawn(spawnPosition, shotDirection,
                    BaseProjectileSpeed * lastStats.projectileSpeedMultiplier,
                    BaseDamage * lastStats.damageMultiplier,
                    isPlayerOwned: true, homing: lastStats.homing,
                    sizeMultiplier: lastStats.projectileSizeMultiplier, color: BoltColor);
            }

            if (currentBurst <= 0)
            {
                isReloading = true;
                reloadDuration = BaseReloadSeconds / Mathf.Max(0.05f, lastStats.reloadSpeedMultiplier);
                reloadTimer = reloadDuration;
            }
        }
    }
}
