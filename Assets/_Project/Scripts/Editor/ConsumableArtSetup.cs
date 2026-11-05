using System.IO;
using ProjectM.Presentation;
using UnityEditor;
using UnityEngine;

namespace ProjectM.Editor
{
    /// <summary>
    /// 3-9a 물약 · 주문서 아트 (결정 2026-10-20, A안 — Docs/소모품_에셋.md): 모델 두 개로 24가지 모습을 만든다.
    /// - 물약: Prop_Potion(jade) 텍스처에서 초록 액체만 골라 12색으로 바꾼 텍스처 · 재질 → Prefabs/Varco/Potion_&lt;색&gt;
    /// - 주문서: Prop_ScrollBlank(빈 종이) + 종이 위에 눕힌 금색 룬 판(룬 글자는 이 스크립트가 획으로 그림) → Prefabs/Varco/Scroll_&lt;룬&gt;
    /// ItemVisualCatalog 키 = "potion_&lt;색&gt;" · "scroll_&lt;룬&gt;" (판마다 아이템 → 모습 배정은 3-9b 식별). 여러 번 실행해도 결과가 같다.
    /// 메뉴 순서: Main Scene → Varco Models → Consumables → FX.
    /// </summary>
    public static class ConsumableArtSetup
    {
        private const string ModelDir = "Assets/_Project/Art/Varco3D/";
        private const string TextureDir = ModelDir + "Textures/";
        private const string MaterialDir = ModelDir + "Materials/";
        private const string PrefabDir = "Assets/_Project/Prefabs/Varco";
        private const string ItemVisualCatalogPath = "Assets/_Project/Data/Visuals/ItemVisualCatalog.asset";
        private const string PotionModel = "Prop_Potion";
        private const string ScrollModel = "Prop_ScrollBlank";
        /// <summary>바닥에서 보일 크기 (m) — 물약 키 · 주문서 긴 쪽.</summary>
        private const float PotionHeight = 0.32f;
        private const float ScrollLength = 0.42f;
        /// <summary>룬 판 크기 (종이 짧은 쪽에 대한 비율).</summary>
        private const float RuneSizeRatio = 0.45f;

        /// <summary>물약 12색 (SPD 순서와 같은 이름) — 액체 중간 밝기의 색. jade는 원본 그대로.</summary>
        public static readonly (string Id, Color Color)[] PotionColors =
        {
            ("crimson", new Color(0.80f, 0.08f, 0.15f)),
            ("amber", new Color(0.95f, 0.50f, 0.08f)),
            ("golden", new Color(1.00f, 0.82f, 0.15f)),
            ("jade", Color.clear), // 원본
            ("blue", new Color(0.10f, 0.25f, 0.95f)),
            ("sky", new Color(0.62f, 0.86f, 1.00f)),
            ("navy", new Color(0.04f, 0.08f, 0.30f)),
            ("magenta", new Color(0.95f, 0.10f, 0.75f)),
            ("bistre", new Color(0.45f, 0.24f, 0.08f)),
            ("charcoal", new Color(0.12f, 0.12f, 0.13f)),
            ("silver", new Color(0.80f, 0.82f, 0.86f)),
            ("ivory", new Color(0.97f, 0.93f, 0.82f)),
        };

        /// <summary>
        /// 주문서 12룬 (사용자가 보낸 앵글로색슨 룬 차트) — 획 = 선분 목록, 0~1 상자 안 좌표(y 위). Oethel은 이미지(rot C)와 달리 차트대로 바르게.
        /// </summary>
        public static readonly (string Id, float[] Strokes)[] Runes =
        {
            ("tyr", new[] { 0.5f, 0.05f, 0.5f, 0.95f, 0.5f, 0.95f, 0.2f, 0.66f, 0.5f, 0.95f, 0.8f, 0.66f }),
            ("feoh", new[] { 0.35f, 0.05f, 0.35f, 0.95f, 0.35f, 0.72f, 0.72f, 0.95f, 0.35f, 0.47f, 0.72f, 0.70f }),
            ("ur", new[] { 0.3f, 0.05f, 0.3f, 0.95f, 0.3f, 0.95f, 0.72f, 0.62f, 0.72f, 0.62f, 0.72f, 0.05f }),
            ("thorn", new[] { 0.35f, 0.05f, 0.35f, 0.95f, 0.35f, 0.72f, 0.68f, 0.5f, 0.68f, 0.5f, 0.35f, 0.28f }),
            ("rad", new[] { 0.32f, 0.05f, 0.32f, 0.95f, 0.32f, 0.95f, 0.68f, 0.75f, 0.68f, 0.75f, 0.32f, 0.55f, 0.32f, 0.55f, 0.70f, 0.05f }),
            ("gyfu", new[] { 0.2f, 0.95f, 0.8f, 0.05f, 0.2f, 0.05f, 0.8f, 0.95f }),
            ("wynn", new[] { 0.35f, 0.05f, 0.35f, 0.95f, 0.35f, 0.95f, 0.68f, 0.75f, 0.68f, 0.75f, 0.35f, 0.55f }),
            ("haegl", new[] { 0.28f, 0.05f, 0.28f, 0.95f, 0.72f, 0.05f, 0.72f, 0.95f, 0.28f, 0.68f, 0.72f, 0.48f, 0.28f, 0.50f, 0.72f, 0.30f }),
            ("is", new[] { 0.5f, 0.05f, 0.5f, 0.95f }),
            ("sigel", new[] { 0.62f, 0.95f, 0.35f, 0.62f, 0.35f, 0.62f, 0.65f, 0.38f, 0.65f, 0.38f, 0.38f, 0.05f }),
            ("beorc", new[] { 0.32f, 0.05f, 0.32f, 0.95f, 0.32f, 0.95f, 0.68f, 0.72f, 0.68f, 0.72f, 0.32f, 0.5f, 0.32f, 0.5f, 0.68f, 0.28f, 0.68f, 0.28f, 0.32f, 0.05f }),
            ("oethel", new[] { 0.5f, 0.95f, 0.78f, 0.66f, 0.5f, 0.95f, 0.22f, 0.66f, 0.22f, 0.66f, 0.80f, 0.08f, 0.78f, 0.66f, 0.20f, 0.08f }),
        };

        private static readonly Color RuneGold = new Color(1f, 0.80f, 0.30f);

        public static string PotionVisualId(string color) => "potion_" + color;
        public static string ScrollVisualId(string rune) => "scroll_" + rune;

        [MenuItem("ProjectM/Setup/Consumables")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[ConsumableArtSetup] 플레이 중에는 실행하지 않는다.");
                return;
            }
            var potionMesh = PrepareModel(PotionModel);
            var scrollMesh = PrepareModel(ScrollModel);
            if (potionMesh == null || scrollMesh == null)
            {
                Debug.LogError("[ConsumableArtSetup] Art/Varco3D/Prop_Potion.fbx · Prop_ScrollBlank.fbx 없음 — Docs/소모품_에셋.md");
                return;
            }
            MainSceneSetup.EnsureFolder(PrefabDir);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(ItemVisualCatalogPath);
            if (catalog == null)
            {
                Debug.LogError("[ConsumableArtSetup] ItemVisualCatalog 없음 — 먼저 Main Scene · Varco Models");
                return;
            }

            // 물약 12색: 텍스처 → 재질 → 프리팹
            var potionBase = ReadTexture(TextureDir + PotionModel + "_base_color.png");
            var potionNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + PotionModel + "_normal.png");
            foreach (var (id, color) in PotionColors)
            {
                string texPath = TextureDir + $"Prop_Potion_{id}_base_color.png";
                WritePng(texPath, color.a > 0f ? Recolor(potionBase, color) : potionBase);
                var mat = EnsureLitMaterial(MaterialDir + $"Prop_Potion_{id}_Mat.mat", texPath, potionNormal);
                var prefab = BuildPrefab(potionMesh, "Potion_" + Capitalize(id), PotionHeight, mat, null);
                catalog.Set(PotionVisualId(id), prefab, upright: true);
            }

            // 주문서 12룬: 룬 텍스처 → 재질 → 빈 종이 + 룬 판 프리팹
            var scrollMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + ScrollModel + "_Mat.mat");
            foreach (var (id, strokes) in Runes)
            {
                string texPath = TextureDir + $"Rune_{id}.png";
                WritePng(texPath, DrawRune(strokes, 256));
                var runeMat = EnsureRuneMaterial(MaterialDir + $"Rune_{id}_Mat.mat", texPath);
                var prefab = BuildPrefab(scrollMesh, "Scroll_" + Capitalize(id), ScrollLength, scrollMat, root => AddRune(root, runeMat));
                catalog.Set(ScrollVisualId(id), prefab, upright: true);
            }
            int floorItems = BuildFloorItems(catalog);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ConsumableArtSetup] 완료: 물약 12 · 주문서 12 · 바닥 아이템 {floorItems}");
        }

        // ── 바닥 아이템 (G-9, 2026-11-05 — ART_ASSETS ★ 2차 12 ~ 18번 · 12-6번 표) ──

        /// <summary>
        /// 아이템 하나 = Varco 모델 하나: 아이템 ID · 모델(Art/Varco3D/&lt;모델&gt;.fbx) · 바닥에서 가장 긴 쪽(m) · 눕히기(X축 도 — Varco가 세워 만든 갑옷 · 고기를 앞면이 위로).
        /// </summary>
        private static readonly (string ItemId, string Model, float Size, float Lie)[] FloorItemModels =
        {
            ("armor_cloth", "Item_ArmorCloth", 0.5f, -90f),
            ("armor_leather", "Item_ArmorLeather", 0.5f, -90f),
            ("armor_mail", "Item_ArmorMail", 0.5f, -90f),
            ("armor_scale", "Item_ArmorScale", 0.5f, -90f),
            ("armor_plate", "Item_ArmorPlate", 0.55f, -90f),
            ("food_ration", "Item_FoodRation", 0.3f, 0f),
            ("food_bread", "Item_FoodBread", 0.25f, 0f),
            ("food_mystery_meat", "Item_MeatMystery", 0.32f, -90f),
            ("food_cooked_meat", "Item_MeatCooked", 0.32f, -90f),
            ("food_frozen_meat", "Item_MeatFrozen", 0.32f, -90f),
            ("gold", "Item_Gold", 0.3f, 0f),
            ("key_iron", "Item_KeyIron", 0.3f, 0f),
            ("dart", "Item_Dart", 0.3f, 0f),
            ("javelin", "Item_Javelin", 0.7f, 0f),
            ("fusion_device", "Item_FusionDevice", 0.4f, 0f),
            // G-11 (2026-11-05): 심은 식물 — 아이템이 아니지만 같은 카탈로그(키 plant_*)를 TrapPresenter가 읽는다. 크기 = 가장 긴 쪽(m)
            ("plant_firebloom", "Prop_PlantFirebloom", 0.5f, 0f),
            ("plant_icecap", "Prop_PlantIcecap", 0.45f, 0f),
            ("plant_sorrowmoss", "Prop_PlantSorrowmoss", 0.5f, 0f),
            ("plant_sungrass", "Prop_PlantSungrass", 0.5f, 0f),
            ("plant_fadeleaf", "Prop_PlantFadeleaf", 0.5f, 0f),
        };

        /// <summary>
        /// 모델 하나 + 색 (결정 2026-11-05, 사용자 — 물약처럼): 흰 표지 마법서 · 흰 씨앗의 흰 부분만 속성 · 식물 색으로 칠한다.
        /// </summary>
        private const string SpellbookModel = "Item_Spellbook";
        private const float SpellbookSize = 0.35f;
        private static readonly (string ItemId, Color Color)[] SpellbookColors =
        {
            ("spellbook_fire", new Color(0.85f, 0.18f, 0.10f)),
            ("spellbook_ice", new Color(0.55f, 0.85f, 1.00f)),
            ("spellbook_lightning", new Color(1.00f, 0.85f, 0.15f)),
            ("spellbook_poison", new Color(0.55f, 0.20f, 0.75f)),
            ("spellbook_water", new Color(0.12f, 0.40f, 0.95f)),
        };
        private const string SeedModel = "Item_Seed";
        private const float SeedSize = 0.22f;
        private static readonly (string ItemId, Color Color)[] SeedColors =
        {
            ("seed_firebloom", new Color(0.95f, 0.40f, 0.10f)),
            ("seed_icecap", new Color(0.60f, 0.90f, 1.00f)),
            ("seed_sorrowmoss", new Color(0.35f, 0.55f, 0.20f)),
            ("seed_sungrass", new Color(0.95f, 0.85f, 0.25f)),
            ("seed_fadeleaf", new Color(0.70f, 0.55f, 0.90f)),
        };

        /// <summary>바닥 아이템 프리팹(Prefabs/Varco/Item_*)을 만들고 카탈로그에 넣는다 (원점 = 바닥 중앙, 눕히지 않음). 모델이 없는 줄은 건너뜀 — 그레이박스 그대로.</summary>
        private static int BuildFloorItems(ItemVisualCatalog catalog)
        {
            int count = 0;
            foreach (var (itemId, model, size, lie) in FloorItemModels)
            {
                var mesh = PrepareModel(model);
                if (mesh == null) { Debug.LogWarning("[ConsumableArtSetup] 바닥 아이템 모델 없음: " + model); continue; }
                var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + model + "_Mat.mat");
                catalog.Set(itemId, BuildPrefab(mesh, model, size, mat, null, lie), upright: true);
                count++;
            }
            count += BuildTinted(catalog, SpellbookModel, SpellbookSize, SpellbookColors);
            count += BuildTinted(catalog, SeedModel, SeedSize, SeedColors);
            return count;
        }

        private static int BuildTinted(ItemVisualCatalog catalog, string model, float size, (string ItemId, Color Color)[] colors)
        {
            var mesh = PrepareModel(model);
            if (mesh == null) { Debug.LogWarning("[ConsumableArtSetup] 바닥 아이템 모델 없음: " + model); return 0; }
            var source = ReadTexture(TextureDir + model + "_base_color.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + model + "_normal.png");
            int count = 0;
            foreach (var (itemId, color) in colors)
            {
                string key = Capitalize(itemId.Substring(itemId.IndexOf('_') + 1));
                string texPath = TextureDir + $"{model}_{key}_base_color.png";
                WritePng(texPath, TintWhite(source, color));
                var mat = EnsureLitMaterial(MaterialDir + $"{model}_{key}_Mat.mat", texPath, normal);
                catalog.Set(itemId, BuildPrefab(mesh, $"{model}_{key}", size, mat, null), upright: true);
                count++;
            }
            return count;
        }

        /// <summary>흰 부분(밝고 채도 낮은 픽셀)만 target 색으로 — 밝기는 유지해 그늘 · 무늬가 남는다. 금속 · 종이 결 같은 회색은 덜 칠한다.</summary>
        public static Texture2D TintWhite(Texture2D source, Color target)
        {
            var px = source.GetPixels();
            var output = new Color[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                Color.RGBToHSV(p, out _, out float s, out float v);
                float w = Mathf.Clamp01((v - 0.55f) / 0.25f) * Mathf.Clamp01((0.25f - s) / 0.15f);
                var tinted = new Color(target.r * v, target.g * v, target.b * v, p.a);
                output[i] = Color.Lerp(p, tinted, w);
            }
            var tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            tex.SetPixels(output);
            tex.Apply();
            return tex;
        }

        // ── 모델 ──

        /// <summary>FBX 가져오기 설정(조명 · 카메라 끔, material_0 → &lt;이름&gt;_Mat 재질)을 맞추고 모델을 돌려준다.</summary>
        private static GameObject PrepareModel(string name)
        {
            string path = ModelDir + name + ".fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return null;
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + name + "_normal.png");
            SetNormalMap(TextureDir + name + "_normal.png");
            SetMaxSize(TextureDir + name + "_base_color.png", 512);
            var mat = EnsureLitMaterial(MaterialDir + name + "_Mat.mat", TextureDir + name + "_base_color.png", normal);
            bool changed = importer.importCameras || importer.importLights;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), "material_0");
            if (!importer.GetExternalObjectMap().TryGetValue(id, out var current) || current != mat)
            {
                importer.AddRemap(id, mat);
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>
        /// 프리팹: 루트(원점 = 바닥 중앙) → 모델(FBX 루트 회전 유지, 가장 긴 쪽 = size m). decorate는 저장 전에 덧붙이기.
        /// </summary>
        private static GameObject BuildPrefab(GameObject model, string prefabName, float size, Material material, System.Action<GameObject> decorate, float lieDegrees = 0f)
        {
            var root = new GameObject(prefabName);
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "Model";
                instance.transform.SetParent(root.transform, false);
                // lieDegrees: 세워서 만들어진 모델(갑옷 · 고기)을 X축으로 눕혀 앞면이 위(고각도 카메라)를 보게
                // 눕힌 뒤 수직축 180° — 위쪽(갑옷 목 · 고기 뼈 끝)이 화면 위(먼 쪽)로
                var lie = lieDegrees != 0f ? Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(lieDegrees, 0f, 0f) : Quaternion.identity;
                instance.transform.localRotation = lie * model.transform.localRotation;
                foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (material != null) foreach (var r in renderers) r.sharedMaterial = material;
                var b = Bounds(renderers);
                float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                instance.transform.localScale = model.transform.localScale * (size / Mathf.Max(1e-4f, longest));
                b = Bounds(renderers);
                instance.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z); // 바닥 중앙 = 원점
                decorate?.Invoke(root);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{prefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Bounds Bounds(Renderer[] renderers)
        {
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>
        /// 종이 위에 룬 판(Quad)을 눕힌다: 가운데 부분 꼭짓점의 가장 높은 곳 바로 위, 위를 향함, 룬 위쪽 = +Z(화면 위).
        /// </summary>
        private static void AddRune(GameObject root, Material runeMaterial)
        {
            var filter = root.GetComponentInChildren<MeshFilter>();
            var renderer = root.GetComponentInChildren<Renderer>();
            if (filter == null || filter.sharedMesh == null) return;
            var b = renderer.bounds;
            float top = b.min.y;
            var m = filter.transform.localToWorldMatrix;
            foreach (var v in filter.sharedMesh.vertices)
            {
                var w = m.MultiplyPoint3x4(v);
                if (Mathf.Abs(w.x - b.center.x) > b.extents.x * 0.2f || Mathf.Abs(w.z - b.center.z) > b.extents.z * 0.2f) continue;
                top = Mathf.Max(top, w.y);
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "Rune";
            quad.transform.SetParent(root.transform, false);
            float side = Mathf.Min(b.size.x, b.size.z) * RuneSizeRatio;
            quad.transform.localScale = new Vector3(side, side, 1f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // 앞면 = 위
            quad.transform.position = new Vector3(b.center.x, top + 0.004f, b.center.z);
            quad.GetComponent<Renderer>().sharedMaterial = runeMaterial;
        }

        // ── 텍스처 ──

        private static Texture2D ReadTexture(string path)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path)); // 가져오기 설정과 상관없이 원본 PNG를 읽음
            return tex;
        }

        private static void WritePng(string path, Texture2D tex)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            SetMaxSize(path, 512);
        }

        /// <summary>
        /// 초록 액체만 target 색으로 (그늘 · 빛 유지): 색조가 초록~청록이고 채도가 있는 픽셀을 무게(0~1)로 골라, target × (밝기 / 액체 평균 밝기)와 섞는다.
        /// </summary>
        public static Texture2D Recolor(Texture2D source, Color target)
        {
            var px = source.GetPixels();
            float sum = 0f; int n = 0;
            foreach (var p in px)
            {
                if (LiquidWeight(p) < 0.99f) continue;
                sum += Luma(p); n++;
            }
            float reference = n > 0 ? sum / n : 0.5f;
            var output = new Color[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                float w = LiquidWeight(p);
                if (w <= 0f) { output[i] = p; continue; }
                float k = Luma(p) / Mathf.Max(0.05f, reference);
                var tinted = new Color(Mathf.Clamp01(target.r * k), Mathf.Clamp01(target.g * k), Mathf.Clamp01(target.b * k), p.a);
                output[i] = Color.Lerp(p, tinted, w);
            }
            var tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            tex.SetPixels(output);
            tex.Apply();
            return tex;
        }

        private static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        /// <summary>액체(초록~청록) 정도: 색조 0.30~0.55, 채도 0.2 이상에서 점점 1로.</summary>
        private static float LiquidWeight(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            if (v < 0.08f) return 0f;
            float hue = 1f - Mathf.Clamp01((Mathf.Abs(h - 0.42f) - 0.10f) / 0.05f);
            float sat = Mathf.Clamp01((s - 0.20f) / 0.15f);
            return hue * sat;
        }

        /// <summary>룬 글자: 획(선분)을 두께 있게 그린 RGBA (안 = 금색 불투명, 밖 = 투명).</summary>
        public static Texture2D DrawRune(float[] strokes, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float half = 0.045f; // 획 두께의 절반 (상자 비율)
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float d = float.MaxValue;
                    for (int s = 0; s + 3 < strokes.Length; s += 4)
                        d = Mathf.Min(d, SegmentDistance(p, new Vector2(strokes[s], strokes[s + 1]), new Vector2(strokes[s + 2], strokes[s + 3])));
                    float a = Mathf.Clamp01((half - d) / 0.01f + 0.5f);
                    px[y * size + x] = new Color(RuneGold.r, RuneGold.g, RuneGold.b, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        // ── 재질 ──

        private static Material EnsureLitMaterial(string path, string basePath, Texture2D normal)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath));
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.SetFloat("_Smoothness", 0.35f);
            mat.SetFloat("_Metallic", 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>룬 재질: URP Lit + 알파 잘라내기(획만) + 금색 발광(Bloom이 번지게).</summary>
        private static Material EnsureRuneMaterial(string path, string texPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            if (!ti.alphaIsTransparency)
            {
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetTexture("_EmissionMap", tex);
            mat.SetColor("_EmissionColor", RuneGold * 1.6f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetFloat("_Cull", 0f); // 양면
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetMaxSize(string path, int size)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti) || ti.maxTextureSize == size) return;
            ti.maxTextureSize = size;
            ti.SaveAndReimport();
        }

        private static void SetNormalMap(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti) || ti.textureType == TextureImporterType.NormalMap) return;
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }

        private static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
