using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Input;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Presentation;
using ProjectM.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace ProjectM.Editor
{
    /// <summary>
    /// Main.unity와 비주얼 에셋을 구성한다. 몇 번 실행해도 결과가 같다 (있으면 찾아 쓰고, 연결만 다시 한다).
    /// 메뉴: ProjectM/Setup/Main Scene. 편집 모드에서만 실행한다.
    /// </summary>
    public static class MainSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string VisualsDir = "Assets/_Project/Data/Visuals";
        private const string TileVisualSetPath = VisualsDir + "/TileVisualSet.asset";
        private const string EntityCatalogPath = VisualsDir + "/EntityVisualCatalog.asset";
        private const string GameConfigPath = "Assets/_Project/Data/Config/GameConfig.asset";
        private const string DungeonConfigPath = "Assets/_Project/Data/Dungeon/DungeonGenConfig.asset";
        private const string PlayerDefPath = "Assets/_Project/Data/Units/Player.asset";
        private const string EnemyDefPath = "Assets/_Project/Data/Units/MeleeEnemy.asset";
        private const string BossDefPath = "Assets/_Project/Data/Units/Boss.asset";
        private const string BossVisualId = "boss";
        private const string SpawnConfigPath = "Assets/_Project/Data/Units/SpawnConfig.asset";
        private const string MagicConfigPath = "Assets/_Project/Data/Magic/MagicConfig.asset";
        private const string SpellCatalogPath = "Assets/_Project/Data/Spells/SpellCatalog.asset";
        private const string StatusCatalogPath = "Assets/_Project/Data/Statuses/StatusCatalog.asset";
        private const string TerrainCatalogPath = "Assets/_Project/Data/Terrains/TerrainCatalog.asset";
        private const string ReactionCatalogPath = "Assets/_Project/Data/Reactions/ReactionCatalog.asset";
        private const string ItemCatalogPath = "Assets/_Project/Data/Items/ItemCatalog.asset";
        private const string FusionCatalogPath = "Assets/_Project/Data/Fusions/FusionCatalog.asset";
        private const string LootConfigPath = "Assets/_Project/Data/Items/LootConfig.asset";
        private const string ConsumableConfigPath = "Assets/_Project/Data/Items/ConsumableConfig.asset";
        // P-3a (결정 2026-10-21): 2D 픽셀 그래픽 — Grid를 눕히지 않고 정사영 카메라, 맵은 PixelMapPresenter. false면 G-1 3D 그대로
        private const bool Pixel2D = true;
        private const int PixelsPerUnit = 64;
        private const string PixelWallTilePath = "Assets/_Project/Art/Pixel/Tiles/Wall/PixelWallTile.asset";
        private const int UnitFootPixels = 26; // 유닛 발 = 칸 아래 끝 + 26px (칸 가운데쯤 — 머리는 위 칸으로, 2026-10-23 사용자 SPD 스크린샷)
        private const int PixelTileSpan = 4; // 바닥 · 벽 큰 그림 = 4×4칸 (결정 2026-10-22)
        private const string PixelTileDir = "Assets/_Project/Art/Pixel/Tiles";
        private const string PixelTileSetPath = "Assets/_Project/Data/Visuals/PixelTileSet.asset";
        private const string SpriteUnlitMaterialPath = "Assets/_Project/Art/Pixel/SpriteUnlit.mat";
        private const string CharacterCatalogPath = "Assets/_Project/Data/Units/CharacterCatalog.asset";
        private const string CharacterArtDir = "Assets/_Project/Art/Pixel/Characters";
        private const string EnemyArtDir = "Assets/_Project/Art/Pixel/Enemies";
        // 2026-10-21 캐릭터 선택 — 모습 ID (3D Main에서는 픽셀 전환 전까지 둘 다 플레이어 모델)
        private static readonly (string Id, string Name)[] Characters = { ("hero_blond", "금발의 마법사"), ("hero_black", "흑발의 마법사") };
        private const string ProgressionConfigPath = "Assets/_Project/Data/Units/ProgressionConfig.asset";
        private const string ItemVisualCatalogPath = "Assets/_Project/Data/Visuals/ItemVisualCatalog.asset";

        /// <summary>플레이어 시작 주문 (BALANCE 4번). 3-1부터 빈 칸 — 주문은 게임 중에 얻고, 테스트는 디버그 창에서 넣는다 (REACTIONS.md D8).</summary>
        private static readonly string[] PlayerStartingSpells = new string[0];

        private const string PlayerVisualId = "player";
        private const string EnemyVisualId = "enemy_melee";

        // 그레이박스: tile.png 슬라이스 0 = 연회색, 1 = 진회색, 3 = 파랑
        private const string FloorTilePath = "Assets/_Project/Art/Greybox/TilePalette/tile_0.asset";
        private const string WallTilePath = "Assets/_Project/Art/Greybox/TilePalette/tile_1.asset";
        private const string StairsTilePath = "Assets/_Project/Art/Greybox/TilePalette/tile_3.asset";
        private const string PlayerSpritePath = "Assets/_Project/Art/Greybox/player.png";
        private const string UnitCircleSpritePath = "Assets/_Project/Art/Greybox/unit_circle.png";
        private const string WhiteSquareSpritePath = "Assets/_Project/Art/Greybox/white_square.png";
        private const string FogTilePath = VisualsDir + "/FogTile.asset";
        private const string StairsUpTilePath = VisualsDir + "/StairsUpTile.asset";
        private const string WallMaterialPath = VisualsDir + "/Materials/WallGreybox.mat";
        /// <summary>코드로 만드는 도형(그레이박스 캡슐 · 바닥 책 상자)용 흰 URP Lit — 색은 틴트 · 칸마다 덮어씀. 빌드 마젠타 방지.</summary>
        private const string ShapeMaterialPath = VisualsDir + "/Materials/ShapeGreybox.mat";
        private const string Urp3DPath = "Assets/_Project/Settings/Rendering/URP3D.asset";
        private const string Renderer3DPath = "Assets/_Project/Settings/Rendering/Renderer3D.asset";

        // HUD 한글 폰트 (2-5): Galmuri11 — SIL OFL 1.1, 배포 가능. 원본 ttf에서 TMP 동적 SDF 에셋을 만든다
        private const string HudFontSourcePath = "Assets/_Project/Art/Fonts/Galmuri11.ttf";
        private const string HudFontAssetPath = "Assets/_Project/Art/Fonts/Galmuri11 SDF.asset";

        [MenuItem("ProjectM/Setup/Main Scene")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[MainSceneSetup] 플레이 중에는 실행하지 않는다.");
                return;
            }

            // 씬을 먼저 연다. OpenScene이 쓰지 않는 에셋을 메모리에서 내리므로, 그 전에 불러온 에셋 참조는 무효가 될 수 있다.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var tileVisuals = EnsureTileVisualSet();
            var catalog = EnsureEntityCatalog();
            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            if (config == null)
            {
                Debug.LogError("[MainSceneSetup] GameConfig 에셋 없음: " + GameConfigPath);
                return;
            }
            // 없으면 필드 초기값(BALANCE 2번)으로 만든다. 있으면 값을 건드리지 않는다.
            var dungeonConfig = LoadOrCreate<DungeonGenConfigAsset>(DungeonConfigPath);
            // 유닛 수치 — 없을 때만 BALANCE 3번 값으로 만든다. 있으면 값을 건드리지 않는다.
            var playerDef = EnsureAsset<UnitDefAsset>(PlayerDefPath,
                ("_visualId", PlayerVisualId), ("_maxHp", 20), ("_attackDamage", 3), ("_sightRange", 8),
                ("_maxMana", 10), ("_startingSpells", PlayerStartingSpells));
            var enemyDef = EnsureAsset<UnitDefAsset>(EnemyDefPath,
                ("_visualId", EnemyVisualId), ("_maxHp", 8), ("_attackDamage", 2), ("_sightRange", 6));
            // 3-6 · 3-7 명중 · 회피 · 경험치 (SPD 영웅 · 쥐) — 아직 0인 칸만 BALANCE 3번 값으로 채운다
            FillZeroInts(playerDef, ("_accuracy", 10), ("_evasion", 5), ("_strength", 10)); // 3-7b 힘 10 (SPD)
            FillEmptyString(playerDef, "_startingArmor", "armor_cloth"); // 3-10a 천 갑옷을 입고 시작 (SPD)
            FillZeroInts(enemyDef, ("_accuracy", 8), ("_evasion", 2), ("_expReward", 1), ("_expMaxLevel", 5));
            var spawnConfig = EnsureAsset<SpawnConfigAsset>(SpawnConfigPath,
                ("_enemyCount", 5), ("_enemyMinDistanceFromStart", 8));
            // 마나 · 장착 칸 — 없을 때만 BALANCE 4번 값으로 만든다
            var magicConfig = EnsureAsset<MagicConfigAsset>(MagicConfigPath,
                ("_manaRegenIntervalTicks", 3), ("_manaRegenAmount", 1), ("_spellSlotCount", 6), ("_inventorySlotCount", 20),
                ("_hpRegenIntervalTicks", 10), ("_hpRegenPercent", 1));
            var spellCatalog = EnsureSpellCatalog();
            var statusCatalog = EnsureStatusCatalog();
            var terrainCatalog = EnsureTerrainCatalog();
            var reactionCatalog = EnsureReactionCatalog();
            var itemCatalog = EnsureItemCatalog();
            var fusionCatalog = EnsureFusionCatalog();

            // G-1 (3D, 결정 2026-10-18): URP Universal Renderer · 조명
            EnsureRenderPipeline3D();
            EnsureLight();

            // Grid를 X축 90° 돌려 바닥(XZ)에 눕힌다 — 칸 (x, y) = 월드 (x + 0.5, 0, y + 0.5). Tilemap 덮개는 로컬 −Z(= 월드 위)로 조금씩 띄운다
            var gridGo = FindOrCreate("Grid", null);
            var grid = GetOrAdd<Grid>(gridGo);
            gridGo.transform.SetPositionAndRotation(Vector3.zero, Pixel2D ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f)); // P-3a 2D: 칸 (x, y) = 월드 (x + 0.5, y + 0.5, 0)
            // 칸 두께 0: 아니면 칸 중앙이 로컬 z 0.5 = 월드 y −0.5가 되어 유닛 · 모듈이 바닥 평면(y = 0, 클릭 · 덮개)보다 0.5 m 아래에 놓인다 (G-2b에서 발견)
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var terrainGo = FindOrCreate("Terrain", gridGo.transform);
            var tilemap = GetOrAdd<Tilemap>(terrainGo);
            GetOrAdd<TilemapRenderer>(terrainGo).sortingOrder = 0;
            terrainGo.transform.localPosition = Vector3.zero;

            var entitiesGo = FindOrCreate("Entities", null);
            var entityPresenter = GetOrAdd<EntityPresenter>(entitiesGo);
            SetRefs(entityPresenter, ("_grid", grid), ("_catalog", catalog), ("_greyboxMaterial", EnsureShapeMaterial()));

            var tilemapPresenter = GetOrAdd<TilemapPresenter>(terrainGo);
            SetRefs(tilemapPresenter, ("_tilemap", tilemap), ("_visuals", tileVisuals), ("_wallMaterial", EnsureWallMaterial()));
            var tilemapSo = new SerializedObject(tilemapPresenter); // G-1: 벽은 3D 블록
            tilemapSo.FindProperty("_walls3D").boolValue = true;
            tilemapSo.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(entityPresenter, ("_ground", tilemapPresenter)); // G-2b: 계단 위 서는 높이
            entityPresenter.ConfigurePixel2D(Pixel2D, EnsureSpriteUnlitMaterial(), PixelsPerUnit, UnitFootPixels); // P-3 ⓑ: 유닛 = 스프라이트
            EditorUtility.SetDirty(entityPresenter);
            var pixelMap = Pixel2D ? EnsurePixelMap(gridGo) : null; // P-3a 2D 맵 (3D TilemapPresenter는 남겨 두되 묶지 않음)

            // 지형 효과 오버레이: 바닥 위 1 cm · 유닛 아래
            var terrainFxGo = FindOrCreate("TerrainFx", gridGo.transform);
            var terrainFxTilemap = GetOrAdd<Tilemap>(terrainFxGo);
            GetOrAdd<TilemapRenderer>(terrainFxGo).sortingOrder = 5;
            terrainFxGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            var terrainOverlay = GetOrAdd<TerrainOverlayPresenter>(terrainFxGo);
            SetRefs(terrainOverlay, ("_tilemap", terrainFxTilemap), ("_tile", EnsureFogTile()));

            // 시야 안개: 엔티티(정렬 10) 위
            var fogGo = FindOrCreate("Fog", gridGo.transform);
            var fogTilemap = GetOrAdd<Tilemap>(fogGo);
            GetOrAdd<TilemapRenderer>(fogGo).sortingOrder = 20;
            fogGo.transform.localPosition = new Vector3(0f, 0f, -0.02f); // 바닥 · 지형 효과 위 (벽은 TilemapPresenter가 시야를 직접 따름)
            var fogPresenter = GetOrAdd<FogPresenter>(fogGo);
            SetRefs(fogPresenter, ("_tilemap", fogTilemap), ("_fogTile", EnsureFogTile()));
            if (Pixel2D) // 2026-10-23: SPD처럼 흐릿한 안개 경계 — 칸별 텍스처를 이중 선형으로 늘린 스프라이트 (Tilemap은 남겨 두되 안 그림)
            {
                var softGo = FindOrCreate("FogSoft", fogGo.transform);
                softGo.transform.localPosition = Vector3.zero;
                var softRenderer = GetOrAdd<SpriteRenderer>(softGo);
                softRenderer.sharedMaterial = EnsureSpriteUnlitMaterial();
                softRenderer.sortingOrder = 20;
                GetOrAdd<TilemapRenderer>(fogGo).sortingOrder = 21; // 안 본 칸 검정은 부드러운 안개 위 (번짐 없음)
                SetRefs(fogPresenter, ("_soft", softRenderer));
            }

            // 주문 조준 미리보기: 안개(20) 위
            var aimGo = FindOrCreate("Aim", gridGo.transform);
            var aimTilemap = GetOrAdd<Tilemap>(aimGo);
            GetOrAdd<TilemapRenderer>(aimGo).sortingOrder = 30;
            aimGo.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            var aimPreview = GetOrAdd<AimPreviewPresenter>(aimGo);
            SetRefs(aimPreview, ("_tilemap", aimTilemap), ("_tile", EnsureFogTile()));

            var cameraGo = GameObject.FindWithTag("MainCamera");
            CameraController cameraController = null;
            if (cameraGo != null)
            {
                cameraController = GetOrAdd<CameraController>(cameraGo);
                cameraController.ConfigurePixel2D(Pixel2D, PixelsPerUnit); // P-3a: 2D 정사영 · 정수 배율 (끄면 G-1 원근 · 기울임)
                var cam = cameraGo.GetComponent<Camera>();
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 100f;
                if (Pixel2D) // P-3a: 하늘 대신 어두운 바탕 (맵 밖 · 빈 칸)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.black; // 2026-10-22 얇은 벽: 벽 속(투명)이 안 본 곳(안개 검정)과 같은 색
                }
            }
            else Debug.LogWarning("[MainSceneSetup] MainCamera 태그 카메라 없음");

            var (hud, inventoryView, result) = EnsureHud();
            var characterSelect = EnsureCharacterSelect(); // 2026-10-21 판 시작 캐릭터 선택 창
            var characterCatalog = EnsureCharacterCatalog();

            // 3-5: 바닥 아이템(마법서) — 배치 수치(없을 때만 BALANCE 11번 값) · 표시 (색 · 반짝임은 메뉴 ProjectM/Setup/FX가 채움)
            var lootConfig = EnsureAsset<LootConfigAsset>(LootConfigPath, ("_maxBooksPerFloor", 2), ("_minDistanceFromStart", 3));
            FillZeroInts(lootConfig, ("_weaponsPerRun", 2), ("_upgradeScrollsPerRun", 3), ("_strengthPotionsPerRun", 2), ("_armorsPerRun", 2)); // 3-10a 갑옷 2 // 3-7a 무기 2 · 3-7b 강화 3 · 힘 2 (BALANCE 11번)
            var consumableConfig = LoadOrCreate<ConsumableConfigAsset>(ConsumableConfigPath); // 3-9: 새로 만들면 필드 초기값 = BALANCE 14번
            var floorItemsGo = FindOrCreate("FloorItems", null);
            var floorItems = GetOrAdd<FloorItemPresenter>(floorItemsGo);
            SetRefs(floorItems, ("_entities", entityPresenter), ("_bookMaterial", EnsureShapeMaterial()),
                ("_models", LoadOrCreate<ItemVisualCatalog>(ItemVisualCatalogPath))); // 3-7a 무기 모델 (Varco Models 메뉴가 채움)
            // 임시 보스 (3-4b) — 없을 때만 BALANCE 3번 값으로
            var bossDef = EnsureAsset<UnitDefAsset>(BossDefPath,
                ("_visualId", BossVisualId), ("_maxHp", 60), ("_attackDamage", 5), ("_sightRange", 10));
            FillZeroInts(bossDef, ("_accuracy", 10), ("_evasion", 8), ("_expReward", 10), ("_expMaxLevel", 30)); // SPD 구 (Goo)
            // 3-6 경험치 · 레벨업 (SPD) — 없을 때만 BALANCE 12번 값
            var progressionConfig = EnsureAsset<ProgressionConfigAsset>(ProgressionConfigPath,
                ("_maxLevel", 30), ("_expBase", 5), ("_expPerLevel", 5), ("_hpPerLevel", 5), ("_manaPerLevel", 1),
                ("_accuracyPerLevel", 1), ("_evasionPerLevel", 1));

            var rootGo = FindOrCreate("GameRoot", null);
            var input = GetOrAdd<PlayerInputController>(rootGo);
            SetRefs(input, ("_camera", cameraGo != null ? cameraGo.GetComponent<Camera>() : null), ("_grid", grid), ("_aimPreview", aimPreview),
                ("_cameraRig", cameraController), // 2026-10-19 둘러보기 · 확대 축소
                ("_hud", hud), ("_inventoryView", inventoryView));

            var root = GetOrAdd<GameRoot>(rootGo);
            SetRefs(root,
                ("_config", config),
                ("_dungeonConfig", dungeonConfig),
                ("_playerDef", playerDef),
                ("_enemyDef", enemyDef),
                ("_bossDef", bossDef),
                ("_result", result),
                ("_spawnConfig", spawnConfig),
                ("_magicConfig", magicConfig),
                ("_spellCatalog", spellCatalog),
                ("_statusCatalog", statusCatalog),
                ("_terrainCatalog", terrainCatalog),
                ("_reactionCatalog", reactionCatalog),
                ("_itemCatalog", itemCatalog),
                ("_fusionCatalog", fusionCatalog),
                ("_lootConfig", lootConfig),
                ("_consumableConfig", consumableConfig),
                ("_characters", characterCatalog),
                ("_characterSelect", characterSelect),
                ("_progressionConfig", progressionConfig),
                ("_floorItems", floorItems),
                ("_terrainOverlay", terrainOverlay),
                ("_tilemapPresenter", tilemapPresenter),
                ("_pixelMap", pixelMap),
                ("_entityPresenter", entityPresenter),
                ("_fogPresenter", fogPresenter),
                ("_input", input),
                ("_hud", hud),
                ("_camera", cameraController));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MainSceneSetup] 완료: " + ScenePath);
        }

        private static TileVisualSet EnsureTileVisualSet()
        {
            var set = LoadOrCreate<TileVisualSet>(TileVisualSetPath);
            set.Set(TileType.Floor, AssetDatabase.LoadAssetAtPath<TileBase>(FloorTilePath));
            set.Set(TileType.Wall, AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath));
            set.Set(TileType.StairsDown, AssetDatabase.LoadAssetAtPath<TileBase>(StairsTilePath));
            set.Set(TileType.StairsUp, EnsureStairsUpTile()); // 3-4a
            EditorUtility.SetDirty(set);
            return set;
        }

        /// <summary>
        /// G-1 (결정 2026-10-18): URP Universal Renderer(3D) 파이프라인 에셋을 만들고(없을 때만) 기본 파이프라인으로 지정한다.
        /// 이전 2D 에셋(URP2D · Renderer2D)은 파일만 남고 쓰이지 않는다. 품질 단계별 재정의는 0-1부터 없음.
        /// </summary>
        private static void EnsureRenderPipeline3D()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(Urp3DPath);
            if (asset == null)
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(Urp3DPath).Replace('\\', '/'));
                var data = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
                AssetDatabase.CreateAsset(data, Renderer3DPath);
                asset = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(data);
                AssetDatabase.CreateAsset(asset, Urp3DPath);
                AssetDatabase.SaveAssets();
            }
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != asset)
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = asset;
            if (QualitySettings.renderPipeline != null && QualitySettings.renderPipeline != asset)
                QualitySettings.renderPipeline = null; // 지금 품질 단계의 재정의가 있으면 기본을 따르게
        }

        /// <summary>G-1: 해 (Directional Light) 하나 — 3D 모델 · 벽이 보이게. 그림자는 부드럽게.</summary>
        private static void EnsureLight()
        {
            var lightGo = FindOrCreate("Sun", null);
            var light = GetOrAdd<Light>(lightGo);
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }

        /// <summary>G-1 그레이박스 벽 재질 (URP Lit, 진회색).</summary>
        private static Material EnsureWallMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
            if (material != null) return material;
            EnsureFolder(System.IO.Path.GetDirectoryName(WallMaterialPath).Replace('\\', '/'));
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "WallGreybox" };
            material.SetColor("_BaseColor", new Color(0.42f, 0.42f, 0.46f));
            AssetDatabase.CreateAsset(material, WallMaterialPath);
            return material;
        }

        /// <summary>
        /// 코드로 만드는 도형용 흰 URP Lit 재질. GameObject.CreatePrimitive가 붙이는 기본 재질(Built-in Standard 셰이더)은
        /// 에디터에서는 보이지만 URP 빌드에 들어가지 않아 마젠타가 된다 — 씬이 참조하는 이 재질로 바꿔 끼운다.
        /// </summary>
        private static Material EnsureShapeMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ShapeMaterialPath);
            if (material != null) return material;
            EnsureFolder(System.IO.Path.GetDirectoryName(ShapeMaterialPath).Replace('\\', '/'));
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ShapeGreybox" };
            material.SetColor("_BaseColor", Color.white);
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, ShapeMaterialPath);
            return material;
        }

        /// <summary>올라가는 계단 타일 (3-4a, 그레이박스): 흰 사각형을 청록색으로 — 내려가는 계단(파랑)과 구분.</summary>
        private static Tile EnsureStairsUpTile()
        {
            var tile = LoadOrCreate<Tile>(StairsUpTilePath);
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquareSpritePath);
            tile.color = new Color(0.25f, 0.85f, 0.75f);
            tile.flags = TileFlags.LockColor;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static EntityVisualCatalog EnsureEntityCatalog()
        {
            var catalog = LoadOrCreate<EntityVisualCatalog>(EntityCatalogPath);
            // G-1 3D 그레이박스: 프리팹이 없으면 캡슐 (색 · 키). Varco 모델은 G-2에서 Prefab에 넣는다 — Setup은 Prefab을 건드리지 않는다
            catalog.Set(PlayerVisualId, AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath), new Color(0.3f, 0.85f, 0.4f), 0.9f);
            catalog.Set(EnemyVisualId, AssetDatabase.LoadAssetAtPath<Sprite>(UnitCircleSpritePath), new Color(0.9f, 0.25f, 0.2f), 0.8f);
            foreach (var (id, _) in Characters) // 고를 수 있는 캐릭터 — 모델은 Varco Models 메뉴가 플레이어 모델로 넣는다
            {
                // 2D(P-3 ⓑ)에서는 스프라이트를 그대로 그리므로 흰색 (3D 캡슐 색은 모델이 있어 안 씀)
                var idle = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharacterArtDir}/{id}.png");
                catalog.Set(id, idle, Color.white, 0.9f);
                // P-3 ⓑ 걷기 (2026-10-22 금발 마법사): {id}_walk_{front,back,side}_0~3 — 없으면 정지 그림만
                Sprite[] Walk(string dir)
                {
                    var frames = new List<Sprite>();
                    for (int i = 0; i < 4; i++)
                    {
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharacterArtDir}/{id}_walk_{dir}_{i}.png");
                        if (sprite != null) frames.Add(sprite);
                    }
                    return frames.ToArray();
                }
                var front = Walk("front");
                if (front.Length > 0) catalog.SetPixel(id, idle, front, Walk("back"), Walk("side"));
                else catalog.SetPixel(id, null, null, null, null);
            }
            // 3-4b 임시 보스: 짙은 자주색, 두 배 키 (판정은 1칸)
            catalog.Set(BossVisualId, AssetDatabase.LoadAssetAtPath<Sprite>(UnitCircleSpritePath), new Color(0.55f, 0.1f, 0.45f), 1.8f);
            // P-3 ⓑ 적 · 보스 2D 정지 그림 (2026-10-23 Varco) — 3D 변형과 같은 순서(토끼 · 쥐 · 슬라임)라 같은 엔티티 = 같은 모습
            Sprite Enemy(string name)
            {
                string png = $"{EnemyArtDir}/{name}.png";
                if (!System.IO.File.Exists(png)) return null;
                PixelTestSetup.ImportPixelSprite(png, SpriteAlignment.BottomCenter);
                return AssetDatabase.LoadAssetAtPath<Sprite>(png);
            }
            Sprite[] Present(params Sprite[] sprites) => System.Array.FindAll(sprites, x => x != null);
            catalog.SetPixelVariants(EnemyVisualId, Present(Enemy("enemy_rabbit"), Enemy("enemy_mouse"), Enemy("enemy_slime")));
            catalog.SetPixelVariants(BossVisualId, Present(Enemy("boss_slime_girl")));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>
        /// 주문 데이터: 없는 ID만 BALANCE 5번 값으로 추가한다 (있는 항목은 사용자가 고친 값을 건드리지 않음).
        /// 2-3 상태 이상은 상태가 아직 없는 항목에만 채운다.
        /// </summary>
        private static SpellCatalogAsset EnsureSpellCatalog()
        {
            var catalog = LoadOrCreate<SpellCatalogAsset>(SpellCatalogPath);
            bool changed = false;
            changed |= catalog.AddIfMissing(new SpellCatalogAsset.Entry
                { Id = "fire_basic", Element = Element.Fire, Shape = SpellShape.Bolt, Range = 6, Damage = 5, ManaCost = 3 });
            changed |= catalog.AddIfMissing(new SpellCatalogAsset.Entry
                { Id = "ice_basic", Element = Element.Ice, Shape = SpellShape.Bolt, Range = 6, Damage = 3, ManaCost = 2 });
            changed |= catalog.AddIfMissing(new SpellCatalogAsset.Entry
                { Id = "lightning_basic", Element = Element.Lightning, Shape = SpellShape.Beam, Range = 5, Damage = 3, ManaCost = 4 });
            changed |= catalog.AddIfMissing(new SpellCatalogAsset.Entry
                { Id = "poison_basic", Element = Element.Poison, Shape = SpellShape.Burst, Range = 5, Damage = 2, ManaCost = 3, Radius = 1 });
            // 3-2 물 주문 (원본 6-1): 게임 중 획득 — 지금은 디버그 창에서 장착
            changed |= catalog.AddIfMissing(new SpellCatalogAsset.Entry
                { Id = "water_basic", Element = Element.Water, Shape = SpellShape.Burst, Range = 5, Damage = 0, ManaCost = 2, Radius = 1 });
            // BALANCE 6번: 주문 → 상태 이상 · 확률 (3-1 D3: 화상 · 둔화 · 중독 70%, 3-2 젖음 100%)
            changed |= catalog.SetStatusIfNone("fire_basic", StatusType.Burn, 70);
            changed |= catalog.SetStatusIfNone("ice_basic", StatusType.Slow, 70);
            changed |= catalog.SetStatusIfNone("lightning_basic", StatusType.Paralyze, 30);
            changed |= catalog.SetStatusIfNone("poison_basic", StatusType.Poison, 70);
            changed |= catalog.SetStatusIfNone("water_basic", StatusType.Wet, 100);
            // BALANCE 7번: 주문 → 지형 효과. 불 단독 화염 지대는 3-1 D2로 없음 (불 + 물 융합으로만)
            changed |= catalog.SetTerrainIfNone("poison_basic", TerrainEffectType.PoisonCloud);
            changed |= catalog.SetTerrainIfNone("water_basic", TerrainEffectType.Water);
            // 2-5: HUD · 로그 표시 이름 (비어 있을 때만)
            changed |= catalog.SetDisplayNameIfEmpty("fire_basic", "불꽃 화살");
            changed |= catalog.SetDisplayNameIfEmpty("ice_basic", "얼음 화살");
            changed |= catalog.SetDisplayNameIfEmpty("lightning_basic", "번개 줄기");
            changed |= catalog.SetDisplayNameIfEmpty("poison_basic", "독 폭발");
            changed |= catalog.SetDisplayNameIfEmpty("water_basic", "물보라");
            if (changed) EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>상태 이상 규칙: 없는 종류만 BALANCE 6번 값으로 추가한다.</summary>
        private static StatusCatalogAsset EnsureStatusCatalog()
        {
            var catalog = LoadOrCreate<StatusCatalogAsset>(StatusCatalogPath);
            bool changed = false;
            changed |= catalog.AddIfMissing(new StatusCatalogAsset.Entry { Type = StatusType.Burn, DurationTicks = 3, DamagePerTick = 1 });
            changed |= catalog.AddIfMissing(new StatusCatalogAsset.Entry { Type = StatusType.Slow, DurationTicks = 3, ExtraActionCostTicks = 1 });
            changed |= catalog.AddIfMissing(new StatusCatalogAsset.Entry { Type = StatusType.Poison, DurationTicks = 5, DamagePerTick = 1 });
            changed |= catalog.AddIfMissing(new StatusCatalogAsset.Entry { Type = StatusType.Paralyze, DurationTicks = 2, PreventsAction = true });
            changed |= catalog.AddIfMissing(new StatusCatalogAsset.Entry { Type = StatusType.Wet, DurationTicks = 3 }); // 3-2
            changed |= catalog.AddIfMissing(new StatusCatalogAsset.Entry { Type = StatusType.Frozen, DurationTicks = 2, PreventsAction = true }); // 3-3a
            if (changed) EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>지형 효과 규칙: 없는 종류만 BALANCE 7번 값으로 추가한다.</summary>
        private static TerrainCatalogAsset EnsureTerrainCatalog()
        {
            var catalog = LoadOrCreate<TerrainCatalogAsset>(TerrainCatalogPath);
            bool changed = false;
            changed |= catalog.AddIfMissing(new TerrainCatalogAsset.Entry
                { Type = TerrainEffectType.FireZone, DurationTicks = 4, AppliesStatus = StatusType.Burn });
            changed |= catalog.AddIfMissing(new TerrainCatalogAsset.Entry
                { Type = TerrainEffectType.PoisonCloud, DurationTicks = 4, AppliesStatus = StatusType.Poison });
            // 3-2: 물 타일 12틱 · 화염 지대 위에는 안 깔림, 오염된 물 8틱 · 젖음 + 매 틱 피해 1 (D7)
            changed |= catalog.AddIfMissing(new TerrainCatalogAsset.Entry
                { Type = TerrainEffectType.Water, DurationTicks = 12, AppliesStatus = StatusType.Wet, BlockedBy = TerrainEffectType.FireZone });
            changed |= catalog.AddIfMissing(new TerrainCatalogAsset.Entry
                { Type = TerrainEffectType.ContaminatedWater, DurationTicks = 8, AppliesStatus = StatusType.Wet, DamagePerTick = 1 });
            // 3-3a: 빙판 8틱 · 위 유닛 둔화 (D4 100%)
            changed |= catalog.AddIfMissing(new TerrainCatalogAsset.Entry
                { Type = TerrainEffectType.IcePatch, DurationTicks = 8, AppliesStatus = StatusType.Slow });
            // 3-9: 마비 가스 4틱 · 위 유닛 마비 (마비 가스 물약, BALANCE 7번)
            changed |= catalog.AddIfMissing(new TerrainCatalogAsset.Entry
                { Type = TerrainEffectType.ParalyticGas, DurationTicks = 4, AppliesStatus = StatusType.Paralyze });
            if (changed) EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>반응 규칙 표: 없는 ID만 REACTIONS.md 3-1 표 · BALANCE 8번 값으로 추가한다.</summary>
        private static ReactionCatalogAsset EnsureReactionCatalog()
        {
            var catalog = LoadOrCreate<ReactionCatalogAsset>(ReactionCatalogPath);
            bool changed = false;
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "toxic_ignite", DisplayName = "독화", Group = "toxic_ignite", Trigger = ReactionTrigger.Status, Status = StatusType.Poison,
                Element = Element.Fire, Priority = 10, ExtraDamage = 2, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "toxic_burst", DisplayName = "유독성 폭발", Trigger = ReactionTrigger.Status, Status = StatusType.Poison,
                Element = Element.Lightning, Priority = 20, ExplosionRadius = 1, ExplosionDamageFromRemaining = true, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "extinguish", DisplayName = "소화", Trigger = ReactionTrigger.Terrain, Terrain = TerrainEffectType.FireZone,
                Element = Element.Ice, Priority = 0, ExtraDamage = 2, ConsumeTrigger = true, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "toxic_ignite_cloud", DisplayName = "독화", Group = "toxic_ignite", Trigger = ReactionTrigger.Terrain,
                Terrain = TerrainEffectType.PoisonCloud, Element = Element.Fire, Priority = 10, ExtraDamage = 2, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "gas_explosion", DisplayName = "가스 폭발", Trigger = ReactionTrigger.Terrain, Terrain = TerrainEffectType.PoisonCloud,
                Element = Element.Lightning, Priority = 20, ConsumeTrigger = true, ExplosionRadius = 1, ExplosionDamage = 2,
                ExplosionStatus = StatusType.Poison, ExplosionStatusChancePercent = 70,
            });
            // 3-2: 증발 (젖음 · 물 타일 · 오염된 물 × 불, 화염 지대 × 물 — 한 그룹) · 오염 (물 타일 × 독)
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "evaporate", DisplayName = "증발", Group = "evaporate", Trigger = ReactionTrigger.Status, Status = StatusType.Wet,
                Element = Element.Fire, Priority = 10, ExtraDamage = 2, ConsumeTrigger = true, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "evaporate_water", DisplayName = "증발", Group = "evaporate", Trigger = ReactionTrigger.Terrain, Terrain = TerrainEffectType.Water,
                Element = Element.Fire, Priority = 10, ExtraDamage = 2, ConsumeTrigger = true, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "evaporate_contaminated", DisplayName = "증발", Group = "evaporate", Trigger = ReactionTrigger.Terrain,
                Terrain = TerrainEffectType.ContaminatedWater, Element = Element.Fire, Priority = 10, ExtraDamage = 2, ConsumeTrigger = true,
                ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "evaporate_fire_zone", DisplayName = "증발", Group = "evaporate", Trigger = ReactionTrigger.Terrain, Terrain = TerrainEffectType.FireZone,
                Element = Element.Water, Priority = 10, ExtraDamage = 2, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "contaminate", DisplayName = "오염", Trigger = ReactionTrigger.Terrain, Terrain = TerrainEffectType.Water,
                Element = Element.Poison, Priority = 0, TransformTerrainTo = TerrainEffectType.ContaminatedWater, ExplosionStatusChancePercent = 100,
            });
            // 3-3a: 빙결 (젖음 · 물 타일 · 오염된 물 × 얼음 — 한 그룹) · 해동 (빙결 · 빙판 × 불 — 한 그룹) · 파쇄 (빙결 × 번개)
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "freeze", DisplayName = "빙결", Group = "freeze", Trigger = ReactionTrigger.Status, Status = StatusType.Wet,
                Element = Element.Ice, Priority = 30, ConsumeTrigger = true, TargetStatus = StatusType.Frozen, TargetStatusChancePercent = 100,
                ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "freeze_water", DisplayName = "결빙", Group = "freeze", Trigger = ReactionTrigger.Terrain, Terrain = TerrainEffectType.Water,
                Element = Element.Ice, Priority = 30, TransformTerrainTo = TerrainEffectType.IcePatch, TransformDurationTicks = 8,
                TargetStatus = StatusType.Frozen, TargetStatusChancePercent = 100, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "freeze_contaminated", DisplayName = "결빙", Group = "freeze", Trigger = ReactionTrigger.Terrain,
                Terrain = TerrainEffectType.ContaminatedWater, Element = Element.Ice, Priority = 30, TransformTerrainTo = TerrainEffectType.IcePatch,
                TransformDurationTicks = 8, TargetStatus = StatusType.Frozen, TargetStatusChancePercent = 100, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "thaw", DisplayName = "해동", Group = "thaw", Trigger = ReactionTrigger.Status, Status = StatusType.Frozen,
                Element = Element.Fire, Priority = 30, ExtraDamage = 1, ConsumeTrigger = true, ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "thaw_ice", DisplayName = "해동", Group = "thaw", Trigger = ReactionTrigger.Terrain, Terrain = TerrainEffectType.IcePatch,
                Element = Element.Fire, Priority = 0, TransformTerrainTo = TerrainEffectType.Water, TransformDurationTicks = 12,
                ExplosionStatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "shatter", DisplayName = "파쇄", Trigger = ReactionTrigger.Status, Status = StatusType.Frozen,
                Element = Element.Lightning, Priority = 30, ExtraDamage = 3, ConsumeTrigger = true, ExplosionStatusChancePercent = 100,
            });
            // 3-3b: 감전 (젖음 × 번개 — 사슬, 인원 제한 없음) · 수면 감전 (물 타일 · 오염된 물 × 번개 — 8칸 · 4명). 셋 다 한 그룹, 전이 피해 50%
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "shock", DisplayName = "감전", Group = "shock", Trigger = ReactionTrigger.Status, Status = StatusType.Wet,
                Element = Element.Lightning, Priority = 20, ExtraDamage = 2, ExplosionStatusChancePercent = 100,
                Spread = ReactionSpread.StatusChain, SpreadRange = 3, SpreadMaxTargets = 0, SpreadDamagePercent = 50,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "shock_water", DisplayName = "수면 감전", Group = "shock", Trigger = ReactionTrigger.Terrain, Terrain = TerrainEffectType.Water,
                Element = Element.Lightning, Priority = 20, ExplosionStatusChancePercent = 100,
                Spread = ReactionSpread.TerrainFlood, SpreadRange = 8, SpreadMaxTargets = 4, SpreadDamagePercent = 50,
            });
            changed |= catalog.AddIfMissing(new ReactionCatalogAsset.Entry
            {
                Id = "shock_contaminated", DisplayName = "수면 감전", Group = "shock", Trigger = ReactionTrigger.Terrain,
                Terrain = TerrainEffectType.ContaminatedWater, Element = Element.Lightning, Priority = 20, ExplosionStatusChancePercent = 100,
                Spread = ReactionSpread.TerrainFlood, SpreadRange = 8, SpreadMaxTargets = 4, SpreadDamagePercent = 50,
            });
            if (changed) EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>안개 타일: 흰 사각형, 칸별 색을 쓸 수 있게 TileFlags.None.</summary>
        private static Tile EnsureFogTile()
        {
            var tile = LoadOrCreate<Tile>(FogTilePath);
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquareSpritePath);
            tile.color = Color.white;
            tile.flags = TileFlags.None;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>
        /// HUD 캔버스 (2-5, 기준 1920×1080): 왼쪽 위 HP · MP 막대, 그 오른쪽 상태 아이콘 줄, 아래로 빠른 칸(3-2a) · 안내 줄, 왼쪽 아래 전투 로그, 가운데 인벤토리 창(3-2a).
        /// </summary>
        private static (HudPresenter, InventoryPresenter, ResultPresenter) EnsureHud()
        {
            var canvasGo = FindOrCreate("HUD", null);
            var canvas = GetOrAdd<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetOrAdd<CanvasScaler>(canvasGo);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var font = EnsureHudFont();
            var topLeft = new Vector2(0, 1);
            var bottomLeft = new Vector2(0, 0);

            // 2-4까지의 한 덩어리 상태 글상자는 막대 · 칸 줄로 나뉘었다
            var oldStatus = canvasGo.transform.Find("Status");
            if (oldStatus != null) Object.DestroyImmediate(oldStatus.gameObject);

            var (hpFill, hpLabel) = EnsureBar(canvasGo.transform, "HpBar", new Vector2(24, -16), new Color(0.85f, 0.22f, 0.2f), font);
            var (mpFill, mpLabel) = EnsureBar(canvasGo.transform, "MpBar", new Vector2(24, -54), new Color(0.25f, 0.5f, 1f), font);
            // 3-6: 경험치 막대 (레벨 · 경험치)
            var (xpFill, xpLabel) = EnsureBar(canvasGo.transform, "XpBar", new Vector2(24, -92), new Color(0.85f, 0.7f, 0.2f), font, 22f, 18f);
            // 3-7a: 든 무기 한 줄 (경험치 막대 오른쪽) · 3-10a: 입은 갑옷 한 줄 (그 위, 마나 막대 오른쪽)
            var weaponLabel = EnsureText(canvasGo.transform, "Weapon", topLeft, new Vector2(404, -90), new Vector2(700, 26), 22, font, TextAlignmentOptions.TopLeft);
            var armorLabel = EnsureText(canvasGo.transform, "Armor", topLeft, new Vector2(404, -58), new Vector2(700, 26), 22, font, TextAlignmentOptions.TopLeft);

            var iconsGo = FindOrCreate("StatusIcons", canvasGo.transform);
            var icons = GetOrAdd<RectTransform>(iconsGo);
            Place(icons, topLeft, new Vector2(404, -16), new Vector2(1000, 30));
            var row = GetOrAdd<HorizontalLayoutGroup>(iconsGo);
            row.spacing = 8;
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            // 3-2a: 주문 장착 칸 글줄(Slots) → 빠른 칸 상자 줄(QuickSlots, 상자는 실행 중에 만든다)
            var oldSlots = canvasGo.transform.Find("Slots");
            if (oldSlots != null) Object.DestroyImmediate(oldSlots.gameObject);
            var quickGo = FindOrCreate("QuickSlots", canvasGo.transform);
            var quick = GetOrAdd<RectTransform>(quickGo);
            Place(quick, topLeft, new Vector2(24, -124), new Vector2(1600, 40));
            var quickRow = GetOrAdd<HorizontalLayoutGroup>(quickGo);
            quickRow.spacing = 6;
            quickRow.childAlignment = TextAnchor.UpperLeft;
            quickRow.childControlWidth = quickRow.childControlHeight = true;
            quickRow.childForceExpandWidth = quickRow.childForceExpandHeight = false;

            var hint = EnsureText(canvasGo.transform, "Hint", topLeft, new Vector2(24, -172), new Vector2(1600, 40), 26, font, TextAlignmentOptions.TopLeft);
            // 로그 8줄: 아래쪽 기준으로 쌓인다 (새 줄이 맨 아래)
            var log = EnsureText(canvasGo.transform, "Log", bottomLeft, new Vector2(24, 24), new Vector2(1100, 300), 26, font, TextAlignmentOptions.BottomLeft);

            // 3-2a 인벤토리 창: 가운데 패널 (칸 · 메뉴는 실행 중에 만든다), 다른 HUD 위에 그린다
            var inventoryGo = FindOrCreate("Inventory", canvasGo.transform);
            var inventoryPanel = GetOrAdd<RectTransform>(inventoryGo);
            Place(inventoryPanel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 360));
            GetOrAdd<Image>(inventoryGo).raycastTarget = false;
            inventoryGo.transform.SetAsLastSibling();
            var inventoryView = GetOrAdd<InventoryPresenter>(canvasGo);
            SetRefs(inventoryView, ("_panel", inventoryPanel), ("_font", font));

            var hud = GetOrAdd<HudPresenter>(canvasGo);
            SetRefs(hud,
                ("_hpFill", hpFill), ("_hpLabel", hpLabel),
                ("_mpFill", mpFill), ("_mpLabel", mpLabel),
                ("_xpFill", xpFill), ("_xpLabel", xpLabel), ("_weaponLabel", weaponLabel), ("_armorLabel", armorLabel),
                ("_statusIcons", icons), ("_quickSlots", quick), ("_hintText", hint), ("_logText", log));
            // 로그 줄 수 (결정 2026-10-16: 8줄). 2-4까지 씬에 저장된 6을 덮어쓴다
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_logLines").intValue = 8;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
            var result = EnsureResult(canvasGo, font);
            return (hud, inventoryView, result);
        }

        /// <summary>결과 화면 (3-4b): 화면 전체 어두운 덮개 + 가운데 상자(제목 · 본문). 맨 위에 그린다. 실행 중에는 판이 끝날 때만 켜진다.</summary>
        private static ResultPresenter EnsureResult(GameObject canvasGo, TMP_FontAsset font)
        {
            var panelGo = FindOrCreate("Result", canvasGo.transform);
            var panel = GetOrAdd<RectTransform>(panelGo);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            var dim = GetOrAdd<Image>(panelGo);
            dim.color = new Color(0f, 0f, 0f, 0.7f);
            dim.raycastTarget = false;

            var boxGo = FindOrCreate("Box", panelGo.transform);
            var box = GetOrAdd<RectTransform>(boxGo);
            Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 600));
            var boxBg = GetOrAdd<Image>(boxGo);
            boxBg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            boxBg.raycastTarget = false;

            var title = EnsureText(boxGo.transform, "Title", new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(940, 60), 44, font, TextAlignmentOptions.Top);
            var body = EnsureText(boxGo.transform, "Body", new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(900, 450), 30, font, TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            panelGo.transform.SetAsLastSibling();

            var result = GetOrAdd<ResultPresenter>(canvasGo);
            SetRefs(result, ("_panel", panel), ("_title", title), ("_body", body));
            return result;
        }

        /// <summary>아이템 규칙 (3-2a): 없는 ID만 BALANCE 9번 값으로 추가한다.</summary>
        private static ItemCatalogAsset EnsureItemCatalog()
        {
            var catalog = LoadOrCreate<ItemCatalogAsset>(ItemCatalogPath);
            bool changed = catalog.AddIfMissing(new ItemCatalogAsset.Entry { Id = "fusion_device", DisplayName = "마법 융합기", Use = ItemUse.Fusion });
            // 3-5 마법서 (BALANCE 9번): 쓰면 그 주문을 배운다. 한 판에 한 권씩 1~4층에 놓인다 (BALANCE 11번)
            changed |= catalog.AddIfMissing(new ItemCatalogAsset.Entry { Id = "spellbook_fire", DisplayName = "불꽃 화살 마법서", Use = ItemUse.LearnSpell, SpellId = "fire_basic" });
            changed |= catalog.AddIfMissing(new ItemCatalogAsset.Entry { Id = "spellbook_ice", DisplayName = "얼음 화살 마법서", Use = ItemUse.LearnSpell, SpellId = "ice_basic" });
            changed |= catalog.AddIfMissing(new ItemCatalogAsset.Entry { Id = "spellbook_lightning", DisplayName = "번개 줄기 마법서", Use = ItemUse.LearnSpell, SpellId = "lightning_basic" });
            changed |= catalog.AddIfMissing(new ItemCatalogAsset.Entry { Id = "spellbook_poison", DisplayName = "독 폭발 마법서", Use = ItemUse.LearnSpell, SpellId = "poison_basic" });
            changed |= catalog.AddIfMissing(new ItemCatalogAsset.Entry { Id = "spellbook_water", DisplayName = "물보라 마법서", Use = ItemUse.LearnSpell, SpellId = "water_basic" });
            // 3-7a 근접 무기 6종 (Docs/장비_데이터.md 1번 표 — SPD 크기, 나누기 3은 카탈로그 공통 값. BALANCE 13번)
            changed |= catalog.AddIfMissing(Weapon("shortsword", "소검", 15, 3, 1f, 1, 1));
            changed |= catalog.AddIfMissing(Weapon("hand_axe", "손도끼", 12, 3, 1.32f, 1, 1));
            changed |= catalog.AddIfMissing(Weapon("spear", "창", 20, 4, 1f, 2, 2));
            changed |= catalog.AddIfMissing(Weapon("quarterstaff", "육척봉", 12, 3, 1f, 1, 1));
            changed |= catalog.AddIfMissing(Weapon("dirk", "더크", 12, 3, 1f, 1, 1));
            changed |= catalog.AddIfMissing(Weapon("sickle", "손낫", 20, 3, 0.68f, 1, 1));
            changed |= catalog.FillWeaponStrength(12); // 3-7b: 요구 힘 (SPD 2티어) — 3-7a 줄에도
            changed |= catalog.FillWeaponSpecials("dirk", 67, 0); // 3-7c: 더크 기습 최소 피해 67%
            changed |= catalog.FillWeaponSpecials("quarterstaff", 0, 2); // 3-7c: 육척봉 방어 0~2
            // 3-7b 힘 물약 · 강화 주문서 (BALANCE 9번) — 한 판에 2 · 3개 (BALANCE 11번)
            changed |= catalog.AddIfMissing(new ItemCatalogAsset.Entry { Id = "potion_strength", DisplayName = "힘 물약", Use = ItemUse.Potion, Amount = 1, Effect = ConsumableEffect.Strength });
            changed |= catalog.AddIfMissing(new ItemCatalogAsset.Entry { Id = "scroll_upgrade", DisplayName = "강화 주문서", Use = ItemUse.Scroll, Amount = 1, Effect = ConsumableEffect.Upgrade });
            changed |= catalog.FillEffect("potion_strength", ConsumableEffect.Strength); // 3-9: 3-7b 줄에 효과
            changed |= catalog.FillEffect("scroll_upgrade", ConsumableEffect.Upgrade);
            // 3-9 물약 · 주문서 (SPD 효과 · 무게, BALANCE 14번) — 판마다 겉모습이 섞인다 (식별)
            changed |= catalog.AddIfMissing(Consumable("potion_healing", "치유 물약", ItemUse.Potion, ConsumableEffect.Healing, 3));
            changed |= catalog.AddIfMissing(Consumable("potion_experience", "경험 물약", ItemUse.Potion, ConsumableEffect.Experience, 1));
            changed |= catalog.AddIfMissing(Consumable("potion_purity", "정화 물약", ItemUse.Potion, ConsumableEffect.Purity, 2));
            changed |= catalog.AddIfMissing(Consumable("potion_toxic_gas", "독가스 물약", ItemUse.Potion, ConsumableEffect.ToxicGas, 3));
            changed |= catalog.AddIfMissing(Consumable("potion_liquid_flame", "액체 화염 물약", ItemUse.Potion, ConsumableEffect.LiquidFlame, 3));
            changed |= catalog.AddIfMissing(Consumable("potion_frost", "냉기 물약", ItemUse.Potion, ConsumableEffect.Frost, 3));
            changed |= catalog.AddIfMissing(Consumable("potion_paralytic_gas", "마비 가스 물약", ItemUse.Potion, ConsumableEffect.ParalyticGas, 2));
            changed |= catalog.AddIfMissing(Consumable("scroll_identify", "식별 주문서", ItemUse.Scroll, ConsumableEffect.Identify, 3));
            changed |= catalog.AddIfMissing(Consumable("scroll_magic_mapping", "지도 주문서", ItemUse.Scroll, ConsumableEffect.MagicMapping, 1));
            changed |= catalog.AddIfMissing(Consumable("scroll_teleportation", "순간이동 주문서", ItemUse.Scroll, ConsumableEffect.Teleportation, 1));
            changed |= catalog.AddIfMissing(Consumable("scroll_recharging", "충전 주문서", ItemUse.Scroll, ConsumableEffect.Recharging, 1));
            changed |= catalog.AddIfMissing(Consumable("scroll_rage", "격노 주문서", ItemUse.Scroll, ConsumableEffect.Rage, 1));
            changed |= catalog.AddIfMissing(Consumable("scroll_retribution", "응징 주문서", ItemUse.Scroll, ConsumableEffect.Retribution, 1));
            // 3-10a 갑옷 5종 (SPD 1~5티어, 요구 힘 8 + 2 × 티어, BALANCE 15번)
            changed |= catalog.AddIfMissing(Armor("armor_cloth", "천 갑옷", 1, 10));
            changed |= catalog.AddIfMissing(Armor("armor_leather", "가죽 갑옷", 2, 12));
            changed |= catalog.AddIfMissing(Armor("armor_mail", "사슬 갑옷", 3, 14));
            changed |= catalog.AddIfMissing(Armor("armor_scale", "비늘 갑옷", 4, 16));
            changed |= catalog.AddIfMissing(Armor("armor_plate", "판금 갑옷", 5, 18));
            if (changed) EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>
        /// P-3a 2D 맵: Grid 아래 "PixelMap" Tilemap(맨 아래 정렬) + PixelMapPresenter + PixelTileSet(Art/Pixel/Tiles의 64px 타일).
        /// 바닥 변형은 floor_*.png 전부. 여러 번 실행해도 결과가 같다.
        /// </summary>
        private static PixelMapPresenter EnsurePixelMap(GameObject gridGo)
        {
            var go = FindOrCreate("PixelMap", gridGo.transform);
            go.transform.localPosition = new Vector3(0f, 0f, 0.01f); // 다른 덮개(지형 효과 · 안개 · 조준)보다 뒤
            var tilemap = GetOrAdd<Tilemap>(go);
            var renderer = GetOrAdd<TilemapRenderer>(go);
            renderer.sortingOrder = -10;
            renderer.sharedMaterial = EnsureSpriteUnlitMaterial();
            // 2026-10-22: 바닥 = 이어지는 큰 그림을 PixelTileSpan × PixelTileSpan으로 자른 floor_NN(아래 줄부터, Tools/tiles_seamless.py),
            // 벽 = 얇은 벽 RuleTile(PixelWallTile — 그림은 Tools/tiles_wall.py가 Tiles/Wall에)
            int cells = PixelTileSpan * PixelTileSpan;
            var floors = new TileBase[cells];
            for (int i = 0; i < cells; i++) floors[i] = EnsurePixelTile($"floor_{i:00}");
            var set = LoadOrCreate<PixelTileSet>(PixelTileSetPath);
            var caps = new TileBase[4]; // 홀로 선 벽(한 칸 기둥 · 얇은 가로벽)의 솟은 윗면 — 위 칸에 덮개 (2026-10-23 SPD식 벽 높이)
            for (int i = 0; i < caps.Length; i++) caps[i] = EnsurePixelTile($"Wall/wall_cap_{i}");
            set.Configure(PixelTileSpan, floors, EnsurePixelWallTile(), caps,
                EnsurePixelTile("stairs_down"), EnsurePixelTile("stairs_up"));
            EditorUtility.SetDirty(set);

            var oldRims = gridGo.transform.Find("PixelRims"); // 이전(덮개 테두리) 방식 정리
            if (oldRims != null) Object.DestroyImmediate(oldRims.gameObject);
            var capGo = FindOrCreate("PixelWallCaps", gridGo.transform); // 맵 위 · 지형 효과 · 안개 아래
            capGo.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            var capMap = GetOrAdd<Tilemap>(capGo);
            var capRenderer = GetOrAdd<TilemapRenderer>(capGo);
            capRenderer.sortingOrder = -9;
            capRenderer.sharedMaterial = renderer.sharedMaterial;

            var presenter = GetOrAdd<PixelMapPresenter>(go);
            SetRefs(presenter, ("_tilemap", tilemap), ("_tiles", set), ("_caps", capMap));
            return presenter;
        }

        /// <summary>
        /// 얇은 벽 RuleTile (2026-10-22): Tiles/Wall의 wall_front_S_X · wall_top_MMM 그림으로 규칙을 매번 새로 만든다(여러 번 실행해도 같음).
        /// 비트 · 모서리 조건은 Tools/tiles_wall.py와 같다. 규칙 순서: 앞면(아래가 바닥, 칸 전체 = 테두리 16 + 벽돌 48) 4종 → 윗면 조합.
        /// 홀로 선 벽의 솟은 윗면은 RuleTile 밖(PixelWallCaps 덮개, wall_cap_M) — SPD처럼 기둥이 1.5칸 (2026-10-23).
        /// 맞는 규칙이 없으면 그림 없음(벽 속).
        /// </summary>
        private static PixelWallTile EnsurePixelWallTile()
        {
            string dir = PixelTileDir + "/Wall";
            Sprite Load(string name)
            {
                string png = $"{dir}/{name}.png";
                if (!System.IO.File.Exists(png)) { Debug.LogWarning("[MainSceneSetup] 벽 그림 없음: " + png); return null; }
                PixelTestSetup.ImportPixelSprite(png, SpriteAlignment.Center);
                return AssetDatabase.LoadAssetAtPath<Sprite>(png);
            }
            RuleTile.TilingRule Rule(Sprite[] sprites, params (int x, int y, int n)[] neighbors)
            {
                var rule = new RuleTile.TilingRule
                {
                    m_Sprites = sprites,
                    m_Output = sprites.Length > 1 ? RuleTile.TilingRuleOutput.OutputSprite.Random : RuleTile.TilingRuleOutput.OutputSprite.Single,
                    m_ColliderType = Tile.ColliderType.None,
                };
                rule.m_Neighbors = new List<int>();
                rule.m_NeighborPositions = new List<Vector3Int>();
                foreach (var (x, y, n) in neighbors)
                {
                    rule.m_NeighborPositions.Add(new Vector3Int(x, y, 0));
                    rule.m_Neighbors.Add(n);
                }
                return rule;
            }
            const int floor = PixelWallTile.Neighbor.Floor, notFloor = PixelWallTile.Neighbor.NotFloor;
            const int shown = PixelWallTile.Neighbor.Shown, hidden = PixelWallTile.Neighbor.Hidden;
            Sprite[] Frames(string key)
            {
                var sprites = new Sprite[PixelTileSpan];
                for (int x = 0; x < PixelTileSpan; x++) sprites[x] = Load($"wall_front_{key}_{x}");
                return sprites;
            }

            var rules = new List<RuleTile.TilingRule>();
            // 앞면 (2026-10-23 사용자 "벽이 낮다" → SPD처럼 칸 전체): 테두리 16 + 벽돌 48 — 높이 64 ≈ 캐릭터 키. 비트 1 오른쪽 · 2 왼쪽이 바닥
            for (int sides = 0; sides < 4; sides++)
                rules.Add(Rule(Frames($"n{sides}"), (0, -1, floor), (1, 0, (sides & 1) != 0 ? floor : notFloor), (-1, 0, (sides & 2) != 0 ? floor : notFloor)));
            for (int m = 1; m < 128; m++) // 윗면: 1 위 · 2 오른쪽 · 4 왼쪽 변, 8 오른쪽위 · 16 왼쪽위 · 32 오른쪽아래 · 64 왼쪽아래 모서리
            {
                bool n = (m & 1) != 0, e = (m & 2) != 0, w = (m & 4) != 0;
                if (((m & 8) != 0 && (n || e)) || ((m & 16) != 0 && (n || w)) || ((m & 32) != 0 && e) || ((m & 64) != 0 && w)) continue;
                var neighbors = new List<(int, int, int)>
                {
                    (0, -1, notFloor), (0, 1, n ? shown : hidden), (1, 0, e ? shown : hidden), (-1, 0, w ? shown : hidden),
                };
                if (!n && !e) neighbors.Add((1, 1, (m & 8) != 0 ? shown : hidden));
                if (!n && !w) neighbors.Add((-1, 1, (m & 16) != 0 ? shown : hidden));
                if (!e) neighbors.Add((1, -1, (m & 32) != 0 ? floor : notFloor));
                if (!w) neighbors.Add((-1, -1, (m & 64) != 0 ? floor : notFloor));
                rules.Add(Rule(new[] { Load($"wall_top_{m:000}") }, neighbors.ToArray()));
            }

            var tile = AssetDatabase.LoadAssetAtPath<PixelWallTile>(PixelWallTilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<PixelWallTile>();
                AssetDatabase.CreateAsset(tile, PixelWallTilePath);
            }
            tile.m_DefaultSprite = null;
            tile.m_DefaultColliderType = Tile.ColliderType.None;
            tile.m_TilingRules = rules;
            tile.UpdateNeighborPositions();
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>Art/Pixel/Tiles/name.png → 픽셀 설정 스프라이트 + 같은 폴더 name.asset Tile. 없으면 null.</summary>
        private static TileBase EnsurePixelTile(string name)
        {
            string png = $"{PixelTileDir}/{name}.png";
            if (!System.IO.File.Exists(png))
            {
                Debug.LogWarning("[MainSceneSetup] 픽셀 타일 없음: " + png);
                return null;
            }
            PixelTestSetup.ImportPixelSprite(png, SpriteAlignment.Center);
            string assetPath = $"{PixelTileDir}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>조명 없이 그대로 그리는 스프라이트 재질 (Universal 렌더러 — P-3a).</summary>
        internal static Material EnsureSpriteUnlitMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, SpriteUnlitMaterialPath);
            return material;
        }

        /// <summary>
        /// 원본 일러스트 가져오기: 도트풍 그림이지만 칸이 일정한 격자가 아니라(판별 결과) 선택 창에서 줄여 보여 주므로 부드러운 필터 · 밉맵 · 압축 없음.
        /// </summary>
        private static void ImportIllustration(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.filterMode = FilterMode.Bilinear;
            settings.mipmapEnabled = true;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        /// <summary>캐릭터 선택 창 (2026-10-21): HUD 위를 덮는 판 — 카드는 실행 중에 만든다.</summary>
        private static CharacterSelectPresenter EnsureCharacterSelect()
        {
            var canvasGo = FindOrCreate("HUD", null);
            var font = EnsureHudFont();
            var panelGo = FindOrCreate("CharacterSelect", canvasGo.transform);
            var panel = GetOrAdd<RectTransform>(panelGo);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            GetOrAdd<Image>(panelGo).raycastTarget = false;
            panelGo.transform.SetAsLastSibling();
            var view = GetOrAdd<CharacterSelectPresenter>(canvasGo);
            SetRefs(view, ("_panel", panel), ("_font", font));
            return view;
        }

        /// <summary>캐릭터 목록 (2026-10-21): 초상 이미지를 픽셀 설정으로 가져오고, 없는 캐릭터만 추가 (사용자가 고친 이름은 그대로).</summary>
        private static CharacterCatalogAsset EnsureCharacterCatalog()
        {
            var catalog = LoadOrCreate<CharacterCatalogAsset>(CharacterCatalogPath);
            bool changed = false;
            foreach (var (id, name) in Characters)
            {
                string path = $"{CharacterArtDir}/{id}.png";
                PixelTestSetup.ImportPixelSprite(path, SpriteAlignment.BottomCenter);
                string illustPath = $"{CharacterArtDir}/{id}_illust.png"; // 7등신 원본 일러스트 (있을 때만, 2026-10-21)
                Sprite illust = null;
                if (System.IO.File.Exists(illustPath))
                {
                    ImportIllustration(illustPath);
                    illust = AssetDatabase.LoadAssetAtPath<Sprite>(illustPath);
                }
                changed |= catalog.AddOrFill(new CharacterCatalogAsset.Entry
                    { Id = id, DisplayName = name, VisualId = id, Portrait = AssetDatabase.LoadAssetAtPath<Sprite>(path), Illustration = illust });
            }
            if (changed) EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static ItemCatalogAsset.Entry Armor(string id, string name, int tier, int strength) =>
            new ItemCatalogAsset.Entry { Id = id, DisplayName = name, Use = ItemUse.EquipArmor, Tier = tier, StrengthRequirement = strength };

        private static ItemCatalogAsset.Entry Consumable(string id, string name, ItemUse use, ConsumableEffect effect, int weight) =>
            new ItemCatalogAsset.Entry { Id = id, DisplayName = name, Use = use, Effect = effect, Weight = weight };

        /// <summary>융합 규칙 (3-2b, REACTIONS.md 5번): 없는 ID만 BALANCE 10번 값으로 추가한다. 상태를 거는 융합이 생기면 확률을 꼭 적는다.</summary>
        private static FusionCatalogAsset EnsureFusionCatalog()
        {
            var catalog = LoadOrCreate<FusionCatalogAsset>(FusionCatalogPath);
            bool changed = false;
            changed |= catalog.AddIfMissing(new FusionCatalogAsset.Entry
            {
                Id = "fusion_fire_water", DisplayName = "용암 분출", ElementA = Element.Fire, ElementB = Element.Water,
                LeavesTerrain = TerrainEffectType.FireZone, TerrainDurationTicks = 6, StatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new FusionCatalogAsset.Entry
            {
                Id = "fusion_water_poison", DisplayName = "오염 물결", ElementA = Element.Water, ElementB = Element.Poison,
                LeavesTerrain = TerrainEffectType.ContaminatedWater, TerrainDurationTicks = 8, StatusChancePercent = 100,
            });
            changed |= catalog.AddIfMissing(new FusionCatalogAsset.Entry
            {
                Id = "fusion_fire_lightning", DisplayName = "폭발 마법", ElementA = Element.Fire, ElementB = Element.Lightning,
                StatusChancePercent = 100,
            });
            // 3-3a: 얼음 + 물 = 빙판 3×3 8틱 + 맞은 대상 빙결 확정
            changed |= catalog.AddIfMissing(new FusionCatalogAsset.Entry
            {
                Id = "fusion_ice_water", DisplayName = "빙결 파도", ElementA = Element.Ice, ElementB = Element.Water,
                LeavesTerrain = TerrainEffectType.IcePatch, TerrainDurationTicks = 8, AppliesStatus = StatusType.Frozen, StatusChancePercent = 100,
            });
            if (changed) EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>막대 하나: 어두운 바탕 + 왼쪽부터 차는 색 칸(Fill) + 가운데 숫자(Label). 360×30.</summary>
        private static (RectTransform fill, TextMeshProUGUI label) EnsureBar(Transform parent, string name, Vector2 topLeft, Color color, TMP_FontAsset font,
            float height = 30f, float fontSize = 24f)
        {
            var go = FindOrCreate(name, parent);
            Place(GetOrAdd<RectTransform>(go), new Vector2(0, 1), topLeft, new Vector2(360, height));
            var bg = GetOrAdd<Image>(go);
            bg.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            bg.raycastTarget = false;

            var fillGo = FindOrCreate("Fill", go.transform);
            var fill = GetOrAdd<RectTransform>(fillGo);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImage = GetOrAdd<Image>(fillGo);
            fillImage.color = color;
            fillImage.raycastTarget = false;

            var label = EnsureText(go.transform, "Label", new Vector2(0, 1), Vector2.zero, new Vector2(360, height), fontSize, font, TextAlignmentOptions.Center);
            label.transform.SetAsLastSibling(); // 막대 위에 그린다
            return (fill, label);
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// HUD 폰트: 원본 ttf에서 TMP 동적 SDF 에셋을 만든다 (없을 때만). 한글은 쓰는 글자만 실행 중에 아틀라스에 추가되고,
        /// 빌드 때 동적 데이터를 비워 git에 글리프 변경이 쌓이지 않게 한다. 원본이 없으면 null (기본 폰트 유지).
        /// </summary>
        private static TMP_FontAsset EnsureHudFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HudFontAssetPath);
            if (existing != null) return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>(HudFontSourcePath);
            if (source == null)
            {
                Debug.LogWarning("[MainSceneSetup] HUD 폰트 원본 없음 — 기본 TMP 폰트 유지: " + HudFontSourcePath);
                return null;
            }

            // 샘플링 48pt(Galmuri11 원래 격자 12px의 4배) · 여백 6 · 1024 아틀라스, 다 차면 아틀라스 추가
            var asset = TMP_FontAsset.CreateFontAsset(source, 48, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = System.IO.Path.GetFileNameWithoutExtension(HudFontAssetPath);
            AssetDatabase.CreateAsset(asset, HudFontAssetPath);
            var so = new SerializedObject(asset); // clearDynamicDataOnBuild는 공개 속성이 없어 직렬화 필드로
            var clearOnBuild = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuild != null) clearOnBuild.boolValue = true;
            else Debug.LogWarning("[MainSceneSetup] m_ClearDynamicDataOnBuild 필드 없음 — 빌드 후 폰트 에셋 변경을 확인할 것");
            so.ApplyModifiedPropertiesWithoutUndo();

            // 아틀라스 텍스처 · 재질을 하위 에셋으로 저장해야 씬에서 참조가 유지된다
            var atlas = asset.atlasTextures[0];
            atlas.name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            // 영문 · 숫자 · 기호는 미리 넣어 둔다 (HUD 첫 표시에서 아틀라스 갱신이 몰리지 않게)
            asset.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~", out _);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, float fontSize,
            TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            var go = FindOrCreate(name, parent);
            var text = GetOrAdd<TextMeshProUGUI>(go);
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            Place((RectTransform)go.transform, anchor, position, size);
            return text;
        }

        /// <summary>없으면 만들고 필드 값을 넣는다. 이미 있으면 그대로 둔다 (사용자가 고친 수치를 덮어쓰지 않음).</summary>
        private static T EnsureAsset<T>(string path, params (string field, object value)[] initialValues) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = LoadOrCreate<T>(path);
            var so = new SerializedObject(asset);
            foreach (var (field, value) in initialValues)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"[MainSceneSetup] {typeof(T).Name}.{field} 필드 없음");
                    continue;
                }
                if (value is int i) prop.intValue = i;
                else if (value is string s) prop.stringValue = s;
                else if (value is string[] list)
                {
                    prop.arraySize = list.Length;
                    for (int k = 0; k < list.Length; k++)
                        prop.GetArrayElementAtIndex(k).stringValue = list[k];
                }
                else Debug.LogError($"[MainSceneSetup] {field}: 지원하지 않는 값 형식 {value?.GetType().Name}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        /// 이미 있는 에셋에 새로 생긴 정수 필드를 채운다: 값이 0인 필드만 (사용자가 고친 값은 그대로 — 여러 번 실행해도 같음).
        /// </summary>
        /// <summary>문자열 필드가 비어 있을 때만 넣는다 (사용자가 고친 값은 그대로).</summary>
        private static void FillEmptyString(ScriptableObject asset, string field, string value)
        {
            var so = new SerializedObject(asset);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[MainSceneSetup] {asset.GetType().Name}.{field} 필드 없음");
                return;
            }
            if (!string.IsNullOrEmpty(prop.stringValue)) return;
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void FillZeroInts(ScriptableObject asset, params (string field, int value)[] values)
        {
            var so = new SerializedObject(asset);
            bool changed = false;
            foreach (var (field, value) in values)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"[MainSceneSetup] {asset.GetType().Name}.{field} 필드 없음");
                    continue;
                }
                if (prop.intValue != 0) continue;
                prop.intValue = value;
                changed = true;
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static ItemCatalogAsset.Entry Weapon(string id, string name, int maxDamage, int efficiency, float accuracy, int cost, int reach) =>
            new ItemCatalogAsset.Entry
            {
                Id = id, DisplayName = name, Use = ItemUse.EquipWeapon,
                MaxDamage = maxDamage, UpgradeEfficiency = efficiency, Accuracy = accuracy, AttackCostTicks = cost, Reach = reach, StrengthRequirement = 12,
            };

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        internal static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        private static GameObject FindOrCreate(string name, Transform parent)
        {
            if (parent != null)
            {
                var child = parent.Find(name);
                if (child != null) return child.gameObject;
            }
            else
            {
                foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (go.name == name) return go;
                }
            }

            var created = new GameObject(name);
            if (parent != null) created.transform.SetParent(parent, false);
            return created;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"[MainSceneSetup] {target.GetType().Name}.{field} 필드 없음");
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
