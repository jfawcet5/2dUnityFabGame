using BeyProject.Data;
using BeyProject.Overworld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildLithographyScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue,
            BeyIdentity rivalTechnician)
        {
            const int width = 18;
            const int height = 14;
            var doorGap = new Vector3Int(0, 7, 0);
            var hallwayGap = new Vector3Int(width - 1, 7, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.lithographyFloor, tiles.wall, doorGap, hallwayGap);

            Vector3 entranceSpawn = new Vector3(2.5f, 7.5f, 0f);
            Vector3 fromHallwaySpawn = new Vector3(hallwayGap.x - 1.5f, hallwayGap.y + 0.5f, 0f);
            Vector3 doorPosition = new Vector3(doorGap.x + 0.5f, doorGap.y + 0.5f, 0f);
            Vector3 hallwayDoorPosition = new Vector3(hallwayGap.x + 0.5f, hallwayGap.y + 0.5f, 0f);

            CreateSpawnPoint("litho_entrance", entranceSpawn);
            CreateSpawnPoint("litho_from_hallway", fromHallwaySpawn);

            CreateCamera(entranceSpawn);
            CreatePlayer(entranceSpawn, characters.player, characters.playerAnimation);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Lithography");

            // Always free once you're through - no key required from this side.
            CreateDoor("lobby_litho_door", doorPosition, new Vector2(1f, 1f), false, null,
                "Lobby", "lobby_from_litho", new Vector2(17.5f, 7.5f), null, null);

            // Always open both ways - the hallway is just a connector, not gated.
            CreateDoor("litho_hallway_door", hallwayDoorPosition, new Vector2(1f, 1f), false, null,
                "CleanroomHallway", "hallway_from_litho", new Vector2(2.5f, 4.5f), null, null);

            CreateDialogueNpc("lithography_technician", new Vector3(5.5f, 7.5f, 0f), characters.technician, new Color(0.55f, 0.75f, 0.85f),
                dialogue.technicianFirstMeeting, dialogue.technicianRepeat, new[]
                {
                    new WorldAction { type = InteractionActionType.GiveItem, item = items.lithographyMask, itemQuantity = 1 }
                });

            CreateWorldInteractable("recipe_terminal", new Vector3(10.5f, 4.5f, 0f), new Vector2(0.8f, 0.8f),
                null, new Color(0.4f, 0.55f, 0.7f), true, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.recipeTerminal },
                    new WorldAction { type = InteractionActionType.GiveItem, item = items.recipeFile, itemQuantity = 1 }
                });

            // Placeholder-battle trigger, reusing OverworldOpponent unchanged. Tucked in a
            // side alcove rather than blocking the main path - since the placeholder battle
            // has no real outcome, there's no way for the player to ever get "past" a
            // permanently-solid opponent, so it has to stay optional, not a gate.
            var rivalGO = new GameObject("Rival Technician", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(OverworldOpponent));
            rivalGO.transform.position = new Vector3(14.5f, 10.5f, 0f);
            CircleCollider2D rivalCollider = rivalGO.GetComponent<CircleCollider2D>();
            rivalCollider.isTrigger = false;
            rivalCollider.radius = 0.4f;
            SpriteRenderer rivalRenderer = rivalGO.GetComponent<SpriteRenderer>();
            rivalRenderer.sprite = characters.rivalTechnician;
            rivalRenderer.sortingOrder = 5;

            var rivalSo = new SerializedObject(rivalGO.GetComponent<OverworldOpponent>());
            rivalSo.FindProperty("identity").objectReferenceValue = rivalTechnician;
            rivalSo.FindProperty("spriteRenderer").objectReferenceValue = rivalRenderer;
            rivalSo.ApplyModifiedPropertiesWithoutUndo();

            CreateItemPickup("litho_process_module", new Vector3(8.5f, 10.5f, 0f), items.processModule);
            CreateItemPickup("litho_experimental_component", new Vector3(12.5f, 5.5f, 0f), items.experimentalComponent);
            // Hidden key item - no door to unlock yet; a forward-looking hook for later content.
            CreateItemPickup("litho_prototype_access_badge", new Vector3(16.5f, 11.5f, 0f), items.prototypeAccessBadge);

            string path = $"{ScenesFolder}/Lithography.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
