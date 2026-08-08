using BeyProject.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.UI
{
    /// <summary>
    /// Sub-panel of the Inventory screen - shown when a row is clicked. Doesn't touch
    /// UIInputLock itself; InventoryUI still owns the lock while this is visible.
    /// </summary>
    public class ItemDetailsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text categoryText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        public void Show(ItemDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.enabled = definition.icon != null;
                iconImage.preserveAspect = true;
            }

            if (nameText != null)
            {
                nameText.text = definition.displayName;
            }

            if (categoryText != null)
            {
                categoryText.text = definition.category.ToString();
            }

            if (descriptionText != null)
            {
                descriptionText.text = definition.description;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
