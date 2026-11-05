using ProjectM.Core;
using ProjectM.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ProjectM.Editor
{
    /// <summary>
    /// G-2a: Varco 모델(Art/Varco3D) → 표시용 프리팹(Prefabs/Varco)을 만들고 EntityVisualCatalog · TileVisualSet에 연결한다.
    /// 몇 번 실행해도 결과가 같다 (프리팹은 같은 경로에 덮어써 GUID 유지). 메뉴: ProjectM/Setup/Varco Models.
    /// 크기 · 방향 기준은 Docs/ART_ASSETS.md 0-1 · 12번 (Varco는 모든 모델을 가장 긴 쪽 1 m로 보내므로 여기서 맞춘다).
    /// 프리팹 구조: 루트(원점 = 발밑 중앙, 앞 = +Z) → 모델(크기 · 방향 보정). 루트는 건드리지 않아 표현 코드가 자유롭게 돌리고 옮긴다.
    /// 애니메이션 (2026-10-19): 양 마법사 · Animation/Idle · Walk(Mixamo)를 Humanoid로 가져와 리타깃하고, Idle ↔ Walk 컨트롤러(bool Moving)를
    /// 플레이어 모델의 Animator에 붙인다. 루트 이동은 클립에 굽고(제자리) 위치 · 방향은 EntityPresenter가 맡는다. 지팡이는 Idle 첫 자세에서 세워 붙인다.
    /// </summary>
    public static class VarcoModelSetup
    {
        private const string ModelDir = "Assets/_Project/Art/Varco3D/";
        private const string PrefabDir = "Assets/_Project/Prefabs/Varco";
        private const string TileVisualSetPath = "Assets/_Project/Data/Visuals/TileVisualSet.asset";
        private const string EntityCatalogPath = "Assets/_Project/Data/Visuals/EntityVisualCatalog.asset";
        private const string ItemVisualCatalogPath = "Assets/_Project/Data/Visuals/ItemVisualCatalog.asset";
        private const string AnimationDir = "Assets/_Project/Animation/";
        private const string PlayerControllerPath = AnimationDir + "PlayerAnimator.controller";
        /// <summary>EntityPresenter가 이동 중에 켜는 bool 파라미터 (EntityPresenter.MovingParameter와 같아야 한다).</summary>
        private const string MovingParameter = "Moving";
        /// <summary>Walk 재생 속도 — 한 칸 이동(0.12초 보간 · 키 반복)이 빨라 걸음을 조금 빠르게.</summary>
        private const float WalkSpeed = 1.5f;
        private const float BlendSeconds = 0.12f;

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
        /// <summary>G-7 (2026-11-03): 캐릭터 선택의 루멘(hero_blond) · 시아(hero_black) — Mixamo 리깅 모델.</summary>
        private static readonly (string VisualId, ModelSpec Spec)[] Heroes =
        {
            ("hero_blond", new ModelSpec("Hero_Blond_Rigged", "Unit_Lumen", 0.57f, 0f)),
            ("hero_black", new ModelSpec("Sia_Rigged", "Unit_Sia", 0.57f, 0f)),
        };
        private static readonly ModelSpec[] MeleeVariants =
        {
            new ModelSpec("Enemy_Rabbit", "Unit_Rabbit", 0.8f, 0f),
            new ModelSpec("Enemy_Mouse", "Unit_Mouse", 0.8f, 0f),
            new ModelSpec("Enemy_Slime", "Unit_Slime", 0.8f, 0f),
        };
        private static readonly ModelSpec Boss = new ModelSpec("Boss_SlimeGirl", "Unit_Boss", 1.8f, 0f);
        /// <summary>리깅한 슬라임 소녀 배율 — Mixamo 결과는 키가 원본과 달라 다시 잰 값 (게임 안 키 1.8 m, ART_ASSETS 12-5).</summary>
        private const float BossRiggedScale = 1.8f;
        /// <summary>
        /// 3-17 구역 적 · 보스 · 상인 (2026-11-05, ART_ASSETS ★ 1차 = 12-5번 표): Varco 원본 `<Model>.fbx` + 텍스처 `Textures/<Model>_base_color · _normal`.
        /// Rigged = 사람형 — Mixamo 자동 리깅한 `<Model>_Rigged.fbx`를 Humanoid로, 플레이어와 같은 Idle · Walk 컨트롤러. 아니면 원본(코드 움직임).
        /// Staff = 오른손에 붙일 지팡이 소품(`Prop_Staff_*`, Scale = 게임 안 길이 m) — 몸 모델에는 든 물건을 넣지 않는다 (결정 2026-11-05, 사용자).
        /// </summary>
        private static readonly (string VisualId, ModelSpec Spec, bool Rigged, ModelSpec? Staff)[] ZoneUnits =
        {
            // 배율 = 게임 안 키(m) ÷ 모델 키 — Mixamo 결과 키(T자세) 약 1.05 ~ 1.2, Varco 원본은 가장 긴 쪽 1 m (다시 재서 맞춤 — ART_ASSETS 12-5)
            ("enemy_goblin_shaman", new ModelSpec("Enemy_GoblinShaman", "Unit_GoblinShaman", 0.8f, 0f), true,
                new ModelSpec("Prop_Staff_Goblin", "", 0.85f, 0f)),                                                 // 0.85 m (작은 고블린)
            ("enemy_scorpion", new ModelSpec("Enemy_Scorpion", "Unit_Scorpion", 0.8f, 0f), false, null),         // 길이 0.8 m (쥐 · 토끼와 같은 배율)
            ("enemy_jackal", new ModelSpec("Enemy_Jackal", "Unit_Jackal", 0.8f, 0f), false, null),
            ("enemy_mummy", new ModelSpec("Enemy_Mummy", "Unit_Mummy", 1f, 0f), true, null),                    // 1.1 m (플레이어보다 조금 큼)
            ("enemy_desert_shaman", new ModelSpec("Enemy_DesertShaman", "Unit_DesertShaman", 0.88f, 0f), true,
                new ModelSpec("Prop_Staff_Sand", "", 1.15f, 0f)),                                                   // 1.05 m
            ("boss_pharaoh", new ModelSpec("Boss_Pharaoh", "Unit_Pharaoh", 1.7f, 0f), true,
                new ModelSpec("Prop_Staff_Pharaoh", "", 1.8f, 0f)),                                                   // 1.85 m (보스 2칸 키, 슬라임 소녀 1.8)
            ("shopkeeper", new ModelSpec("NPC_Shopkeeper", "Unit_Shopkeeper", 0.9f, 0f), true, null),          // 0.95 m
        };
        private static readonly ModelSpec Staff = new ModelSpec("Prop_MageStaff", "", 0.85f, 0f);
        private static readonly ModelSpec Floor = new ModelSpec("Tile_Floor", "Tile_Floor", 1f, 0f);
        private static readonly ModelSpec Wall = new ModelSpec("Tile_Wall", "Tile_Wall", 1f, 0f);
        private static readonly ModelSpec Pillar = new ModelSpec("Tile_Pillar", "Tile_Pillar", 1f, 0f);
        private static readonly ModelSpec StairsDown = new ModelSpec("Tile_StairsDown", "Tile_StairsDown", 1f, 0f);
        private static readonly ModelSpec StairsUp = new ModelSpec("Tile_StairsUp", "Tile_StairsUp", 1f, 0f);

        /// <summary>
        /// 무기 6종 (3-7a — Docs/무기_에셋.md 1번 표 = ART_ASSETS 12-2): 아이템 ID · 모델 · 게임 안 길이(m) · 쥐는 곳(길이 비율, 아래 끝 0 → 위 끝 1).
        /// Tilt = 손에서 앞으로 기울이는 각도(도) — 모두 칼처럼 앞으로 눕혀 든다 (사용자 요청 2026-10-19 — 세워 들지 않음). 긴 무기는 쥐는 곳을 낮춰 뒤로 덜 나오게.
        /// Varco 무기 FBX는 높이 1 m · 날이 위(+Y) · 원점 가운데.
        /// </summary>
        private static readonly (string ItemId, string Model, float Length, float Grip, float Tilt)[] WeaponSpecs =
        {
            ("shortsword", "Prop_Weapon_Shortsword", 0.45f, 0.12f, 70f),
            ("hand_axe", "Prop_Weapon_HandAxe", 0.35f, 0.25f, 70f),
            ("spear", "Prop_Weapon_Spear", 1.10f, 0.30f, 70f),
            ("quarterstaff", "Prop_Weapon_Quarterstaff", 1.00f, 0.30f, 70f),
            ("dirk", "Prop_Weapon_Dirk", 0.25f, 0.15f, 70f),
            ("sickle", "Prop_Weapon_Sickle", 0.30f, 0.15f, 70f),
        };

        /// <summary>지팡이를 쥐는 높이 (지팡이 길이에 대한 비율) — 손이 이 지점에 오게 붙인다.</summary>
        private const float StaffGripRatio = 0.4f;
        private const string StaffHandBone = "RightHand";
        /// <summary>3-11a 그레이박스 풀잎 높이 (m): 짧은 풀 · 높은 풀 (ART_ASSETS.md 12-4).</summary>
        private const float GrassBladeHeight = 0.08f;
        private const float TallGrassBladeHeight = 0.5f;
        private const string GrassMaterialPath = PrefabDir + "/Mat_GrassBlade.mat";
        private const string DoorMaterialPath = PrefabDir + "/Mat_DoorWood.mat";

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

            // 애니메이션 (2026-10-19): 양 · 클립을 Humanoid로 (리타깃), 컨트롤러는 Idle 기본 · Moving이면 Walk
            EnsureHumanoid(ModelDir + Player.Model + ".fbx", null);
            var idle = EnsureHumanoid(AnimationDir + "Idle.fbx", "Idle");
            var walk = EnsureHumanoid(AnimationDir + "Walk.fbx", "Walk");
            var controller = idle != null && walk != null ? EnsurePlayerController(idle, walk) : null;
            EnsureRiggedCharacters();
            if (controller == null) Debug.LogWarning("[VarcoModelSetup] Animation/Idle.fbx · Walk.fbx 없음 — 플레이어 애니메이션 생략 (코드 움직임만)");

            var player = BuildPrefab(Player, sheep =>
            {
                SetupAnimator(sheep, controller, idle);
                AttachStaff(sheep);
                AttachWeapons(sheep); // 3-7a: 손에 무기 6종을 꺼 둔 채 붙임 — 들면 EntityPresenter가 켜고 지팡이를 숨김
            });
            catalog.SetPrefab("player", player);
            foreach (var (id, spec) in Heroes) // 캐릭터 선택 — 양 마법사와 같은 컨트롤러 · 무기, 모델이 없으면 양 마법사
            {
                var hero = BuildPrefab(spec, model =>
                {
                    SetupAnimator(model, controller, idle);
                    AttachWeapons(model); // 2026-11-05 사용자 요청: 루멘 · 시아는 맨손으로 시작 — 지팡이를 붙이지 않음 (무기를 들면 그 무기만)
                });
                catalog.SetPrefab(id, hero != null ? hero : player);
            }
            var variants = new GameObject[MeleeVariants.Length];
            for (int i = 0; i < MeleeVariants.Length; i++)
                variants[i] = BuildPrefab(MeleeVariants[i], null);
            catalog.SetVariants("enemy_melee", variants);
            catalog.SetPrefab("enemy_bunny", variants[0]); // 3-17b 적 종류마다 한 모델
            catalog.SetPrefab("enemy_rat", variants[1]);
            catalog.SetPrefab("enemy_slime", variants[2]);
            // 2026-11-05 사용자: 슬라임 소녀가 T자세로 굳어 있음 → Mixamo 자동 리깅 `Boss_SlimeGirl_Rigged.fbx`(Humanoid)에 플레이어와 같은 Idle · Walk. 없으면 원본(코드 움직임)
            string bossRiggedPath = ModelDir + Boss.Model + "_Rigged.fbx";
            bool bossRigged = EnsureVarcoMaterial(Boss.Model, bossRiggedPath);
            if (bossRigged) EnsureHumanoid(bossRiggedPath, null);
            var bossPrefab = bossRigged
                ? BuildPrefab(new ModelSpec(Boss.Model + "_Rigged", Boss.Prefab, BossRiggedScale, Boss.Yaw), model => SetupAnimator(model, controller, idle))
                : BuildPrefab(Boss, null);
            catalog.SetPrefab("boss", bossPrefab);
            catalog.SetPrefab("boss_slime_girl", bossPrefab); // 3-17d 5층 보스 = 슬라임 소녀 모델
            foreach (var (id, spec, rigged, staff) in ZoneUnits) // 3-17 구역 적 · 보스 · 상인 — 모델이 없으면 그레이박스 그대로
            {
                var source = rigged ? new ModelSpec(spec.Model + "_Rigged", spec.Prefab, spec.Scale, spec.Yaw) : spec;
                if (!EnsureVarcoMaterial(spec.Model, ModelDir + source.Model + ".fbx")) continue;
                if (rigged) EnsureHumanoid(ModelDir + source.Model + ".fbx", null);
                bool hasStaff = staff.HasValue && EnsureVarcoMaterial(staff.Value.Model, ModelDir + staff.Value.Model + ".fbx");
                var prefab = BuildPrefab(source, rigged ? (System.Action<GameObject>)(model =>
                {
                    SetupAnimator(model, controller, idle);
                    if (hasStaff) AttachStaff(model, staff.Value);
                }) : null);
                if (prefab != null) catalog.SetPrefab(id, prefab);
            }
            EditorUtility.SetDirty(catalog);

            var floorPrefab = BuildPrefab(Floor, SinkToGround);
            tiles.SetModule(TileType.Floor, floorPrefab, randomRotation: true);
            // 3-11a 풀 (그레이박스): 바닥 모듈 + 초록 풀잎 — 높은 풀은 0.15 m보다 높아 시야를 직접 따른다 (TilemapPresenter)
            tiles.SetModule(TileType.Grass, BuildGrassPrefab(floorPrefab, "Tile_Grass", GrassBladeHeight), randomRotation: true);
            tiles.SetModule(TileType.TallGrass, BuildGrassPrefab(floorPrefab, "Tile_TallGrass", TallGrassBladeHeight), randomRotation: true);
            // 3-11c 문 (그레이박스): 바닥 + 문틀 + 문짝 — 방향은 TilemapPresenter가 옆 벽을 보고 돌린다 (문짝이 벽 사이를 막게)
            tiles.SetModule(TileType.Door, BuildDoorPrefab(floorPrefab, "Tile_Door", open: false));
            tiles.SetModule(TileType.OpenDoor, BuildDoorPrefab(floorPrefab, "Tile_OpenDoor", open: true));
            tiles.SetModule(TileType.LockedDoor, AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Tile_Door.prefab")); // 3-15b: 닫힌 문 모양 (색은 타일)
            tiles.SetModule(TileType.AlchemyPot, BuildPotPrefab(floorPrefab)); // 3-16c 연금 솥 (그레이박스)
            tiles.SetModule(TileType.Wall, BuildPrefab(Wall, null));
            tiles.SetModule(TileType.StairsDown, BuildPrefab(StairsDown, null));
            tiles.SetModule(TileType.StairsUp, BuildPrefab(StairsUp, null));
            tiles.SetPillarModule(BuildPrefab(Pillar, null));
            // G-2b: 올라가는 계단(0.57 m) 위에 선 유닛은 가운데 단 높이에 — 이미 값이 있으면(사용자가 고친 값) 두고 비었을 때만
            if (tiles.StandHeight(TileType.StairsUp) == 0f) tiles.SetStandHeight(TileType.StairsUp, StairsUpStandHeight);
            EditorUtility.SetDirty(tiles);

            BuildWeaponPrefabs();
            EnableInstancing();
            AssetDatabase.SaveAssets();
            Debug.Log("[VarcoModelSetup] 완료: " + PrefabDir);
        }

        /// <summary>
        /// 3-11a 풀 모듈 (그레이박스 — 모델이 생기면 ModelSpec으로 바꾼다): 바닥 모듈 + 4 × 4 풀잎(가는 상자). 잎 위치 · 높이는 고정 무늬 (난수 안 씀).
        /// </summary>
        private static GameObject BuildGrassPrefab(GameObject floor, string name, float bladeHeight)
        {
            var root = new GameObject(name);
            try
            {
                if (floor != null) ((GameObject)PrefabUtility.InstantiatePrefab(floor)).transform.SetParent(root.transform, false);
                var material = EnsureGreyboxMaterial(GrassMaterialPath, new Color(0.3f, 0.6f, 0.2f));
                for (int i = 0; i < 16; i++)
                {
                    int gx = i % 4, gz = i / 4;
                    float jitterX = ((i * 37) % 11) / 11f - 0.5f, jitterZ = ((i * 53) % 13) / 13f - 0.5f;
                    float h = bladeHeight * (0.7f + 0.3f * (((i * 29) % 7) / 6f));
                    var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(blade.GetComponent<Collider>());
                    blade.name = "Blade";
                    blade.transform.SetParent(root.transform, false);
                    blade.transform.localPosition = new Vector3(-0.375f + gx * 0.25f + jitterX * 0.12f, h * 0.5f, -0.375f + gz * 0.25f + jitterZ * 0.12f);
                    blade.transform.localRotation = Quaternion.Euler(0f, (i * 47) % 90, 0f);
                    blade.transform.localScale = new Vector3(0.06f, h, 0.06f);
                    blade.GetComponent<Renderer>().sharedMaterial = material;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 3-11c 문 모듈 (그레이박스): 바닥 모듈 + 기둥 둘 · 상인방 + 문짝. 문짝은 X축(좌우 벽 사이)을 막는다 — 닫힘 = 가운데,
        /// 열림 = 왼쪽 기둥을 축으로 90° 돌아 Z축으로. 높이 1 m라 시야를 직접 따른다.
        /// </summary>
        private static GameObject BuildDoorPrefab(GameObject floor, string name, bool open)
        {
            var root = new GameObject(name);
            try
            {
                if (floor != null) ((GameObject)PrefabUtility.InstantiatePrefab(floor)).transform.SetParent(root.transform, false);
                var material = EnsureGreyboxMaterial(DoorMaterialPath, new Color(0.45f, 0.3f, 0.18f));
                Box(root.transform, material, "PostL", new Vector3(-0.45f, 0.5f, 0f), new Vector3(0.1f, 1f, 0.16f));
                Box(root.transform, material, "PostR", new Vector3(0.45f, 0.5f, 0f), new Vector3(0.1f, 1f, 0.16f));
                Box(root.transform, material, "Lintel", new Vector3(0f, 1f, 0f), new Vector3(1f, 0.1f, 0.16f));
                if (open) Box(root.transform, material, "Panel", new Vector3(-0.38f, 0.46f, 0.4f), new Vector3(0.06f, 0.9f, 0.78f));
                else Box(root.transform, material, "Panel", new Vector3(0f, 0.46f, 0f), new Vector3(0.8f, 0.9f, 0.06f));
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>3-16c 연금 솥 (그레이박스): 바닥 + 보라 원기둥 솥.</summary>
        private static GameObject BuildPotPrefab(GameObject floor)
        {
            var root = new GameObject("Tile_AlchemyPot");
            try
            {
                if (floor != null) ((GameObject)PrefabUtility.InstantiatePrefab(floor)).transform.SetParent(root.transform, false);
                var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(pot.GetComponent<Collider>());
                pot.name = "Pot";
                pot.transform.SetParent(root.transform, false);
                pot.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                pot.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
                pot.GetComponent<Renderer>().sharedMaterial = EnsureGreyboxMaterial(PrefabDir + "/Mat_AlchemyPot.mat", new Color(0.45f, 0.25f, 0.6f));
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/Tile_AlchemyPot.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void Box(Transform parent, Material material, string name, Vector3 position, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material EnsureGreyboxMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
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

        /// <summary>
        /// FBX를 Humanoid(이 모델로 아바타 생성)로 가져온다. clipName이 있으면 첫 클립을 그 이름 · 반복 · 루트 회전 · 높이 굽기 · 앞뒤 이동은 루트로(제자리)로 맞추고 돌려준다.
        /// 이미 같으면 다시 가져오지 않는다 (멱등).
        /// </summary>
        private static AnimationClip EnsureHumanoid(string path, string clipName)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return null;
            bool dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Human || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }
            if (clipName != null)
            {
                var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                if (clips.Length > 0)
                {
                    var c = clips[0];
                    if (c.name != clipName || !c.loopTime || !c.lockRootRotation || !c.lockRootHeightY || c.lockRootPositionXZ || clips.Length != 1)
                    {
                        c.name = clipName;
                        c.loopTime = true;
                        c.lockRootRotation = true;   // 루트 회전 · 높이는 포즈에 굽는다
                        c.lockRootHeightY = true;
                        // 앞뒤 이동(XZ)은 굽지 않는다 — 구우면 몸이 한 주기 동안 0.7 m 앞으로 밀려 나갔다가 튀어 돌아온다(G-2c 버그).
                        // 루트로 빼고 Animator.applyRootMotion = false로 버리면 몸은 제자리에서 걷고, 이동은 EntityPresenter가 한다
                        c.lockRootPositionXZ = false;
                        c.keepOriginalOrientation = true;
                        c.keepOriginalPositionY = true;
                        c.keepOriginalPositionXZ = true;
                        importer.clipAnimations = new[] { c };
                        dirty = true;
                    }
                }
            }
            if (dirty) importer.SaveAndReimport();
            if (clipName == null) return null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is AnimationClip clip && clip.name == clipName) return clip;
            return null;
        }

        /// <summary>
        /// G-7 (2026-11-03): Mixamo 리깅한 캐릭터(루멘 · 시아)를 Humanoid로 가져오고, 텍스처를 색(sRGB) · 노멀 맵으로 맞춘다.
        /// 재질은 FBX에 묻힌 이름(`<이름>_Mat`)이 같은 이름의 Textures/ 그림을 찾는다. 4K 그림(시아 상의 · 하의)은 4096까지 쓴다.
        /// </summary>
        private static void EnsureRiggedCharacters()
        {
            foreach (var (model, parts) in RiggedCharacters)
            {
                foreach (var part in parts)
                {
                    EnsureCharacterTexture($"{ModelDir}Textures/{model}_{part}_base_color.png", normal: false);
                    EnsureCharacterTexture($"{ModelDir}Textures/{model}_{part}_normal.png", normal: true);
                }
                var path = $"{ModelDir}{model}_Rigged.fbx";
                if (AssetImporter.GetAtPath(path) == null) { Debug.LogWarning("[VarcoModelSetup] 없음: " + path); continue; }
                EnsureHumanoid(path, null);
                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
                if (avatar == null || !avatar.isHuman) Debug.LogWarning("[VarcoModelSetup] Humanoid 아바타가 잡히지 않음: " + path);
            }
        }

        private static readonly (string Model, string[] Parts)[] RiggedCharacters =
        {
            ("Hero_Blond", new[] { "Body", "Coat", "Head" }),
            ("Sia", new[] { "Body", "Top", "Bottom", "Head" }),
        };

        private static void EnsureCharacterTexture(string path, bool normal)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int maxSize = Mathf.Max(width, height) > 2048 ? 4096 : 2048;
            if (importer.textureType == type && importer.maxTextureSize == maxSize && importer.sRGBTexture == !normal) return;
            importer.textureType = type;
            importer.sRGBTexture = !normal;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Varco 모델 재질 (2026-11-05): `Materials/<model>_Mat`(URP Lit, 색 · 노멀 = Textures/<model>_base_color · _normal — FBX에 묻힌 그림을 꺼내 둔 것)을
        /// 만들고 FBX의 묻힌 재질을 모두 그것으로 바꾼다. Varco FBX는 묻힌 이름이 모두 같아(material_0) 이름으로 찾게 두면 섞인다. 이미 같으면 아무것도 안 함.
        /// FBX나 텍스처가 없으면 false.
        /// </summary>
        private static bool EnsureVarcoMaterial(string model, string fbxPath)
        {
            if (!(AssetImporter.GetAtPath(fbxPath) is ModelImporter importer)) { Debug.LogWarning("[VarcoModelSetup] 없음: " + fbxPath); return false; }
            string colorPath = $"{ModelDir}Textures/{model}_base_color.png", normalPath = $"{ModelDir}Textures/{model}_normal.png";
            EnsureCharacterTexture(colorPath, normal: false);
            EnsureCharacterTexture(normalPath, normal: true);
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
            if (color == null) { Debug.LogWarning("[VarcoModelSetup] 텍스처 없음: " + colorPath); return false; }
            var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            string materialPath = $"{ModelDir}Materials/{model}_Mat.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            if (material.GetTexture("_BaseMap") != color || material.GetTexture("_BumpMap") != normalMap)
            {
                material.SetTexture("_BaseMap", color);
                material.SetTexture("_BumpMap", normalMap);
                if (normalMap != null) material.EnableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(material);
            }

            bool dirty = false;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (!(o is Material embedded) || embedded == material) continue;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), material);
                dirty = true;
            }
            if (dirty) importer.SaveAndReimport();
            return true;
        }

        /// <summary>Idle(기본) ↔ Walk 컨트롤러 (bool Moving). 있으면 상태 · 전이만 다시 맞춘다 (GUID 유지).</summary>
        private static AnimatorController EnsurePlayerController(AnimationClip idle, AnimationClip walk)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerControllerPath);
            bool hasParam = false;
            foreach (var p in controller.parameters) hasParam |= p.name == MovingParameter && p.type == AnimatorControllerParameterType.Bool;
            if (!hasParam) controller.AddParameter(MovingParameter, AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;
            var idleState = FindOrAddState(machine, "Idle", idle);
            var walkState = FindOrAddState(machine, "Walk", walk);
            walkState.speed = WalkSpeed;
            machine.defaultState = idleState;
            EnsureTransition(idleState, walkState, AnimatorConditionMode.If);
            EnsureTransition(walkState, idleState, AnimatorConditionMode.IfNot);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState FindOrAddState(AnimatorStateMachine machine, string name, Motion motion)
        {
            foreach (var child in machine.states)
            {
                if (child.state.name != name) continue;
                child.state.motion = motion;
                return child.state;
            }
            var state = machine.AddState(name);
            state.motion = motion;
            return state;
        }

        private static void EnsureTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode)
        {
            // 이미 표대로면 그대로 둔다 — 다시 만들면 컨트롤러 파일이 실행할 때마다 바뀐다
            foreach (var t in from.transitions)
            {
                if (t.destinationState != to) continue;
                var c = t.conditions;
                if (!t.hasExitTime && Mathf.Approximately(t.duration, BlendSeconds) && c.Length == 1 && c[0].mode == mode && c[0].parameter == MovingParameter)
                    return;
            }
            foreach (var t in from.transitions)
                if (t.destinationState == to) from.RemoveTransition(t); // 조건 · 시간을 표대로 다시
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = BlendSeconds;
            transition.AddCondition(mode, 0f, MovingParameter);
        }

        /// <summary>모델 루트(FBX, Humanoid라 Animator가 있음)에 컨트롤러를 붙이고 Idle 첫 자세로 둔다 — 지팡이를 그 자세에서 세워 붙이기 위해.</summary>
        private static void SetupAnimator(GameObject sheep, AnimatorController controller, AnimationClip idle)
        {
            if (controller == null) return;
            var animator = sheep.GetComponent<Animator>();
            if (animator == null) animator = sheep.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false; // 위치 · 방향은 EntityPresenter
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            if (idle == null || animator.avatar == null) return;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            var graph = PlayableGraph.Create("IdlePose");
            try
            {
                var output = AnimationPlayableOutput.Create(graph, "Pose", animator);
                var playable = AnimationClipPlayable.Create(graph, idle);
                playable.SetApplyFootIK(false);
                output.SetSourcePlayable(playable);
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                graph.Play();
                playable.SetTime(0);
                graph.Evaluate(0f);
            }
            finally
            {
                graph.Destroy();
            }
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        /// <summary>지팡이를 오른손 뼈에 붙인다: 세워 든 채, 손이 지팡이 길이의 StaffGripRatio 지점에 오게.</summary>
        private static void AttachStaff(GameObject sheep) => AttachStaff(sheep, Staff);

        /// <summary>
        /// 지팡이(Varco 소품, 높이 1 m · 위 = +Y)를 오른손 뼈에 세워 붙인다 — spec.Scale = 게임 안 길이(m), 손이 길이의 StaffGripRatio 지점.
        /// 2026-11-05: 구역 적 · 보스의 지팡이도 몸과 따로 만들어 여기서 붙인다 (사용자 결정 — 몸 모델에 들고 있는 물건을 넣지 않음).
        /// </summary>
        private static void AttachStaff(GameObject sheep, ModelSpec spec)
        {
            var model = LoadModel(spec.Model);
            var hand = FindHand(sheep);
            if (model == null || hand == null)
            {
                Debug.LogWarning("[VarcoModelSetup] 지팡이(" + spec.Model + ") 또는 손 뼈(" + StaffHandBone + ") 없음 — 지팡이 생략");
                return;
            }
            var staff = (GameObject)PrefabUtility.InstantiatePrefab(model);
            staff.name = "Staff";
            StripColliders(staff);
            float length = spec.Scale; // 원본 1 m
            staff.transform.SetParent(hand, true);
            staff.transform.localScale = model.transform.localScale * (spec.Scale / Mathf.Max(1e-4f, hand.lossyScale.y));
            var upright = sheep.transform.parent; // 프리팹 루트 (Y-up)
            staff.transform.rotation = upright.rotation * Quaternion.Euler(0f, spec.Yaw, 0f) * model.transform.localRotation; // 세워서
            staff.transform.position = hand.position - upright.up * (length * StaffGripRatio);
        }

        /// <summary>
        /// 무기를 오른손 뼈에 붙인다 (3-7a): 지팡이와 같이 세워 들고, 손이 길이의 Grip 지점에 오게. 이름 "Weapon_&lt;아이템 ID&gt;", 처음엔 꺼 둠.
        /// </summary>
        private static void AttachWeapons(GameObject sheep)
        {
            var hand = FindHand(sheep);
            if (hand == null) return;
            var upright = sheep.transform.parent; // 프리팹 루트 (Y-up)
            foreach (var spec in WeaponSpecs)
            {
                var model = LoadModel(spec.Model);
                if (model == null) continue;
                var weapon = (GameObject)PrefabUtility.InstantiatePrefab(model);
                weapon.name = EntityPresenter.HeldWeaponPrefix + spec.ItemId;
                StripColliders(weapon);
                weapon.transform.SetParent(hand, true);
                weapon.transform.localScale = model.transform.localScale * (spec.Length / Mathf.Max(1e-4f, hand.lossyScale.y));
                var tilt = upright.rotation * Quaternion.Euler(spec.Tilt, 0f, 0f); // 날(+Y)을 앞(+Z)으로 Tilt도
                weapon.transform.rotation = tilt * model.transform.localRotation;
                // 원점이 가운데라 아래 끝 = 원점 − 길이/2 → 손이 아래 끝에서 길이 × Grip 지점에 오게 (날 방향 축을 따라)
                weapon.transform.position = hand.position - (tilt * Vector3.up) * (spec.Length * (spec.Grip - 0.5f));
                weapon.SetActive(false);
            }
        }

        /// <summary>
        /// 바닥에 놓을 무기 프리팹 (3-7a): Prefabs/Varco/Weapon_&lt;모델&gt; — 세운 모습, 원점 가운데, 길이 = 표 값. ItemVisualCatalog에 연결.
        /// </summary>
        private static void BuildWeaponPrefabs()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(ItemVisualCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemVisualCatalog>();
                AssetDatabase.CreateAsset(catalog, ItemVisualCatalogPath);
            }
            foreach (var spec in WeaponSpecs)
            {
                var prefab = BuildPrefab(new ModelSpec(spec.Model, "Weapon_" + spec.Model.Replace("Prop_Weapon_", ""), spec.Length, 0f), null);
                if (prefab != null) catalog.Set(spec.ItemId, prefab);
            }
            EditorUtility.SetDirty(catalog);
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

        /// <summary>오른손 뼈 — Humanoid 아바타가 있으면 그 매핑(Mixamo는 `mixamorig:RightHand`), 없으면 이름으로.</summary>
        private static Transform FindHand(GameObject model)
        {
            var animator = model.GetComponent<Animator>();
            var hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            return hand != null ? hand : FindDeep(model.transform, StaffHandBone);
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
