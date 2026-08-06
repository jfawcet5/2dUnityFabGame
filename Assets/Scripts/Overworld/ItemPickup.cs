using BeyProject.Core;
using BeyProject.Data;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// An item sitting on the floor. Auto-collected by walking over it (unlike NPCs, which
    /// require an explicit interact keypress) - picking up loose items shouldn't need a
    /// button press. Remembers collection via a save flag so it stays gone across scene
    /// reloads/loads, regardless of how the room got loaded.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private string pickupId;
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity = 1;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.IsItemCollected(pickupId))
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || Inventory.Instance == null)
            {
                return;
            }

            Inventory.Instance.AddItem(item, quantity);

            if (SaveSystem.Instance != null && !string.IsNullOrEmpty(pickupId))
            {
                SaveSystem.Instance.MarkItemCollected(pickupId);
            }

            Destroy(gameObject);
        }
    }
}
