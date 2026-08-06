using BeyProject.Core;
using BeyProject.Player;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Restores player health, on a cooldown rather than a one-shot flag. An exploration
    /// reward that stays useful on a return trip is worth detouring for twice; a consumed
    /// one is dead scenery the moment it's used. Reuses the existing IInteractable framework,
    /// sibling of FabricationStation.
    /// </summary>
    public class RepairStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private float healAmount = 45f;
        [SerializeField] private float cooldownSeconds = 25f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color readyColor = new Color(0.4f, 0.9f, 0.55f);
        [SerializeField] private Color spentColor = new Color(0.35f, 0.4f, 0.38f);

        private float readyAtTime;

        private bool IsReady => Time.time >= readyAtTime;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = PlaceholderSprite.CreateSquare(readyColor);
            }
        }

        private void Update()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = IsReady ? readyColor : spentColor;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!IsReady)
            {
                DialogueUI.Instance?.Show("Repair Station",
                    new[] { $"Recharging. Ready in {Mathf.CeilToInt(readyAtTime - Time.time)}s." }, null);
                return;
            }

            var health = interactor != null ? interactor.GetComponent<PlayerHealth>() : null;
            if (health == null)
            {
                return;
            }

            if (health.CurrentHealth >= health.MaxHealth)
            {
                DialogueUI.Instance?.Show("Repair Station", new[] { "Diagnostics nominal. No repairs needed." }, null);
                return;
            }

            health.Heal(healAmount);
            readyAtTime = Time.time + cooldownSeconds;

            AudioManager.Instance?.PlayUIClick();
            DialogueUI.Instance?.Show("Repair Station", new[] { $"Systems repaired. +{Mathf.RoundToInt(healAmount)} integrity." }, null);
        }
    }
}
