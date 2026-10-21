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
            if (controller == null) Debug.LogWarning("[VarcoModelSetup] Animation/Idle.fbx · Walk.fbx 없음 — 플레이어 애니메이션 생략 (코드 움직임만)");

            var player = BuildPrefab(Player, sheep =>
            {
                SetupAnimator(sheep, controller, idle);
                AttachStaff(sheep);
                AttachWeapons(sheep); // 3-7a: 손에 무기 6종을 꺼 둔 채 붙임 — 들면 EntityPresenter가 켜고 지팡이를 숨김
            });
            catalog.SetPrefab("player", player);
            foreach (var id in new[] { "hero_blond", "hero_black" }) catalog.SetPrefab(id, player); // 2026-10-21 캐릭터 선택 — 픽셀 전환 전까지 같은 모델
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

            BuildWeaponPrefabs();
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

        /// <summary>
        /// 무기를 오른손 뼈에 붙인다 (3-7a): 지팡이와 같이 세워 들고, 손이 길이의 Grip 지점에 오게. 이름 "Weapon_&lt;아이템 ID&gt;", 처음엔 꺼 둠.
        /// </summary>
        private static void AttachWeapons(GameObject sheep)
        {
            var hand = FindDeep(sheep.transform, StaffHandBone);
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
