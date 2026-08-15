using System;
using UnityEngine;

namespace BeyProject.Data
{
    /// <summary>
    /// Chip-module-only data, grouped into one nested block so items that aren't chip modules
    /// (key items, lore notes, quest components - the majority of ItemDatabase) don't carry a
    /// wall of unused combat fields on every asset. Only meaningful when the owning
    /// ItemDefinition.category == ItemCategory.ChipModule.
    ///
    /// The multiplier fields below are applied by ChipManager from EVERY slot, not only the
    /// slot they're named after - that's what lets loadouts synergise (a cooling module that
    /// trades fire rate for energy cost, a battery that trades move speed for capacity)
    /// instead of each slot owning one isolated number.
    /// </summary>
    [Serializable]
    public class ChipModuleStats
    {
        public ChipSlotType chipSlot;
        public ProcessorBehaviorType processorBehavior;

        [Header("Additive")]
        public int batteryBonus;
        public int cacheBonus;

        [Header("Multiplicative - applied from any slot")]
        public float coolingCostMultiplier = 1f;
        public float coolingRegenMultiplier = 1f;
        public float damageMultiplier = 1f;
        public float projectileSizeMultiplier = 1f;
        public float moveSpeedMultiplier = 1f;
        public float reloadSpeedMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float projectileSpeedMultiplier = 1f;

        [Header("Behaviour")]
        public int projectileCount = 1;
        public bool homing;

        [Header("Projectile Visual (optional) - overrides the default placeholder bolt")]
        public ProjectileVisual projectileVisual;

        [TextArea]
        public string chipOutputDescription = "";

        /// <summary>
        /// The cost side of the module, stated plainly. Shown next to the benefit in the
        /// Fabrication screen so a choice reads as a trade rather than as an upgrade.
        /// </summary>
        [TextArea]
        public string chipTradeoffDescription = "";
    }
}
