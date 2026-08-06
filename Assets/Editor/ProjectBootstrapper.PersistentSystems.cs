using BeyProject.Core;
using BeyProject.Data;
using BeyProject.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        /// <summary>
        /// GameManager/Inventory/SaveSystem/AudioManager/EventSystem + the persistent UI
        /// canvas (Dialogue/Pause/Inventory/ItemDetails), saved as one prefab so every scene
        /// can pull it in via PersistentSystemsLoader regardless of which scene Play mode
        /// starts from.
        /// </summary>
        private static void BuildPersistentSystemsPrefab(ItemDatabase itemDatabase)
        {
            var root = new GameObject("PersistentSystems", typeof(GameManager), typeof(Inventory), typeof(SaveSystem), typeof(AudioManager),
                typeof(PauseManager), typeof(InventoryManager), typeof(ChipManager));

            var chipManagerSO = new SerializedObject(root.GetComponent<ChipManager>());
            chipManagerSO.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
            chipManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemGO.transform.SetParent(root.transform);

            var canvasGO = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(root.transform);
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);

            BuildDialoguePanel(canvasGO.transform);
            BuildPausePanel(canvasGO.transform);
            BuildInventoryPanel(canvasGO.transform, itemDatabase);
            BuildFabricationPanel(canvasGO.transform, itemDatabase);
            BuildCombatHudPanel(canvasGO.transform);
            BuildRoomTitlePanel(canvasGO.transform);
            BuildSceneFadePanel(canvasGO.transform); // must be built last - rendered on top of every other panel during transitions

            PrefabUtility.SaveAsPrefabAsset(root, $"{ResourcesFolder}/PersistentSystems.prefab");
            Object.DestroyImmediate(root);
        }

        // Every panel root GameObject below is left ACTIVE in the saved prefab, on purpose:
        // each controller's own Awake() hides its panel after setting its static Instance.
        // If we deactivated the panel here instead, Awake would never run until something
        // reactivated it - which nothing would, since Instance would never get set. Don't
        // "clean up" by disabling these before saving the prefab.

        private static void BuildDialoguePanel(Transform canvasTransform)
        {
            var panelGO = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image), typeof(DialogueUI));
            panelGO.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.02f);
            panelRect.anchorMax = new Vector2(0.95f, 0.22f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            Text speaker = CreateText(panelGO.transform, "SpeakerText", "Speaker", 16, TextAnchor.UpperLeft,
                new Color(0.8f, 0.85f, 1f), new Vector2(0.02f, 0.62f), new Vector2(0.6f, 0.95f), Vector2.zero, Vector2.zero);

            Text line = CreateText(panelGO.transform, "LineText", "", 18, TextAnchor.UpperLeft, Color.white,
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.62f), Vector2.zero, Vector2.zero);

            Button continueButton = CreateButton(panelGO.transform, "ContinueButton", "Continue (E)",
                new Vector2(0.78f, 0.02f), new Vector2(0.98f, 0.2f));

            var so = new SerializedObject(panelGO.GetComponent<DialogueUI>());
            so.FindProperty("panelRoot").objectReferenceValue = panelGO;
            so.FindProperty("speakerText").objectReferenceValue = speaker;
            so.FindProperty("lineText").objectReferenceValue = line;
            so.FindProperty("continueButton").objectReferenceValue = continueButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPausePanel(Transform canvasTransform)
        {
            var panelGO = new GameObject("PausePanel", typeof(RectTransform), typeof(Image), typeof(PauseMenuController));
            panelGO.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            CreateText(panelGO.transform, "Title", "Paused", 28, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.85f), Vector2.zero, Vector2.zero);

            Button resume = CreateButton(panelGO.transform, "ResumeButton", "Resume", new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.65f));
            Button save = CreateButton(panelGO.transform, "SaveButton", "Save", new Vector2(0.35f, 0.4f), new Vector2(0.65f, 0.5f));
            Button quit = CreateButton(panelGO.transform, "QuitButton", "Quit to Main Menu", new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.35f));

            Text saved = CreateText(panelGO.transform, "SavedFeedback", "Saved!", 18, TextAnchor.MiddleCenter,
                new Color(0.5f, 1f, 0.5f), new Vector2(0.35f, 0.18f), new Vector2(0.65f, 0.24f), Vector2.zero, Vector2.zero);

            var so = new SerializedObject(panelGO.GetComponent<PauseMenuController>());
            so.FindProperty("panelRoot").objectReferenceValue = panelGO;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("saveButton").objectReferenceValue = save;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("savedFeedbackText").objectReferenceValue = saved;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildInventoryPanel(Transform canvasTransform, ItemDatabase itemDatabase)
        {
            var panelGO = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image), typeof(InventoryUI));
            panelGO.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelGO.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

            CreateText(panelGO.transform, "Title", "Inventory (I to close)", 24, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 0.9f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            CreateText(panelGO.transform, "CollectiblesHeader", "Collectibles", 18, TextAnchor.MiddleLeft,
                new Color(0.8f, 0.8f, 0.9f), new Vector2(0.02f, 0.82f), new Vector2(0.33f, 0.9f), Vector2.zero, Vector2.zero);

            Transform collectiblesList = CreateListContainer(panelGO.transform, "CollectiblesList",
                new Vector2(0.02f, 0.1f), new Vector2(0.33f, 0.82f));

            CreateText(panelGO.transform, "DocumentsHeader", "Documents", 18, TextAnchor.MiddleLeft,
                new Color(0.75f, 0.85f, 0.95f), new Vector2(0.35f, 0.82f), new Vector2(0.65f, 0.9f), Vector2.zero, Vector2.zero);

            Transform documentsList = CreateListContainer(panelGO.transform, "DocumentsList",
                new Vector2(0.35f, 0.1f), new Vector2(0.65f, 0.82f));

            CreateText(panelGO.transform, "KeyItemsHeader", "Key Items", 18, TextAnchor.MiddleLeft,
                new Color(1f, 0.9f, 0.6f), new Vector2(0.67f, 0.82f), new Vector2(0.98f, 0.9f), Vector2.zero, Vector2.zero);

            Transform keyItemsList = CreateListContainer(panelGO.transform, "KeyItemsList",
                new Vector2(0.67f, 0.1f), new Vector2(0.98f, 0.82f));

            ItemDetailsPanel detailsPanel = BuildItemDetailsPanel(panelGO.transform);

            var so = new SerializedObject(panelGO.GetComponent<InventoryUI>());
            so.FindProperty("panelRoot").objectReferenceValue = panelGO;
            so.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
            so.FindProperty("collectiblesListParent").objectReferenceValue = collectiblesList;
            so.FindProperty("documentsListParent").objectReferenceValue = documentsList;
            so.FindProperty("keyItemsListParent").objectReferenceValue = keyItemsList;
            so.FindProperty("detailsPanel").objectReferenceValue = detailsPanel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemDetailsPanel BuildItemDetailsPanel(Transform parent)
        {
            var panelGO = new GameObject("ItemDetailsPanel", typeof(RectTransform), typeof(Image), typeof(ItemDetailsPanel));
            panelGO.transform.SetParent(parent, false);
            RectTransform rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.15f);
            rect.anchorMax = new Vector2(0.85f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panelGO.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.98f);

            var iconGO = new GameObject("Icon", typeof(Image));
            iconGO.transform.SetParent(panelGO.transform, false);
            Image icon = iconGO.GetComponent<Image>();
            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.65f);
            iconRect.anchorMax = new Vector2(0.35f, 0.95f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Text name = CreateText(panelGO.transform, "NameText", "", 22, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0.4f, 0.8f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

            Text category = CreateText(panelGO.transform, "CategoryText", "", 16, TextAnchor.MiddleLeft,
                new Color(0.7f, 0.8f, 1f), new Vector2(0.4f, 0.68f), new Vector2(0.95f, 0.8f), Vector2.zero, Vector2.zero);

            Text description = CreateText(panelGO.transform, "DescriptionText", "", 16, TextAnchor.UpperLeft,
                new Color(0.9f, 0.9f, 0.9f), new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.6f), Vector2.zero, Vector2.zero);

            Button close = CreateButton(panelGO.transform, "CloseButton", "Close", new Vector2(0.4f, 0.03f), new Vector2(0.6f, 0.12f));

            var so = new SerializedObject(panelGO.GetComponent<ItemDetailsPanel>());
            so.FindProperty("panelRoot").objectReferenceValue = panelGO;
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("categoryText").objectReferenceValue = category;
            so.FindProperty("descriptionText").objectReferenceValue = description;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();

            return panelGO.GetComponent<ItemDetailsPanel>();
        }

        private static void BuildFabricationPanel(Transform canvasTransform, ItemDatabase itemDatabase)
        {
            var panelGO = new GameObject("FabricationPanel", typeof(RectTransform), typeof(Image), typeof(FabricationUI));
            panelGO.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelGO.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.09f, 0.97f);

            CreateText(panelGO.transform, "Title", "Fabrication Station", 24, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 0.9f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            Text coreText = CreateText(panelGO.transform, "CoreText", "Core: Standard Processor", 18, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.85f, 1f), new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.9f), Vector2.zero, Vector2.zero);

            // Three columns: what's installed, what's available, and what the numbers do.
            // The stat column is the whole point of the redesign - a module's tradeoff has to
            // be visible at the moment of choosing, not discovered in the next fight.
            CreateText(panelGO.transform, "InstalledHeader", "Installed Modifications", 15, TextAnchor.MiddleLeft,
                new Color(0.75f, 1f, 0.8f), new Vector2(0.03f, 0.73f), new Vector2(0.35f, 0.8f), Vector2.zero, Vector2.zero);
            Transform installedList = CreateListContainer(panelGO.transform, "InstalledList",
                new Vector2(0.03f, 0.42f), new Vector2(0.35f, 0.73f));

            CreateText(panelGO.transform, "AvailableHeader", "Available Components", 15, TextAnchor.MiddleLeft,
                new Color(1f, 0.9f, 0.7f), new Vector2(0.03f, 0.35f), new Vector2(0.35f, 0.42f), Vector2.zero, Vector2.zero);
            Transform availableList = CreateListContainer(panelGO.transform, "AvailableList",
                new Vector2(0.03f, 0.04f), new Vector2(0.35f, 0.35f));

            CreateText(panelGO.transform, "StatsHeader", "Chip Output", 15, TextAnchor.MiddleLeft,
                new Color(0.8f, 0.88f, 1f), new Vector2(0.38f, 0.73f), new Vector2(0.7f, 0.8f), Vector2.zero, Vector2.zero);
            Text statsText = CreateText(panelGO.transform, "StatsText", "", 13, TextAnchor.UpperLeft,
                new Color(0.88f, 0.88f, 0.94f), new Vector2(0.38f, 0.14f), new Vector2(0.7f, 0.73f), Vector2.zero, Vector2.zero);

            CreateText(panelGO.transform, "DetailHeader", "Component Detail", 15, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.8f, 1f), new Vector2(0.72f, 0.73f), new Vector2(0.97f, 0.8f), Vector2.zero, Vector2.zero);
            Text moduleDetailText = CreateText(panelGO.transform, "ModuleDetailText",
                "Select a component to preview its effect on the chip.", 13, TextAnchor.UpperLeft,
                new Color(0.85f, 0.85f, 0.9f), new Vector2(0.72f, 0.14f), new Vector2(0.97f, 0.73f), Vector2.zero, Vector2.zero);

            Button viewArchitectureButton = CreateButton(panelGO.transform, "ViewArchitectureButton", "View Chip Architecture",
                new Vector2(0.38f, 0.03f), new Vector2(0.66f, 0.11f));
            Button closeButton = CreateButton(panelGO.transform, "CloseButton", "Close",
                new Vector2(0.72f, 0.03f), new Vector2(0.97f, 0.11f));

            // Chip Architecture sub-view - a read-only text block overlaying the same lists area.
            var architectureGO = new GameObject("ArchitecturePanel", typeof(RectTransform), typeof(Image));
            architectureGO.transform.SetParent(panelGO.transform, false);
            RectTransform architectureRect = architectureGO.GetComponent<RectTransform>();
            architectureRect.anchorMin = new Vector2(0.03f, 0.14f);
            architectureRect.anchorMax = new Vector2(0.97f, 0.8f);
            architectureRect.offsetMin = Vector2.zero;
            architectureRect.offsetMax = Vector2.zero;
            architectureGO.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.98f);

            Text architectureText = CreateText(architectureGO.transform, "ArchitectureText", "", 18, TextAnchor.UpperLeft, Color.white,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

            var so = new SerializedObject(panelGO.GetComponent<FabricationUI>());
            so.FindProperty("panelRoot").objectReferenceValue = panelGO;
            so.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
            so.FindProperty("coreText").objectReferenceValue = coreText;
            so.FindProperty("installedListParent").objectReferenceValue = installedList;
            so.FindProperty("availableListParent").objectReferenceValue = availableList;
            so.FindProperty("statsText").objectReferenceValue = statsText;
            so.FindProperty("moduleDetailText").objectReferenceValue = moduleDetailText;
            so.FindProperty("architecturePanelRoot").objectReferenceValue = architectureGO;
            so.FindProperty("architectureText").objectReferenceValue = architectureText;
            so.FindProperty("viewArchitectureButton").objectReferenceValue = viewArchitectureButton;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Bottom-left status stack (health / energy / burst pips / reload), a chip-effects
        /// line beneath it, an objective line top-right, and a boss bar across the top. The
        /// boss bar and reload bar are the only parts that toggle - everything else is always
        /// on, since a HUD that appears and disappears is harder to read at a glance than one
        /// that's simply empty.
        /// </summary>
        private static void BuildCombatHudPanel(Transform canvasTransform)
        {
            var panelGO = new GameObject("CombatHudPanel", typeof(RectTransform), typeof(CombatHUD));
            panelGO.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(16f, 16f);
            panelRect.sizeDelta = new Vector2(260f, 108f);

            Image healthFill = BuildHudBar(panelGO.transform, "HealthBar", new Vector2(0f, 0.76f), new Vector2(1f, 0.95f),
                new Color(0.2f, 0.05f, 0.05f, 0.85f), new Color(0.8f, 0.2f, 0.2f, 0.95f));
            Text healthText = CreateText(panelGO.transform, "HealthText", "HP  100 / 100", 13, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0.02f, 0.76f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);

            Image energyFill = BuildHudBar(panelGO.transform, "EnergyBar", new Vector2(0f, 0.53f), new Vector2(1f, 0.72f),
                new Color(0.05f, 0.1f, 0.2f, 0.85f), new Color(0.3f, 0.7f, 0.95f, 0.95f));
            Text energyText = CreateText(panelGO.transform, "EnergyText", "EN  100 / 100", 13, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0.02f, 0.53f), new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero);

            // Burst pips are added at runtime by CombatHUD, because burst capacity changes
            // with the installed Cache module - a fixed set built here would be wrong the
            // moment the player swaps one.
            var pipsGO = new GameObject("BurstPips", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            pipsGO.transform.SetParent(panelGO.transform, false);
            RectTransform pipsRect = pipsGO.GetComponent<RectTransform>();
            pipsRect.anchorMin = new Vector2(0f, 0.34f);
            pipsRect.anchorMax = new Vector2(0.62f, 0.5f);
            pipsRect.offsetMin = Vector2.zero;
            pipsRect.offsetMax = Vector2.zero;
            HorizontalLayoutGroup pipsLayout = pipsGO.GetComponent<HorizontalLayoutGroup>();
            pipsLayout.spacing = 3f;
            pipsLayout.childForceExpandWidth = false;
            pipsLayout.childForceExpandHeight = false;
            pipsLayout.childAlignment = TextAnchor.MiddleLeft;

            Text burstText = CreateText(panelGO.transform, "BurstText", "6 / 6", 13, TextAnchor.MiddleRight,
                new Color(0.8f, 0.9f, 1f), new Vector2(0.62f, 0.34f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);

            var reloadRootGO = new GameObject("ReloadRoot", typeof(RectTransform));
            reloadRootGO.transform.SetParent(panelGO.transform, false);
            RectTransform reloadRootRect = reloadRootGO.GetComponent<RectTransform>();
            reloadRootRect.anchorMin = new Vector2(0f, 0.19f);
            reloadRootRect.anchorMax = new Vector2(1f, 0.31f);
            reloadRootRect.offsetMin = Vector2.zero;
            reloadRootRect.offsetMax = Vector2.zero;

            Image reloadFill = BuildHudBar(reloadRootGO.transform, "ReloadBar", Vector2.zero, Vector2.one,
                new Color(0.15f, 0.12f, 0.05f, 0.85f), new Color(1f, 0.8f, 0.35f, 0.95f));
            reloadFill.fillAmount = 0f;

            Text chipEffectsText = CreateText(panelGO.transform, "ChipEffectsText", "Standard Chip", 12, TextAnchor.MiddleLeft,
                new Color(0.7f, 0.78f, 0.9f), new Vector2(0f, 0f), new Vector2(1f, 0.17f), Vector2.zero, Vector2.zero);

            Text objectiveText = CreateText(canvasTransform, "ObjectiveText", "", 15, TextAnchor.UpperRight,
                new Color(1f, 0.92f, 0.7f), new Vector2(0.55f, 0.88f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);

            // Boss bar - full width across the top, hidden until BossEnemy.Active is set.
            var bossRootGO = new GameObject("BossRoot", typeof(RectTransform));
            bossRootGO.transform.SetParent(canvasTransform, false);
            RectTransform bossRootRect = bossRootGO.GetComponent<RectTransform>();
            bossRootRect.anchorMin = new Vector2(0.14f, 0.9f);
            bossRootRect.anchorMax = new Vector2(0.86f, 0.97f);
            bossRootRect.offsetMin = Vector2.zero;
            bossRootRect.offsetMax = Vector2.zero;

            Image bossFill = BuildHudBar(bossRootGO.transform, "BossBar", new Vector2(0f, 0f), new Vector2(1f, 0.6f),
                new Color(0.18f, 0.04f, 0.04f, 0.9f), new Color(0.9f, 0.3f, 0.2f, 0.95f));
            Text bossNameText = CreateText(bossRootGO.transform, "BossNameText", "", 14, TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.8f), new Vector2(0f, 0.6f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            bossRootGO.SetActive(false);
            reloadRootGO.SetActive(false);

            var so = new SerializedObject(panelGO.GetComponent<CombatHUD>());
            so.FindProperty("healthFillImage").objectReferenceValue = healthFill;
            so.FindProperty("healthText").objectReferenceValue = healthText;
            so.FindProperty("energyFillImage").objectReferenceValue = energyFill;
            so.FindProperty("energyText").objectReferenceValue = energyText;
            so.FindProperty("burstText").objectReferenceValue = burstText;
            so.FindProperty("burstPipsParent").objectReferenceValue = pipsGO.transform;
            so.FindProperty("reloadRoot").objectReferenceValue = reloadRootGO;
            so.FindProperty("reloadFillImage").objectReferenceValue = reloadFill;
            so.FindProperty("chipEffectsText").objectReferenceValue = chipEffectsText;
            so.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            so.FindProperty("bossRoot").objectReferenceValue = bossRootGO;
            so.FindProperty("bossFillImage").objectReferenceValue = bossFill;
            so.FindProperty("bossNameText").objectReferenceValue = bossNameText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Background + horizontally-filled foreground pair, returning the fill Image.
        ///
        /// The fill MUST have a sprite assigned: Image.Type.Filled is ignored entirely when
        /// sprite is null (the Image degrades to a plain quad), so fillAmount silently does
        /// nothing. That is exactly why the previous health/energy bars never visibly moved.
        /// </summary>
        private static Image BuildHudBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Color backgroundColor, Color fillColor)
        {
            Sprite uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

            var bgGO = new GameObject($"{name}Bg", typeof(Image));
            bgGO.transform.SetParent(parent, false);
            Image background = bgGO.GetComponent<Image>();
            background.sprite = uiSprite;
            background.type = Image.Type.Sliced;
            background.color = backgroundColor;
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = anchorMin;
            bgRect.anchorMax = anchorMax;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var fillGO = new GameObject($"{name}Fill", typeof(Image));
            fillGO.transform.SetParent(bgGO.transform, false);
            Image fill = fillGO.GetComponent<Image>();
            fill.sprite = uiSprite;
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            return fill;
        }

        private static void BuildRoomTitlePanel(Transform canvasTransform)
        {
            var panelGO = new GameObject("RoomTitlePanel", typeof(RectTransform), typeof(RoomTitleUI));
            panelGO.transform.SetParent(canvasTransform, false);

            Text title = CreateText(panelGO.transform, "TitleText", "", 26, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.96f), Vector2.zero, Vector2.zero);
            title.color = new Color(1f, 1f, 1f, 0f);

            var so = new SerializedObject(panelGO.GetComponent<RoomTitleUI>());
            so.FindProperty("titleText").objectReferenceValue = title;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSceneFadePanel(Transform canvasTransform)
        {
            var panelGO = new GameObject("SceneFadePanel", typeof(RectTransform), typeof(Image), typeof(SceneFadeUI));
            panelGO.transform.SetParent(canvasTransform, false);
            RectTransform rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image fadeImage = panelGO.GetComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = false;

            var so = new SerializedObject(panelGO.GetComponent<SceneFadeUI>());
            so.FindProperty("fadeImage").objectReferenceValue = fadeImage;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePersistentSystemsLoaderObject()
        {
            new GameObject("PersistentSystemsLoader", typeof(PersistentSystemsLoader));
        }
    }
}
