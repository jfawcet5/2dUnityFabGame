using BeyProject.Combat;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        /// <summary>
        /// The arena escalates with the boss rather than being uniformly lethal from the
        /// start. Phase 1 is a clean floor with pillars - all about reading the ring bursts.
        /// Phase 2 brings steam vents online, taking away the easy middle. Phase 3 adds a
        /// rotating laser sweep and two support enemies, so the safe ground is now moving.
        ///
        /// Hazards and adds are placed here but handed to BossEnemy, which activates them on
        /// phase transitions - the arena state lives with the encounter that drives it rather
        /// than in a second controller that would have to stay in sync with the boss's health.
        /// </summary>
        private static void BuildBossRoomScene(RoomTiles tiles, CharacterSprites characters)
        {
            const int width = 18;
            const int height = 16;
            var combatGap = new Vector3Int(0, 8, 0);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateRoomTilemap(width, height, tiles.bossRoomFloor, tiles.wall, combatGap);

            Vector3 entrySpawn = new Vector3(combatGap.x + 2.5f, combatGap.y + 0.5f, 0f);
            Vector3 combatDoorPosition = new Vector3(combatGap.x + 0.5f, combatGap.y + 0.5f, 0f);
            var arenaCenter = new Vector3(10f, 8f, 0f);

            CreateSpawnPoint("boss_room_entry", entrySpawn);

            CreateCamera(entrySpawn);
            CreatePlayer(entrySpawn, characters.player, characters.playerAnimation);
            CreatePersistentSystemsLoaderObject();
            CreateRoomAmbience();
            CreateRoomIntro("Containment Core");

            // Always open from this side - retreat back to the Combat Room is always allowed.
            CreateDoor("combat_boss_door", combatDoorPosition, new Vector2(1f, 1f), false, null,
                "CombatRoom", "combat_room_from_boss", new Vector2(14.5f, 7.5f), null, null);

            // Indestructible pillars: the only reliable cover from ring bursts, and the
            // reason phase 1 is about positioning rather than pure dodging.
            var pillarColor = new Color(0.32f, 0.26f, 0.3f);
            CreateCover("boss_pillar_nw", new Vector3(5.5f, 12.5f, 0f), new Vector2(1.2f, 1.2f), characters.cover, pillarColor, destructible: false);
            CreateCover("boss_pillar_sw", new Vector3(5.5f, 3.5f, 0f), new Vector2(1.2f, 1.2f), characters.cover, pillarColor, destructible: false);
            CreateCover("boss_pillar_ne", new Vector3(14.5f, 12.5f, 0f), new Vector2(1.2f, 1.2f), characters.cover, pillarColor, destructible: false);
            CreateCover("boss_pillar_se", new Vector3(14.5f, 3.5f, 0f), new Vector2(1.2f, 1.2f), characters.cover, pillarColor, destructible: false);

            // Phase 2: steam vents either side of the core, squeezing the comfortable
            // mid-range orbit the player settles into during phase 1.
            var steamColor = new Color(1f, 0.75f, 0.45f, 0.8f);
            GameObject ventA = CreateHazard("boss_vent_a", new Vector3(6.5f, 8f, 0f), new Vector2(2.5f, 2.5f), characters.hazard,
                HazardType.SteamVent, steamColor, damagePerTick: 9f, cycleSeconds: 3.4f, activePhaseSeconds: 1.3f);
            GameObject ventB = CreateHazard("boss_vent_b", new Vector3(13.5f, 8f, 0f), new Vector2(2.5f, 2.5f), characters.hazard,
                HazardType.SteamVent, steamColor, damagePerTick: 9f, cycleSeconds: 3.4f, activePhaseSeconds: 1.3f);

            // Phase 3: a beam sweeping the arena from the core outward.
            GameObject laser = CreateHazard("boss_laser", arenaCenter, new Vector2(13f, 0.7f), characters.hazard,
                HazardType.RotatingLaser, new Color(1f, 0.35f, 0.35f, 0.75f), damagePerTick: 11f,
                rotationDegreesPerSecond: 38f);

            // Phase 3 adds - fast movers specifically, because by that phase the player is
            // committed to dodging patterns and can least afford something chasing them.
            EnemyBase addA = CreateEnemy("boss_add_a", new Vector3(7.5f, 11.5f, 0f), characters.enemyFast, new Color(0.75f, 0.9f, 0.3f),
                EnemyType.Fast, maxHealth: 18f, moveSpeed: 3f, contactDamage: 9f, canShoot: false, preferredRange: 3f);
            EnemyBase addB = CreateEnemy("boss_add_b", new Vector3(12.5f, 4.5f, 0f), characters.enemyFast, new Color(0.75f, 0.9f, 0.3f),
                EnemyType.Fast, maxHealth: 18f, moveSpeed: 3f, contactDamage: 9f, canShoot: false, preferredRange: 3f);

            CreateBoss("thermal_runaway_core", arenaCenter, characters.boss, new Color(0.85f, 0.2f, 0.2f),
                maxHealth: 260f, attackIntervalSeconds: 2.5f, projectilesPerBurst: 9, projectileSpeed: 3.5f, projectileDamage: 10f,
                returnSceneName: "Lobby", returnSpawnPointId: "lobby_from_startroom", returnFallbackPosition: new Vector2(1.5f, 7.5f),
                phaseTwoHazards: new[] { ventA, ventB },
                phaseThreeHazards: new[] { laser },
                phaseThreeSupportEnemies: new[] { addA.gameObject, addB.gameObject });

            string path = $"{ScenesFolder}/BossRoom.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
