using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Input;
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
        private const string SpawnConfigPath = "Assets/_Project/Data/Units/SpawnConfig.asset";
        private const string MagicConfigPath = "Assets/_Project/Data/Magic/MagicConfig.asset";
        private const string SpellCatalogPath = "Assets/_Project/Data/Spells/SpellCatalog.asset";
        private const string StatusCatalogPath = "Assets/_Project/Data/Statuses/StatusCatalog.asset";

        /// <summary>플레이어 시작 주문 (BALANCE 4번). 주문 내용은 2-2 주문 데이터에서 같은 ID로 정의한다.</summary>
        private static readonly string[] PlayerStartingSpells = { "fire_basic", "ice_basic", "lightning_basic", "poison_basic" };

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
            var spawnConfig = EnsureAsset<SpawnConfigAsset>(SpawnConfigPath,
                ("_enemyCount", 5), ("_enemyMinDistanceFromStart", 8));
            // 마나 · 장착 칸 — 없을 때만 BALANCE 4번 값으로 만든다
            var magicConfig = EnsureAsset<MagicConfigAsset>(MagicConfigPath,
                ("_manaRegenIntervalTicks", 3), ("_manaRegenAmount", 1), ("_spellSlotCount", 4));
            var spellCatalog = EnsureSpellCatalog();
            var statusCatalog = EnsureStatusCatalog();

            var gridGo = FindOrCreate("Grid", null);
            var grid = GetOrAdd<Grid>(gridGo);

            var terrainGo = FindOrCreate("Terrain", gridGo.transform);
            var tilemap = GetOrAdd<Tilemap>(terrainGo);
            GetOrAdd<TilemapRenderer>(terrainGo).sortingOrder = 0;

            var entitiesGo = FindOrCreate("Entities", null);
            var entityPresenter = GetOrAdd<EntityPresenter>(entitiesGo);
            SetRefs(entityPresenter, ("_grid", grid), ("_catalog", catalog));

            var tilemapPresenter = GetOrAdd<TilemapPresenter>(terrainGo);
            SetRefs(tilemapPresenter, ("_tilemap", tilemap), ("_visuals", tileVisuals));

            // 시야 안개: 엔티티(정렬 10) 위
            var fogGo = FindOrCreate("Fog", gridGo.transform);
            var fogTilemap = GetOrAdd<Tilemap>(fogGo);
            GetOrAdd<TilemapRenderer>(fogGo).sortingOrder = 20;
            var fogPresenter = GetOrAdd<FogPresenter>(fogGo);
            SetRefs(fogPresenter, ("_tilemap", fogTilemap), ("_fogTile", EnsureFogTile()));

            // 주문 조준 미리보기: 안개(20) 위
            var aimGo = FindOrCreate("Aim", gridGo.transform);
            var aimTilemap = GetOrAdd<Tilemap>(aimGo);
            GetOrAdd<TilemapRenderer>(aimGo).sortingOrder = 30;
            var aimPreview = GetOrAdd<AimPreviewPresenter>(aimGo);
            SetRefs(aimPreview, ("_tilemap", aimTilemap), ("_tile", EnsureFogTile()));

            var cameraGo = GameObject.FindWithTag("MainCamera");
            CameraController cameraController = null;
            if (cameraGo != null) cameraController = GetOrAdd<CameraController>(cameraGo);
            else Debug.LogWarning("[MainSceneSetup] MainCamera 태그 카메라 없음");

            var hud = EnsureHud();

            var rootGo = FindOrCreate("GameRoot", null);
            var input = GetOrAdd<PlayerInputController>(rootGo);
            SetRefs(input, ("_camera", cameraGo != null ? cameraGo.GetComponent<Camera>() : null), ("_grid", grid), ("_aimPreview", aimPreview));

            var root = GetOrAdd<GameRoot>(rootGo);
            SetRefs(root,
                ("_config", config),
                ("_dungeonConfig", dungeonConfig),
                ("_playerDef", playerDef),
                ("_enemyDef", enemyDef),
                ("_spawnConfig", spawnConfig),
                ("_magicConfig", magicConfig),
                ("_spellCatalog", spellCatalog),
                ("_statusCatalog", statusCatalog),
                ("_tilemapPresenter", tilemapPresenter),
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
            EditorUtility.SetDirty(set);
            return set;
        }

        private static EntityVisualCatalog EnsureEntityCatalog()
        {
            var catalog = LoadOrCreate<EntityVisualCatalog>(EntityCatalogPath);
            catalog.Set(PlayerVisualId, AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath), Color.white);
            // 그레이박스: 흰 원(player.png와 같은 모양)을 빨갛게
            catalog.Set(EnemyVisualId, AssetDatabase.LoadAssetAtPath<Sprite>(UnitCircleSpritePath), new Color(0.9f, 0.25f, 0.2f));
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
            // BALANCE 6번: 주문 → 상태 이상 · 확률
            changed |= catalog.SetStatusIfNone("fire_basic", StatusType.Burn, 100);
            changed |= catalog.SetStatusIfNone("ice_basic", StatusType.Slow, 100);
            changed |= catalog.SetStatusIfNone("lightning_basic", StatusType.Paralyze, 30);
            changed |= catalog.SetStatusIfNone("poison_basic", StatusType.Poison, 100);
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

        /// <summary>HUD 캔버스: 왼쪽 위 체력 줄 + 그 아래 전투 로그.</summary>
        private static HudPresenter EnsureHud()
        {
            var canvasGo = FindOrCreate("HUD", null);
            var canvas = GetOrAdd<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetOrAdd<CanvasScaler>(canvasGo);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 상태 2줄 (체력 · 마나 / 주문 장착 칸)
            var status = EnsureText(canvasGo.transform, "Status", new Vector2(24, -16), new Vector2(1400, 100), 36);
            var log = EnsureText(canvasGo.transform, "Log", new Vector2(24, -120), new Vector2(1400, 260), 26);

            var hud = GetOrAdd<HudPresenter>(canvasGo);
            SetRefs(hud, ("_statusText", status), ("_logText", log));
            return hud;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, Vector2 topLeft, Vector2 size, float fontSize)
        {
            var go = FindOrCreate(name, parent);
            var text = GetOrAdd<TextMeshProUGUI>(go);
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;
            text.raycastTarget = false;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
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

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folder)
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
