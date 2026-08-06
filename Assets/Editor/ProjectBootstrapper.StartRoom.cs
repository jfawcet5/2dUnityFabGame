using BeyProject.Overworld;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildStartRoomScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue)
        {
            const int width = 12;
            const int height = 10;
            var lobbyGap = new Vector3Int(0, 4, 0);
            var explorationGap = new Vector3Int(width - 1, 4, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.startRoomFloor, tiles.wall, lobbyGap, explorationGap);

            Vector3 entrySpawn = new Vector3(lobbyGap.x + 2.5f, lobbyGap.y + 0.5f, 0f);
            Vector3 fromExplorationSpawn = new Vector3(explorationGap.x - 2.5f, explorationGap.y + 0.5f, 0f);
            Vector3 lobbyDoorPosition = new Vector3(lobbyGap.x + 0.5f, lobbyGap.y + 0.5f, 0f);
            Vector3 explorationDoorPosition = new Vector3(explorationGap.x + 0.5f, explorationGap.y + 0.5f, 0f);

            CreateSpawnPoint("start_room_entry", entrySpawn);
            CreateSpawnPoint("start_room_from_exploration", fromExplorationSpawn);

            CreateCamera(entrySpawn);
            CreatePlayer(entrySpawn, characters.player);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Start Room");

            // Repeatable, dialogue-only - explains the combat prototype controls.
            CreateWorldInteractable("briefing_terminal", new Vector3(6.5f, 6.5f, 0f), new Vector2(0.9f, 0.9f),
                null, new Color(0.5f, 0.6f, 0.9f), false, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.briefingTerminal }
                });

            // Always open both ways - always unlocked from the Lobby side, matching the door there.
            CreateDoor("lobby_startroom_door", lobbyDoorPosition, new Vector2(1f, 1f), false, null,
                "Lobby", "lobby_from_startroom", new Vector2(3.5f, 3.5f), null, null);

            CreateDoor("startroom_exploration_door", explorationDoorPosition, new Vector2(1f, 1f), false, null,
                "ExplorationRoom", "exploration_room_from_start", new Vector2(2.5f, 7.5f), null, null);

            string path = $"{ScenesFolder}/StartRoom.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
