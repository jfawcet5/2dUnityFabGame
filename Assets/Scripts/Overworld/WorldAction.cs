using System;
using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.Overworld
{
    public enum InteractionActionType
    {
        ShowDialogue,
        GiveItem,
        LaunchInterface,
        UnlockDoor,
        SetFlag
    }

    [Serializable]
    public class WorldAction
    {
        public InteractionActionType type;

        [Header("ShowDialogue")]
        public DialogueSequence dialogue;

        [Header("GiveItem")]
        public ItemDefinition item;
        public int itemQuantity = 1;
        [Tooltip("If set, rolled fresh each time this action fires instead of always giving item.")]
        public LootPool lootPool;

        [Header("LaunchInteface")]
        public GameObject interfaceInteractable;

        [Header("UnlockDoor")]
        public Door doorToUnlock;

        [Header("SetFlag")]
        public string flagKey;
    }

    /// <summary>
    /// Runs an ordered WorldAction list, shared by WorldInteractable and DialogueNPC so the
    /// one tricky piece - a ShowDialogue action must pause the whole sequence until the
    /// dialogue box actually closes, not fire the next action while it's still open - only
    /// has to be implemented correctly once.
    /// </summary>
    public static class WorldActionExecutor
    {
        public static void Execute(WorldAction[] actions, GameObject interactor, int startIndex = 0)
        {
            if (actions == null)
            {
                return;
            }

            for (int i = startIndex; i < actions.Length; i++)
            {
                if (!RunImmediateAction(actions, interactor, i))
                {
                    return;
                }
            }
        }

        private static bool RunImmediateAction(WorldAction[] actions, GameObject interactor, int index)
        {
            WorldAction action = actions[index];

            switch (action.type)
            {
                case InteractionActionType.GiveItem:
                    return !GiveItem(action);

                case InteractionActionType.UnlockDoor:
                    if (action.doorToUnlock != null)
                    {
                        action.doorToUnlock.UnlockRemotely();
                    }
                    break;

                case InteractionActionType.SetFlag:
                    if (!string.IsNullOrEmpty(action.flagKey) && SaveSystem.Instance != null)
                    {
                        SaveSystem.Instance.SetFlag(action.flagKey);
                    }
                    break;

                case InteractionActionType.LaunchInterface:
                    IInterfaceLauncher launcher = action.interfaceInteractable.GetComponent<IInterfaceLauncher>();
                    if (launcher!= null)
                    {
                        launcher.OpenInterface();
                    }
                    break;

                case InteractionActionType.ShowDialogue:
                    if (action.dialogue != null && DialogueUI.Instance != null)
                    {
                        int resumeIndex = index + 1;
                        DialogueUI.Instance.Show(action.dialogue.speakerName, action.dialogue.lines,
                            () => Execute(actions, interactor, resumeIndex));
                        return false; // pause here - the rest resumes via the dialogue's completion callback
                    }
                    break;
            }

            return true;
        }

        private static bool GiveItem(WorldAction action)
        {
            ItemDefinition resolvedItem = action.item;
            int resolvedQuantity = action.itemQuantity;

            // Rolled fresh every time this action fires
            if (action.lootPool != null)
            {
                LootPoolEntry picked = action.lootPool.Roll();
                resolvedItem = picked?.item;
                resolvedQuantity = picked?.quantity ?? 0;
            }

            bool alreadyDiscovered = resolvedItem != null && resolvedItem.category == ItemCategory.Document
                && MetaProgress.Instance != null && MetaProgress.Instance.IsLoreDiscovered(resolvedItem.id);

            if (resolvedItem != null && !alreadyDiscovered && Inventory.Instance != null)
            {
                Inventory.Instance.AddItem(resolvedItem, resolvedQuantity);
                UnlockManager.Instance?.ReportItemDiscovered(resolvedItem);
                if (resolvedItem.category == ItemCategory.Document)
                {
                    MetaProgress.Instance?.MarkLoreDiscovered(resolvedItem.id);
                }
                DialogueUI.Instance?.Show("", new string[] { $"Obtained: {resolvedItem.displayName} x{resolvedQuantity}" }, null);
                return true;
            }

            return false;
        }
    }
}
