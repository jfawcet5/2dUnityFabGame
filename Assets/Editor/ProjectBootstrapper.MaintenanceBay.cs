using BeyProject.Combat;
using BeyProject.Overworld;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        /// <summary>
        /// Optional combat room hanging off the Exploration Room - entirely skippable, which
        /// is the point: it rewards curiosity without gating the critical path.
        ///
        /// It's also where the objective system earns its keep. Rather than another "kill
        /// everything" room, the enemies here are invulnerable until both shield generators
        /// are down, which forces the player to ignore what's shooting at them and deal with
        /// the thing that isn't - a different decision from the Combat Room using the exact
        /// same combat verbs. The reward is a prototype Processor module.
        /// </summary>
        private static void BuildMaintenanceBayScene(RoomTiles tiles, CharacterSprites characters, ItemSet items, DialogueSet dialogue)
        {
            const int width = 15;
            const int height = 12;
            var explorationGap = new Vector3Int(0, 6, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.maintenanceBayFloor, tiles.wall, explorationGap);

            Vector3 entrySpawn = new Vector3(explorationGap.x + 2.5f, explorationGap.y + 0.5f, 0f);
            Vector3 explorationDoorPosition = new Vector3(explorationGap.x + 0.5f, explorationGap.y + 0.5f, 0f);

            CreateSpawnPoint("maintenance_bay_entry", entrySpawn);

            CreateCamera(entrySpawn);
            CreatePlayer(entrySpawn, characters.player);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Maintenance Bay");

            // Explains the objective on arrival, so "my shots aren't doing anything" is
            // answered before it becomes confusing rather than after.
            CreateWorldInteractable("maintenance_bay_terminal", new Vector3(3.5f, 9.5f, 0f), new Vector2(0.9f, 0.9f),
                null, new Color(0.5f, 0.7f, 0.8f), false, new[]
                {
                    new WorldAction { type = InteractionActionType.ShowDialogue, dialogue = dialogue.maintenanceBayTerminal }
                });

            // Generators sit in opposite corners behind the enemies - reaching them means
            // moving through the fight, not sniping from the doorway.
            CreateShieldGenerator("maintenance_generator_a", new Vector3(12.5f, 9.5f, 0f), characters.shieldGenerator,
                new Color(0.4f, 0.9f, 0.85f), maxHealth: 45f);
            CreateShieldGenerator("maintenance_generator_b", new Vector3(12.5f, 2.5f, 0f), characters.shieldGenerator,
                new Color(0.4f, 0.9f, 0.85f), maxHealth: 45f);

            var coverColor = new Color(0.42f, 0.44f, 0.48f);
            CreateCover("maintenance_cover_a", new Vector3(7f, 8.5f, 0f), new Vector2(1f, 2f), characters.cover, coverColor);
            CreateCover("maintenance_cover_b", new Vector3(7f, 3.5f, 0f), new Vector2(1f, 2f), characters.cover, coverColor);

            CreateExplosive("maintenance_barrel", new Vector3(9.5f, 6f, 0f), characters.explosive, new Color(0.9f, 0.45f, 0.15f));

            CreateEnemy("maintenance_defensive", new Vector3(9.5f, 9f, 0f), characters.enemyDefensive, new Color(0.3f, 0.5f, 0.9f),
                EnemyType.Defensive, maxHealth: 46f, moveSpeed: 1.4f, contactDamage: 9f,
                canShoot: true, preferredRange: 5.5f, attackIntervalSeconds: 2f, projectileSpeed: 4.8f, projectileDamage: 8f);

            CreateEnemy("maintenance_basic", new Vector3(9.5f, 3f, 0f), characters.enemyBasic, new Color(0.9f, 0.55f, 0.2f),
                EnemyType.Basic, maxHealth: 30f, moveSpeed: 2f, contactDamage: 8f,
                canShoot: true, preferredRange: 4.5f, attackIntervalSeconds: 2.4f, projectileSpeed: 4.5f, projectileDamage: 7f);

            CreateEnemy("maintenance_fast", new Vector3(11.5f, 6f, 0f), characters.enemyFast, new Color(0.75f, 0.9f, 0.3f),
                EnemyType.Fast, maxHealth: 20f, moveSpeed: 3.2f, contactDamage: 11f, canShoot: false, preferredRange: 3f);

            // The reward. Behind everything, so clearing the room is the only way to reach it.
            CreateItemPickup("maintenance_cascade_processor", new Vector3(13.5f, 6f, 0f), items.cascadeProcessor);

            CreateRepairStation(new Vector3(2.5f, 2.5f, 0f), characters.repairStation);

            CreateDoor("exploration_maintenance_door", explorationDoorPosition, new Vector2(1f, 1f), false, null,
                "ExplorationRoom", "exploration_room_from_maintenance", new Vector2(9.5f, 2.5f), null, null);

            // No door to gate - this room's payoff is the pickup itself. The controller still
            // runs so the objective readout and the "Route Clear" banner behave as they do
            // everywhere else.
            CreateCombatEncounter(null, "maintenance_bay_cleared", CombatObjectiveType.DestroyShieldGeneratorsThenEnemies,
                "Destroy the shield generators, then clear the bay");

            string path = $"{ScenesFolder}/MaintenanceBay.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
