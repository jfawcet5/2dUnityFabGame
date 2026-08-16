using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BeyProject.Core
{
    [Serializable]
    public class MetaCounterEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class MetaProgressData
    {
        public List<string> unlockedFlags = new List<string>();
        public List<MetaCounterEntry> counters = new List<MetaCounterEntry>();
        public string selectedStartingProcessorId = "";
    }

    /// <summary>
    /// Persistent singleton, sibling to SaveSystem but deliberately separate: SaveSystem's
    /// flags are run-scoped and get wiped every GameManager.EndRun() (see SaveSystem.
    /// ResetToDefaults), while this survives runs entirely - meta-progression per
    /// roadmap.txt's "full run reset, unlocks/meta remain" design. Pure storage only - it
    /// never decides *when* something should unlock (that's UnlockManager's job), the same
    /// separation SaveSystem already has from the things that call SetFlag on it.
    /// </summary>
    public class MetaProgress : MonoBehaviour
    {
        public static MetaProgress Instance { get; private set; }

        private readonly HashSet<string> unlockedFlags = new HashSet<string>();
        private readonly Dictionary<string, int> counters = new Dictionary<string, int>();

        public string SelectedStartingProcessorId { get; private set; } = "";

        public string SavePath => Path.Combine(Application.persistentDataPath, "metaprogress.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            LoadFromDisk();
        }

        public bool HasUnlock(string key)
        {
            return !string.IsNullOrEmpty(key) && unlockedFlags.Contains(key);
        }

        /// <summary>Write-through: saves immediately on every new unlock rather than requiring
        /// an explicit Save() call elsewhere - losing a permanent unlock because the player
        /// forgot to save would defeat the point of it being permanent.</summary>
        public void Unlock(string key)
        {
            if (string.IsNullOrEmpty(key) || !unlockedFlags.Add(key))
            {
                return;
            }

            Save();
        }

        public int GetCounter(string key)
        {
            return !string.IsNullOrEmpty(key) && counters.TryGetValue(key, out int value) ? value : 0;
        }

        public void IncrementCounter(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            counters.TryGetValue(key, out int current);
            counters[key] = current + 1;
            Save();
        }

        public bool IsLoreDiscovered(string documentId) => HasUnlock($"lore_{documentId}_discovered");
        public void MarkLoreDiscovered(string documentId) => Unlock($"lore_{documentId}_discovered");

        public void SetSelectedStartingProcessor(string itemId)
        {
            SelectedStartingProcessorId = itemId ?? "";
            Save();
        }

        public void Save()
        {
            var data = new MetaProgressData
            {
                selectedStartingProcessorId = SelectedStartingProcessorId
            };

            data.unlockedFlags.AddRange(unlockedFlags);

            foreach (var kvp in counters)
            {
                data.counters.Add(new MetaCounterEntry { key = kvp.Key, value = kvp.Value });
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }

        public void LoadFromDisk()
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            var data = JsonUtility.FromJson<MetaProgressData>(File.ReadAllText(SavePath));

            unlockedFlags.Clear();
            foreach (string flag in data.unlockedFlags)
            {
                unlockedFlags.Add(flag);
            }

            counters.Clear();
            foreach (var entry in data.counters)
            {
                counters[entry.key] = entry.value;
            }

            SelectedStartingProcessorId = data.selectedStartingProcessorId ?? "";
        }
    }
}
