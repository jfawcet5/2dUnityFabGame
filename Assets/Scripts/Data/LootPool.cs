using System;
using System.Collections.Generic;
using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Data
{
    [Serializable]
    public class LootPoolEntry
    {
        public ItemDefinition item;
        public int quantity = 1;
        public float weight = 1f;

        [Tooltip("Optional MetaProgress key. While set and not yet unlocked, this entry is excluded from the roll.")]
        public string requiredUnlock;
    }

    /// <summary>
    /// A weighted set of possible rewards a location can roll from instead of always giving one
    /// fixed item - authored as data (create an asset, no code changes) same as UnlockRuleSet.
    /// A null-item entry is a deliberate "nothing" slot, so a pool can whiff by design rather
    /// than always guaranteeing a reward.
    /// </summary>
    [CreateAssetMenu(fileName = "LootPool", menuName = "Bey Project/Loot Pool")]
    public class LootPool : ScriptableObject
    {
        public LootPoolEntry[] entries = new LootPoolEntry[0];

        /// <summary>Weighted-random pick among currently-eligible entries (requiredUnlock
        /// satisfied or unset). Returns null if there are no entries or nothing is eligible -
        /// both read as "nothing here," not an error.</summary>
        public LootPoolEntry Roll()
        {
            if (entries == null || entries.Length == 0)
            {
                return null;
            }

            var eligible = new List<LootPoolEntry>();
            float totalWeight = 0f;

            foreach (LootPoolEntry entry in entries)
            {
                if (entry == null || entry.weight <= 0f)
                {
                    continue;
                }

                bool unlockOk = string.IsNullOrEmpty(entry.requiredUnlock)
                    || (MetaProgress.Instance != null && MetaProgress.Instance.HasUnlock(entry.requiredUnlock));

                if (!unlockOk)
                {
                    continue;
                }

                eligible.Add(entry);
                totalWeight += entry.weight;
            }

            if (eligible.Count == 0 || totalWeight <= 0f)
            {
                return null;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            foreach (LootPoolEntry entry in eligible)
            {
                cumulative += entry.weight;
                if (roll <= cumulative)
                {
                    return entry;
                }
            }

            return eligible[eligible.Count - 1];
        }
    }
}
