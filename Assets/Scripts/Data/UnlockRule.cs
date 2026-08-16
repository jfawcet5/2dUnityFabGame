using UnityEngine;

namespace BeyProject.Data
{
    public enum UnlockTriggerType
    {
        ItemDiscovered,
        BossDefeatedCount,
        ComponentSetInRun,
    }

    /// <summary>
    /// Data-only "when [trigger] happens, set [unlockKey]" rule, evaluated by UnlockManager
    /// and stored (once satisfied) in MetaProgress. unlockKey is an arbitrary string, not tied
    /// to any specific content type - it might gate an ItemPickup's requiredUnlock, a
    /// StartingLoadoutSelector, or (once those systems exist) a cosmetic or room unlock. New
    /// content is authored by creating a rule asset, never by editing code.
    /// </summary>
    [CreateAssetMenu(fileName = "UnlockRule", menuName = "Bey Project/Unlock Rule")]
    public class UnlockRule : ScriptableObject
    {
        public string unlockKey = "";
        public UnlockTriggerType triggerType;

        [Header("ItemDiscovered")]
        public ItemDefinition triggerItem;

        [Header("BossDefeatedCount")]
        public string bossId = "";
        public int requiredCount = 1;

        [Header("ComponentSetInRun")]
        public ItemDefinition[] requiredItems = new ItemDefinition[0];

        [Header("Feedback")]
        [TextArea]
        public string unlockMessage = "";
    }
}
