using ProjectM.Core;
using ProjectM.Presentation;
using UnityEditor;
using UnityEngine;

namespace ProjectM.Editor
{
    /// <summary>
    /// G-2a: Varco 모델(Art/Varco3D) → 표시용 프리팹(Prefabs/Varco)을 만들고 EntityVisualCatalog · TileVisualSet에 연결한다.
    /// 몇 번 실행해도 결과가 같다 (프리팹은 같은 경로에 덮어써 GUID 유지). 메뉴: ProjectM/Setup/Varco Models.
    /// 크기 · 방향 기준은 Docs/ART_ASSETS.md 0-1 · 12번 (Varco는 모든 모델을 가장 긴 쪽 1 m로 보내므로 여기서 맞춘다).
    /// 프리팹 구조: 루트(원점 = 발밑 중앙, 앞 = +Z) → 모델(크기 · 방향 보정). 루트는 건드리지 않아 표현 코드가 자유롭게 돌리고 옮긴다.
    /// </summary>
    public static class VarcoModelSetup
    {
        private const string ModelDir = "Assets/_Project/Art/Varco3D/";
        private const string PrefabDir = "Assets/_Project/Prefabs/Varco";
        private const string TileVisualSetPath = "Assets/_Project/Data/Visuals/TileVisualSet.asset";
        private const string EntityCatalogPath = "Assets/_Project/Data/Visuals/EntityVisualCatalog.asset";

        /// <summary>모델별 보정: 크기 배율 · 앞 방향 보정(Y 회전, 도). ART_ASSETS.md 12번 표와 같은 값.</summary>
        private struct ModelSpec
        {
            public string Model;
            public string Prefab;
            public float Scale;
            public float Yaw;

            public ModelSpec(string model, string prefab, float scale, float yaw)
            {
                Model = model;
                Prefab = prefab;
                Scale = scale;
                Yaw = yaw;
            }
        }

        private static readonly ModelSpec Player = new ModelSpec("SheepMage", "Unit_Player", 1f, 0f);
        private static readonly ModelSpec[] MeleeVariants =
        {
            new ModelSpec("Enemy_Rabbit", "Unit_Rabbit", 0.8f, 0f),
            new ModelSpec("Enemy_Mouse", "Unit_Mouse", 0.8f, 0f),
            new ModelSpec("Enemy_Slime", "Unit_Slime", 0.8f, 0f),
        };
        private static readonly ModelSpec Boss = new ModelSpec("Boss_SlimeGirl", "Unit_Boss", 1.8f, 0f);
        private static readonly ModelSpec Staff = new ModelSpec("Prop_MageStaff", "", 0.85f, 0f);
        private static readonly ModelSpec Floor = new ModelSpec("Tile_Floor", "Tile_Floor", 1f, 0f);
        private static readonly ModelSpec Wall = new ModelSpec("Tile_Wall", "Tile_Wall", 1f, 0f);
        private static readonly ModelSpec Pillar = new ModelSpec("Tile_Pillar", "Tile_Pillar", 1f, 0f);
        private static readonly ModelSpec StairsDown = new ModelSpec("Tile_StairsDown", "Tile_StairsDown", 1f, 0f);
        private static readonly ModelSpec StairsUp = new ModelSpec("Tile_StairsUp", "Tile_StairsUp", 1f, 0f);

        /// <summary>지팡이를 쥐는 높이 (지팡이 길이에 대한 비율) — 손이 이 지점에 오게 붙인다.</summary>
        private const float StaffGripRatio = 0.4f;
        private const string StaffHandBone = "RightHand";
        /// <summary>올라가는 계단 위 발밑 높이 (m) — ART_ASSETS.md 12번.</summary>
        private const float StairsUpStandHeight = 0.3f;

        [MenuItem("ProjectM/Setup/Varco Models")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[VarcoModelSetup] 플레이 중에는 실행하지 않는다.");
                return;
            }
            MainSceneSetup.EnsureFolder(PrefabDir);
            DisableSceneObjectImport();

            var catalog = AssetDatabase.LoadAssetAtPath<EntityVisualCatalog>(EntityCatalogPath);
            var tiles = AssetDatabase.LoadAssetAtPath<TileVisualSet>(TileVisualSetPath);
            if (catalog == null || tiles == null)
            {
                Debug.LogError("[VarcoModelSetup] 먼저 ProjectM/Setup/Main Scene을 실행한다 (카탈로그 · 타일 세트 없음).");
                return;
            }

            var player = BuildPrefab(Player, AttachStaff);
            catalog.SetPrefab("player", player);
            var variants = new GameObject[MeleeVariants.Length];
            for (int i = 0; i < MeleeVariants.Length; i++)
                variants[i] = BuildPrefab(MeleeVariants[i], null);
            catalog.SetVariants("enemy_melee", variants);
            catalog.SetPrefab("boss", BuildPrefab(Boss, null));
            EditorUtility.SetDirty(catalog);

            tiles.SetModule(TileType.Floor, BuildPrefab(Floor, SinkToGround), randomRotation: true);
            tiles.SetModule(TileType.Wall, BuildPrefab(Wall, null));
            tiles.SetModule(TileType.StairsDown, BuildPrefab(StairsDown, null));
            tiles.SetModule(TileType.StairsUp, BuildPrefab(StairsUp, null));
            tiles.SetPillarModule(BuildPrefab(Pillar, null));
            // G-2b: 올라가는 계단(0.57 m) 위에 선 유닛은 가운데 단 높이에 — 이미 값이 있으면(사용자가 고친 값) 두고 비었을 때만
            if (tiles.StandHeight(TileType.StairsUp) == 0f) tiles.SetStandHeight(TileType.StairsUp, StairsUpStandHeight);
            EditorUtility.SetDirty(tiles);

            EnableInstancing();
            AssetDatabase.SaveAssets();
            Debug.Log("[VarcoModelSetup] 완료: " + PrefabDir);
        }

        private static GameObject BuildPrefab(ModelSpec spec, System.Action<GameObject> decorate)
        {
            var model = LoadModel(spec.Model);
            if (model == null) return null;
            var root = new GameObject(spec.Prefab);
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "Model";
                instance.transform.SetParent(root.transform, false);
                // FBX 루트의 축 변환 회전 · 배율(Z-up → Y-up)은 그대로 두고 그 위에 방향 · 크기 보정을 곱한다
                instance.transform.localRotation = Quaternion.Euler(0f, spec.Yaw, 0f) * model.transform.localRotation;
                instance.transform.localScale = model.transform.localScale * spec.Scale;
                StripColliders(instance);
                decorate?.Invoke(instance);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{spec.Prefab}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>지팡이를 오른손 뼈에 붙인다: 세워 든 채, 손이 지팡이 길이의 StaffGripRatio 지점에 오게.</summary>
        private static void AttachStaff(GameObject sheep)
        {
            var model = LoadModel(Staff.Model);
            var hand = FindDeep(sheep.transform, StaffHandBone);
            if (model == null || hand == null)
            {
                Debug.LogWarning("[VarcoModelSetup] 지팡이 또는 손 뼈(" + StaffHandBone + ") 없음 — 지팡이 생략");
                return;
            }
            var staff = (GameObject)PrefabUtility.InstantiatePrefab(model);
            staff.name = "Staff";
            StripColliders(staff);
            float length = Staff.Scale; // 원본 1 m
            staff.transform.SetParent(hand, true);
            staff.transform.localScale = model.transform.localScale * (Staff.Scale / Mathf.Max(1e-4f, hand.lossyScale.y));
            var upright = sheep.transform.parent; // 프리팹 루트 (Y-up)
            staff.transform.rotation = upright.rotation * Quaternion.Euler(0f, Staff.Yaw, 0f) * model.transform.localRotation; // 세워서
            staff.transform.position = hand.position - upright.up * (length * StaffGripRatio);
        }

        /// <summary>바닥 판: 윗면이 y = 0이 되게 내린다 — 유닛 발밑 · 지형 효과 · 안개 · 조준 덮개(y 0.01~0.03)가 그대로 바닥 위에 온다.</summary>
        private static void SinkToGround(GameObject floor)
        {
            float top = float.MinValue;
            foreach (var r in floor.GetComponentsInChildren<Renderer>(true))
                top = Mathf.Max(top, r.bounds.max.y);
            if (top > float.MinValue) floor.transform.localPosition -= Vector3.up * (top - floor.transform.parent.position.y);
        }

        /// <summary>
        /// 블렌더에서 내보낸 FBX에는 기본 씬의 조명 · 카메라가 섞여 올 수 있다 (SheepMage에 1000 W 점광원 — 플레이어 주변이 하얗게 날아감).
        /// Varco3D 폴더의 모든 FBX에서 조명 · 카메라 가져오기를 끈다.
        /// </summary>
        private static void DisableSceneObjectImport()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelDir.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
                if (!importer.importLights && !importer.importCameras) continue;
                importer.importLights = false;
                importer.importCameras = false;
                importer.SaveAndReimport();
            }
        }

        private static GameObject LoadModel(string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + name + ".fbx");
            if (model == null) Debug.LogWarning("[VarcoModelSetup] 모델 없음: " + ModelDir + name + ".fbx");
            return model;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);
        }

        /// <summary>Varco 재질은 칸마다 같은 모듈을 수백 개 그리므로 GPU 인스턴싱을 켠다.</summary>
        private static void EnableInstancing()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { ModelDir + "Materials" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.enableInstancing) continue;
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
            }
        }
    }
}
