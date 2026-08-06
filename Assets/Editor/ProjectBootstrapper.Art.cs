using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private struct RoomTiles
        {
            public TileBase lobbyFloor;
            public TileBase lithographyFloor;
            public TileBase wall;
            public TileBase breakRoomFloor;
            public TileBase hallwayFloor;
            public TileBase storageFloor;
            public TileBase startRoomFloor;
            public TileBase explorationRoomFloor;
            public TileBase fabricationRoomFloor;
            public TileBase combatRoomFloor;
            public TileBase bossRoomFloor;
            public TileBase maintenanceBayFloor;
        }

        private struct CharacterSprites
        {
            public Sprite player;
            public Sprite receptionist;
            public Sprite floorSupervisor;
            public Sprite technician;
            public Sprite rivalTechnician;
            public Sprite offDutyEngineer;
            public Sprite passingTechnician;
            public Sprite oldAutomationUnit;
            public Sprite enemyBasic;
            public Sprite enemyDefensive;
            public Sprite enemyFast;
            public Sprite boss;

            public Sprite cover;
            public Sprite explosive;
            public Sprite turret;
            public Sprite shieldGenerator;
            public Sprite combatSwitch;
            public Sprite repairStation;
            public Sprite hazard;
        }

        /// <summary>
        /// Silhouettes for the generated placeholder art. Enemy archetypes get distinct
        /// shapes rather than distinct colours alone: at a glance during a fight, shape reads
        /// far faster than hue, and "which of these is the one that dashes" is a question the
        /// player has to answer in about a third of a second.
        /// </summary>
        private enum PixelShape
        {
            Circle,
            ArmoredSquare,
            Chevron,
            Ring,
            Cross,
            Barrel,
            Hexagon
        }

        private static CharacterSprites BuildCharacterSprites()
        {
            return new CharacterSprites
            {
                player = GenerateCharacterSprite("char_player", new Color(0.2f, 0.5f, 0.95f)),
                receptionist = GenerateCharacterSprite("char_receptionist", new Color(0.6f, 0.6f, 0.9f)),
                floorSupervisor = GenerateCharacterSprite("char_floor_supervisor", new Color(0.5f, 0.75f, 0.55f)),
                technician = GenerateCharacterSprite("char_technician", new Color(0.55f, 0.75f, 0.85f)),
                rivalTechnician = GenerateCharacterSprite("char_rival_technician", new Color(0.8f, 0.3f, 0.3f)),
                offDutyEngineer = GenerateCharacterSprite("char_off_duty_engineer", new Color(0.75f, 0.6f, 0.45f)),
                passingTechnician = GenerateCharacterSprite("char_passing_technician", new Color(0.7f, 0.85f, 0.9f)),
                oldAutomationUnit = GenerateCharacterSprite("char_old_automation_unit", new Color(0.55f, 0.55f, 0.6f)),
                // Shape carries the role, colour only reinforces it: round chaser, armoured
                // block, forward-pointing chevron for the one that charges you.
                enemyBasic = GeneratePixelSprite("char_enemy_basic", new Color(0.9f, 0.55f, 0.2f), PixelShape.Circle),
                enemyDefensive = GeneratePixelSprite("char_enemy_defensive", new Color(0.3f, 0.5f, 0.9f), PixelShape.ArmoredSquare),
                enemyFast = GeneratePixelSprite("char_enemy_fast", new Color(0.75f, 0.9f, 0.3f), PixelShape.Chevron),
                boss = GeneratePixelSprite("char_boss", new Color(0.85f, 0.2f, 0.2f), PixelShape.Ring),

                cover = GeneratePixelSprite("prop_cover", new Color(0.45f, 0.47f, 0.52f), PixelShape.ArmoredSquare),
                explosive = GeneratePixelSprite("prop_explosive", new Color(0.9f, 0.45f, 0.15f), PixelShape.Barrel),
                turret = GeneratePixelSprite("prop_turret", new Color(0.75f, 0.3f, 0.55f), PixelShape.Hexagon),
                shieldGenerator = GeneratePixelSprite("prop_shield_generator", new Color(0.4f, 0.9f, 0.85f), PixelShape.Ring),
                combatSwitch = GeneratePixelSprite("prop_combat_switch", new Color(0.6f, 0.3f, 0.3f), PixelShape.Cross),
                repairStation = GeneratePixelSprite("prop_repair_station", new Color(0.4f, 0.9f, 0.55f), PixelShape.Cross),
                hazard = GenerateTileSprite("prop_hazard", new Color(1f, 1f, 1f), new Color(0.85f, 0.85f, 0.85f))
            };
        }

        private static RoomTiles BuildSharedTiles()
        {
            Sprite lobbyFloorSprite = GenerateTileSprite("tile_lobby_floor", new Color(0.72f, 0.78f, 0.85f), new Color(0.6f, 0.68f, 0.76f));
            Sprite lithoFloorSprite = GenerateTileSprite("tile_litho_floor", new Color(0.55f, 0.68f, 0.8f), new Color(0.42f, 0.55f, 0.68f));
            Sprite wallSprite = GenerateTileSprite("tile_wall", new Color(0.35f, 0.37f, 0.42f), new Color(0.22f, 0.24f, 0.28f));
            Sprite breakRoomFloorSprite = GenerateTileSprite("tile_breakroom_floor", new Color(0.8f, 0.68f, 0.5f), new Color(0.68f, 0.55f, 0.38f));
            Sprite hallwayFloorSprite = GenerateTileSprite("tile_hallway_floor", new Color(0.85f, 0.87f, 0.88f), new Color(0.72f, 0.75f, 0.77f));
            Sprite storageFloorSprite = GenerateTileSprite("tile_storage_floor", new Color(0.5f, 0.46f, 0.4f), new Color(0.38f, 0.34f, 0.3f));
            Sprite startRoomFloorSprite = GenerateTileSprite("tile_start_room_floor", new Color(0.55f, 0.6f, 0.65f), new Color(0.42f, 0.47f, 0.52f));
            Sprite explorationRoomFloorSprite = GenerateTileSprite("tile_exploration_room_floor", new Color(0.45f, 0.55f, 0.45f), new Color(0.32f, 0.42f, 0.32f));
            Sprite fabricationRoomFloorSprite = GenerateTileSprite("tile_fabrication_room_floor", new Color(0.6f, 0.5f, 0.7f), new Color(0.48f, 0.38f, 0.58f));
            Sprite combatRoomFloorSprite = GenerateTileSprite("tile_combat_room_floor", new Color(0.55f, 0.3f, 0.3f), new Color(0.42f, 0.2f, 0.2f));
            Sprite bossRoomFloorSprite = GenerateTileSprite("tile_boss_room_floor", new Color(0.25f, 0.18f, 0.22f), new Color(0.15f, 0.1f, 0.13f));
            Sprite maintenanceBayFloorSprite = GenerateTileSprite("tile_maintenance_bay_floor", new Color(0.42f, 0.44f, 0.38f), new Color(0.3f, 0.32f, 0.26f));

            return new RoomTiles
            {
                lobbyFloor = CreateTile("Tile_LobbyFloor", lobbyFloorSprite, Tile.ColliderType.None),
                lithographyFloor = CreateTile("Tile_LithographyFloor", lithoFloorSprite, Tile.ColliderType.None),
                wall = CreateTile("Tile_Wall", wallSprite, Tile.ColliderType.Sprite),
                breakRoomFloor = CreateTile("Tile_BreakRoomFloor", breakRoomFloorSprite, Tile.ColliderType.None),
                hallwayFloor = CreateTile("Tile_HallwayFloor", hallwayFloorSprite, Tile.ColliderType.None),
                storageFloor = CreateTile("Tile_StorageFloor", storageFloorSprite, Tile.ColliderType.None),
                startRoomFloor = CreateTile("Tile_StartRoomFloor", startRoomFloorSprite, Tile.ColliderType.None),
                explorationRoomFloor = CreateTile("Tile_ExplorationRoomFloor", explorationRoomFloorSprite, Tile.ColliderType.None),
                fabricationRoomFloor = CreateTile("Tile_FabricationRoomFloor", fabricationRoomFloorSprite, Tile.ColliderType.None),
                combatRoomFloor = CreateTile("Tile_CombatRoomFloor", combatRoomFloorSprite, Tile.ColliderType.None),
                bossRoomFloor = CreateTile("Tile_BossRoomFloor", bossRoomFloorSprite, Tile.ColliderType.None),
                maintenanceBayFloor = CreateTile("Tile_MaintenanceBayFloor", maintenanceBayFloorSprite, Tile.ColliderType.None)
            };
        }

        private static TileBase CreateTile(string name, Sprite sprite, Tile.ColliderType colliderType)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = colliderType;
            return (TileBase)CreateOrReplaceAsset(tile, $"{TilesFolder}/{name}.asset");
        }

        private static Sprite GenerateTileSprite(string name, Color fill, Color border)
        {
            var texture = new Texture2D(PixelsPerUnit, PixelsPerUnit, TextureFormat.RGBA32, false);
            for (int y = 0; y < PixelsPerUnit; y++)
            {
                for (int x = 0; x < PixelsPerUnit; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == PixelsPerUnit - 1 || y == PixelsPerUnit - 1;
                    texture.SetPixel(x, y, isBorder ? border : fill);
                }
            }
            texture.Apply();

            return SaveTextureAsSprite(texture, $"{ArtFolder}/{name}.png");
        }

        private static Sprite GenerateCharacterSprite(string name, Color fill)
        {
            var texture = new Texture2D(PixelsPerUnit, PixelsPerUnit, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((PixelsPerUnit - 1) / 2f, (PixelsPerUnit - 1) / 2f);
            float radius = PixelsPerUnit / 2f - 1f;
            Color outline = fill * 0.7f;
            outline.a = 1f;

            for (int y = 0; y < PixelsPerUnit; y++)
            {
                for (int x = 0; x < PixelsPerUnit; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    Color color;
                    if (dist > radius)
                    {
                        color = new Color(0f, 0f, 0f, 0f);
                    }
                    else if (dist > radius - 1.5f)
                    {
                        color = outline;
                    }
                    else
                    {
                        color = fill;
                    }
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            return SaveTextureAsSprite(texture, $"{ArtFolder}/{name}.png");
        }

        /// <summary>
        /// One parametric generator for every combat silhouette, rather than a near-identical
        /// per-shape function each time a new prop is added. Every shape is outlined with a
        /// darkened edge so it stays readable against any floor tile.
        /// </summary>
        private static Sprite GeneratePixelSprite(string name, Color fill, PixelShape shape)
        {
            var texture = new Texture2D(PixelsPerUnit, PixelsPerUnit, TextureFormat.RGBA32, false);
            var center = new Vector2((PixelsPerUnit - 1) / 2f, (PixelsPerUnit - 1) / 2f);
            float extent = PixelsPerUnit / 2f - 1f;

            Color outline = fill * 0.6f;
            outline.a = 1f;
            var clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < PixelsPerUnit; y++)
            {
                for (int x = 0; x < PixelsPerUnit; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    bool inside;
                    bool isEdge;

                    switch (shape)
                    {
                        case PixelShape.ArmoredSquare:
                            // Heavy double border - reads as plated even at 16px.
                            float box = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                            inside = box <= extent;
                            isEdge = inside && box > extent - 3f;
                            break;

                        case PixelShape.Chevron:
                            // Arrowhead pointing +X, so a charger visibly has a "front".
                            inside = Mathf.Abs(dy) <= extent && dx <= extent - 1f &&
                                     dx >= -extent + Mathf.Abs(dy) * 0.35f - 1f &&
                                     Mathf.Abs(dy) <= extent - dx * 0.55f;
                            isEdge = inside && (Mathf.Abs(dy) > extent - dx * 0.55f - 1.6f || dx > extent - 2.6f);
                            break;

                        case PixelShape.Ring:
                            inside = distance <= extent && distance >= extent * 0.42f;
                            isEdge = inside && (distance > extent - 1.5f || distance < extent * 0.42f + 1.5f);
                            break;

                        case PixelShape.Cross:
                            inside = (Mathf.Abs(dx) <= extent * 0.34f && Mathf.Abs(dy) <= extent) ||
                                     (Mathf.Abs(dy) <= extent * 0.34f && Mathf.Abs(dx) <= extent);
                            isEdge = false;
                            break;

                        case PixelShape.Barrel:
                            // Tall rounded canister with a banded top - the classic "shoot me".
                            inside = Mathf.Abs(dx) <= extent * 0.62f && Mathf.Abs(dy) <= extent;
                            isEdge = inside && (Mathf.Abs(dx) > extent * 0.62f - 1.4f ||
                                                Mathf.Abs(Mathf.Abs(dy) - extent * 0.45f) < 0.9f);
                            break;

                        case PixelShape.Hexagon:
                            float hex = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dx) * 0.5f + Mathf.Abs(dy) * 0.87f);
                            inside = hex <= extent;
                            isEdge = inside && hex > extent - 1.6f;
                            break;

                        default:
                            inside = distance <= extent;
                            isEdge = inside && distance > extent - 1.5f;
                            break;
                    }

                    texture.SetPixel(x, y, !inside ? clear : isEdge ? outline : fill);
                }
            }
            texture.Apply();

            return SaveTextureAsSprite(texture, $"{ArtFolder}/{name}.png");
        }

        // Diamond silhouette (Manhattan-distance) - normal collectibles.
        private static Sprite GenerateItemSprite(string name, Color fill)
        {
            var texture = new Texture2D(PixelsPerUnit, PixelsPerUnit, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((PixelsPerUnit - 1) / 2f, (PixelsPerUnit - 1) / 2f);
            float half = PixelsPerUnit / 2f - 1f;
            Color outline = fill * 0.7f;
            outline.a = 1f;

            for (int y = 0; y < PixelsPerUnit; y++)
            {
                for (int x = 0; x < PixelsPerUnit; x++)
                {
                    float manhattan = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                    Color color;
                    if (manhattan > half)
                    {
                        color = new Color(0f, 0f, 0f, 0f);
                    }
                    else if (manhattan > half - 1.5f)
                    {
                        color = outline;
                    }
                    else
                    {
                        color = fill;
                    }
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            return SaveTextureAsSprite(texture, $"{ArtFolder}/{name}.png");
        }

        // Bordered square + centered white "chip" - key items, so they read as badge/card
        // shaped at a glance, distinct from the diamond collectibles.
        private static Sprite GenerateKeyItemSprite(string name, Color fill)
        {
            var texture = new Texture2D(PixelsPerUnit, PixelsPerUnit, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((PixelsPerUnit - 1) / 2f, (PixelsPerUnit - 1) / 2f);
            float chipRadius = PixelsPerUnit / 5f;
            Color border = fill * 0.6f;
            border.a = 1f;

            for (int y = 0; y < PixelsPerUnit; y++)
            {
                for (int x = 0; x < PixelsPerUnit; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == PixelsPerUnit - 1 || y == PixelsPerUnit - 1;
                    float dist = Vector2.Distance(new Vector2(x, y), center);

                    Color color;
                    if (isBorder)
                    {
                        color = border;
                    }
                    else if (dist <= chipRadius)
                    {
                        color = Color.white;
                    }
                    else
                    {
                        color = fill;
                    }
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            return SaveTextureAsSprite(texture, $"{ArtFolder}/{name}.png");
        }

        private static Sprite SaveTextureAsSprite(Texture2D texture, string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullPath = Path.Combine(projectRoot, assetPath);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
