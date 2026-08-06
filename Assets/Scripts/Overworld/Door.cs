using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Locked -> solid collider, blocks movement, E-press checks for the required key item.
    /// Unlocked -> trigger collider, walking through travels to the target room. Two Door
    /// instances (one per room) sharing the same doorId form a pair; unlocking one marks a
    /// save flag both sides check, so re-entering either scene restores the correct state.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] private string doorId;
        [SerializeField] private bool startsLocked = true;
        [SerializeField] private ItemDefinition requiredKeyItem;

        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetSpawnPointId;
        [SerializeField] private Vector2 fallbackTargetPosition;

        [SerializeField] private BoxCollider2D doorCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;

        private Collider2D[] collisionResults = new Collider2D[10];

        private bool isLocked;

        private void Awake()
        {
            if (doorCollider == null)
            {
                doorCollider = GetComponent<BoxCollider2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            bool alreadyOpened = SaveSystem.Instance != null && SaveSystem.Instance.IsDoorOpened(doorId);
            SetLocked(startsLocked && !alreadyOpened);
        }

        public void Interact(GameObject interactor)
        {
            if (!isLocked)
            {
                return;
            }

            if (requiredKeyItem != null && Inventory.Instance != null && Inventory.Instance.HasItem(requiredKeyItem.id))
            {
                Unlock(playFeedback: true);
            }
            else
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayLocked();
                }

                string requirement = requiredKeyItem != null ? requiredKeyItem.displayName : "a key item";
                ShowFeedback($"Locked. Requires {requirement}.");
            }
        }

        /// <summary>Lets another system (e.g. a WorldInteractable's UnlockDoor action) open this door.</summary>
        public void UnlockRemotely()
        {
            if (isLocked)
            {
                Unlock(playFeedback: false);
            }
        }

        private void Unlock(bool playFeedback)
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.MarkDoorOpened(doorId);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDoor();
            }

            if (playFeedback && requiredKeyItem != null)
            {
                ShowFeedback($"The {requiredKeyItem.displayName} unlocks the door.", CheckOverlap);
            }

            SetLocked(false);
        }

        private void SetLocked(bool locked)
        {
            isLocked = locked;

            if (doorCollider != null)
            {
                doorCollider.isTrigger = !locked;
            }

            if (spriteRenderer != null)
            {
                Sprite sprite = locked ? lockedSprite : unlockedSprite;
                if (sprite != null)
                {
                    spriteRenderer.sprite = sprite;
                }
                else
                {
                    spriteRenderer.color = locked ? new Color(0.6f, 0.25f, 0.25f) : new Color(0.3f, 0.6f, 0.3f);
                }
            }
        }

        private void CheckOverlap()
        {
            Debug.Log("Check Overlap");

            ContactFilter2D filter = new ContactFilter2D();
            
            int hits = Physics2D.OverlapBox(doorCollider.bounds.center, doorCollider.bounds.size, 0, filter, collisionResults);

            for (int i = 0; i < hits; i++)
            {
                Collider2D other = collisionResults[i];
                if (other.gameObject == this.gameObject)
                {
                    continue;
                }

                if (other.CompareTag("Player") && GameManager.Instance != null)
                {
                    GameManager.Instance.TravelToRoom(targetSceneName, targetSpawnPointId, fallbackTargetPosition);
                }
            }
        }

        private void ShowFeedback(string line, System.Action callback = null)
        {
            if (DialogueUI.Instance != null)
            {
                DialogueUI.Instance.Show("Door", new[] { line }, callback);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isLocked || !other.CompareTag("Player") || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.TravelToRoom(targetSceneName, targetSpawnPointId, fallbackTargetPosition);
        }
    }
}
