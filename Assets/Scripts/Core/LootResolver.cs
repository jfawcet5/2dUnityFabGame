using BeyProject.Data;

namespace BeyProject.Core
{
    /// <summary>
    /// Sticky-per-run resolution for a floor pickup's item: the first time a given rollId is
    /// seen this run, rolls pool (or just uses fallbackItem if pool is null - every existing
    /// fixed-item pickup keeps working unchanged) and remembers the result (including a whiff)
    /// in SaveSystem, so leaving and re-entering the same room within a run always shows the
    /// same thing. GameManager.EndRun() wipes that cache, so a new run rolls fresh.
    ///
    /// WorldAction's GiveItem and EnemyBase's death drops deliberately do NOT go through this -
    /// see the plan notes on those call sites for why a fresh roll-every-time is correct there.
    /// </summary>
    public static class LootResolver
    {
        public static ItemDefinition Resolve(string rollId, LootPool pool, ItemDefinition fallbackItem,
            ItemDatabase itemDatabase, out int quantity)
        {
            if (pool == null)
            {
                quantity = 1;
                return fallbackItem;
            }

            if (SaveSystem.Instance != null && SaveSystem.Instance.TryGetRolledLoot(rollId, out string cachedId, out int cachedQuantity))
            {
                quantity = cachedQuantity;
                return string.IsNullOrEmpty(cachedId) ? null : itemDatabase?.GetById(cachedId);
            }

            LootPoolEntry picked = pool.Roll();
            ItemDefinition resolved = picked?.item;
            quantity = picked?.quantity ?? 0;

            SaveSystem.Instance?.SetRolledLoot(rollId, resolved != null ? resolved.id : "", quantity);

            return resolved;
        }
    }
}
