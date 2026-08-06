using System;
using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Overworld
{
    [Serializable]
    public class FlagDialogueOverride
    {
        public string requiredFlag;
        public ItemDefinition requiredItem;
        public DialogueSequence dialogue;
    }

    /// <summary>
    /// An NPC that talks. First meeting shows firstMeetingDialogue and (once) runs
    /// onFirstMeetActions; every later interaction shows repeatDialogue instead, and actions
    /// never re-run (no duplicate item grants on repeat visits or after a save load).
    ///
    /// flagDialogueOverrides lets the same NPC say something different once a flag is set or
    /// an item is held (checked in order, first satisfied entry wins), overriding whichever
    /// of firstMeeting/repeat would otherwise show - this is how NPC dialogue reacts to
    /// progression without hardcoding a one-off script per NPC.
    /// </summary>
    public class DialogueNPC : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcId;
        [SerializeField] private DialogueSequence firstMeetingDialogue;
        [SerializeField] private DialogueSequence repeatDialogue;
        [SerializeField] private WorldAction[] onFirstMeetActions;
        [SerializeField] private FlagDialogueOverride[] flagDialogueOverrides;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.6f, 0.6f, 0.9f);

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = PlaceholderSprite.CreateSquare(fallbackColor);
            }
        }

        public void Interact(GameObject interactor)
        {
            bool alreadyMet = SaveSystem.Instance != null && SaveSystem.Instance.HasMetNpc(npcId);
            DialogueSequence overrideSequence = ResolveOverride();

            if (!alreadyMet)
            {
                DialogueSequence sequence = overrideSequence != null ? overrideSequence : firstMeetingDialogue;
                if (sequence == null || DialogueUI.Instance == null)
                {
                    RunFirstMeetActions(interactor);
                    return;
                }

                DialogueUI.Instance.Show(sequence.speakerName, sequence.lines, () =>
                {
                    RunFirstMeetActions(interactor);
                });
            }
            else
            {
                DialogueSequence sequence = overrideSequence != null
                    ? overrideSequence
                    : (repeatDialogue != null ? repeatDialogue : firstMeetingDialogue);

                if (sequence != null && DialogueUI.Instance != null)
                {
                    DialogueUI.Instance.Show(sequence.speakerName, sequence.lines, null);
                }
            }
        }

        private DialogueSequence ResolveOverride()
        {
            if (flagDialogueOverrides == null)
            {
                return null;
            }

            foreach (FlagDialogueOverride entry in flagDialogueOverrides)
            {
                if (entry == null || entry.dialogue == null)
                {
                    continue;
                }

                bool flagOk = string.IsNullOrEmpty(entry.requiredFlag) || (SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(entry.requiredFlag));
                bool itemOk = entry.requiredItem == null || (Inventory.Instance != null && Inventory.Instance.HasItem(entry.requiredItem.id));

                if (flagOk && itemOk)
                {
                    return entry.dialogue;
                }
            }

            return null;
        }

        private void RunFirstMeetActions(GameObject interactor)
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.MarkNpcMet(npcId);
            }

            WorldActionExecutor.Execute(onFirstMeetActions, interactor);
        }
    }
}
