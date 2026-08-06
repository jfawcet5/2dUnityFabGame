using System.Collections.Generic;
using UnityEngine;

namespace BeyProject.Data
{
    /// <summary>
    /// Single lookup table from item id -> ItemDefinition, so the inventory UI can resolve
    /// display data for whatever ids Inventory/SaveSystem are holding without a second
    /// source of truth for "what items exist."
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Bey Project/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public ItemDefinition[] allItems = new ItemDefinition[0];

        private Dictionary<string, ItemDefinition> lookup;

        public ItemDefinition GetById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            if (lookup == null)
            {
                BuildLookup();
            }

            lookup.TryGetValue(itemId, out ItemDefinition definition);
            return definition;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, ItemDefinition>();
            foreach (ItemDefinition item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.id))
                {
                    lookup[item.id] = item;
                }
            }
        }
    }
}
