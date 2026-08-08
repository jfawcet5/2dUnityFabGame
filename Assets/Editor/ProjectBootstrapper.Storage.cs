using BeyProject.Overworld;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildStorageScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue)
        {
            const int width = 16;
            const int height = 12;
            var doorGap = new Vector3Int(0, 6, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.storageFloor, tiles.wall, doorGap);

            Vector3 entranceSpawn = new Vector3(doorGap.x + 2.5f, doorGap.y + 0.5f, 0f);
            Vector3 doorPosition = new Vector3(doorGap.x + 0.5f, doorGap.y + 0.5f, 0f);

            CreateSpawnPoint("storage_entrance", entranceSpawn);

            CreateCamera(entranceSpawn);
            CreatePlayer(entranceSpawn, characters.player, characters.playerAnimation);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Storage");

            // Always free once you're through - the hatch only gates entry from the hallway side.
            CreateDoor("hallway_storage_hatch", doorPosition, new Vector2(1f, 1f), false, null,
                "CleanroomHallway", "hallway_from_storage", new Vector2(12.5f, 4.5f), null, null);

            // A quirky non-human "NPC" - variety beyond flavor human characters, still no progression.
            CreateDialogueNpc("old_automation_unit", new Vector3(8.5f, 8.5f, 0f), characters.oldAutomationUnit, new Color(0.55f, 0.55f, 0.6f),
                dialogue.oldAutomationUnit, dialogue.oldAutomationUnit, new WorldAction[0]);

            // Environmental progression via an inventory item, not a door - the concrete
            // example tying Milestone 1's Calibration Tool (found in Lobby) into new content.
            CreateWorldInteractable("disabled_terminal", new Vector3(12.5f, 4.5f, 0f), new Vector2(0.8f, 0.8f),
                null, new Color(0.4f, 0.45f, 0.5f), true, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.disabledTerminalUnlocked },
                    new WorldAction { type = InteractionActionType.GiveItem, item = items.manufacturingReport, itemQuantity = 1 }
                },
                requiredItem: items.calibrationTool, lockedDialogue: dialogue.disabledTerminalLocked);

            CreateItemPickup("storage_prototype_documentation", new Vector3(5.5f, 9.5f, 0f), items.prototypeDocumentation);

            string path = $"{ScenesFolder}/Storage.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
