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
    public class SaveData
    {
        public string currentRoomScene;
        public Vector2 currentRoomPosition;
        public List<ItemCountEntry> itemCounts = new List<ItemCountEntry>();
        public List<string> flags = new List<string>();
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

        public void ResetToDefaults()
        {
            flags.Clear();
            if (Inventory.Instance != null)
            {
                Inventory.Instance.Clear();
            }
        }

        private static Vector2 GetPlayerPosition()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }
    }
}
