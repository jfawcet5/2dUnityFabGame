using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// An item sitting on the floor. Auto-collected by walking over it (unlike NPCs, which
    /// require an explicit interact keypress) - picking up loose items shouldn't need a
    /// button press. Remembers collection via a save flag so it stays gone across scene
    /// reloads/loads, regardless of how the room got loaded.
    ///
    /// Two optional, independent extensions on top of that baseline - both no-ops when left
    /// blank, so every existing pickup is unaffected:
    ///   requiredFlag/lockedHint - the exact "optional gating" pattern WorldInteractable
    ///                             already uses, for a reward that should only be collectible
    ///                             after something else happens (e.g. clearing a room).
    ///   choiceGroupId           - marks this pickup as one option in a mutually-exclusive
    ///                             group; taking it destroys every sibling sharing the same
    ///                             id (including ones outside the loaded scene, via the same
    ///                             flag checked in Awake), so only one option can ever be
    ///                             kept. Unlike a normal pickup, a choice-group one does NOT
    ///                             auto-collect on touch - it requires an interact press and
    ///                             shows a warning first (via IInteractable/DialogueUI, the
    ///                             same "explain before it happens" pattern a locked Door
    ///                             already uses), so the player can't lose the other options
    ///                             by walking through them before realizing it was a choice.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupId;
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity = 1;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Optional gating")]
        [SerializeField] private string requiredFlag;
        [SerializeField] private string lockedHint;

        [Header("Optional choice group - taking this destroys every sibling sharing the same id")]
        [SerializeField] private string choiceGroupId;
        [SerializeField]
        private string choiceWarning = "Taking this will overload the other prototypes here - you can only keep one.";

        private bool hintShown;

        private void Awake()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.IsItemCollected(pickupId))
            {
                Destroy(gameObject);
                return;
            }

            if (!string.IsNullOrEmpty(choiceGroupId) && SaveSystem.Instance != null
                && SaveSystem.Instance.HasFlag(ChoiceGroupFlag))
            {
                Destroy(gameObject);
                return;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                Color color = item != null ? item.color : Color.yellow;
                spriteRenderer.sprite = PlaceholderSprite.CreateSquare(color);
            }
        }

        private string ChoiceGroupFlag => $"choice_group_{choiceGroupId}_taken";

        /// <summary>
        /// Only choice-group pickups do anything here - a normal pickup is already gone (via
        /// OnTriggerEnter2D) well before an interact press could ever reach it.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (string.IsNullOrEmpty(choiceGroupId))
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredFlag) && (SaveSystem.Instance == null || !SaveSystem.Instance.HasFlag(requiredFlag)))
            {
                if (!hintShown && !string.IsNullOrEmpty(lockedHint) && DialogueUI.Instance != null)
                {
                    hintShown = true;
                    DialogueUI.Instance.Show("", new[] { lockedHint }, null);
                }
                return;
            }

            if (DialogueUI.Instance != null && !string.IsNullOrEmpty(choiceWarning))
            {
                DialogueUI.Instance.Show("", new[] { choiceWarning }, Collect);
            }
            else
            {
                Collect();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Choice-group pickups are interact-only (see Interact above) so the warning is
            // guaranteed to be seen before the item - and its siblings - are gone.
            if (!string.IsNullOrEmpty(choiceGroupId) || !other.CompareTag("Player") || Inventory.Instance == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredFlag)
                && (SaveSystem.Instance == null || !SaveSystem.Instance.HasFlag(requiredFlag)))
            {
                if (!hintShown && !string.IsNullOrEmpty(lockedHint) && DialogueUI.Instance != null)
                {
                    hintShown = true;
                    DialogueUI.Instance.Show("", new[] { lockedHint }, null);
                }
                return;
            }

            Collect();
        }

        private void Collect()
        {
            if (Inventory.Instance == null)
            {
                Debug.Log("Inventory is null");
                return;
            }

            Inventory.Instance.AddItem(item, quantity);

            if (SaveSystem.Instance != null && !string.IsNullOrEmpty(pickupId))
            {
                SaveSystem.Instance.MarkItemCollected(pickupId);
            }

            if (!string.IsNullOrEmpty(choiceGroupId))
            {
                SaveSystem.Instance?.SetFlag(ChoiceGroupFlag);

                foreach (ItemPickup sibling in FindObjectsOfType<ItemPickup>())
                {
                    if (sibling != this && sibling.choiceGroupId == choiceGroupId)
                    {
                        Destroy(sibling.gameObject);
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}
