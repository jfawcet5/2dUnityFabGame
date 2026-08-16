using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Persistent singleton that decides *when* something unlocks - MetaProgress only stores
    /// the result. Game systems (ItemPickup, BossEnemy, ...) report events here; this scans
    /// the configured UnlockRuleSet for matching rules and, the first time a rule's condition
    /// is met, writes its unlockKey into MetaProgress and shows the optional feedback message.
    /// </summary>
    public class UnlockManager : MonoBehaviour
    {
        public static UnlockManager Instance { get; private set; }

        [SerializeField] private UnlockRuleSet ruleSet;

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

        /// <summary>Call whenever an item is collected, any category. Checks ItemDiscovered
        /// rules for this item, then re-checks ComponentSetInRun rules since a fresh pickup
        /// may be the one that completes a set.</summary>
        public void ReportItemDiscovered(ItemDefinition item)
        {
            if (ruleSet == null || item == null)
            {
                return;
            }

            foreach (UnlockRule rule in ruleSet.rules)
            {
                if (rule == null || MetaProgress.Instance == null || MetaProgress.Instance.HasUnlock(rule.unlockKey))
                {
                    continue;
                }

                if (rule.triggerType == UnlockTriggerType.ItemDiscovered && rule.triggerItem == item)
                {
                    TryUnlock(rule);
                }
                else if (rule.triggerType == UnlockTriggerType.ComponentSetInRun && AllPresent(rule.requiredItems))
                {
                    TryUnlock(rule);
                }
            }
        }

        /// <summary>Call whenever a boss is defeated. Increments its permanent defeat counter,
        /// then checks BossDefeatedCount rules for that boss.</summary>
        public void ReportBossDefeated(string bossId)
        {
            if (ruleSet == null || string.IsNullOrEmpty(bossId) || MetaProgress.Instance == null)
            {
                return;
            }

            string counterKey = $"boss_{bossId}_defeats";
            MetaProgress.Instance.IncrementCounter(counterKey);
            int count = MetaProgress.Instance.GetCounter(counterKey);

            foreach (UnlockRule rule in ruleSet.rules)
            {
                if (rule == null || MetaProgress.Instance.HasUnlock(rule.unlockKey))
                {
                    continue;
                }

                if (rule.triggerType == UnlockTriggerType.BossDefeatedCount && rule.bossId == bossId
                    && count >= rule.requiredCount)
                {
                    TryUnlock(rule);
                }
            }
        }

        private static bool AllPresent(ItemDefinition[] items)
        {
            if (items == null || items.Length == 0 || Inventory.Instance == null)
            {
                return false;
            }

            foreach (ItemDefinition item in items)
            {
                if (item == null || !Inventory.Instance.HasItem(item.id))
                {
                    return false;
                }
            }

            return true;
        }

        private void TryUnlock(UnlockRule rule)
        {
            if (string.IsNullOrEmpty(rule.unlockKey))
            {
                return;
            }

            MetaProgress.Instance.Unlock(rule.unlockKey);

            if (!string.IsNullOrEmpty(rule.unlockMessage))
            {
                DialogueUI.Instance?.Show("Unlocked", new[] { rule.unlockMessage }, null);
            }
        }
    }
}
