using BeyProject.Core;
using BeyProject.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.UI
{
    /// <summary>
    /// Pure view for the inventory screen. InventoryManager owns the open/closed state, the
    /// "I" toggle, and UIInputLock ownership - it calls ShowPanel()/HidePanel() here. Rows are
    /// built at runtime from Inventory.GetAllCounts() joined against ItemDatabase, split into
    /// Collectibles / Documents / Key Items - no prefab asset, same "build UI via code"
    /// approach the bootstrapper already uses elsewhere.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private Transform collectiblesListParent;
        [SerializeField] private Transform documentsListParent;
        [SerializeField] private Transform keyItemsListParent;
        [SerializeField] private ItemDetailsPanel detailsPanel;

        private Font font;

        private void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ShowPanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (detailsPanel != null)
            {
                detailsPanel.Hide();
            }

            Refresh();
        }

        public void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Refresh()
        {
            ClearChildren(collectiblesListParent);
            ClearChildren(documentsListParent);
            ClearChildren(keyItemsListParent);

            if (Inventory.Instance == null || itemDatabase == null)
            {
                return;
            }

            foreach (var kvp in Inventory.Instance.GetAllCounts())
            {
                ItemDefinition definition = itemDatabase.GetById(kvp.Key);
                if (definition == null)
                {
                    continue;
                }

                Transform parent;
                if (definition.isKeyItem)
                {
                    parent = keyItemsListParent;
                }
                else if (definition.category == ItemCategory.Document)
                {
                    parent = documentsListParent;
                }
                else
                {
                    parent = collectiblesListParent;
                }

                BuildRow(parent, definition, kvp.Value);
            }
        }

        private void BuildRow(Transform parent, ItemDefinition definition, int count)
        {
            if (parent == null)
            {
                return;
            }

            var rowGO = new GameObject(definition.displayName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rowGO.transform.SetParent(parent, false);
            rowGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            rowGO.GetComponent<LayoutElement>().preferredHeight = 40f;

            var iconGO = new GameObject("Icon", typeof(Image));
            iconGO.transform.SetParent(rowGO.transform, false);
            Image iconImage = iconGO.GetComponent<Image>();
            iconImage.sprite = definition.icon;
            iconImage.enabled = definition.icon != null;
            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(6f, 0f);
            iconRect.sizeDelta = new Vector2(30f, 30f);

            var labelGO = new GameObject("Label", typeof(Text));
            labelGO.transform.SetParent(rowGO.transform, false);
            Text label = labelGO.GetComponent<Text>();
            label.font = font;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.text = count > 1 ? $"{definition.displayName} x{count}" : definition.displayName;
            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(44f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);

            rowGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayUIClick();
                if (detailsPanel != null)
                {
                    detailsPanel.Show(definition);
                }
            });
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
