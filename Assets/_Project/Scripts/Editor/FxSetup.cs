using System;
using System.Collections.Generic;
using ProjectM.Presentation;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace ProjectM.Editor
{
    /// <summary>
    /// G-3 이펙트 구성 (메뉴 ProjectM/Setup/FX, 몇 번 실행해도 결과가 같다). Main Scene · Varco Models 메뉴 다음에 실행한다.
    /// 1. 가져온 팩(Assets/_Project/FX) 재질 중 Built-in 셰이더를 URP로 변환(Unity 변환기) + 소프트 파티클 끄기(깊이 텍스처 없이도 보이게).
    /// 2. 바닥 무늬(지형 5) · 조준 데칼(4) · 번개 선 텍스처를 코드로 그려 PNG + 타일로 만든다 (Art/FX/Generated, Data/Visuals/FxTiles).
    /// 3. FxCatalog(Data/Visuals/FxCatalog.asset)를 아래 표로 채운다 — 표 = ART_ASSETS.md 13번.
    /// 4. 씬: Fx 오브젝트(FxPresenter) · 지형 덮개 · 조준 타일 연결, GameRoot._fx, Bloom(약하게, 사용자 결정 2026-10-19) 볼륨 · 카메라 후처리.
    /// </summary>
    public static class FxSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string FxRoot = "Assets/_Project/FX";
        private const string Hovl = FxRoot + "/Hovl Studio/Magic effects pack/Prefabs/";
        private const string Eric = FxRoot + "/Eric VFX Studio/Free Game VFX/Prefab/";
        private const string GeneratedDir = "Assets/_Project/Art/FX/Generated";
        private const string TileDir = "Assets/_Project/Data/Visuals/FxTiles";
        private const string CatalogPath = "Assets/_Project/Data/Visuals/FxCatalog.asset";
        private const string LineMaterialPath = "Assets/_Project/Art/FX/Generated/LightningLine.mat";
        private const string PostProfilePath = "Assets/_Project/Settings/Rendering/PostFX.asset";
        private const int TextureSize = 64;

        // 속성 색 (ART_ASSETS.md 0-2)
        private static readonly Color Fire = new Color(1f, 0.5f, 0.1f);
        private static readonly Color Ice = new Color(0.35f, 0.85f, 1f);
        private static readonly Color Lightning = new Color(1f, 0.95f, 0.25f);
        private static readonly Color Poison = new Color(0.7f, 0.3f, 1f);
        private static readonly Color Water = new Color(0.25f, 0.5f, 1f);
        private static readonly Color Dirty = new Color(0.4f, 0.6f, 0.15f);
        private static readonly Color Steam = new Color(0.92f, 0.94f, 0.97f);
        private static readonly Color Steel = new Color(0.85f, 0.88f, 0.95f); // 3-7a 무기 반짝임
        private static readonly Color PotionRed = new Color(0.95f, 0.25f, 0.25f); // 3-7b 힘 물약
        private static readonly Color ScrollGold = new Color(1f, 0.82f, 0.35f); // 3-7b 강화 주문서
        private static readonly Color Keep = Color.clear; // 원래 색

        private struct Row
        {
            public string Key, Prefab;
            public float Scale, Speed, Lifetime, Height;
            public Color Tint;

            public Row(string key, string prefab, float scale, Color tint, float speed = 1f, float lifetime = 0f, float height = 0f)
            {
                Key = key; Prefab = prefab; Scale = scale; Tint = tint; Speed = speed; Lifetime = lifetime; Height = height;
            }
        }

        // 크기는 팩 원본 대비 배율 (원본이 3~12 m라 1칸 = 1 m에 맞춰 줄임). 한 번 재생은 Lifetime 초 뒤 지움 · Speed로 1초 안에.
        // Height > 0 = 바닥 기준(칸 바닥에서 띄움), 0 = 몸 · 지팡이 높이 그대로.
        private static readonly Row[] Table =
        {
            // 지형 효과 (칸 루프) — 물 · 빙판은 바닥 무늬만
            new Row("terrain.FireZone", Hovl + "Sparks/Sparks flashing yellow", 1.4f, Fire),
            new Row("terrain.PoisonCloud", Hovl + "Smoke effects/Smoke ground", 0.16f, Poison, 0.8f),
            new Row("terrain.ContaminatedWater", Hovl + "Sparks/Sparks flashing green", 0.45f, Dirty),
            // 상태 이상 (유닛에 붙는 루프)
            new Row("status.Frozen", Hovl + "Environment/Crystal effect blue", 0.55f, Keep, 1f, 0f, 0.45f),
            new Row("status.Paralyze", Hovl + "Character auras/Lightning aura", 0.32f, Lightning, 1f, 0f, 0.4f),
            new Row("status.Burn", Hovl + "Sparks/Sparks flashing red", 0.5f, Fire, 1f, 0f, 0.3f),
            new Row("status.Poison", Hovl + "Character auras/Star aura", 0.3f, Poison, 1f, 0f, 0.4f),
            new Row("status.Slow", Hovl + "Sparks/Sparks white", 0.45f, Ice, 1f, 0f, 0.05f),
            new Row("status.Wet", Hovl + "Sparks/Sparks blue", 0.4f, Water, 1f, 0f, 0.5f),
            // 시전: 지팡이 끝 빛
            new Row("cast.Fire", Hovl + "Hits and explosions/Holy hit", 0.14f, Fire, 1.5f, 0.6f),
            new Row("cast.Ice", Hovl + "Hits and explosions/Holy hit", 0.14f, Ice, 1.5f, 0.6f),
            new Row("cast.Lightning", Hovl + "Hits and explosions/Holy hit", 0.14f, Lightning, 1.5f, 0.6f),
            new Row("cast.Poison", Hovl + "Hits and explosions/Holy hit", 0.14f, Poison, 1.5f, 0.6f),
            new Row("cast.Water", Hovl + "Hits and explosions/Holy hit", 0.14f, Water, 1.5f, 0.6f),
            // 투사체 (날아가는 동안 루프)
            new Row("projectile.Fire", Eric + "FX_Fireball", 0.09f, Keep),
            new Row("projectile.Ice", Hovl + "Sparks/Sparks white", 1.2f, Ice),
            new Row("projectile.Poison", Hovl + "Sparks/Sparks pink", 1.2f, Poison),
            new Row("projectile.Water", Hovl + "Sparks/Sparks blue", 1.2f, Water),
            // 맞는 칸 (단일 · 광선 끝)
            new Row("impact.Fire", Hovl + "Hits and explosions/Explosion", 0.16f, Keep, 1.5f, 1f),
            new Row("impact.Ice", Hovl + "Hits and explosions/Snow hit", 0.35f, Keep, 1.3f, 0.9f),
            new Row("impact.Lightning", Hovl + "Hits and explosions/Electro hit", 0.5f, Lightning, 1.3f, 0.8f),
            new Row("impact.Poison", Eric + "FX_Purple_Hit_02", 0.15f, Keep, 1f, 0.7f),
            new Row("impact.Water", Hovl + "Sparks/Sparks explode blue", 0.25f, Water, 1.3f, 0.9f),
            // 3×3 폭발 (중심 칸 바닥)
            new Row("burst.Fire", Hovl + "AoE effects/Ground AOE explosion", 0.33f, Keep, 1.6f, 1f, 0.02f),
            new Row("burst.Ice", Hovl + "AoE effects/Snow AOE", 0.33f, Keep, 2f, 1f, 0.02f),
            new Row("burst.Poison", Hovl + "AoE effects/Red energy explosion", 0.4f, Poison, 1.6f, 1f, 0.02f),
            new Row("burst.Water", Hovl + "AoE effects/Smoke AOE explosion", 0.33f, Water, 1.6f, 1f, 0.02f),
            new Row("burst.Lightning", Hovl + "Hits and explosions/Electro hit", 1f, Lightning, 1.3f, 0.8f, 0.02f),
            // 융합 주문 (맞는 효과만 바꿈)
            new Row("fusion.fusion_fire_water", Hovl + "AoE effects/Meteors AOE", 0.33f, Keep, 2f, 1f, 0.02f),
            new Row("fusion.fusion_water_poison", Hovl + "AoE effects/Smoke AOE explosion", 0.33f, Dirty, 1.6f, 1f, 0.02f),
            new Row("fusion.fusion_fire_lightning", Hovl + "Hits and explosions/Explosion", 0.22f, Keep, 1.5f, 1f),
            new Row("fusion.fusion_ice_water", Hovl + "Magic circles/Freeze circle", 0.38f, Keep, 2f, 1f, 0.02f),
            // 반응 (REACTIONS.md / ReactionCatalog ID)
            new Row("reaction.toxic_ignite", Eric + "FX_Purple_Hit_02", 0.2f, Keep, 1f, 0.7f),
            new Row("reaction.toxic_ignite_cloud", Eric + "FX_Purple_Hit_02", 0.2f, Keep, 1f, 0.7f),
            new Row("reaction.toxic_burst", Hovl + "AoE effects/Red energy explosion", 0.4f, Poison, 1.6f, 1f, 0.02f),
            new Row("reaction.gas_explosion", Hovl + "AoE effects/Ground AOE explosion", 0.33f, Keep, 1.6f, 1f, 0.02f),
            new Row("reaction.evaporate", Hovl + "Smoke effects/Smoke puff", 0.1f, Steam, 1.5f, 1f),
            new Row("reaction.evaporate_water", Hovl + "Smoke effects/Smoke puff", 0.12f, Steam, 1.5f, 1f, 0.02f),
            new Row("reaction.evaporate_contaminated", Hovl + "Smoke effects/Smoke puff", 0.12f, Steam, 1.5f, 1f, 0.02f),
            new Row("reaction.evaporate_fire_zone", Hovl + "Smoke effects/Smoke puff", 0.12f, Steam, 1.5f, 1f, 0.02f),
            new Row("reaction.extinguish", Hovl + "Smoke effects/Smoke puff", 0.09f, Steam, 1.5f, 1f),
            new Row("reaction.contaminate", Eric + "FX_Green_Hit", 0.18f, Keep, 1f, 0.7f, 0.02f),
            new Row("reaction.freeze", Hovl + "Magic circles/Freeze circle", 0.13f, Keep, 2f, 1f, 0.02f),
            new Row("reaction.freeze_water", Hovl + "Magic circles/Freeze circle", 0.13f, Keep, 2f, 1f, 0.02f),
            new Row("reaction.freeze_contaminated", Hovl + "Magic circles/Freeze circle", 0.13f, Keep, 2f, 1f, 0.02f),
            new Row("reaction.thaw", Hovl + "Sparks/Sparks explode blue", 0.15f, Ice, 1.3f, 0.9f),
            new Row("reaction.thaw_ice", Hovl + "Sparks/Sparks explode blue", 0.15f, Ice, 1.3f, 0.9f, 0.02f),
            new Row("reaction.shatter", Hovl + "Hits and explosions/Snow hit", 0.45f, Keep, 1.3f, 0.9f),
            new Row("reaction.shock", Hovl + "Hits and explosions/Electro hit", 0.5f, Lightning, 1.3f, 0.8f),
            new Row("reaction.shock_water", Hovl + "Hits and explosions/Electro hit", 0.5f, Lightning, 1.3f, 0.8f),
            new Row("reaction.shock_contaminated", Hovl + "Hits and explosions/Electro hit", 0.5f, Lightning, 1.3f, 0.8f),
            // 바닥 마법서 (3-5): 틴트 = 책 색, 프리팹 = 책 주위 반짝임 루프 (FloorItemPresenter)
            new Row("item.spellbook_fire", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Fire, 1f, 0f, 0.25f),
            new Row("item.spellbook_ice", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Ice, 1f, 0f, 0.25f),
            new Row("item.spellbook_lightning", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Lightning, 1f, 0f, 0.25f),
            new Row("item.spellbook_poison", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Poison, 1f, 0f, 0.25f),
            new Row("item.spellbook_water", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Water, 1f, 0f, 0.25f),
            // 3-7a 무기: 은빛 반짝임 (모델은 ItemVisualCatalog — 색은 반짝임에만)
            new Row("item.shortsword", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Steel, 1f, 0f, 0.25f),
            new Row("item.hand_axe", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Steel, 1f, 0f, 0.25f),
            new Row("item.spear", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Steel, 1f, 0f, 0.25f),
            new Row("item.quarterstaff", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Steel, 1f, 0f, 0.25f),
            new Row("item.dirk", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Steel, 1f, 0f, 0.25f),
            new Row("item.sickle", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Steel, 1f, 0f, 0.25f),
            // 3-7b 힘 물약 · 강화 주문서: 모델이 없어 책 상자(색 = 틴트) + 반짝임
            new Row("item.potion_strength", Hovl + "Sparks/Sparks flashing yellow", 0.35f, PotionRed, 1f, 0f, 0.25f),
            new Row("item.scroll_upgrade", Hovl + "Sparks/Sparks flashing yellow", 0.35f, ScrollGold, 1f, 0f, 0.25f),
            // 3-9 겉모습 (potion_<색> · scroll_<룬>): 모델은 ItemVisualCatalog, 반짝임은 앞부분 줄 하나 — 색으로 정체가 드러나지 않게 공통
            new Row("item.potion", Hovl + "Sparks/Sparks flashing yellow", 0.35f, Steel, 1f, 0f, 0.25f),
            new Row("item.scroll", Hovl + "Sparks/Sparks flashing yellow", 0.35f, ScrollGold, 1f, 0f, 0.25f),
        };

        // ── 2D 픽셀 그림 (P-3 ⓓ, 2026-10-26) — 키 → Art/Pixel/Fx/<그림>_0~3 · 화면 너비(칸) · 프레임/초 · 색 (a = 0이면 그대로) ──
        // 그림은 Varco 시트를 Tools/fx_cut.py로 자른 것 (Docs/PixelTest/fx_src/README.md). 3D 표에 없는 키(물 지형 · 번개 선)는 그림만 있는 줄로 만든다.
        private const string PixelFxDir = "Assets/_Project/Art/Pixel/Fx";

        private struct PixelRow
        {
            public string Key, Art;
            public float Cells, Fps;
            public Color Tint;

            public PixelRow(string key, string art, float cells, float fps, Color tint = default)
            {
                Key = key; Art = art; Cells = cells; Fps = fps; Tint = tint;
            }
        }

        private static readonly Color CastFire = new Color(1f, 0.75f, 0.5f);
        private static readonly Color CastIce = new Color(0.7f, 0.92f, 1f);
        private static readonly Color CastLightning = new Color(1f, 0.97f, 0.6f);
        private static readonly Color CastPoison = new Color(0.85f, 0.65f, 1f);
        private static readonly Color CastWater = new Color(0.6f, 0.75f, 1f);

        private static readonly PixelRow[] PixelTable =
        {
            new PixelRow("terrain.FireZone", "terrain_fire", 1f, 6f),
            new PixelRow("terrain.PoisonCloud", "terrain_poison_cloud", 1f, 5f),
            new PixelRow("terrain.Water", "terrain_water", 1f, 4f),
            new PixelRow("terrain.ContaminatedWater", "terrain_contaminated_water", 1f, 4f),

            new PixelRow("status.Frozen", "status_frozen", 1f, 4f),
            new PixelRow("status.Paralyze", "status_paralyze", 1f, 8f),
            new PixelRow("status.Burn", "status_burn", 1f, 8f),
            new PixelRow("status.Poison", "status_poison", 1f, 6f),
            new PixelRow("status.Slow", "status_slow", 0.9f, 6f),
            new PixelRow("status.Wet", "status_wet", 1f, 6f),

            new PixelRow("cast.Fire", "cast", 0.8f, 14f, CastFire),
            new PixelRow("cast.Ice", "cast", 0.8f, 14f, CastIce),
            new PixelRow("cast.Lightning", "cast", 0.8f, 14f, CastLightning),
            new PixelRow("cast.Poison", "cast", 0.8f, 14f, CastPoison),
            new PixelRow("cast.Water", "cast", 0.8f, 14f, CastWater),

            new PixelRow("projectile.Fire", "projectile_fire", 1.2f, 12f),
            new PixelRow("projectile.Ice", "projectile_ice", 1.2f, 12f),
            new PixelRow("projectile.Poison", "projectile_poison", 1.2f, 12f),
            new PixelRow("projectile.Water", "projectile_water", 1.2f, 12f),

            new PixelRow("impact.Fire", "impact_fire", 1.5f, 12f),
            new PixelRow("impact.Ice", "impact_ice", 1.5f, 12f),
            new PixelRow("impact.Lightning", "impact_lightning", 1.5f, 12f),
            new PixelRow("impact.Poison", "impact_poison", 1.5f, 12f),
            new PixelRow("impact.Water", "impact_water", 1.5f, 12f),

            new PixelRow("burst.Fire", "burst_fire", 3f, 10f),
            new PixelRow("burst.Ice", "burst_ice", 3f, 10f),
            new PixelRow("burst.Poison", "burst_poison", 3f, 10f),
            new PixelRow("burst.Water", "burst_water", 3f, 10f),
            new PixelRow("burst.Lightning", "burst_lightning", 3f, 10f),

            new PixelRow("fusion.fusion_fire_water", "lava_eruption", 3f, 9f),
            new PixelRow("fusion.fusion_water_poison", "contaminated_wave", 3f, 9f),
            new PixelRow("fusion.fusion_fire_lightning", "big_explosion", 1.5f, 10f),
            new PixelRow("fusion.fusion_ice_water", "frost_wave", 3f, 9f),

            new PixelRow("reaction.toxic_ignite", "toxic_ignite", 1.5f, 10f),
            new PixelRow("reaction.toxic_ignite_cloud", "toxic_ignite", 1.5f, 10f),
            new PixelRow("reaction.toxic_burst", "toxic_burst", 3f, 10f),
            new PixelRow("reaction.gas_explosion", "gas_explosion", 3f, 10f),
            new PixelRow("reaction.evaporate", "steam", 1.5f, 8f),
            new PixelRow("reaction.evaporate_water", "steam", 1.5f, 8f),
            new PixelRow("reaction.evaporate_contaminated", "steam", 1.5f, 8f),
            new PixelRow("reaction.evaporate_fire_zone", "steam", 1.5f, 8f),
            new PixelRow("reaction.extinguish", "steam", 1.5f, 8f),
            new PixelRow("reaction.contaminate", "contaminate", 1.5f, 10f),
            new PixelRow("reaction.freeze", "freeze_circle", 1.5f, 10f),
            new PixelRow("reaction.freeze_water", "freeze_circle", 1.5f, 10f),
            new PixelRow("reaction.freeze_contaminated", "freeze_circle", 1.5f, 10f),
            new PixelRow("reaction.thaw", "thaw", 1.5f, 10f),
            new PixelRow("reaction.thaw_ice", "thaw", 1.5f, 10f),
            new PixelRow("reaction.shatter", "shatter", 1.5f, 12f),
            new PixelRow("reaction.shock", "shock", 1.5f, 12f),
            new PixelRow("reaction.shock_water", "shock", 1.5f, 12f),
            new PixelRow("reaction.shock_contaminated", "shock", 1.5f, 12f),

            new PixelRow("line.Lightning", "line_lightning", 1f, 16f),
            new PixelRow("line.Fire", "line_lightning", 1f, 16f, new Color(1f, 0.6f, 0.3f)),
        };

        /// <summary>그림 4장을 픽셀 스프라이트로 가져와 순서대로 (없으면 빈 배열 + 경고).</summary>
        private static Sprite[] LoadPixelFrames(string art)
        {
            var frames = new List<Sprite>();
            for (int i = 0; ; i++)
            {
                string path = $"{PixelFxDir}/{art}_{i}.png";
                if (AssetDatabase.LoadMainAssetAtPath(path) == null) break;
                PixelTestSetup.ImportPixelSprite(path, SpriteAlignment.Center);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) frames.Add(sprite);
            }
            if (frames.Count == 0) Debug.LogWarning($"[FxSetup] 2D 그림 없음: {PixelFxDir}/{art}_0.png");
            return frames.ToArray();
        }

        [MenuItem("ProjectM/Setup/FX")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[FxSetup] 플레이 중에는 실행하지 않는다.");
                return;
            }

            // 씬을 먼저 연다 (OpenScene이 쓰지 않는 에셋을 내리므로 에셋은 그 뒤에 불러온다)
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int converted = ConvertMaterials();
            var tiles = EnsurePatternTiles();
            var lineMaterial = EnsureLineMaterial();
            var catalog = EnsureCatalog(out int missing);

            var entities = UnityEngine.Object.FindAnyObjectByType<EntityPresenter>();
            var fxGo = FindOrCreateRoot("Fx");
            var fx = fxGo.GetComponent<FxPresenter>();
            if (fx == null) fx = fxGo.AddComponent<FxPresenter>();
            SetRefs(fx, ("_catalog", catalog), ("_entities", entities), ("_lineMaterial", lineMaterial));
            fx.ConfigurePixel2D(MainSceneSetup.Pixel2D, MainSceneSetup.EnsureSpriteUnlitMaterial()); // P-3 ⓓ: 2D면 그림 스프라이트
            EditorUtility.SetDirty(fx);

            var overlay = UnityEngine.Object.FindAnyObjectByType<TerrainOverlayPresenter>();
            if (overlay != null)
                SetRefs(overlay, ("_fireZoneTile", tiles["fx_scorch"]), ("_poisonCloudTile", tiles["fx_haze"]), ("_waterTile", tiles["fx_water"]),
                    ("_contaminatedWaterTile", tiles["fx_dirty_water"]), ("_icePatchTile", tiles["fx_ice"]));
            else Debug.LogWarning("[FxSetup] TerrainOverlayPresenter 없음 — 먼저 ProjectM/Setup/Main Scene");

            var aim = UnityEngine.Object.FindAnyObjectByType<AimPreviewPresenter>();
            if (aim != null)
                SetRefs(aim, ("_pathTile", tiles["aim_path"]), ("_affectedTile", tiles["aim_affected"]), ("_cursorTile", tiles["aim_cursor"]), ("_blockedTile", tiles["aim_blocked"]));

            var floorItems = UnityEngine.Object.FindAnyObjectByType<FloorItemPresenter>(); // 3-5: 바닥 마법서 색 · 반짝임
            if (floorItems != null) SetRefs(floorItems, ("_catalog", catalog));

            var root = UnityEngine.Object.FindAnyObjectByType<GameRoot>();
            if (root != null) SetRefs(root, ("_fx", fx));
            else Debug.LogWarning("[FxSetup] GameRoot 없음 — 먼저 ProjectM/Setup/Main Scene");

            EnsureBloom();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FxSetup] 완료: 재질 변환 {converted}개 · 효과 표 {catalog.Count}줄" + (missing > 0 ? $" · 프리팹 없음 {missing}줄 (경고 확인)" : ""));
        }

        // ── 1. 재질 ──

        /// <summary>Built-in 파티클 · Standard 셰이더 → URP (Unity 변환기). 이미 URP면 건너뛴다. 소프트 파티클은 모두 끈다.</summary>
        private static int ConvertMaterials()
        {
            int converted = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { FxRoot }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.shader == null) continue;
                string shader = material.shader.name;
                MaterialUpgrader upgrader = null;
                if (shader == "Particles/Standard Unlit" || shader == "Particles/Standard Surface") upgrader = new ParticleUpgrader(shader);
                else if (shader == "Standard") upgrader = new StandardUpgrader(shader);
                bool changed = false;
                if (upgrader != null)
                {
                    MaterialUpgrader.Upgrade(material, upgrader, MaterialUpgrader.UpgradeFlags.None);
                    converted++;
                    changed = true;
                }
                // 소프트 파티클은 카메라 깊이 텍스처가 필요하다 — 지금 렌더러는 꺼져 있어 연기 · 불꽃이 통째로 투명해진다 (G-3에서 발견)
                if (material.IsKeywordEnabled("_SOFTPARTICLES_ON"))
                {
                    material.DisableKeyword("_SOFTPARTICLES_ON");
                    if (material.HasProperty("_SoftParticlesEnabled")) material.SetFloat("_SoftParticlesEnabled", 0f);
                    changed = true;
                }
                if (changed) EditorUtility.SetDirty(material);
            }
            return converted;
        }

        // ── 2. 바닥 무늬 · 데칼 ──

        private static Dictionary<string, Tile> EnsurePatternTiles()
        {
            EnsureFolder(GeneratedDir);
            EnsureFolder(TileDir);
            var tiles = new Dictionary<string, Tile>();
            void Make(string name, Func<float, float, float> alpha) => tiles[name] = EnsureTile(name, alpha);

            // 지형 바닥 무늬 (흰색 + 알파 — 색은 TerrainOverlayPresenter 색)
            Make("fx_scorch", (u, v) => Edge(u, v, 0.14f) * Mathf.Clamp01(0.3f + 0.55f * Noise(u, v, 5f, 1) + (Hash(u * 64f, v * 64f, 7) > 0.94f ? 0.6f : 0f)));
            Make("fx_haze", (u, v) => Edge(u, v, 0.2f) * Mathf.Clamp01(0.2f + 0.6f * Noise(u, v, 3f, 2)));
            Make("fx_water", (u, v) => Mathf.Clamp01(0.45f + 0.3f * Ripple(u, v)));
            Make("fx_dirty_water", (u, v) => Mathf.Clamp01(0.5f + 0.2f * Ripple(u, v) + (Noise(u, v, 4f, 3) > 0.62f ? 0.3f : 0f)));
            Make("fx_ice", (u, v) => Mathf.Clamp01(0.5f + 0.15f * Noise(u, v, 2f, 4) + Crack(u, v) + 0.2f * Mathf.Clamp01(1f - Mathf.Abs(u - v) * 6f)));
            // 조준 데칼
            Make("aim_path", (u, v) => Mathf.Clamp01(1f - (Dist(u, v) - 0.12f) / 0.06f));
            Make("aim_affected", (u, v) => Border(u, v, 0.09f) > 0f ? 1f : 0.28f);
            Make("aim_cursor", (u, v) => Corner(u, v));
            Make("aim_blocked", (u, v) => Mathf.Max(Cross(u, v), Border(u, v, 0.05f)));
            return tiles;
        }

        private static float Edge(float u, float v, float width) => Mathf.SmoothStep(0f, 1f, Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) / width);
        private static float Dist(float u, float v) => Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
        private static float Ripple(float u, float v) => Mathf.Sin((v * 3f + 0.12f * Mathf.Sin(u * Mathf.PI * 4f)) * Mathf.PI * 2f); // 칸 경계에서 이어지게 정수 주기
        private static float Border(float u, float v, float t) => (u < t || u > 1f - t || v < t || v > 1f - t) ? 1f : 0f;

        private static float Corner(float u, float v)
        {
            const float t = 0.09f, len = 0.32f;
            bool nearX = u < len || u > 1f - len, nearY = v < len || v > 1f - len;
            bool edgeX = u < t || u > 1f - t, edgeY = v < t || v > 1f - t;
            return (edgeX && nearY) || (edgeY && nearX) ? 1f : 0f;
        }

        private static float Cross(float u, float v)
        {
            const float t = 0.08f;
            bool inner = u > 0.18f && u < 0.82f && v > 0.18f && v < 0.82f;
            return inner && (Mathf.Abs(u - v) < t || Mathf.Abs(u + v - 1f) < t) ? 1f : 0f;
        }

        private static float Crack(float u, float v)
        {
            float a = Mathf.Abs(Mathf.Sin(u * 7.3f + v * 2.9f + Noise(u, v, 3f, 5) * 3f));
            float b = Mathf.Abs(Mathf.Sin(v * 6.1f - u * 3.7f + Noise(u, v, 3f, 6) * 3f));
            return (a < 0.05f || b < 0.05f) ? 0.45f : 0f;
        }

        /// <summary>정해진 값의 해시 (난수 아님 — 같은 입력 = 같은 무늬, 다시 실행해도 같은 PNG).</summary>
        private static float Hash(float x, float y, int seed)
        {
            float h = Mathf.Sin(Mathf.Floor(x) * 127.1f + Mathf.Floor(y) * 311.7f + seed * 74.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        /// <summary>칸 경계에서 이어지는 값 노이즈 (주기 = 1칸).</summary>
        private static float Noise(float u, float v, float freq, int seed)
        {
            float x = u * freq, y = v * freq;
            int n = Mathf.RoundToInt(freq);
            float fx = x - Mathf.Floor(x), fy = y - Mathf.Floor(y);
            int x0 = Mathf.FloorToInt(x) % n, y0 = Mathf.FloorToInt(y) % n;
            int x1 = (x0 + 1) % n, y1 = (y0 + 1) % n;
            float sx = fx * fx * (3f - 2f * fx), sy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0, seed), b = Hash(x1, y0, seed), c = Hash(x0, y1, seed), d = Hash(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        private static Tile EnsureTile(string name, Func<float, float, float> alpha)
        {
            string texturePath = $"{GeneratedDir}/{name}.png";
            WriteTexture(texturePath, alpha);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            string tilePath = $"{TileDir}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.flags = TileFlags.None;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>흰색 + 알파 PNG를 쓰고 스프라이트(1칸 = 1 유닛)로 가져온다. 내용이 같으면 파일을 다시 쓰지 않는다.</summary>
        private static void WriteTexture(string path, Func<float, float, float> alpha)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
                for (int x = 0; x < TextureSize; x++)
                {
                    float a = Mathf.Clamp01(alpha((x + 0.5f) / TextureSize, (y + 0.5f) / TextureSize));
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            texture.SetPixels32(pixels);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            string full = System.IO.Path.GetFullPath(path);
            bool same = System.IO.File.Exists(full) && System.IO.File.ReadAllBytes(full).AsSpan().SequenceEqual(png);
            if (!same)
            {
                System.IO.File.WriteAllBytes(full, png);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            bool dirty = importer.textureType != TextureImporterType.Sprite || !Mathf.Approximately(importer.spritePixelsPerUnit, TextureSize)
                || importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp || importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (!dirty) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TextureSize;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        /// <summary>번개 선 재질: URP Particles/Unlit 가산 + 가운데가 밝은 띠 텍스처. 색은 LineRenderer 정점 색.</summary>
        private static Material EnsureLineMaterial()
        {
            string texturePath = $"{GeneratedDir}/fx_line.png";
            WriteTexture(texturePath, (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v - 0.5f) * 2f), 1.6f));
            var material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, LineMaterialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", new Color(2f, 2f, 2f, 1f)); // Bloom에 걸리게 밝게
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f); // 가산
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ── 3. 효과 표 ──

        private static FxCatalog EnsureCatalog(out int missing)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FxCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FxCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            missing = 0;
            var keys = new HashSet<string>();
            var pixel = new Dictionary<string, FxCatalog.Entry>();
            var frameCache = new Dictionary<string, Sprite[]>();
            foreach (var p in PixelTable)
            {
                if (!frameCache.TryGetValue(p.Art, out var frames)) frameCache[p.Art] = frames = LoadPixelFrames(p.Art);
                pixel[p.Key] = new FxCatalog.Entry { Key = p.Key, Frames = frames, Fps = p.Fps, PixelCells = p.Cells, PixelTint = p.Tint, Scale = 1f, Speed = 1f };
            }

            foreach (var row in Table)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(row.Prefab + ".prefab");
                if (prefab == null)
                {
                    Debug.LogWarning($"[FxSetup] 프리팹 없음 — {row.Key}: {row.Prefab}");
                    missing++;
                }
                keys.Add(row.Key);
                var entry = pixel.TryGetValue(row.Key, out var px) ? px : new FxCatalog.Entry();
                pixel.Remove(row.Key);
                entry.Key = row.Key; entry.Prefab = prefab; entry.Scale = row.Scale; entry.Tint = row.Tint;
                entry.Speed = row.Speed; entry.Lifetime = row.Lifetime; entry.Height = row.Height;
                catalog.Set(entry);
            }
            var lineTints = new Dictionary<string, Color> { { "line.Lightning", new Color(1f, 0.92f, 0.35f, 1f) }, { "line.Fire", new Color(1f, 0.55f, 0.2f, 1f) } };
            foreach (var pair in pixel) // 3D 표에 없는 키: 그림만 (번개 선은 3D 선 색도)
            {
                var entry = pair.Value;
                if (lineTints.TryGetValue(pair.Key, out var tint)) entry.Tint = tint;
                catalog.Set(entry);
                keys.Add(pair.Key);
            }
            catalog.RemoveExcept(keys);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ── 4. Bloom (약하게) ──

        private static void EnsureBloom()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PostProfilePath);
            }
            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
                bloom.name = "Bloom";
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }
            bloom.active = true;
            bloom.threshold.Override(1.1f); // 밝은 이펙트(HDR 색)만 번지게
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.6f);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profile);

            var volumeGo = FindOrCreateRoot("PostFX");
            var volume = volumeGo.GetComponent<Volume>();
            if (volume == null) volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;

            var cameraGo = GameObject.FindWithTag("MainCamera");
            if (cameraGo == null) return;
            var data = cameraGo.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cameraGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(data);
        }

        // ── 도우미 ──

        private static GameObject FindOrCreateRoot(string name)
        {
            foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == name) return go;
            return new GameObject(name);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        private static void SetRefs(UnityEngine.Object target, params (string field, UnityEngine.Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"[FxSetup] {target.GetType().Name}.{field} 필드 없음");
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
