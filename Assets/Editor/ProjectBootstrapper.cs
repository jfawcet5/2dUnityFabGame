using BeyProject.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.EditorTools
{
    /// <summary>
    /// One-shot (re-runnable) setup: generates placeholder pixel-art sprites/tiles/data
    /// assets and builds every scene (Main Menu, Lobby, Lithography, Battle) plus the
    /// persistent systems prefab, from scratch. Keeping this as a script rather than
    /// hand-authored scene/asset files means it's reproducible and easy to tweak.
    ///
    /// Split into a partial class across ProjectBootstrapper.*.cs files, one per concern
    /// (art, items, persistent systems, each scene, shared room-building helpers). This file
    /// is just orchestration + the utilities every other file shares.
    ///
    /// Run via Tools/Bey Project/Bootstrap Initial Project, or headlessly with:
    /// -executeMethod BeyProject.EditorTools.ProjectBootstrapper.BuildInitialProject
    /// </summary>
    public static partial class ProjectBootstrapper
    {
        private const string ArtFolder = "Assets/Art/Generated";
        private const string TilesFolder = "Assets/Tiles";
        private const string DataFolder = "Assets/Data";
        private const string DialogueFolder = "Assets/Data/Dialogue";
        private const string ScenesFolder = "Assets/Scenes";
        private const string ResourcesFolder = "Assets/Resources";

        private const int PixelsPerUnit = 16;

        [MenuItem("Tools/Bey Project/Bootstrap Initial Project")]
        public static void BuildInitialProject()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(TilesFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(DialogueFolder);
            EnsureFolder(ScenesFolder);
            EnsureFolder(ResourcesFolder);

            RoomTiles sharedTiles = BuildSharedTiles();
            CharacterSprites characters = BuildCharacterSprites();
            ItemSet items = BuildItems();
            DialogueSet dialogue = BuildDialogue();
            BeyIdentity rivalTechnician = CreateBeyIdentity("RivalTechnician", "rival_tech_01", "Rival Technician", new Color(0.8f, 0.3f, 0.3f));

            ItemDatabase itemDatabase = BuildItemDatabase(
                items.wafer, items.materialSample, items.calibrationTool, items.cleanroomKeycard,
                items.lithographyMask, items.recipeFile, items.processModule, items.experimentalComponent,
                items.prototypeAccessBadge,
                items.internalEmail, items.engineerNotes, items.failedExperimentLog,
                items.manufacturingReport, items.prototypeDocumentation, items.maintenancePass,
                items.powerComponent, items.memoryModule, items.parallelProcessingModule,
                items.focusingAlgorithmModule, items.predictiveTargetingModule, items.coolingLayer, items.siliconWafer,
                items.overclockLayer, items.capacitorBank, items.streamlinedCache, items.cascadeProcessor);

            AssetDatabase.SaveAssets();

            BuildPersistentSystemsPrefab(itemDatabase);

            BuildMainMenuScene();
            BuildLobbyScene(sharedTiles, characters, items, dialogue);
            BuildLithographyScene(sharedTiles, characters, items, dialogue, rivalTechnician);
            BuildBreakRoomScene(sharedTiles, characters, items, dialogue);
            BuildCleanroomHallwayScene(sharedTiles, characters, items, dialogue);
            BuildStorageScene(sharedTiles, characters, items, dialogue);
            BuildStartRoomScene(sharedTiles, characters, items, dialogue);
            BuildExplorationRoomScene(sharedTiles, characters, items, dialogue);
            BuildMaintenanceBayScene(sharedTiles, characters, items, dialogue);
            BuildFabricationRoomScene(sharedTiles, characters, items, dialogue);
            BuildCombatRoomScene(sharedTiles, characters);
            BuildBossRoomScene(sharedTiles, characters);
            BuildBattleScene();

            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("ProjectBootstrapper: initial project build complete.");
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Lobby.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Lithography.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/BreakRoom.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/CleanroomHallway.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Storage.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/StartRoom.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/ExplorationRoom.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/MaintenanceBay.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/FabricationRoom.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/CombatRoom.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/BossRoom.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Battle.unity", true)
            };
        }

        // ---------- Shared utilities ----------

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static Object CreateOrReplaceAsset(Object asset, string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        // ---------- Small UI-building helpers, shared by every scene's UI ----------

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment,
            Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = content;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CreateText(go.transform, "Text", label, 18, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return go.GetComponent<Button>();
        }

        private static Transform CreateListContainer(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go.transform;
        }
    }
}
