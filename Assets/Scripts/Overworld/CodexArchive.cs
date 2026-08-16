using System.Collections.Generic;
using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Hub-only "read again" archive for permanently-discovered lore documents - sourced from
    /// MetaProgress (survives run resets), unlike the pause-menu InventoryUI's Documents tab
    /// which reflects only the current run's Inventory. Reuses DialogueUI for the reveal
    /// rather than a new scrollable list+details panel - the codebase's established idiom for
    /// text content (DialogueNPC, WorldInteractable ShowDialogue actions, death/victory lines),
    /// and lower risk than hand-authoring a new UI hierarchy.
    ///
    /// Its own dedicated IInterfaceLauncher (mirrors FabricationStation) rather than a new
    /// InteractionActionType - triggered via the hub's WorldInteractable Terminal, whose
    /// LaunchInterface action points at this object.
    /// </summary>
    public class CodexArchive : MonoBehaviour, IInterfaceLauncher
    {
        [SerializeField] private ItemDatabase itemDatabase;

        public void OpenInterface()
        {
            var lines = new List<string>();

            if (itemDatabase != null)
            {
                foreach (ItemDefinition item in itemDatabase.allItems)
                {
                    if (item != null && item.category == ItemCategory.Document
                        && MetaProgress.Instance != null && MetaProgress.Instance.IsLoreDiscovered(item.id))
                    {
                        lines.Add($"{item.displayName}: {item.description}");
                    }
                }
            }

            if (lines.Count == 0)
            {
                lines.Add("No archived records yet - explore the fab to find some.");
            }

            AudioManager.Instance?.PlayUIClick();
            DialogueUI.Instance?.Show("Archive", lines.ToArray(), null);
        }
    }
}
