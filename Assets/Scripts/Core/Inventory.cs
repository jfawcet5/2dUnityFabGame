using System.Collections.Generic;
using BeyProject.Data;
using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Persistent item counter. Tracks counts by item id; the inventory UI resolves
    /// display data via ItemDatabase, and SaveSystem persists/restores counts directly.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void AddItem(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                return;
            }

            counts.TryGetValue(item.id, out int current);
            counts[item.id] = current + quantity;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayItemPickup();
            }

            Debug.Log($"Picked up {quantity}x {item.displayName} (total: {counts[item.id]})");
        }

        public int GetCount(string itemId)
        {
            return counts.TryGetValue(itemId, out int count) ? count : 0;
        }

        public bool HasItem(string itemId)
        {
            return GetCount(itemId) > 0;
        }

        public void SetCount(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            if (count <= 0)
            {
                counts.Remove(itemId);
            }
            else
            {
                counts[itemId] = count;
            }
        }

        public Dictionary<string, int> GetAllCounts()
        {
            return new Dictionary<string, int>(counts);
        }

        public void Clear()
        {
            counts.Clear();
        }
    }
}
