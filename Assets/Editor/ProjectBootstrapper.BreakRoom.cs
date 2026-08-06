using BeyProject.Overworld;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildBreakRoomScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue)
        {
            const int width = 16;
            const int height = 12;
            var doorGap = new Vector3Int(8, 0, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.breakRoomFloor, tiles.wall, doorGap);

            Vector3 entranceSpawn = new Vector3(doorGap.x + 0.5f, doorGap.y + 2f, 0f);
            Vector3 doorPosition = new Vector3(doorGap.x + 0.5f, doorGap.y + 0.5f, 0f);

            CreateSpawnPoint("breakroom_entrance", entranceSpawn);

            CreateCamera(entranceSpawn);
            CreatePlayer(entranceSpawn, characters.player);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Break Room");

            // Always open both ways - not every room needs to be gated.
            CreateDoor("lobby_breakroom_door", doorPosition, new Vector2(1f, 1f), false, null,
                "Lobby", "lobby_from_breakroom", new Vector2(3.5f, 3.5f), null, null);

            // Pure flavor - no items, no progression. Some NPCs just make the world feel lived in.
            CreateDialogueNpc("off_duty_engineer", new Vector3(4.5f, 6.5f, 0f), characters.offDutyEngineer, new Color(0.75f, 0.6f, 0.45f),
                dialogue.offDutyEngineer, dialogue.offDutyEngineer, new WorldAction[0]);

            // Dialogue-only interactable, repeatable - an interactive object doesn't have to give anything.
            CreateWorldInteractable("whiteboard", new Vector3(11.5f, 8.5f, 0f), new Vector2(0.8f, 0.8f),
                null, new Color(0.85f, 0.85f, 0.85f), false, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.whiteboard }
                });

            CreateItemPickup("breakroom_internal_email", new Vector3(6.5f, 4.5f, 0f), items.internalEmail);
            // Hidden collectible - tucked behind furniture in a far corner.
            CreateItemPickup("breakroom_engineer_notes", new Vector3(13.5f, 10.5f, 0f), items.engineerNotes);

            string path = $"{ScenesFolder}/BreakRoom.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
