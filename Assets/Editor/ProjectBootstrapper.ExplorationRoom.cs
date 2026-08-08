using BeyProject.Overworld;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildExplorationRoomScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue)
        {
            const int width = 18;
            const int height = 14;
            var startGap = new Vector3Int(0, 7, 0);
            var fabricationGap = new Vector3Int(width - 1, 7, 0);
            // Third exit, in the floor of the room, leading to the optional Maintenance Bay.
            var maintenanceGap = new Vector3Int(9, 0, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.explorationRoomFloor, tiles.wall, startGap, fabricationGap, maintenanceGap);

            Vector3 fromStartSpawn = new Vector3(startGap.x + 2.5f, startGap.y + 0.5f, 0f);
            Vector3 fromFabricationSpawn = new Vector3(fabricationGap.x - 2.5f, fabricationGap.y + 0.5f, 0f);
            Vector3 fromMaintenanceSpawn = new Vector3(maintenanceGap.x + 0.5f, maintenanceGap.y + 2.5f, 0f);
            Vector3 startDoorPosition = new Vector3(startGap.x + 0.5f, startGap.y + 0.5f, 0f);
            Vector3 fabricationDoorPosition = new Vector3(fabricationGap.x + 0.5f, fabricationGap.y + 0.5f, 0f);
            Vector3 maintenanceDoorPosition = new Vector3(maintenanceGap.x + 0.5f, maintenanceGap.y + 0.5f, 0f);

            CreateSpawnPoint("exploration_room_from_start", fromStartSpawn);
            CreateSpawnPoint("exploration_room_from_fabrication", fromFabricationSpawn);
            CreateSpawnPoint("exploration_room_from_maintenance", fromMaintenanceSpawn);

            CreateCamera(fromStartSpawn);
            CreatePlayer(fromStartSpawn, characters.player, characters.playerAnimation);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Exploration Room");

            // Environmental object - dialogue only, no gameplay effect, just world-building flavor.
            CreateWorldInteractable("component_scanner", new Vector3(9.5f, 3.5f, 0f), new Vector2(0.9f, 0.9f),
                null, new Color(0.5f, 0.7f, 0.6f), false, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.componentScanner }
                });

            // Lore terminals: repeatable, no reward beyond context. The thermal log is placed
            // on the path to the Fabrication Room specifically because it telegraphs the
            // boss's vulnerability window to anyone who reads it before the fight.
            CreateWorldInteractable("lore_terminal_thermal", new Vector3(16.5f, 11.5f, 0f), new Vector2(0.9f, 0.9f),
                null, new Color(0.55f, 0.6f, 0.75f), false, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.loreTerminalThermal }
                });

            CreateWorldInteractable("lore_terminal_architecture", new Vector3(1.5f, 2.5f, 0f), new Vector2(0.9f, 0.9f),
                null, new Color(0.55f, 0.6f, 0.75f), false, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.loreTerminalArchitecture }
                });

            CreateItemPickup("exploration_power_component", new Vector3(3.5f, 4.5f, 0f), items.powerComponent);
            CreateItemPickup("exploration_memory_module", new Vector3(14.5f, 4.5f, 0f), items.memoryModule);
            CreateItemPickup("exploration_parallel_processing", new Vector3(4.5f, 10.5f, 0f), items.parallelProcessingModule);
            CreateItemPickup("exploration_focusing_algorithm", new Vector3(13.5f, 10.5f, 0f), items.focusingAlgorithmModule);
            CreateItemPickup("exploration_predictive_targeting", new Vector3(9.5f, 12.5f, 0f), items.predictiveTargetingModule);
            CreateItemPickup("exploration_cooling_layer", new Vector3(9.5f, 1.5f, 0f), items.coolingLayer);
            // Flavor-only collectible - a keepsake, no chip effect.
            CreateItemPickup("exploration_silicon_wafer", new Vector3(1.5f, 12.5f, 0f), items.siliconWafer);

            // Secret: a one-shot loose panel tucked in the far corner that hands over a
            // synergy module. Nothing signposts it, and nothing needs it.
            CreateWorldInteractable("hidden_panel_overclock", new Vector3(16.5f, 1.5f, 0f), new Vector2(0.9f, 0.9f),
                null, new Color(0.4f, 0.42f, 0.4f), true, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.hiddenCache },
                    new WorldAction { type = InteractionActionType.GiveItem, item = items.overclockLayer, itemQuantity = 1 }
                });

            CreateRepairStation(new Vector3(2.5f, 7.5f, 0f), characters.repairStation);

            CreateDoor("startroom_exploration_door", startDoorPosition, new Vector2(1f, 1f), false, null,
                "StartRoom", "start_room_from_exploration", new Vector2(9.5f, 4.5f), null, null);

            CreateDoor("exploration_fabrication_door", fabricationDoorPosition, new Vector2(1f, 1f), false, null,
                "FabricationRoom", "fabrication_room_from_exploration", new Vector2(2.5f, 4.5f), null, null);

            // Optional route - always open, never required.
            CreateDoor("exploration_maintenance_door", maintenanceDoorPosition, new Vector2(1f, 1f), false, null,
                "MaintenanceBay", "maintenance_bay_entry", new Vector2(2.5f, 6.5f), null, null);

            string path = $"{ScenesFolder}/ExplorationRoom.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
