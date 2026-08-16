using System;
using System.Collections.Generic;
using System.IO;
using BeyProject.Data;
using UnityEngine;

namespace BeyProject.Core
{
    [Serializable]
    public class ItemCountEntry
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class RolledLootEntry
    {
        public string rollId;
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class SaveData
    {
        public string currentRoomScene;
        public Vector2 currentRoomPosition;
        public List<ItemCountEntry> itemCounts = new List<ItemCountEntry>();
        public List<string> flags = new List<string>();
        public List<RolledLootEntry> rolledLoot = new List<RolledLootEntry>();
        public string equippedBattery = "";
        public string equippedCache = "";
        public string equippedProcessor = "";
        public string equippedCooling = "";
    }

    /// <summary>
    /// Persistent singleton (lives alongside GameManager). Owns a generic, namespaced flag
    /// store (doors/items/NPCs/events all use the same HasFlag/SetFlag mechanism, so no
    /// bespoke save field is needed per feature) plus JSON persistence to disk.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private readonly HashSet<string> flags = new HashSet<string>();
        private readonly Dictionary<string, RolledLootEntry> rolledLoot = new Dictionary<string, RolledLootEntry>();

        public string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
        }

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
            {
                flags.Add(flag);
            }
        }

        public bool IsDoorOpened(string doorId) => HasFlag($"door_opened_{doorId}");
        public void MarkDoorOpened(string doorId) => SetFlag($"door_opened_{doorId}");

        public bool IsItemCollected(string pickupId) => HasFlag($"item_collected_{pickupId}");
        public void MarkItemCollected(string pickupId) => SetFlag($"item_collected_{pickupId}");

        public bool HasMetNpc(string npcId) => HasFlag($"npc_{npcId}_met");
        public void MarkNpcMet(string npcId) => SetFlag($"npc_{npcId}_met");

        public bool IsInteractableUsed(string interactableId) => HasFlag($"interactable_{interactableId}_used");
        public void MarkInteractableUsed(string interactableId) => SetFlag($"interactable_{interactableId}_used");

        /// <summary>Sticky per-run loot roll cache - a rollId is resolved once per run (see
        /// LootResolver) and remembered here (including a whiff, stored as an empty itemId) so
        /// leaving and re-entering the same room mid-run doesn't re-roll it.</summary>
        public bool TryGetRolledLoot(string rollId, out string itemId, out int quantity)
        {
            if (!string.IsNullOrEmpty(rollId) && rolledLoot.TryGetValue(rollId, out RolledLootEntry entry))
            {
                itemId = entry.itemId;
                quantity = entry.quantity;
                return true;
            }

            itemId = null;
            quantity = 0;
            return false;
        }

        public void SetRolledLoot(string rollId, string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(rollId))
            {
                return;
            }

            rolledLoot[rollId] = new RolledLootEntry { rollId = rollId, itemId = itemId ?? "", quantity = quantity };
        }

        public bool HasSaveFile() => File.Exists(SavePath);

        public void Save()
        {
            var data = new SaveData
            {
                currentRoomScene = GameManager.Instance != null ? GameManager.Instance.CurrentRoomSceneName : null,
                currentRoomPosition = GetPlayerPosition()
            };

            if (Inventory.Instance != null)
            {
                foreach (var kvp in Inventory.Instance.GetAllCounts())
                {
                    data.itemCounts.Add(new ItemCountEntry { itemId = kvp.Key, count = kvp.Value });
                }
            }

            data.flags.AddRange(flags);
            data.rolledLoot.AddRange(rolledLoot.Values);

            if (ChipManager.Instance != null)
            {
                data.equippedBattery = ChipManager.Instance.GetEquippedId(ChipSlotType.Battery);
                data.equippedCache = ChipManager.Instance.GetEquippedId(ChipSlotType.Cache);
                data.equippedProcessor = ChipManager.Instance.GetEquippedId(ChipSlotType.Processor);
                data.equippedCooling = ChipManager.Instance.GetEquippedId(ChipSlotType.Cooling);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"Saved game to {SavePath}");
        }

        public void LoadFromDisk()
        {
            if (!HasSaveFile())
            {
                Debug.LogWarning("SaveSystem: no save file to load.");
                return;
            }

            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

            ResetToDefaults();

            foreach (string flag in data.flags)
            {
                flags.Add(flag);
            }

            foreach (RolledLootEntry entry in data.rolledLoot)
            {
                rolledLoot[entry.rollId] = entry;
            }

            if (Inventory.Instance != null)
            {
                foreach (var entry in data.itemCounts)
                {
                    Inventory.Instance.SetCount(entry.itemId, entry.count);
                }
            }

            if (ChipManager.Instance != null)
            {
                ChipManager.Instance.LoadEquipped(data.equippedBattery, data.equippedCache, data.equippedProcessor, data.equippedCooling);
            }

            if (GameManager.Instance != null && !string.IsNullOrEmpty(data.currentRoomScene))
            {
                GameManager.Instance.TravelToRoom(data.currentRoomScene, null, data.currentRoomPosition);
            }
        }

        /// <summary>Wipes all run-scoped state: flags (doors/enemies/hazards/pickups/NPCs -
        /// everything currently uses this same namespaced HashSet, so a full clear makes the
        /// whole world fresh again), inventory, and the equipped chip build. Used by both
        /// "New Game" and GameManager.EndRun() (death/boss-clear), matching the Isaac-style
        /// "full run reset, unlocks/meta remain" design - there's no separate persistent meta
        /// store yet, so once one exists it needs to live outside this flag set rather than
        /// inside it.</summary>
        public void ResetToDefaults()
        {
            flags.Clear();
            rolledLoot.Clear();
            Inventory.Instance?.Clear();
            ChipManager.Instance?.ResetToDefaults();
        }

        private static Vector2 GetPlayerPosition()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }
    }
}
