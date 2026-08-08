using BeyProject.Overworld;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildCleanroomHallwayScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue)
        {
            const int width = 16;
            const int height = 8;
            var lithoGap = new Vector3Int(0, 4, 0);
            var storageGap = new Vector3Int(width - 1, 4, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.hallwayFloor, tiles.wall, lithoGap, storageGap);

            Vector3 fromLithoSpawn = new Vector3(lithoGap.x + 2.5f, lithoGap.y + 0.5f, 0f);
            Vector3 fromStorageSpawn = new Vector3(storageGap.x - 2.5f, storageGap.y + 0.5f, 0f);
            Vector3 lithoDoorPosition = new Vector3(lithoGap.x + 0.5f, lithoGap.y + 0.5f, 0f);
            Vector3 storageDoorPosition = new Vector3(storageGap.x + 0.5f, storageGap.y + 0.5f, 0f);

            CreateSpawnPoint("hallway_from_litho", fromLithoSpawn);
            CreateSpawnPoint("hallway_from_storage", fromStorageSpawn);

            CreateCamera(fromLithoSpawn);
            CreatePlayer(fromLithoSpawn, characters.player, characters.playerAnimation);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Cleanroom Hallway");

            // Always open both ways - the hallway is just a connector, not gated.
            CreateDoor("litho_hallway_door", lithoDoorPosition, new Vector2(1f, 1f), false, null,
                "Lithography", "litho_from_hallway", new Vector2(15.5f, 7.5f), null, null);

            // The Maintenance Hatch - locked from this side until the player has the Maintenance Pass.
            CreateDoor("hallway_storage_hatch", storageDoorPosition, new Vector2(1f, 1f), true, items.maintenancePass,
                "Storage", "storage_entrance", new Vector2(2.5f, 6.5f), null, null);

            // Dialogue reacts to progression: default hint until the player holds the
            // Maintenance Pass, then acknowledges it - no hardcoded one-off script needed.
            CreateDialogueNpc("passing_technician", new Vector3(8.5f, 5.5f, 0f), characters.passingTechnician, new Color(0.7f, 0.85f, 0.9f),
                dialogue.passingTechnicianDefault, dialogue.passingTechnicianDefault, new WorldAction[0],
                new[]
                {
                    new FlagDialogueOverride { requiredItem = items.maintenancePass, dialogue = dialogue.passingTechnicianWithPass }
                });

            CreateWorldInteractable("tool_rack", new Vector3(5.5f, 2.5f, 0f), new Vector2(0.8f, 0.8f),
                null, new Color(0.5f, 0.5f, 0.55f), true, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.toolRack },
                    new WorldAction { type = InteractionActionType.GiveItem, item = items.maintenancePass, itemQuantity = 1 }
                });

            // Hidden collectible - tucked off to the side of the connector room.
            CreateItemPickup("hallway_failed_experiment_log", new Vector3(13.5f, 6.5f, 0f), items.failedExperimentLog);

            string path = $"{ScenesFolder}/CleanroomHallway.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
