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
        /// The main gate encounter. Deliberately not "two enemies in an empty room": the
        /// space is broken up by cover so there are angles to win and lose, a turret that
        /// keeps firing while the player deals with the chasers (forcing target priority),
        /// barrels that punish clustering for both sides, and an electrified strip that makes
        /// the shortest path across the room the worst one.
        ///
        /// The enemy trio is spread across three approach lanes rather than stacked, so they
        /// arrive staggered and the player faces a sequence of decisions instead of one blob.
        /// </summary>
        private static void BuildCombatRoomScene(RoomTiles tiles, CharacterSprites characters)
        {
            const int width = 18;
            const int height = 14;
            var fabricationGap = new Vector3Int(0, 7, 0);
            var bossGap = new Vector3Int(width - 1, 7, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.combatRoomFloor, tiles.wall, fabricationGap, bossGap);

            Vector3 fromFabricationSpawn = new Vector3(fabricationGap.x + 2.5f, fabricationGap.y + 0.5f, 0f);
            Vector3 fromBossSpawn = new Vector3(bossGap.x - 2.5f, bossGap.y + 0.5f, 0f);
            Vector3 fabricationDoorPosition = new Vector3(fabricationGap.x + 0.5f, fabricationGap.y + 0.5f, 0f);
            Vector3 bossDoorPosition = new Vector3(bossGap.x + 0.5f, bossGap.y + 0.5f, 0f);

            CreateSpawnPoint("combat_room_from_fabrication", fromFabricationSpawn);
            CreateSpawnPoint("combat_room_from_boss", fromBossSpawn);

            CreateCamera(fromFabricationSpawn);
            CreatePlayer(fromFabricationSpawn, characters.player);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Combat Room");

            // Cover. The two central blocks split the room into a north and south route, so
            // the player has to commit to a side and the turret can't cover both at once.
            var coverColor = new Color(0.5f, 0.42f, 0.42f);
            CreateCover("combat_cover_north", new Vector3(7f, 10f, 0f), new Vector2(2.4f, 1f), characters.cover, coverColor);
            CreateCover("combat_cover_south", new Vector3(7f, 4f, 0f), new Vector2(2.4f, 1f), characters.cover, coverColor);
            CreateCover("combat_cover_mid", new Vector3(11.5f, 7f, 0f), new Vector2(1f, 3f), characters.cover, coverColor);
            // Indestructible pillars near the entrance guarantee the player always has
            // something to break line of sight on, even after everything else is rubble.
            CreateCover("combat_pillar_a", new Vector3(3.5f, 10.5f, 0f), new Vector2(1f, 1f), characters.cover,
                new Color(0.34f, 0.32f, 0.36f), destructible: false);
            CreateCover("combat_pillar_b", new Vector3(3.5f, 3.5f, 0f), new Vector2(1f, 1f), characters.cover,
                new Color(0.34f, 0.32f, 0.36f), destructible: false);

            // Barrels sit next to the cover, so the enemies that path around it walk into
            // detonation range - a reward for shooting the environment rather than the enemy.
            CreateExplosive("combat_barrel_north", new Vector3(8.8f, 9.4f, 0f), characters.explosive, new Color(0.9f, 0.45f, 0.15f));
            CreateExplosive("combat_barrel_south", new Vector3(8.8f, 4.6f, 0f), characters.explosive, new Color(0.9f, 0.45f, 0.15f));

            // Electrified strip guarding the boss door - the direct route is the punished one.
            CreateHazard("combat_electric_floor", new Vector3(14f, 7f, 0f), new Vector2(2f, 4f), characters.hazard,
                HazardType.ElectricFloor, new Color(0.5f, 0.85f, 1f, 0.8f), damagePerTick: 8f,
                cycleSeconds: 3.2f, activePhaseSeconds: 1.5f);

            // Turret in the far corner: out of the melee, always relevant, never the first
            // thing you can safely deal with.
            CreateTurret("combat_turret", new Vector3(15.5f, 11.5f, 0f), characters.turret, new Color(0.75f, 0.3f, 0.55f),
                maxHealth: 45f, range: 9f, fireIntervalSeconds: 2f, projectileDamage: 9f);

            // Three lanes, three roles, three arrival times.
            CreateEnemy("combat_room_basic", new Vector3(9.5f, 11.5f, 0f), characters.enemyBasic, new Color(0.9f, 0.55f, 0.2f),
                EnemyType.Basic, maxHealth: 34f, moveSpeed: 2.1f, contactDamage: 8f,
                canShoot: true, preferredRange: 5f, attackIntervalSeconds: 2.3f, projectileSpeed: 4.5f, projectileDamage: 7f);

            CreateEnemy("combat_room_defensive", new Vector3(13.5f, 4f, 0f), characters.enemyDefensive, new Color(0.3f, 0.5f, 0.9f),
                EnemyType.Defensive, maxHealth: 58f, moveSpeed: 1.5f, contactDamage: 10f,
                canShoot: true, preferredRange: 6.5f, attackIntervalSeconds: 1.9f, projectileSpeed: 5f, projectileDamage: 9f);

            CreateEnemy("combat_room_fast", new Vector3(10f, 7.5f, 0f), characters.enemyFast, new Color(0.75f, 0.9f, 0.3f),
                EnemyType.Fast, maxHealth: 22f, moveSpeed: 3.1f, contactDamage: 12f,
                canShoot: false, preferredRange: 3f);

            CreateDoor("fabrication_combat_door", fabricationDoorPosition, new Vector2(1f, 1f), false, null,
                "FabricationRoom", "fabrication_room_from_combat", new Vector2(10.5f, 4.5f), null, null);

            // Starts locked - only CombatEncounterController.UnlockRemotely() opens it, once
            // every hostile in the room is down. The turret is optional: it isn't part of the
            // objective, so the player chooses between silencing it and pushing through.
            Door bossDoor = CreateDoor("combat_boss_door", bossDoorPosition, new Vector2(1f, 1f), true, null,
                "BossRoom", "boss_room_entry", new Vector2(2.5f, 7.5f), null, null);

            CreateCombatEncounter(bossDoor, "combat_room_cleared", CombatObjectiveType.DefeatAllEnemies,
                "Clear all hostiles to open the containment door");

            string path = $"{ScenesFolder}/CombatRoom.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
