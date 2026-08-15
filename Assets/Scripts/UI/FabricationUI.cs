using System.Text;
using BeyProject.Core;
using BeyProject.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.UI
{
    /// <summary>
    /// Fabrication Station screen: install owned chip modules and view the current chip
    /// configuration ("Chip Architecture"). Opened only via FabricationStation.Interact() and
    /// closed only via the Close button - deliberately has no key-toggle Update() of its own
    /// (same as ItemDetailsPanel), sidestepping the "Update() on a self-deactivating
    /// GameObject" bug class that Pause/Inventory needed dedicated managers to fix.
    ///
    /// Selecting an available module previews it: the stat panel shows what each number
    /// would become and colours gains green / losses red. Without that, tradeoff modules are
    /// indistinguishable from upgrades until several minutes of play later.
    /// </summary>
    public class FabricationUI : MonoBehaviour
    {
        public static FabricationUI Instance { get; private set; }

        private static readonly ChipSlotType[] AllSlots =
        {
            ChipSlotType.Processor, ChipSlotType.Cache, ChipSlotType.Battery, ChipSlotType.Cooling
        };

        private static readonly Color GainColor = new Color(0.5f, 1f, 0.6f);
        private static readonly Color LossColor = new Color(1f, 0.5f, 0.45f);
        private static readonly Color NeutralColor = new Color(0.85f, 0.85f, 0.9f);

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private Text coreText;
        [SerializeField] private Transform installedListParent;
        [SerializeField] private Transform availableListParent;
        [SerializeField] private Text statsText;
        [SerializeField] private Text moduleDetailText;
        [SerializeField] private GameObject architecturePanelRoot;
        [SerializeField] private Text architectureText;
        [SerializeField] private Button viewArchitectureButton;
        [SerializeField] private Button closeButton;

        private Font font;
        private ItemDefinition previewModule;

        private void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (viewArchitectureButton != null)
            {
                viewArchitectureButton.onClick.AddListener(ToggleArchitectureView);
            }

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

        public void Show()
        {
            UIInputLock.TryAcquire(this);

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (architecturePanelRoot != null)
            {
                architecturePanelRoot.SetActive(false);
            }

            previewModule = null;
            Refresh();
        }

        public void Hide()
        {
            UIInputLock.Release(this);
            previewModule = null;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void ToggleArchitectureView()
        {
            if (architecturePanelRoot == null)
            {
                return;
            }

            bool showingArchitecture = !architecturePanelRoot.activeSelf;
            architecturePanelRoot.SetActive(showingArchitecture);

            if (showingArchitecture)
            {
                RefreshArchitectureView();
            }
        }

        private void Refresh()
        {
            ClearChildren(installedListParent);
            ClearChildren(availableListParent);

            if (ChipManager.Instance == null || itemDatabase == null)
            {
                return;
            }

            ItemDefinition processor = ChipManager.Instance.GetEquipped(ChipSlotType.Processor);
            if (coreText != null)
            {
                coreText.text = $"Core: {(processor != null ? processor.displayName : "Standard Processor")}";
            }

            foreach (ChipSlotType slot in AllSlots)
            {
                BuildInstalledRow(slot, ChipManager.Instance.GetEquipped(slot));
            }

            if (Inventory.Instance != null)
            {
                foreach (var kvp in Inventory.Instance.GetAllCounts())
                {
                    ItemDefinition definition = itemDatabase.GetById(kvp.Key);
                    if (definition == null || definition.category != ItemCategory.ChipModule)
                    {
                        continue;
                    }

                    if (ChipManager.Instance.GetEquipped(definition.chipModule.chipSlot) == definition)
                    {
                        continue;
                    }

                    BuildAvailableRow(definition);
                }
            }

            RefreshStats();
            RefreshModuleDetail();
        }

        /// <summary>
        /// Live stats, or live-vs-preview when a module is selected. Comparing against
        /// ChipManager's own preview path (rather than recomputing here) means the numbers
        /// shown can't drift from what installing actually does.
        /// </summary>
        private void RefreshStats()
        {
            if (statsText == null || ChipManager.Instance == null)
            {
                return;
            }

            ChipStats current = ChipManager.Instance.GetCurrentStats();
            bool previewing = previewModule != null;
            ChipStats preview = previewing ? ChipManager.Instance.GetStatsWithPreview(previewModule) : current;

            var builder = new StringBuilder();
            builder.AppendLine(previewing ? $"PROJECTED  —  {previewModule.displayName}" : "CURRENT CHIP OUTPUT");
            builder.AppendLine();

            AppendStat(builder, "Damage / shot", current.damageMultiplier * 10f, preview.damageMultiplier * 10f, higherIsBetter: true, "0.#");
            AppendStat(builder, "Projectiles", current.projectileCount, preview.projectileCount, true, "0");
            AppendStat(builder, "Fire rate", current.fireRateMultiplier, preview.fireRateMultiplier, true, "0.00x");
            AppendStat(builder, "Burst size", current.burstCapacity, preview.burstCapacity, true, "0");
            AppendStat(builder, "Energy pool", current.maxEnergy, preview.maxEnergy, true, "0");
            AppendStat(builder, "Energy / shot", current.shotEnergyCost, preview.shotEnergyCost, higherIsBetter: false, "0.#");
            AppendStat(builder, "Energy regen", current.energyRegenRate, preview.energyRegenRate, true, "0.#");
            AppendStat(builder, "Reload speed", current.reloadSpeedMultiplier, preview.reloadSpeedMultiplier, true, "0.00x");
            AppendStat(builder, "Move speed", current.moveSpeedMultiplier, preview.moveSpeedMultiplier, true, "0.00x");
            AppendStat(builder, "Shot speed", current.projectileSpeedMultiplier, preview.projectileSpeedMultiplier, true, "0.00x");
            AppendStat(builder, "Shot size", current.projectileSizeMultiplier, preview.projectileSizeMultiplier, true, "0.00x");

            builder.AppendLine();
            builder.AppendLine($"Homing: {(preview.homing ? "Yes" : "No")}");
            builder.Append($"Output: {ChipManager.Instance.GetOutputDescription()}");

            statsText.text = builder.ToString();
        }

        private static void AppendStat(StringBuilder builder, string label, float current, float preview,
            bool higherIsBetter, string format)
        {
            string suffix = format.EndsWith("x") ? "x" : "";
            string numberFormat = suffix.Length > 0 ? format.Substring(0, format.Length - 1) : format;

            string currentText = current.ToString(numberFormat) + suffix;

            if (Mathf.Approximately(current, preview))
            {
                builder.AppendLine($"{label,-16}{currentText}");
                return;
            }

            bool improved = higherIsBetter ? preview > current : preview < current;
            string hex = ColorUtility.ToHtmlStringRGB(improved ? GainColor : LossColor);
            string previewText = preview.ToString(numberFormat) + suffix;

            builder.AppendLine($"{label,-16}{currentText}  <color=#{hex}>-> {previewText}</color>");
        }

        private void RefreshModuleDetail()
        {
            if (moduleDetailText == null)
            {
                return;
            }

            if (previewModule == null)
            {
                moduleDetailText.text = "Select a component to preview its effect on the chip.";
                moduleDetailText.color = NeutralColor;
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine(previewModule.description);

            if (!string.IsNullOrEmpty(previewModule.chipModule.chipTradeoffDescription))
            {
                string hex = ColorUtility.ToHtmlStringRGB(LossColor);
                builder.AppendLine();
                builder.Append($"<color=#{hex}>Tradeoff: {previewModule.chipModule.chipTradeoffDescription}</color>");
            }

            moduleDetailText.text = builder.ToString();
            moduleDetailText.color = NeutralColor;
        }

        private void RefreshArchitectureView()
        {
            if (architectureText == null || ChipManager.Instance == null)
            {
                return;
            }

            ItemDefinition processor = ChipManager.Instance.GetEquipped(ChipSlotType.Processor);
            ItemDefinition cache = ChipManager.Instance.GetEquipped(ChipSlotType.Cache);
            ItemDefinition battery = ChipManager.Instance.GetEquipped(ChipSlotType.Battery);
            ItemDefinition cooling = ChipManager.Instance.GetEquipped(ChipSlotType.Cooling);
            ChipStats stats = ChipManager.Instance.GetCurrentStats();

            architectureText.text =
                "CHIP ARCHITECTURE\n\n" +
                $"Processing: {(processor != null ? processor.displayName : "Standard Processor")}\n" +
                $"Memory:     {(cache != null ? cache.displayName : "Standard Cache")}\n" +
                $"Power:      {(battery != null ? battery.displayName : "Standard Battery")}\n" +
                $"Cooling:    {(cooling != null ? cooling.displayName : "Standard Cooling")}\n\n" +
                $"Output: {ChipManager.Instance.GetOutputDescription()}\n\n" +
                $"Each shot fires {stats.projectileCount} projectile(s) for " +
                $"{(stats.damageMultiplier * 10f):0.#} damage each, costing " +
                $"{stats.shotEnergyCost:0.#} energy of a {stats.maxEnergy:0} pool.\n" +
                $"You can fire {stats.burstCapacity} time(s) before reloading.";
        }

        private void BuildInstalledRow(ChipSlotType slot, ItemDefinition definition)
        {
            if (installedListParent == null)
            {
                return;
            }

            bool empty = definition == null;
            string label = empty ? $"{slot}: Standard" : $"{slot}: {definition.displayName}";

            var rowGO = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowGO.transform.SetParent(installedListParent, false);
            rowGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, empty ? 0.03f : 0.08f);
            rowGO.GetComponent<LayoutElement>().preferredHeight = 30f;

            var labelGO = new GameObject("Label", typeof(Text));
            labelGO.transform.SetParent(rowGO.transform, false);
            Text text = labelGO.GetComponent<Text>();
            text.font = font;
            text.fontSize = 15;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = empty ? new Color(0.55f, 0.55f, 0.6f) : new Color(0.75f, 1f, 0.8f);
            text.text = label;
            StretchInto(labelGO, 10f, empty ? -10f : -72f);

            if (empty)
            {
                return;
            }

            // Uninstalling matters as much as installing once modules have real downsides -
            // without it a tradeoff module is a one-way door.
            Button remove = BuildSmallButton(rowGO.transform, "Remove");
            remove.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayUIClick();
                ChipManager.Instance?.Uninstall(slot);
                previewModule = null;
                Refresh();
            });
        }

        private void BuildAvailableRow(ItemDefinition definition)
        {
            if (availableListParent == null)
            {
                return;
            }

            var rowGO = new GameObject(definition.displayName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rowGO.transform.SetParent(availableListParent, false);

            bool selected = previewModule == definition;
            rowGO.GetComponent<Image>().color = selected ? new Color(0.4f, 0.7f, 1f, 0.22f) : new Color(1f, 1f, 1f, 0.06f);
            rowGO.GetComponent<LayoutElement>().preferredHeight = 38f;

            var labelGO = new GameObject("Label", typeof(Text));
            labelGO.transform.SetParent(rowGO.transform, false);
            Text label = labelGO.GetComponent<Text>();
            label.font = font;
            label.fontSize = 15;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.text = $"{definition.displayName}\n<size=12><color=#9aa0b5>{definition.chipModule.chipSlot} slot</color></size>";
            StretchInto(labelGO, 10f, -76f);

            // Clicking the row previews; the explicit Install button commits. Selecting and
            // installing being the same click would mean never seeing the projected stats.
            rowGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayUIClick();
                previewModule = previewModule == definition ? null : definition;
                Refresh();
            });

            Button install = BuildSmallButton(rowGO.transform, "Install");
            install.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayUIClick();
                ChipManager.Instance?.Install(definition);
                previewModule = null;
                Refresh();
            });
        }

        private Button BuildSmallButton(Transform parent, string caption)
        {
            var buttonGO = new GameObject($"{caption}Button", typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);
            buttonGO.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.36f);

            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-6f, 0f);
            rect.sizeDelta = new Vector2(62f, 24f);

            var captionGO = new GameObject("Text", typeof(Text));
            captionGO.transform.SetParent(buttonGO.transform, false);
            Text text = captionGO.GetComponent<Text>();
            text.font = font;
            text.fontSize = 13;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = caption;
            StretchInto(captionGO, 0f, 0f);

            return buttonGO.GetComponent<Button>();
        }

        private static void StretchInto(GameObject go, float leftInset, float rightInset)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(leftInset, 0f);
            rect.offsetMax = new Vector2(rightInset, 0f);
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
