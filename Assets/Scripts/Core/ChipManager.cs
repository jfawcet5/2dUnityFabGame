using System.Collections.Generic;
using BeyProject.Data;
using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Persistent singleton (lives on the PersistentSystems root, alongside GameManager/
    /// PauseManager/InventoryManager) owning the player's chip loadout: which ItemDefinition
    /// (category == ChipModule) is equipped in each of the four slots. Chip modules are just
    /// ItemDefinitions collected through the existing Inventory/ItemDatabase pipeline - this
    /// only tracks which owned ones are currently *installed*, it doesn't duplicate ownership.
    ///
    /// Stats are accumulated across ALL four equipped modules rather than each slot owning
    /// one isolated number, so loadouts compound: a fire-rate cooling module genuinely
    /// changes what a multi-projectile processor feels like, and two modules that each cost
    /// energy stack into a build that has to be managed rather than held down.
    /// </summary>
    public class ChipManager : MonoBehaviour
    {
        public static ChipManager Instance { get; private set; }

        [SerializeField] private ItemDatabase itemDatabase;

        private static readonly ChipSlotType[] AllSlots =
        {
            ChipSlotType.Battery, ChipSlotType.Cache, ChipSlotType.Processor, ChipSlotType.Cooling
        };

        private string equippedBattery = "";
        private string equippedCache = "";
        private string equippedProcessor = "";
        private string equippedCooling = "";

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Install(ItemDefinition module)
        {
            if (module == null || module.category != ItemCategory.ChipModule)
            {
                return;
            }

            switch (module.chipModule.chipSlot)
            {
                case ChipSlotType.Battery:
                    equippedBattery = module.id;
                    break;
                case ChipSlotType.Cache:
                    equippedCache = module.id;
                    break;
                case ChipSlotType.Processor:
                    equippedProcessor = module.id;
                    break;
                case ChipSlotType.Cooling:
                    equippedCooling = module.id;
                    break;
            }
        }

        /// <summary>Returns a slot to its baseline "Standard" state.</summary>
        public void Uninstall(ChipSlotType slot)
        {
            switch (slot)
            {
                case ChipSlotType.Battery:
                    equippedBattery = "";
                    break;
                case ChipSlotType.Cache:
                    equippedCache = "";
                    break;
                case ChipSlotType.Processor:
                    equippedProcessor = "";
                    break;
                case ChipSlotType.Cooling:
                    equippedCooling = "";
                    break;
            }
        }

        public ItemDefinition GetEquipped(ChipSlotType slot)
        {
            if (itemDatabase == null)
            {
                return null;
            }

            return itemDatabase.GetById(GetEquippedId(slot));
        }

        public ChipStats GetCurrentStats()
        {
            return ComputeStats(null, null);
        }

        /// <summary>
        /// The stats the player would have if <paramref name="preview"/> were installed.
        /// Backs the Fabrication screen's before/after deltas - the same accumulation path as
        /// the live stats, so a preview can never disagree with what installing actually does.
        /// </summary>
        public ChipStats GetStatsWithPreview(ItemDefinition preview)
        {
            if (preview == null || preview.category != ItemCategory.ChipModule)
            {
                return GetCurrentStats();
            }

            return ComputeStats(preview, null);
        }

        /// <summary>The stats the player would have with <paramref name="slot"/> emptied.</summary>
        public ChipStats GetStatsWithoutSlot(ChipSlotType slot)
        {
            return ComputeStats(null, slot);
        }

        private ChipStats ComputeStats(ItemDefinition overrideModule, ChipSlotType? clearedSlot)
        {
            ChipStats stats = ChipStats.Default;

            float maxEnergyBonus = 0f;
            int burstBonus = 0;

            // Every equipped module's damageMultiplier also votes on projectile size (see
            // below) - an empty/neutral slot votes the baseline 1f, same as the multiplicative
            // stat fields already implicitly treat it via the "module == null" skip.
            float damageSum = 0f;
            ProjectileVisual resolvedVisual = null;
            int resolvedVisualPriority = -1;

            foreach (ChipSlotType slot in AllSlots)
            {
                ItemDefinition module = GetEquipped(slot);

                if (overrideModule != null && overrideModule.chipModule.chipSlot == slot)
                {
                    module = overrideModule;
                }
                else if (clearedSlot.HasValue && clearedSlot.Value == slot)
                {
                    module = null;
                }

                damageSum += module != null ? module.chipModule.damageMultiplier : 1f;

                if (module != null && module.chipModule.projectileVisual != null)
                {
                    int priority = VisualPriority(slot);
                    if (priority > resolvedVisualPriority)
                    {
                        resolvedVisual = module.chipModule.projectileVisual;
                        resolvedVisualPriority = priority;
                    }
                }

                if (module == null)
                {
                    continue;
                }

                maxEnergyBonus += module.chipModule.batteryBonus;
                burstBonus += module.chipModule.cacheBonus;

                stats.shotEnergyCost *= module.chipModule.coolingCostMultiplier;
                stats.energyRegenRate *= module.chipModule.coolingRegenMultiplier;
                stats.damageMultiplier *= module.chipModule.damageMultiplier;
                stats.projectileSizeMultiplier *= module.chipModule.projectileSizeMultiplier;
                stats.moveSpeedMultiplier *= module.chipModule.moveSpeedMultiplier;
                stats.reloadSpeedMultiplier *= module.chipModule.reloadSpeedMultiplier;
                stats.fireRateMultiplier *= module.chipModule.fireRateMultiplier;
                stats.projectileSpeedMultiplier *= module.chipModule.projectileSpeedMultiplier;

                // Multiplied rather than assigned so a future splitter in a second slot
                // compounds with the processor instead of silently overwriting it.
                stats.projectileCount *= Mathf.Max(1, module.chipModule.projectileCount);
                stats.homing |= module.chipModule.homing;
            }

            stats.maxEnergy = Mathf.Max(10f, stats.maxEnergy + maxEnergyBonus);
            stats.burstCapacity = Mathf.Max(1, stats.burstCapacity + burstBonus);
            stats.shotEnergyCost = Mathf.Max(1f, stats.shotEnergyCost);
            stats.projectileCount = Mathf.Clamp(stats.projectileCount, 1, 12);

            // Standardized visual influence: higher-damage modules read as a visually bigger
            // shot, lower-damage ones read as smaller, averaged (not multiplied) across all 4
            // slots so it's weighed against whatever else is equipped instead of compounding
            // without bound as more modules are added later. Layered on top of any explicitly
            // hand-authored projectileSizeMultiplier, then clamped the same defensive way
            // projectileCount is above.
            float sizeFromDamage = damageSum / AllSlots.Length;
            stats.projectileSizeMultiplier = Mathf.Clamp(stats.projectileSizeMultiplier * sizeFromDamage, 0.4f, 3f);

            stats.projectileVisual = resolvedVisual;

            return stats;
        }

        /// <summary>
        /// Which slot's projectileVisual wins when more than one equipped module sets one -
        /// Processor first, matching GetOutputDescription's existing precedent that Processor
        /// is authoritative for the weapon's identity, then Battery/Cache/Cooling as a
        /// deterministic tie-break.
        /// </summary>
        private static int VisualPriority(ChipSlotType slot)
        {
            switch (slot)
            {
                case ChipSlotType.Processor: return 3;
                case ChipSlotType.Battery: return 2;
                case ChipSlotType.Cache: return 1;
                default: return 0;
            }
        }

        public string GetOutputDescription()
        {
            ItemDefinition processor = GetEquipped(ChipSlotType.Processor);
            if (processor != null && !string.IsNullOrEmpty(processor.chipModule.chipOutputDescription))
            {
                return processor.chipModule.chipOutputDescription;
            }

            return "Standard Energy Bolt";
        }

        /// <summary>Short labels for every installed module - drives the HUD's active-effects line.</summary>
        public List<string> GetActiveEffectLabels()
        {
            var labels = new List<string>();

            foreach (ChipSlotType slot in AllSlots)
            {
                ItemDefinition module = GetEquipped(slot);
                if (module != null)
                {
                    labels.Add(module.displayName);
                }
            }

            return labels;
        }

        public string GetEquippedId(ChipSlotType slot)
        {
            return slot switch
            {
                ChipSlotType.Battery => equippedBattery,
                ChipSlotType.Cache => equippedCache,
                ChipSlotType.Processor => equippedProcessor,
                ChipSlotType.Cooling => equippedCooling,
                _ => ""
            };
        }

        public void LoadEquipped(string battery, string cache, string processor, string cooling)
        {
            equippedBattery = battery ?? "";
            equippedCache = cache ?? "";
            equippedProcessor = processor ?? "";
            equippedCooling = cooling ?? "";
        }
    }
}
