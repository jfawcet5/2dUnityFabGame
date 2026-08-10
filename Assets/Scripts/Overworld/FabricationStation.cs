using BeyProject.Core;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Opens the Fabrication UI. Its own dedicated IInteractable (sibling of
    /// OverworldOpponent) rather than routed through WorldActionExecutor, since "open a
    /// custom panel" isn't one of the existing InteractionActionType cases.
    /// </summary>
    public class FabricationStation : MonoBehaviour, IInterfaceLauncher
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.6f, 0.5f, 0.7f);

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
            AudioManager.Instance?.PlayUIClick();
            FabricationUI.Instance?.Show();
        }

        public void OpenInterface()
        {
            AudioManager.Instance?.PlayUIClick();
            FabricationUI.Instance?.Show();
        }
    }
}
