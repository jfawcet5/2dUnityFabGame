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
                WorldAction action = actions[i];

                if (action.type == InteractionActionType.ShowDialogue && action.dialogue != null && DialogueUI.Instance != null)
                {
                    int resumeIndex = i + 1;
                    DialogueUI.Instance.Show(action.dialogue.speakerName, action.dialogue.lines,
                        () => Execute(actions, interactor, resumeIndex));
                    return; // pause here - the rest resumes via the dialogue's completion callback
                }

                RunImmediateAction(action, interactor);
            }
        }

        private static void RunImmediateAction(WorldAction action, GameObject interactor)
        {
            switch (action.type)
            {
                case InteractionActionType.GiveItem:
                    if (action.item != null && Inventory.Instance != null)
                    {
                        Inventory.Instance.AddItem(action.item, action.itemQuantity);
                        DialogueUI.Instance?.Show("", new string[] { $"Obtained: {action.item.displayName} x{action.itemQuantity}" }, null);
                    }
                    break;

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
                    // No dialogue asset assigned or no DialogueUI available - nothing to show.
                    break;
            }
        }
    }
}
