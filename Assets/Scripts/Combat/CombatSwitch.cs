using System;
using BeyProject.Core;
using BeyProject.Overworld;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// Objective prop the player presses E on. Reuses the existing IInteractable/
    /// PlayerInteractor framework rather than adding a second interaction path, so a switch
    /// behaves exactly like every other prompt in the game. Activation persists via the same
    /// SaveSystem flag idiom, so a half-finished objective survives leaving and returning.
    /// </summary>
    public class CombatSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private string switchId;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.6f, 0.3f, 0.3f);
        [SerializeField] private Color activeColor = new Color(0.4f, 0.95f, 0.5f);

        public event Action Activated;

        public bool IsActivated { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = PlaceholderSprite.CreateSquare(inactiveColor);
            }

            if (!string.IsNullOrEmpty(switchId) &&
                SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(ActivatedFlag))
            {
                IsActivated = true;
            }

            ApplyColor();
        }

        private string ActivatedFlag => $"combat_switch_{switchId}";

        public void Interact(GameObject interactor)
        {
            if (IsActivated)
            {
                return;
            }

            IsActivated = true;

            if (!string.IsNullOrEmpty(switchId))
            {
                SaveSystem.Instance?.SetFlag(ActivatedFlag);
            }

            AudioManager.Instance?.PlayUIClick();
            CombatFeedback.Telegraph(transform.position, activeColor, 1.4f);
            ApplyColor();

            Activated?.Invoke();
        }

        private void ApplyColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = IsActivated ? activeColor : inactiveColor;
            }
        }
    }
}
