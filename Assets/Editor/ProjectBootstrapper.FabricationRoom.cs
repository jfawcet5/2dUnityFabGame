using BeyProject.Overworld;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildFabricationRoomScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue)
        {
            const int width = 14;
            const int height = 10;
            var explorationGap = new Vector3Int(0, 4, 0);
            var combatGap = new Vector3Int(width - 1, 4, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.fabricationRoomFloor, tiles.wall, explorationGap, combatGap);

            Vector3 fromExplorationSpawn = new Vector3(explorationGap.x + 2.5f, explorationGap.y + 0.5f, 0f);
            Vector3 fromCombatSpawn = new Vector3(combatGap.x - 2.5f, combatGap.y + 0.5f, 0f);
            Vector3 explorationDoorPosition = new Vector3(explorationGap.x + 0.5f, explorationGap.y + 0.5f, 0f);
            Vector3 combatDoorPosition = new Vector3(combatGap.x + 0.5f, combatGap.y + 0.5f, 0f);

            CreateSpawnPoint("fabrication_room_from_exploration", fromExplorationSpawn);
            CreateSpawnPoint("fabrication_room_from_combat", fromCombatSpawn);

            CreateCamera(fromExplorationSpawn);
            CreatePlayer(fromExplorationSpawn, characters.player, characters.playerAnimation);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Fabrication Room");

            CreateFabricationStation(new Vector3(7f, 5f, 0f), new Color(0.6f, 0.5f, 0.7f));

            // Last stop before the Combat Room - topping up here is the reason to walk back
            // after a failed attempt rather than pushing on at low health.
            CreateRepairStation(new Vector3(11.5f, 7.5f, 0f), characters.repairStation);

            // Two more synergy modules, deliberately sitting next to the station that makes
            // them meaningful: the player finds them at the exact moment they can experiment.
            CreateItemPickup("fabrication_streamlined_cache", new Vector3(4.5f, 7.5f, 0f), items.streamlinedCache);
            CreateItemPickup("fabrication_capacitor_bank", new Vector3(4.5f, 2.5f, 0f), items.capacitorBank);

            CreateWorldInteractable("fabrication_design_archive", new Vector3(11.5f, 2.5f, 0f), new Vector2(0.9f, 0.9f),
                null, new Color(0.55f, 0.6f, 0.75f), false, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.loreTerminalArchitecture }
                });

            CreateDoor("exploration_fabrication_door", explorationDoorPosition, new Vector2(1f, 1f), false, null,
                "ExplorationRoom", "exploration_room_from_fabrication", new Vector2(14.5f, 7.5f), null, null);

            // Ungated - a player who skipped every module can still fight with the baseline "Standard" chip.
            CreateDoor("fabrication_combat_door", combatDoorPosition, new Vector2(1f, 1f), false, null,
                "CombatRoom", "combat_room_from_fabrication", new Vector2(2.5f, 7.5f), null, null);

            string path = $"{ScenesFolder}/FabricationRoom.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
