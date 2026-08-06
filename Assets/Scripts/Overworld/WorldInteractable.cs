using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Generic interactable for machines/cabinets/lockers/computers/crates - one flexible
    /// component driven by an ordered WorldAction list instead of a bespoke script per
    /// object type. See WorldActionExecutor for how the action sequence actually runs.
    ///
    /// Optionally gated by an item and/or a flag (both optional, both checked if set) - this
    /// is how "disabled terminal"/"security checkpoint" style environmental progression is
    /// built: no new gameplay system, just a condition on the same component every other
    /// interactable already uses.
    /// </summary>
    public class WorldInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactableId;
        [SerializeField] private bool oneShot = true;
        [SerializeField] private WorldAction[] actions;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.5f, 0.5f, 0.55f);

        [Header("Optional gating")]
        [SerializeField] private ItemDefinition requiredItem;
        [SerializeField] private string requiredFlag;
        [SerializeField] private DialogueSequence lockedDialogue;

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
            if (oneShot && SaveSystem.Instance != null && SaveSystem.Instance.IsInteractableUsed(interactableId))
            {
                return;
            }

            if (!IsGateSatisfied())
            {
                if (lockedDialogue != null && DialogueUI.Instance != null)
                {
                    DialogueUI.Instance.Show(lockedDialogue.speakerName, lockedDialogue.lines, null);
                }
                return;
            }

            if (oneShot && SaveSystem.Instance != null)
            {
                SaveSystem.Instance.MarkInteractableUsed(interactableId);
            }

            WorldActionExecutor.Execute(actions, interactor);
        }

        private bool IsGateSatisfied()
        {
            bool itemOk = requiredItem == null || (Inventory.Instance != null && Inventory.Instance.HasItem(requiredItem.id));
            bool flagOk = string.IsNullOrEmpty(requiredFlag) || (SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(requiredFlag));
            return itemOk && flagOk;
        }
    }
}
