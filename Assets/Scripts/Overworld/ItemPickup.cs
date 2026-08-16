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
    /// Optional, independent extensions on top of that baseline - all no-ops when left blank,
    /// so every existing pickup is unaffected:
    ///   requiredFlag/lockedHint - the exact "optional gating" pattern WorldInteractable
    ///                             already uses, for a reward that should only be collectible
    ///                             after something else happens within the current run (e.g.
    ///                             clearing a room) - resets with SaveSystem every run.
    ///   requiredUnlock          - same idea but backed by MetaProgress instead of SaveSystem,
    ///                             for a pickup that should only ever appear once a permanent
    ///                             UnlockRule has fired (e.g. discovering the document that
    ///                             unlocks it) - survives GameManager.EndRun().
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
    ///   lootPool                - when set, resolved via LootResolver instead of always giving
    ///                             the fixed item field (sticky per run - see LootResolver).
    ///                             Either way, if the resolved item is a Document the player has
    ///                             already permanently discovered (MetaProgress), or the pool
    ///                             rolled nothing, this pickup self-destroys in Awake same as an
    ///                             already-collected one - a document you've read doesn't keep
    ///                             reappearing, and a whiffed roll just isn't there.
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

        [Header("Optional permanent unlock gate (MetaProgress, survives run resets)")]
        [SerializeField] private string requiredUnlock;

        [Header("Optional loot pool - rolled once per run instead of always giving item")]
        [SerializeField] private LootPool lootPool;
        [SerializeField] private ItemDatabase itemDatabase;

        [Header("Optional choice group - taking this destroys every sibling sharing the same id")]
        [SerializeField] private string choiceGroupId;
        [SerializeField]
        private string choiceWarning = "Taking this will overload the other prototypes here - you can only keep one.";

        private bool hintShown;

        public bool requiresInteraction = true;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(pickupId) && SaveSystem.Instance != null && SaveSystem.Instance.IsItemCollected(pickupId))
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

            // Runtime-spawned drops (EnemyBase.SpawnDrop) arrive with item and lootPool both
            // still null and call InitializeAsDrop right after Instantiate - skip resolution
            // entirely for them so this block doesn't destroy the still-being-set-up instance.
            if (item != null || lootPool != null)
            {
                item = LootResolver.Resolve(pickupId, lootPool, item, itemDatabase, out quantity);

                bool alreadyDiscovered = item != null && item.category == ItemCategory.Document
                    && MetaProgress.Instance != null && MetaProgress.Instance.IsLoreDiscovered(item.id);

                if (item == null || alreadyDiscovered)
                {
                    Destroy(gameObject);
                    return;
                }
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

        /// <summary>For a runtime-spawned drop (EnemyBase.SpawnDrop) - sets the already-resolved
        /// item directly, bypassing Awake's pool-resolution path (which already ran and found
        /// nothing to do, since this instance starts with item/lootPool both null).</summary>
        public void InitializeAsDrop(ItemDefinition droppedItem, int qty)
        {
            item = droppedItem;
            quantity = qty;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null && item != null)
            {
                spriteRenderer.sprite = PlaceholderSprite.CreateSquare(item.color);
            }
        }

        private string ChoiceGroupFlag => $"choice_group_{choiceGroupId}_taken";

        /// <summary>
        /// Only choice-group pickups do anything here - a normal pickup is already gone (via
        /// OnTriggerEnter2D) well before an interact press could ever reach it.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (string.IsNullOrEmpty(choiceGroupId) && !requiresInteraction)
            {
                return;
            }

            if (!IsGateSatisfied())
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

            if (requiresInteraction)
            {
                return;
            }

            if (!IsGateSatisfied())
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

        private bool IsGateSatisfied()
        {
            bool flagOk = string.IsNullOrEmpty(requiredFlag)
                || (SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(requiredFlag));
            bool unlockOk = string.IsNullOrEmpty(requiredUnlock)
                || (MetaProgress.Instance != null && MetaProgress.Instance.HasUnlock(requiredUnlock));
            return flagOk && unlockOk;
        }

        private void Collect()
        {
            if (Inventory.Instance == null)
            {
                Debug.Log("Inventory is null");
                return;
            }

            Inventory.Instance.AddItem(item, quantity);

            UnlockManager.Instance?.ReportItemDiscovered(item);

            if (item.category == ItemCategory.Document)
            {
                MetaProgress.Instance?.MarkLoreDiscovered(item.id);
            }

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
