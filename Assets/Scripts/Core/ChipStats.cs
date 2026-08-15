using BeyProject.Data;

namespace BeyProject.Core
{
    /// <summary>
    /// Computed, ready-to-use combat numbers for the player's current chip loadout. Every
    /// field here is something the player can feel in play - there is deliberately no
    /// "power level" or raw stat total, because the point of the chip system is that builds
    /// differ in shape rather than in magnitude.
    /// </summary>
    public struct ChipStats
    {
        public float maxEnergy;
        public int burstCapacity;
        public float energyRegenRate;
        public float shotEnergyCost;
        public float damageMultiplier;
        public int projectileCount;
        public float projectileSizeMultiplier;
        public bool homing;

        /// <summary>Highest-priority equipped module's custom projectile look, or null to use
        /// the default placeholder bolt - see ChipManager.ComputeStats.</summary>
        public ProjectileVisual projectileVisual;

        public float moveSpeedMultiplier;
        public float reloadSpeedMultiplier;
        public float fireRateMultiplier;
        public float projectileSpeedMultiplier;

        /// <summary>Baseline loadout - the "Standard" chip with nothing installed.</summary>
        public static ChipStats Default => new ChipStats
        {
            maxEnergy = 100f,
            burstCapacity = 6,
            energyRegenRate = 12f,
            shotEnergyCost = 12f,
            damageMultiplier = 1f,
            projectileCount = 1,
            projectileSizeMultiplier = 1f,
            homing = false,
            moveSpeedMultiplier = 1f,
            reloadSpeedMultiplier = 1f,
            fireRateMultiplier = 1f,
            projectileSpeedMultiplier = 1f
        };
    }
}
