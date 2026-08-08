using BeyProject.Overworld;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildLobbyScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue)
        {
            const int width = 20;
            const int height = 14;
            var doorGap = new Vector3Int(width - 1, 7, 0);
            var breakRoomGap = new Vector3Int(10, height - 1, 0);
            var startRoomGap = new Vector3Int(0, 7, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.lobbyFloor, tiles.wall, doorGap, breakRoomGap, startRoomGap);

            Vector3 startSpawn = new Vector3(3.5f, 3.5f, 0f);
            Vector3 fromLithoSpawn = new Vector3(17.5f, 7.5f, 0f);
            Vector3 fromBreakRoomSpawn = new Vector3(breakRoomGap.x + 0.5f, breakRoomGap.y - 1.5f, 0f);
            Vector3 fromStartRoomSpawn = new Vector3(startRoomGap.x + 1.5f, startRoomGap.y + 0.5f, 0f);
            Vector3 doorPosition = new Vector3(doorGap.x + 0.5f, doorGap.y + 0.5f, 0f);
            Vector3 breakRoomDoorPosition = new Vector3(breakRoomGap.x + 0.5f, breakRoomGap.y + 0.5f, 0f);
            Vector3 startRoomDoorPosition = new Vector3(startRoomGap.x + 0.5f, startRoomGap.y + 0.5f, 0f);

            CreateSpawnPoint("lobby_start", startSpawn);
            CreateSpawnPoint("lobby_from_litho", fromLithoSpawn);
            CreateSpawnPoint("lobby_from_breakroom", fromBreakRoomSpawn);
            CreateSpawnPoint("lobby_from_startroom", fromStartRoomSpawn);

            CreateCamera(startSpawn);
            CreatePlayer(startSpawn, characters.player, characters.playerAnimation);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Lobby");

            CreateDialogueNpc("receptionist", new Vector3(5.5f, 3.5f, 0f), characters.receptionist, new Color(0.6f, 0.6f, 0.9f),
                dialogue.receptionist, dialogue.receptionist, new WorldAction[0]);

            CreateDialogueNpc("floor_supervisor", new Vector3(16.5f, 9.5f, 0f), characters.floorSupervisor, new Color(0.5f, 0.75f, 0.55f),
                dialogue.floorSupervisor, dialogue.floorSupervisor, new WorldAction[0]);

            CreateWorldInteractable("supply_cabinet", new Vector3(5.5f, 9.5f, 0f), new Vector2(0.8f, 0.8f),
                null, new Color(0.6f, 0.6f, 0.65f), true, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.supplyCabinet },
                    new WorldAction { type = InteractionActionType.GiveItem, item = items.cleanroomKeycard, itemQuantity = 1 }
                });

            CreateItemPickup("lobby_wafer", new Vector3(8.5f, 5.5f, 0f), items.wafer);
            CreateItemPickup("lobby_material_sample", new Vector3(11.5f, 10.5f, 0f), items.materialSample);
            // Hidden collectible - tucked in a far corner, off the direct spawn-to-door path.
            CreateItemPickup("lobby_calibration_tool", new Vector3(2.5f, 11.5f, 0f), items.calibrationTool);

            CreateDoor("lobby_litho_door", doorPosition, new Vector2(1f, 1f), true, items.cleanroomKeycard,
                "Lithography", "litho_entrance", new Vector2(2.5f, 7.5f), null, null);

            // Always open both ways - not every room needs to be gated.
            CreateDoor("lobby_breakroom_door", breakRoomDoorPosition, new Vector2(1f, 1f), false, null,
                "BreakRoom", "breakroom_entrance", new Vector2(8.5f, 2.5f), null, null);

            // Entry to the Combat Prototype wing - always open, no key needed.
            CreateDoor("lobby_startroom_door", startRoomDoorPosition, new Vector2(1f, 1f), false, null,
                "StartRoom", "start_room_entry", new Vector2(2.5f, 4.5f), null, null);

            string path = $"{ScenesFolder}/Lobby.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
