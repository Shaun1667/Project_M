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
        private const string TerrainCatalogPath = "Assets/_Project/Data/Terrains/TerrainCatalog.asset";

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
            var spawnConfig = EnsureAsset<SpawnConfigAsset>(SpawnConfigPath,
                ("_enemyCount", 5), ("_enemyMinDistanceFromStart", 8));
            // 마나 · 장착 칸 — 없을 때만 BALANCE 4번 값으로 만든다
            var magicConfig = EnsureAsset<MagicConfigAsset>(MagicConfigPath,
                ("_manaRegenIntervalTicks", 3), ("_manaRegenAmount", 1), ("_spellSlotCount", 4));
            var spellCatalog = EnsureSpellCatalog();
            var statusCatalog = EnsureStatusCatalog();
            var terrainCatalog = EnsureTerrainCatalog();

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

            // 지형 효과 오버레이: 바닥(0) 위 · 유닛(10) 아래
            var terrainFxGo = FindOrCreate("TerrainFx", gridGo.transform);
            var terrainFxTilemap = GetOrAdd<Tilemap>(terrainFxGo);
            GetOrAdd<TilemapRenderer>(terrainFxGo).sortingOrder = 5;
            var terrainOverlay = GetOrAdd<TerrainOverlayPresenter>(terrainFxGo);
            SetRefs(terrainOverlay, ("_tilemap", terrainFxTilemap), ("_tile", EnsureFogTile()));

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
                ("_terrainCatalog", terrainCatalog),
                ("_terrainOverlay", terrainOverlay),
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
            // BALANCE 7번: 주문 → 지형 효과
            changed |= catalog.SetTerrainIfNone("fire_basic", TerrainEffectType.FireZone);
            changed |= catalog.SetTerrainIfNone("poison_basic", TerrainEffectType.PoisonCloud);
            // 2-5: HUD · 로그 표시 이름 (비어 있을 때만)
            changed |= catalog.SetDisplayNameIfEmpty("fire_basic", "불꽃 화살");
            changed |= catalog.SetDisplayNameIfEmpty("ice_basic", "얼음 화살");
            changed |= catalog.SetDisplayNameIfEmpty("lightning_basic", "번개 줄기");
            changed |= catalog.SetDisplayNameIfEmpty("poison_basic", "독 폭발");
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

        /// <summary>지형 효과 규칙: 없는 종류만 BALANCE 7번 값으로 추가한다.</summary>
        private static TerrainCatalogAsset EnsureTerrainCatalog()
        {
            var catalog = LoadOrCreate<TerrainCatalogAsset>(TerrainCatalogPath);
            bool changed = false;
            changed |= catalog.AddIfMissing(new TerrainCatalogAsset.Entry
                { Type = TerrainEffectType.FireZone, DurationTicks = 4, AppliesStatus = StatusType.Burn });
            changed |= catalog.AddIfMissing(new TerrainCatalogAsset.Entry
                { Type = TerrainEffectType.PoisonCloud, DurationTicks = 4, AppliesStatus = StatusType.Poison });
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
        /// HUD 캔버스 (2-5, 기준 1920×1080): 왼쪽 위 HP · MP 막대, 그 오른쪽 상태 아이콘 줄, 아래로 주문 칸 · 안내 줄, 왼쪽 아래 전투 로그.
        /// </summary>
        private static HudPresenter EnsureHud()
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

            var iconsGo = FindOrCreate("StatusIcons", canvasGo.transform);
            var icons = GetOrAdd<RectTransform>(iconsGo);
            Place(icons, topLeft, new Vector2(404, -16), new Vector2(1000, 30));
            var row = GetOrAdd<HorizontalLayoutGroup>(iconsGo);
            row.spacing = 8;
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var slots = EnsureText(canvasGo.transform, "Slots", topLeft, new Vector2(24, -94), new Vector2(1600, 40), 28, font, TextAlignmentOptions.TopLeft);
            var hint = EnsureText(canvasGo.transform, "Hint", topLeft, new Vector2(24, -134), new Vector2(1600, 40), 26, font, TextAlignmentOptions.TopLeft);
            // 로그 8줄: 아래쪽 기준으로 쌓인다 (새 줄이 맨 아래)
            var log = EnsureText(canvasGo.transform, "Log", bottomLeft, new Vector2(24, 24), new Vector2(1100, 300), 26, font, TextAlignmentOptions.BottomLeft);

            var hud = GetOrAdd<HudPresenter>(canvasGo);
            SetRefs(hud,
                ("_hpFill", hpFill), ("_hpLabel", hpLabel),
                ("_mpFill", mpFill), ("_mpLabel", mpLabel),
                ("_statusIcons", icons), ("_slotsText", slots), ("_hintText", hint), ("_logText", log));
            // 로그 줄 수 (결정 2026-10-16: 8줄). 2-4까지 씬에 저장된 6을 덮어쓴다
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_logLines").intValue = 8;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        /// <summary>막대 하나: 어두운 바탕 + 왼쪽부터 차는 색 칸(Fill) + 가운데 숫자(Label). 360×30.</summary>
        private static (RectTransform fill, TextMeshProUGUI label) EnsureBar(Transform parent, string name, Vector2 topLeft, Color color, TMP_FontAsset font)
        {
            var go = FindOrCreate(name, parent);
            Place(GetOrAdd<RectTransform>(go), new Vector2(0, 1), topLeft, new Vector2(360, 30));
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

            var label = EnsureText(go.transform, "Label", new Vector2(0, 1), Vector2.zero, new Vector2(360, 30), 24, font, TextAlignmentOptions.Center);
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
