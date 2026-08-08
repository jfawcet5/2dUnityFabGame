using BeyProject.Combat;
using BeyProject.Core;
using BeyProject.Data;
using BeyProject.Overworld;
using BeyProject.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static (Tilemap ground, Tilemap walls) CreateRoomTilemap(int width, int height, TileBase floorTile,
            TileBase wallTile, params Vector3Int[] doorGaps)
        {
            var gridGO = new GameObject("Grid", typeof(Grid));
            gridGO.GetComponent<Grid>().cellSize = new Vector3(1f, 1f, 0f);

            var groundGO = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundGO.transform.SetParent(gridGO.transform);
            Tilemap ground = groundGO.GetComponent<Tilemap>();

            var wallsGO = new GameObject("Walls", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
            wallsGO.transform.SetParent(gridGO.transform);
            Tilemap walls = wallsGO.GetComponent<Tilemap>();

            bool IsDoorGap(int x, int y)
            {
                foreach (Vector3Int gap in doorGaps)
                {
                    if (gap.x == x && gap.y == y)
                    {
                        return true;
                    }
                }
                return false;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    bool isBorder = x == 0 || y == 0 || x == width - 1 || y == height - 1;

                    if (isBorder && !IsDoorGap(x, y))
                    {
                        walls.SetTile(pos, wallTile);
                    }
                    else
                    {
                        ground.SetTile(pos, floorTile);
                    }
                }
            }

            return (ground, walls);
        }

        private static void CreateCamera(Vector3 focusPosition)
        {
            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow2D));
            camGO.tag = "MainCamera";
            Camera cam = camGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGO.transform.position = focusPosition + new Vector3(0f, 0f, -10f);
        }

        private static void CreatePlayer(Vector3 position, Sprite sprite, PlayerAnimationSprites animation)
        {
            var playerGO = new GameObject("Player", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer),
                typeof(HitFlash), typeof(PlayerController2D), typeof(PlayerInteractor), typeof(PlayerCombat), typeof(PlayerHealth),
                typeof(PlayerAnimator));
            playerGO.tag = "Player";
            playerGO.transform.position = position;

            Rigidbody2D body = playerGO.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            CircleCollider2D collider = playerGO.GetComponent<CircleCollider2D>();
            collider.radius = 0.4f;

            SpriteRenderer renderer = playerGO.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;

            HitFlash flash = WireHitFlash(playerGO, renderer);

            var healthSO = new SerializedObject(playerGO.GetComponent<PlayerHealth>());
            healthSO.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            healthSO.FindProperty("hitFlash").objectReferenceValue = flash;
            healthSO.ApplyModifiedPropertiesWithoutUndo();

            var animatorSO = new SerializedObject(playerGO.GetComponent<PlayerAnimator>());
            animatorSO.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            WriteSpriteArray(animatorSO.FindProperty("downFrames"), animation.down);
            WriteSpriteArray(animatorSO.FindProperty("upFrames"), animation.up);
            WriteSpriteArray(animatorSO.FindProperty("leftFrames"), animation.left);
            WriteSpriteArray(animatorSO.FindProperty("rightFrames"), animation.right);
            animatorSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteSpriteArray(SerializedProperty arrayProp, Sprite[] entries)
        {
            entries ??= new Sprite[0];
            arrayProp.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }
        }

        private static void CreateSpawnPoint(string spawnId, Vector3 position)
        {
            var go = new GameObject($"SpawnPoint - {spawnId}", typeof(RoomSpawnPoint));
            go.transform.position = position;
            go.GetComponent<RoomSpawnPoint>().spawnId = spawnId;
        }

        private static void CreateRoomAmbience()
        {
            new GameObject("RoomAmbience", typeof(RoomAmbience));
        }

        private static void CreateRoomIntro(string roomDisplayName)
        {
            var go = new GameObject("RoomIntro", typeof(RoomIntro));
            var so = new SerializedObject(go.GetComponent<RoomIntro>());
            so.FindProperty("roomDisplayName").stringValue = roomDisplayName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Door CreateDoor(string doorId, Vector3 position, Vector2 size, bool startsLocked, ItemDefinition requiredKeyItem,
            string targetScene, string targetSpawnPointId, Vector2 fallbackTargetPosition, Sprite lockedSprite, Sprite unlockedSprite)
        {
            var doorGO = new GameObject($"Door - {doorId}", typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(Door));
            doorGO.transform.position = position;

            BoxCollider2D collider = doorGO.GetComponent<BoxCollider2D>();
            collider.size = size;

            SpriteRenderer renderer = doorGO.GetComponent<SpriteRenderer>();
            renderer.sprite = startsLocked ? lockedSprite : unlockedSprite;
            renderer.sortingOrder = 3;

            Door door = doorGO.GetComponent<Door>();
            var so = new SerializedObject(door);
            so.FindProperty("doorId").stringValue = doorId;
            so.FindProperty("startsLocked").boolValue = startsLocked;
            so.FindProperty("requiredKeyItem").objectReferenceValue = requiredKeyItem;
            so.FindProperty("targetSceneName").stringValue = targetScene;
            so.FindProperty("targetSpawnPointId").stringValue = targetSpawnPointId;
            so.FindProperty("fallbackTargetPosition").vector2Value = fallbackTargetPosition;
            so.FindProperty("doorCollider").objectReferenceValue = collider;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("lockedSprite").objectReferenceValue = lockedSprite;
            so.FindProperty("unlockedSprite").objectReferenceValue = unlockedSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            return door;
        }

        private static DialogueNPC CreateDialogueNpc(string npcId, Vector3 position, Sprite sprite, Color fallbackColor,
            DialogueSequence firstMeeting, DialogueSequence repeat, WorldAction[] onFirstMeetActions,
            FlagDialogueOverride[] flagOverrides = null)
        {
            var npcGO = new GameObject($"NPC - {npcId}", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(DialogueNPC));
            npcGO.transform.position = position;

            CircleCollider2D collider = npcGO.GetComponent<CircleCollider2D>();
            collider.isTrigger = false;
            collider.radius = 0.4f;

            SpriteRenderer renderer = npcGO.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;

            DialogueNPC npc = npcGO.GetComponent<DialogueNPC>();
            var so = new SerializedObject(npc);
            so.FindProperty("npcId").stringValue = npcId;
            so.FindProperty("firstMeetingDialogue").objectReferenceValue = firstMeeting;
            so.FindProperty("repeatDialogue").objectReferenceValue = repeat;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("fallbackColor").colorValue = fallbackColor;
            WriteWorldActionsArray(so.FindProperty("onFirstMeetActions"), onFirstMeetActions);
            WriteFlagOverridesArray(so.FindProperty("flagDialogueOverrides"), flagOverrides);
            so.ApplyModifiedPropertiesWithoutUndo();

            return npc;
        }

        private static WorldInteractable CreateWorldInteractable(string interactableId, Vector3 position, Vector2 size,
            Sprite sprite, Color fallbackColor, bool oneShot, WorldAction[] actions,
            ItemDefinition requiredItem = null, string requiredFlag = null, DialogueSequence lockedDialogue = null)
        {
            var go = new GameObject($"Interactable - {interactableId}", typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(WorldInteractable));
            go.transform.position = position;

            BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.size = size;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 4;

            WorldInteractable interactable = go.GetComponent<WorldInteractable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("interactableId").stringValue = interactableId;
            so.FindProperty("oneShot").boolValue = oneShot;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("fallbackColor").colorValue = fallbackColor;
            so.FindProperty("requiredItem").objectReferenceValue = requiredItem;
            so.FindProperty("requiredFlag").stringValue = requiredFlag ?? "";
            so.FindProperty("lockedDialogue").objectReferenceValue = lockedDialogue;
            WriteWorldActionsArray(so.FindProperty("actions"), actions);
            so.ApplyModifiedPropertiesWithoutUndo();

            return interactable;
        }

        private static ItemPickup CreateItemPickup(string pickupId, Vector3 position, ItemDefinition item)
        {
            var go = new GameObject($"ItemPickup - {pickupId}", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(ItemPickup));
            go.transform.position = position;

            CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = item != null ? item.icon : null;
            renderer.sortingOrder = 4;

            ItemPickup pickup = go.GetComponent<ItemPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("pickupId").stringValue = pickupId;
            so.FindProperty("item").objectReferenceValue = item;
            so.FindProperty("quantity").intValue = 1;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return pickup;
        }

        private static void WriteWorldActionsArray(SerializedProperty arrayProp, WorldAction[] actions)
        {
            actions ??= new WorldAction[0];
            arrayProp.arraySize = actions.Length;

            for (int i = 0; i < actions.Length; i++)
            {
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
                WorldAction action = actions[i];

                element.FindPropertyRelative("type").enumValueIndex = (int)action.type;
                element.FindPropertyRelative("dialogue").objectReferenceValue = action.dialogue;
                element.FindPropertyRelative("item").objectReferenceValue = action.item;
                element.FindPropertyRelative("itemQuantity").intValue = action.itemQuantity;
                element.FindPropertyRelative("battleOpponent").objectReferenceValue = action.battleOpponent;
                element.FindPropertyRelative("doorToUnlock").objectReferenceValue = action.doorToUnlock;
                element.FindPropertyRelative("flagKey").stringValue = action.flagKey ?? "";
            }
        }

        private static void WriteFlagOverridesArray(SerializedProperty arrayProp, FlagDialogueOverride[] overrides)
        {
            overrides ??= new FlagDialogueOverride[0];
            arrayProp.arraySize = overrides.Length;

            for (int i = 0; i < overrides.Length; i++)
            {
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
                FlagDialogueOverride entry = overrides[i];

                element.FindPropertyRelative("requiredFlag").stringValue = entry.requiredFlag ?? "";
                element.FindPropertyRelative("requiredItem").objectReferenceValue = entry.requiredItem;
                element.FindPropertyRelative("dialogue").objectReferenceValue = entry.dialogue;
            }
        }

        private static EnemyBase CreateEnemy(string enemyId, Vector3 position, Sprite sprite, Color fallbackColor,
            EnemyType enemyType, float maxHealth, float moveSpeed, float contactDamage,
            bool canShoot = true, float preferredRange = 4.5f, float attackIntervalSeconds = 2.2f,
            float projectileSpeed = 4.5f, float projectileDamage = 7f)
        {
            var go = new GameObject($"Enemy - {enemyId}", typeof(CircleCollider2D), typeof(SpriteRenderer),
                typeof(HitFlash), typeof(EnemyBase));
            go.transform.position = position;

            CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = sprite != null ? Color.white : fallbackColor;
            renderer.sortingOrder = 5;

            HitFlash flash = WireHitFlash(go, renderer);

            EnemyBase enemy = go.GetComponent<EnemyBase>();
            var so = new SerializedObject(enemy);
            so.FindProperty("enemyId").stringValue = enemyId;
            so.FindProperty("enemyType").enumValueIndex = (int)enemyType;
            so.FindProperty("maxHealth").floatValue = maxHealth;
            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.FindProperty("contactDamage").floatValue = contactDamage;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("hitFlash").objectReferenceValue = flash;
            so.FindProperty("canShoot").boolValue = canShoot;
            so.FindProperty("preferredRange").floatValue = preferredRange;
            so.FindProperty("attackIntervalSeconds").floatValue = attackIntervalSeconds;
            so.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            so.FindProperty("projectileDamage").floatValue = projectileDamage;
            so.ApplyModifiedPropertiesWithoutUndo();

            return enemy;
        }

        /// <summary>
        /// Adds/wires the HitFlash every damageable prop shares. HitFlash owns the renderer's
        /// colour outright, so it must be told which renderer it drives at build time.
        /// </summary>
        private static HitFlash WireHitFlash(GameObject go, SpriteRenderer renderer)
        {
            HitFlash flash = go.GetComponent<HitFlash>();
            if (flash == null)
            {
                flash = go.AddComponent<HitFlash>();
            }

            var so = new SerializedObject(flash);
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return flash;
        }

        private static CoverObject CreateCover(string coverId, Vector3 position, Vector2 size, Sprite sprite,
            Color color, bool destructible = true, float maxHealth = 40f)
        {
            var go = new GameObject($"Cover - {coverId}", typeof(BoxCollider2D), typeof(SpriteRenderer),
                typeof(HitFlash), typeof(CoverObject));
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            // Collider stays 1x1 in local space - the transform scale carries the real size,
            // so one sprite works at any footprint without a second art asset.
            go.GetComponent<BoxCollider2D>().size = Vector2.one;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 4;

            HitFlash flash = WireHitFlash(go, renderer);

            CoverObject cover = go.GetComponent<CoverObject>();
            var so = new SerializedObject(cover);
            so.FindProperty("coverId").stringValue = coverId;
            so.FindProperty("destructible").boolValue = destructible;
            so.FindProperty("maxHealth").floatValue = maxHealth;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("fallbackColor").colorValue = color;
            so.FindProperty("hitFlash").objectReferenceValue = flash;
            so.ApplyModifiedPropertiesWithoutUndo();

            return cover;
        }

        private static ExplosiveObject CreateExplosive(string explosiveId, Vector3 position, Sprite sprite, Color color,
            float explosionRadius = 2.6f, float explosionDamage = 34f)
        {
            var go = new GameObject($"Explosive - {explosiveId}", typeof(CircleCollider2D), typeof(SpriteRenderer),
                typeof(HitFlash), typeof(ExplosiveObject));
            go.transform.position = position;

            CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
            collider.radius = 0.38f;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 4;

            HitFlash flash = WireHitFlash(go, renderer);

            ExplosiveObject explosive = go.GetComponent<ExplosiveObject>();
            var so = new SerializedObject(explosive);
            so.FindProperty("explosiveId").stringValue = explosiveId;
            so.FindProperty("explosionRadius").floatValue = explosionRadius;
            so.FindProperty("explosionDamage").floatValue = explosionDamage;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("fallbackColor").colorValue = color;
            so.FindProperty("hitFlash").objectReferenceValue = flash;
            so.ApplyModifiedPropertiesWithoutUndo();

            return explosive;
        }

        private static Turret CreateTurret(string turretId, Vector3 position, Sprite sprite, Color color,
            float maxHealth = 45f, float range = 7.5f, float fireIntervalSeconds = 1.8f, float projectileDamage = 9f)
        {
            var go = new GameObject($"Turret - {turretId}", typeof(CircleCollider2D), typeof(SpriteRenderer),
                typeof(HitFlash), typeof(Turret));
            go.transform.position = position;

            CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
            collider.radius = 0.42f;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 5;

            HitFlash flash = WireHitFlash(go, renderer);

            Turret turret = go.GetComponent<Turret>();
            var so = new SerializedObject(turret);
            so.FindProperty("turretId").stringValue = turretId;
            so.FindProperty("maxHealth").floatValue = maxHealth;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("fireIntervalSeconds").floatValue = fireIntervalSeconds;
            so.FindProperty("projectileDamage").floatValue = projectileDamage;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("fallbackColor").colorValue = color;
            so.FindProperty("hitFlash").objectReferenceValue = flash;
            so.ApplyModifiedPropertiesWithoutUndo();

            return turret;
        }

        private static GameObject CreateHazard(string hazardName, Vector3 position, Vector2 size, Sprite sprite,
            HazardType hazardType, Color activeColor, float damagePerTick = 7f, float cycleSeconds = 3f,
            float activePhaseSeconds = 1.4f, float rotationDegreesPerSecond = 45f)
        {
            var go = new GameObject($"Hazard - {hazardName}", typeof(BoxCollider2D), typeof(SpriteRenderer),
                typeof(EnvironmentalHazard));
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = activeColor;
            // Under the actors, over the floor - a hazard the player is standing in must not
            // hide the player.
            renderer.sortingOrder = 2;

            var idleColor = new Color(activeColor.r, activeColor.g, activeColor.b, activeColor.a * 0.22f);

            EnvironmentalHazard hazard = go.GetComponent<EnvironmentalHazard>();
            var so = new SerializedObject(hazard);
            so.FindProperty("hazardType").enumValueIndex = (int)hazardType;
            so.FindProperty("damagePerTick").floatValue = damagePerTick;
            so.FindProperty("cycleSeconds").floatValue = cycleSeconds;
            so.FindProperty("activePhaseSeconds").floatValue = activePhaseSeconds;
            so.FindProperty("rotationDegreesPerSecond").floatValue = rotationDegreesPerSecond;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("activeColor").colorValue = activeColor;
            so.FindProperty("idleColor").colorValue = idleColor;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static ShieldGenerator CreateShieldGenerator(string generatorId, Vector3 position, Sprite sprite,
            Color color, float maxHealth = 55f)
        {
            var go = new GameObject($"ShieldGenerator - {generatorId}", typeof(CircleCollider2D), typeof(SpriteRenderer),
                typeof(HitFlash), typeof(ShieldGenerator));
            go.transform.position = position;

            CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
            collider.radius = 0.42f;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 5;

            HitFlash flash = WireHitFlash(go, renderer);

            ShieldGenerator generator = go.GetComponent<ShieldGenerator>();
            var so = new SerializedObject(generator);
            so.FindProperty("generatorId").stringValue = generatorId;
            so.FindProperty("maxHealth").floatValue = maxHealth;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("fallbackColor").colorValue = color;
            so.FindProperty("hitFlash").objectReferenceValue = flash;
            so.ApplyModifiedPropertiesWithoutUndo();

            return generator;
        }

        private static CombatSwitch CreateCombatSwitch(string switchId, Vector3 position, Sprite sprite)
        {
            var go = new GameObject($"Switch - {switchId}", typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(CombatSwitch));
            go.transform.position = position;

            go.GetComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 4;

            CombatSwitch combatSwitch = go.GetComponent<CombatSwitch>();
            var so = new SerializedObject(combatSwitch);
            so.FindProperty("switchId").stringValue = switchId;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return combatSwitch;
        }

        private static RepairStation CreateRepairStation(Vector3 position, Sprite sprite, float healAmount = 45f)
        {
            var go = new GameObject("Repair Station", typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(RepairStation));
            go.transform.position = position;

            go.GetComponent<BoxCollider2D>().size = new Vector2(1.1f, 1.1f);

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 4;

            RepairStation station = go.GetComponent<RepairStation>();
            var so = new SerializedObject(station);
            so.FindProperty("healAmount").floatValue = healAmount;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return station;
        }

        private static FabricationStation CreateFabricationStation(Vector3 position, Color fallbackColor)
        {
            var go = new GameObject("Fabrication Station", typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(FabricationStation));
            go.transform.position = position;

            BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(1.2f, 1.2f);

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 4;

            FabricationStation station = go.GetComponent<FabricationStation>();
            var so = new SerializedObject(station);
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("fallbackColor").colorValue = fallbackColor;
            so.ApplyModifiedPropertiesWithoutUndo();

            return station;
        }

        private static void CreateCombatEncounter(Door targetDoor, string clearedFlag = "combat_room_cleared",
            CombatObjectiveType objective = CombatObjectiveType.DefeatAllEnemies, string objectiveAnnouncement = "")
        {
            var go = new GameObject("CombatEncounterController", typeof(CombatEncounterController));
            var so = new SerializedObject(go.GetComponent<CombatEncounterController>());
            so.FindProperty("targetDoor").objectReferenceValue = targetDoor;
            so.FindProperty("clearedFlag").stringValue = clearedFlag;
            so.FindProperty("objective").enumValueIndex = (int)objective;
            so.FindProperty("objectiveAnnouncement").stringValue = objectiveAnnouncement;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BossEnemy CreateBoss(string bossId, Vector3 position, Sprite sprite, Color fallbackColor, float maxHealth,
            float attackIntervalSeconds, int projectilesPerBurst, float projectileSpeed, float projectileDamage,
            string returnSceneName, string returnSpawnPointId, Vector2 returnFallbackPosition,
            GameObject[] phaseTwoHazards = null, GameObject[] phaseThreeHazards = null, GameObject[] phaseThreeSupportEnemies = null)
        {
            var go = new GameObject($"Boss - {bossId}", typeof(CircleCollider2D), typeof(SpriteRenderer),
                typeof(HitFlash), typeof(BossEnemy));
            go.transform.position = position;
            go.transform.localScale = new Vector3(2f, 2f, 1f);

            CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
            collider.radius = 0.45f;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = sprite != null ? Color.white : fallbackColor;
            renderer.sortingOrder = 5;

            HitFlash flash = WireHitFlash(go, renderer);

            BossEnemy boss = go.GetComponent<BossEnemy>();
            var so = new SerializedObject(boss);
            so.FindProperty("bossId").stringValue = bossId;
            so.FindProperty("maxHealth").floatValue = maxHealth;
            so.FindProperty("attackIntervalSeconds").floatValue = attackIntervalSeconds;
            so.FindProperty("projectilesPerBurst").intValue = projectilesPerBurst;
            so.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            so.FindProperty("projectileDamage").floatValue = projectileDamage;
            so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            so.FindProperty("hitFlash").objectReferenceValue = flash;
            so.FindProperty("returnSceneName").stringValue = returnSceneName;
            so.FindProperty("returnSpawnPointId").stringValue = returnSpawnPointId;
            so.FindProperty("returnFallbackPosition").vector2Value = returnFallbackPosition;
            WriteGameObjectArray(so.FindProperty("phaseTwoHazards"), phaseTwoHazards);
            WriteGameObjectArray(so.FindProperty("phaseThreeHazards"), phaseThreeHazards);
            WriteGameObjectArray(so.FindProperty("phaseThreeSupportEnemies"), phaseThreeSupportEnemies);
            so.ApplyModifiedPropertiesWithoutUndo();

            return boss;
        }

        private static void WriteGameObjectArray(SerializedProperty arrayProp, GameObject[] entries)
        {
            entries ??= new GameObject[0];
            arrayProp.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }
        }
    }
}
