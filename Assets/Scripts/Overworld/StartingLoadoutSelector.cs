using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Hub object representing one selectable starting "gun type" - the Processor slot per
    /// roadmap.txt's Ideas section. Interacting sets it as MetaProgress's
    /// SelectedStartingProcessorId, which ChipManager.ResetToDefaults() installs at the start
    /// of the next run. Own dedicated IInteractable with its own gating (parallel to how
    /// FabricationStation is a standalone interactable rather than routed through
    /// WorldActionExecutor) since "pick one of several options" isn't an existing
    /// InteractionActionType.
    /// </summary>
    public class StartingLoadoutSelector : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition processorModule;

        [Tooltip("MetaProgress key required to select this option. Empty = always available.")]
        [SerializeField] private string requiredUnlock;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.5f, 0.7f, 0.9f);

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
            if (processorModule == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredUnlock)
                && (MetaProgress.Instance == null || !MetaProgress.Instance.HasUnlock(requiredUnlock)))
            {
                DialogueUI.Instance?.Show("", new[] { "Not yet unlocked." }, null);
                return;
            }

            MetaProgress.Instance?.SetSelectedStartingProcessor(processorModule.id);
            AudioManager.Instance?.PlayUIClick();
            DialogueUI.Instance?.Show("", new[] { $"Starting loadout set: {processorModule.displayName}" }, null);
        }
    }
}
