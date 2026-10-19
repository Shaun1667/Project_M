using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Presentation;
using UnityEngine;
using UnityEngine.Tilemaps;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.Tests
{
    public class PresenterTests
    {
        private GameObject _root;
        private Grid _grid;
        private Tilemap _tilemap;
        private Tile _floorTile;
        private Tile _wallTile;
        private TileVisualSet _tileVisuals;
        private EntityVisualCatalog _catalog;
        private Sprite _sprite;
        private Texture2D _texture;
        private SimEventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PresenterTestRoot");
            _grid = _root.AddComponent<Grid>();
            var tilemapGo = new GameObject("Tilemap");
            tilemapGo.transform.SetParent(_root.transform, false);
            _tilemap = tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();

            _floorTile = ScriptableObject.CreateInstance<Tile>();
            _wallTile = ScriptableObject.CreateInstance<Tile>();
            _tileVisuals = ScriptableObject.CreateInstance<TileVisualSet>();
            _tileVisuals.Set(TileType.Floor, _floorTile);
            _tileVisuals.Set(TileType.Wall, _wallTile);

            _texture = new Texture2D(4, 4);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            _catalog = ScriptableObject.CreateInstance<EntityVisualCatalog>();
            _catalog.Set("player", _sprite, Color.white);

            _bus = new SimEventBus();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_floorTile);
            Object.DestroyImmediate(_wallTile);
            Object.DestroyImmediate(_tileVisuals);
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        private TilemapPresenter CreateTilemapPresenter()
        {
            var presenter = _root.AddComponent<TilemapPresenter>();
            presenter.Configure(_tilemap, _tileVisuals);
            presenter.Bind(_bus);
            return presenter;
        }

        private EntityPresenter CreateEntityPresenter()
        {
            var go = new GameObject("Entities");
            go.transform.SetParent(_root.transform, false);
            var presenter = go.AddComponent<EntityPresenter>();
            presenter.Configure(_grid, _catalog);
            presenter.Bind(_bus);
            return presenter;
        }

        [Test]
        public void MapLoaded_DrawsTilesByType_EmptyLeftBlank()
        {
            CreateTilemapPresenter();
            var map = new GridMap(3, 2);
            map.Set(new GridPos(0, 0), TileType.Wall);
            map.Set(new GridPos(2, 1), TileType.Floor);

            _bus.Publish(new MapLoadedEvent(map));
            _bus.Flush();

            Assert.That(_tilemap.GetTile(new Vector3Int(0, 0, 0)), Is.SameAs(_wallTile));
            Assert.That(_tilemap.GetTile(new Vector3Int(2, 1, 0)), Is.SameAs(_floorTile), "x · y 방향 일치");
            Assert.That(_tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.Null, "Empty는 그리지 않음");
        }

        [Test]
        public void MapLoaded_ReplacesPreviousMap()
        {
            CreateTilemapPresenter();
            var big = new GridMap(5, 5);
            big.Set(new GridPos(4, 4), TileType.Wall);
            _bus.Publish(new MapLoadedEvent(big));
            _bus.Publish(new MapLoadedEvent(new GridMap(2, 2)));
            _bus.Flush();

            Assert.That(_tilemap.GetTile(new Vector3Int(4, 4, 0)), Is.Null);
        }

        [Test]
        public void TileChanged_UpdatesOneTile()
        {
            CreateTilemapPresenter();
            _bus.Publish(new MapLoadedEvent(new GridMap(2, 2)));
            _bus.Publish(new TileChangedEvent(new GridPos(1, 0), TileType.Wall));
            _bus.Flush();

            Assert.That(_tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(_wallTile));
        }

        [Test]
        public void Entity_SpawnMoveRemove_FollowsCellCenters()
        {
            var presenter = CreateEntityPresenter();
            var id = new EntityId(7);

            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(2, 3), "player"));
            _bus.Flush();
            Assert.That(presenter.TryGetView(id, out var view), Is.True);
            Assert.That(view.Renderers.Length, Is.GreaterThan(0), "프리팹이 없으면 그레이박스 캡슐 (G-1)");
            Assert.That(view.Root.GetComponentInChildren<Collider>(), Is.Null, "충돌체 없음 (규칙 2)");
            Assert.That(view.Root.position, Is.EqualTo(_grid.GetCellCenterWorld(new Vector3Int(2, 3, 0))));

            _bus.Publish(new EntityMovedEvent(id, new GridPos(2, 3), new GridPos(3, 3)));
            _bus.Flush();
            presenter.FinishMotions(); // G-2b: 이동은 보간 — 끝나면 칸 중앙
            Assert.That(view.Root.position, Is.EqualTo(_grid.GetCellCenterWorld(new Vector3Int(3, 3, 0))));

            _bus.Publish(new EntityRemovedEvent(id));
            _bus.Flush();
            Assert.That(presenter.TryGetView(id, out _), Is.False);
            Assert.That(presenter.ViewCount, Is.EqualTo(0));
        }

        [Test]
        public void Unbind_StopsUpdates()
        {
            var presenter = CreateEntityPresenter();
            presenter.Unbind();

            _bus.Publish(new EntitySpawnedEvent(new EntityId(1), new GridPos(0, 0), "player"));
            _bus.Flush();
            Assert.That(presenter.ViewCount, Is.EqualTo(0));
        }

        // ── 1-7 시야 표시 · 디버그 맵 전체 밝히기 ──

        private static ProjectM.Units.VisibilityChangedEvent Visibility(int w, int h, params GridPos[] visible)
        {
            var v = new bool[w * h];
            foreach (var p in visible) v[p.Y * w + p.X] = true;
            return new ProjectM.Units.VisibilityChangedEvent(w, h, v, (bool[])v.Clone());
        }

        [Test]
        public void Entity_HiddenOutsideVisibility_RevealAllShowsAndRestores()
        {
            var presenter = CreateEntityPresenter();
            var id = new EntityId(3);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(4, 0), "player"));
            _bus.Publish(Visibility(5, 1, new GridPos(0, 0)));
            _bus.Flush();
            presenter.TryGetView(id, out var view);
            Assert.That(view.Visible, Is.False, "시야 밖이면 숨김");
            Assert.That(view.Renderers[0].enabled, Is.False);

            presenter.RevealAll = true;
            Assert.That(view.Visible, Is.True, "디버그 밝히기");
            presenter.RevealAll = false;
            Assert.That(view.Visible, Is.False, "끄면 시야대로");
        }

        // ── G-1 3D (결정 2026-10-18) ──

        // 벽은 3D 블록: 걸을 수 있는 칸과 이웃한 벽만, Tilemap에는 안 그림. 안 본 벽 = 숨김, 본 적 있는 벽 = 보이되 어둡게, 밝히기 = 전부
        [Test]
        public void Walls3D_OnlyEdgeWalls_FollowVisibility()
        {
            var presenter = CreateTilemapPresenter();
            presenter.ConfigureWalls(true);
            var wide = new GridMap(7, 3); // 바닥 (1,1) ~ (3,1), 나머지 벽
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 7; x++)
                    wide.Set(new GridPos(x, y), x >= 1 && x <= 3 && y == 1 ? TileType.Floor : TileType.Wall);
            _bus.Publish(new MapLoadedEvent(wide));
            _bus.Flush();

            Assert.That(_tilemap.GetTile(new Vector3Int(0, 0, 0)), Is.Null, "벽은 Tilemap에 안 그림");
            Assert.That(_tilemap.GetTile(new Vector3Int(2, 1, 0)), Is.SameAs(_floorTile));
            Assert.That(presenter.TryGetWall(new GridPos(0, 1), out _), Is.True, "바닥 옆 벽");
            Assert.That(presenter.TryGetWall(new GridPos(6, 1), out _), Is.False, "안쪽 벽(바닥과 안 닿음)은 생략");
            Assert.That(presenter.WallCount, Is.EqualTo(5 * 2 + 2), "바닥 3칸을 둘러싼 벽 12");
            presenter.TryGetWall(new GridPos(0, 1), out var renderers);
            Assert.That(renderers[0].GetComponent<Collider>(), Is.Null, "충돌체 없음");

            var visible = new bool[7 * 3];
            var explored = new bool[7 * 3];
            explored[1 * 7 + 0] = true; // (0,1)은 본 적만 있음
            _bus.Publish(new ProjectM.Units.VisibilityChangedEvent(7, 3, visible, explored));
            _bus.Flush();
            Assert.That(renderers[0].enabled, Is.True, "본 적 있는 벽은 보임 (어둡게)");
            presenter.TryGetWall(new GridPos(4, 1), out var unseen);
            Assert.That(unseen[0].enabled, Is.False, "안 본 벽은 숨김");
            presenter.RevealAll = true;
            Assert.That(unseen[0].enabled, Is.True, "디버그 밝히기");
        }

        [Test]
        public void Entity_FacesMoveDirection_TintFromStatus()
        {
            var presenter = CreateEntityPresenter();
            var id = new EntityId(5);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(2, 2), "player"));
            _bus.Publish(new EntityMovedEvent(id, new GridPos(2, 2), new GridPos(3, 2)));
            _bus.Flush();
            presenter.TryGetView(id, out var view);
            var to = _grid.GetCellCenterWorld(new Vector3Int(3, 2, 0)) - _grid.GetCellCenterWorld(new Vector3Int(2, 2, 0));
            Assert.That(Vector3.Angle(view.Root.forward, new Vector3(to.x, 0f, to.z)), Is.LessThan(1f), "이동 방향을 바라봄");

            _bus.Publish(new ProjectM.Status.StatusChangedEvent(id, ProjectM.Status.StatusType.Burn, 3, true));
            _bus.Flush();
            Assert.That(presenter.CurrentTint(id), Is.Not.EqualTo(Color.white), "화상 틴트");
        }

        // 화면 → 바닥 평면 교점 → 칸: X축 90° 돈 Grid에서 칸 중앙을 바로 위에서 보면 그 칸
        [Test]
        public void ScreenToGround_ThroughRotatedGrid_GivesCell()
        {
            _root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var center = _grid.GetCellCenterWorld(new Vector3Int(4, 7, 0));
            Assert.That(center.y, Is.EqualTo(0f).Within(1e-4f), "칸은 바닥(y = 0)에");
            Assert.That(new Vector2(center.x, center.z), Is.EqualTo(new Vector2(4.5f, 7.5f)));

            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(_root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            camGo.transform.SetPositionAndRotation(center + new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
            var screen = cam.WorldToScreenPoint(center);
            Assert.That(CameraController.ScreenToGround(cam, new Vector2(screen.x, screen.y), out var ground), Is.True);
            Assert.That(_grid.WorldToCell(ground), Is.EqualTo(new Vector3Int(4, 7, 0)));
        }

        // ── 둘러보기 · 확대 축소 (2026-10-19 사용자 요청) ──

        private CameraController Rig()
        {
            var camGo = new GameObject("Rig");
            camGo.transform.SetParent(_root.transform, false);
            camGo.AddComponent<Camera>();
            var rig = camGo.AddComponent<CameraController>();
            rig.Apply();
            rig.SetPanBounds(20f, 10f);
            rig.SnapToTarget(new Vector3(5.5f, 0f, 5.5f));
            return rig;
        }

        [Test]
        public void Camera_PanBy_MovesLookTarget_ClampedToMap_FreeLook()
        {
            var rig = Rig();
            rig.PanBy(new Vector3(3f, 7f, 2f));
            Assert.That(rig.LookTarget, Is.EqualTo(new Vector3(8.5f, 0f, 7.5f)), "높이는 무시");
            Assert.That(rig.IsFreeLook, Is.True);
            float distance = Vector3.Distance(rig.transform.position, rig.LookTarget);
            Assert.That(distance, Is.EqualTo(rig.Distance).Within(1e-3f), "보는 곳에서 같은 거리");
            rig.PanBy(new Vector3(100f, 0f, -100f));
            Assert.That(rig.LookTarget, Is.EqualTo(new Vector3(20f, 0f, 0f)), "맵 밖으로 안 나감");
        }

        [Test]
        public void Camera_FollowTarget_EndsFreeLook()
        {
            var rig = Rig();
            rig.PanBy(new Vector3(4f, 0f, 0f));
            rig.FollowTarget(new Vector3(6.5f, 0f, 5.5f));
            Assert.That(rig.IsFreeLook, Is.False, "플레이어가 움직이면 다시 따라감 (사용자 요청)");
            Assert.That(rig.LookTarget, Is.EqualTo(new Vector3(6.5f, 0f, 5.5f)));
        }

        [Test]
        public void Camera_Zoom_ClampsDistance_KeepsTarget()
        {
            var rig = Rig();
            float start = rig.Distance;
            rig.Zoom(1f);
            Assert.That(rig.Distance, Is.LessThan(start), "휠 올림 = 확대(가까이)");
            rig.Zoom(-2f);
            Assert.That(rig.Distance, Is.GreaterThan(start), "휠 내림 = 축소(멀리)");
            for (int i = 0; i < 50; i++) rig.Zoom(-1f);
            float far = rig.Distance;
            rig.Zoom(-1f);
            Assert.That(rig.Distance, Is.EqualTo(far), "최대 거리에서 멈춤");
            for (int i = 0; i < 50; i++) rig.Zoom(1f);
            Assert.That(rig.Distance, Is.GreaterThanOrEqualTo(1f));
            Assert.That(rig.LookTarget, Is.EqualTo(new Vector3(5.5f, 0f, 5.5f)), "보는 곳은 그대로");
            Assert.That(Vector3.Distance(rig.transform.position, rig.LookTarget), Is.EqualTo(rig.Distance).Within(1e-3f));
        }

        // ── G-2a Varco 모델 연결 (결정 2026-10-18) ──

        /// <summary>시험용 모듈 "프리팹": 충돌체 없는 큐브 (원점 = 바닥 중앙, 키 height).</summary>
        private GameObject TestModule(string name, float height, bool sunk = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_root.transform, false);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.SetParent(root.transform, false);
            cube.transform.localScale = new Vector3(1f, height, 1f);
            cube.transform.localPosition = new Vector3(0f, sunk ? -height * 0.5f : height * 0.5f, 0f);
            return root;
        }

        // 근접 적 = 토끼 · 쥐 · 슬라임 중 엔티티 ID로 고정 (표시만, 난수 안 씀). 변형이 없으면 Prefab
        [Test]
        public void Catalog_PickPrefab_VariantFixedById()
        {
            var a = TestModule("A", 1f);
            var b = TestModule("B", 1f);
            var single = TestModule("Single", 1f);
            _catalog.SetPrefab("enemy", single);
            _catalog.TryGet("enemy", out var plain);
            Assert.That(EntityVisualCatalog.PickPrefab(plain, 4), Is.SameAs(single), "변형 없으면 Prefab");

            _catalog.SetVariants("enemy", new[] { a, b });
            _catalog.TryGet("enemy", out var entry);
            Assert.That(EntityVisualCatalog.PickPrefab(entry, 4), Is.SameAs(a));
            Assert.That(EntityVisualCatalog.PickPrefab(entry, 5), Is.SameAs(b));
            Assert.That(EntityVisualCatalog.PickPrefab(entry, 5), Is.SameAs(b), "같은 ID = 늘 같은 모습");
        }

        // 프리팹 모델은 텍스처 그대로 (카탈로그 색을 곱하지 않음) — 상태 틴트만
        [Test]
        public void Entity_Prefab_NoCatalogTint()
        {
            _catalog.Set("enemy", _sprite, Color.red);
            _catalog.SetPrefab("enemy", TestModule("EnemyModel", 0.8f));
            var presenter = CreateEntityPresenter();
            var id = new EntityId(9);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(1, 1), "enemy"));
            _bus.Flush();
            Assert.That(presenter.CurrentTint(id), Is.EqualTo(Color.white));
        }

        // 바닥 · 계단은 모듈이 있으면 3D (Tilemap에는 안 그림), 외톨이 벽 = 기둥 + 아래 바닥, 낮은 바닥 판은 안개 덮개가 가리므로 시야와 무관
        [Test]
        public void Modules3D_FloorStairsPillar_LowModulesIgnoreVisibility()
        {
            _tileVisuals.SetModule(TileType.Floor, TestModule("Floor", 0.06f, sunk: true), randomRotation: true);
            _tileVisuals.SetModule(TileType.StairsDown, TestModule("Stairs", 0.4f));
            _tileVisuals.SetModule(TileType.Wall, TestModule("WallModule", 1f));
            _tileVisuals.SetPillarModule(TestModule("Pillar", 1f));
            var presenter = CreateTilemapPresenter();
            presenter.ConfigureWalls(true);

            var map = new GridMap(7, 5); // 둘레 벽, 안쪽 바닥, (3,2) 외톨이 벽, (5,2) 계단
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 7; x++)
                    map.Set(new GridPos(x, y), x == 0 || y == 0 || x == 6 || y == 4 ? TileType.Wall : TileType.Floor);
            map.Set(new GridPos(3, 2), TileType.Wall);
            map.Set(new GridPos(5, 2), TileType.StairsDown);
            _bus.Publish(new MapLoadedEvent(map));
            _bus.Flush();

            Assert.That(_tilemap.GetTile(new Vector3Int(1, 1, 0)), Is.Null, "모듈이 있는 바닥은 Tilemap에 안 그림");
            Assert.That(presenter.TryGetModule(new GridPos(1, 1), out var floor), Is.True);
            Assert.That(presenter.TryGetModule(new GridPos(5, 2), out _), Is.True, "계단 모듈");
            Assert.That(presenter.TryGetModule(new GridPos(3, 2), out var pillar), Is.True);
            Assert.That(pillar.name, Does.StartWith("Pillar"), "외톨이 벽 = 기둥");
            Assert.That(pillar.transform.childCount, Is.EqualTo(2), "기둥 + 아래 바닥");
            presenter.TryGetModule(new GridPos(0, 2), out var edge);
            Assert.That(edge.name, Does.StartWith("Wall"), "이어진 벽 = 벽 모듈");
            Assert.That(presenter.WallCount, Is.EqualTo(7 * 2 + 3 * 2 + 1), "둘레 20 + 기둥 1");

            var none = new bool[7 * 5];
            _bus.Publish(new ProjectM.Units.VisibilityChangedEvent(7, 5, none, (bool[])none.Clone()));
            _bus.Flush();
            Assert.That(floor.GetComponentInChildren<Renderer>().enabled, Is.True, "낮은 바닥 판은 숨기지 않음 (안개가 가림)");
            presenter.TryGetModule(new GridPos(5, 2), out var stairs);
            Assert.That(stairs.GetComponentInChildren<Renderer>().enabled, Is.False, "높은 모듈(계단)은 안 본 칸이면 숨김");
        }

        // 벽이 바뀌면 이웃의 모양도 다시 고른다: 기둥 옆에 벽이 생기면 둘 다 벽 블록
        [Test]
        public void Modules3D_TileChanged_RebuildsNeighbours()
        {
            _tileVisuals.SetModule(TileType.Wall, TestModule("WallModule", 1f));
            _tileVisuals.SetPillarModule(TestModule("Pillar", 1f));
            var presenter = CreateTilemapPresenter();
            presenter.ConfigureWalls(true);
            var map = new GridMap(5, 3);
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 3; y++)
                    map.Set(new GridPos(x, y), TileType.Floor);
            map.Set(new GridPos(2, 1), TileType.Wall);
            _bus.Publish(new MapLoadedEvent(map));
            _bus.Flush();
            presenter.TryGetModule(new GridPos(2, 1), out var before);
            Assert.That(before.name, Does.StartWith("Pillar"));

            _bus.Publish(new TileChangedEvent(new GridPos(3, 1), TileType.Wall));
            _bus.Flush();
            presenter.TryGetModule(new GridPos(2, 1), out var after);
            Assert.That(after.name, Does.StartWith("Wall"), "이웃에 벽이 생겨 기둥 → 벽");
            Assert.That(presenter.WallCount, Is.EqualTo(2));
        }

        // ── G-2b 코드 움직임 (결정 2026-10-18) ──

        // 이동은 칸 사이를 보간 (로직은 기다리지 않음 — 목표는 바로 바뀜), 제자리 이동(층 이동)은 바로 옮김
        [Test]
        public void Motion_MoveInterpolates_SameCellSnaps()
        {
            var presenter = CreateEntityPresenter();
            var id = new EntityId(2);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(0, 0), "player"));
            _bus.Publish(new EntityMovedEvent(id, new GridPos(0, 0), new GridPos(1, 0)));
            _bus.Flush();
            presenter.TryGetView(id, out var view);
            var a = _grid.GetCellCenterWorld(new Vector3Int(0, 0, 0));
            var b = _grid.GetCellCenterWorld(new Vector3Int(1, 0, 0));
            Assert.That(view.IsSettled, Is.False);
            presenter.Advance(0.05f);
            Assert.That(view.Root.position.x, Is.GreaterThan(a.x).And.LessThan(b.x), "가는 중");
            presenter.FinishMotions();
            Assert.That(view.Root.position, Is.EqualTo(b));
            Assert.That(view.IsSettled, Is.True);

            _bus.Publish(new EntityMovedEvent(id, new GridPos(9, 9), new GridPos(9, 9)));
            _bus.Flush();
            Assert.That(view.Root.position, Is.EqualTo(_grid.GetCellCenterWorld(new Vector3Int(9, 9, 0))), "층 이동은 바로");
        }

        // 버그 수정 (2026-10-19): 직선으로 꾹 누르다 대각선으로 꺾으면 보이는 위치가 뒤처져 있어도 순간 이동하지 않고 이어서 보간
        [Test]
        public void Motion_DiagonalStepWhileLagging_DoesNotSnap()
        {
            var presenter = CreateEntityPresenter();
            var id = new EntityId(4);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(0, 0), "player"));
            _bus.Publish(new EntityMovedEvent(id, new GridPos(0, 0), new GridPos(1, 0)));
            _bus.Flush();
            presenter.TryGetView(id, out var view);
            presenter.Advance(0.02f); // 첫 걸음 보간이 거의 시작만 함 (보이는 위치 ≈ (0, 0))
            var shown = view.Root.position;
            _bus.Publish(new EntityMovedEvent(id, new GridPos(1, 0), new GridPos(2, 1))); // 대각선 — 보이는 곳에서 약 2.2칸
            _bus.Flush();
            Assert.That(view.IsSettled, Is.False, "순간 이동하지 않고 보간 중");
            Assert.That(Vector3.Distance(view.Root.position, shown), Is.LessThan(0.3f), "보이던 자리에서 이어서 출발");
            presenter.FinishMotions();
            Assert.That(view.Root.position, Is.EqualTo(_grid.GetCellCenterWorld(new Vector3Int(2, 1, 0))));
        }

        // 애니메이션 (2026-10-19): 걸어서 옮기면 Walk, 이동 + 유지 시간이 지나면 Idle, 제자리 이동(층 이동)은 걷지 않음
        [Test]
        public void Motion_WalkWhileMoving_IdleAfterHold_TeleportDoesNotWalk()
        {
            var presenter = CreateEntityPresenter();
            var id = new EntityId(3);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(0, 0), "player"));
            _bus.Flush();
            presenter.TryGetView(id, out var view);
            presenter.Advance(0.01f);
            Assert.That(view.Walking, Is.False, "처음엔 Idle");

            _bus.Publish(new EntityMovedEvent(id, new GridPos(0, 0), new GridPos(1, 0)));
            _bus.Flush();
            presenter.Advance(0.05f);
            Assert.That(view.Walking, Is.True, "걷는 중");
            presenter.Advance(0.2f); // 보간(0.12) 끝 · 유지 시간(0.2) 안
            Assert.That(view.Walking, Is.True, "이어 걸을 수 있게 잠깐 유지");
            presenter.Advance(0.5f);
            Assert.That(view.Walking, Is.False, "멈추면 Idle");

            _bus.Publish(new EntityMovedEvent(id, new GridPos(5, 5), new GridPos(5, 5)));
            _bus.Flush();
            presenter.Advance(0.01f);
            Assert.That(view.Walking, Is.False, "층 이동은 걷지 않음");
            Assert.That(view.Animator, Is.Null, "그레이박스 캡슐은 애니메이션 없음 — 코드 움직임만");
        }

        // 근접 공격 = 공격자가 대상 쪽으로 찔렀다 돌아옴, 대상은 번쩍(밝게)
        [Test]
        public void Motion_AttackLungesAttacker_FlashesTarget()
        {
            var presenter = CreateEntityPresenter();
            var attacker = new EntityId(1);
            var target = new EntityId(2);
            _bus.Publish(new EntitySpawnedEvent(attacker, new GridPos(0, 0), "player"));
            _bus.Publish(new EntitySpawnedEvent(target, new GridPos(1, 0), "player"));
            _bus.Publish(new ProjectM.Units.UnitDamagedEvent(target, attacker, 3, 5, 8, new DamageCause(DamageKind.Attack)));
            _bus.Flush();
            presenter.TryGetView(attacker, out var a);
            presenter.TryGetView(target, out var t);
            var home = _grid.GetCellCenterWorld(new Vector3Int(0, 0, 0));

            presenter.Advance(0.09f); // 찌르기 절반쯤
            Assert.That(a.Root.position.x, Is.GreaterThan(home.x + 0.1f), "대상 쪽으로 찌름");
            var block = new MaterialPropertyBlock();
            t.Renderers[0].GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").r, Is.GreaterThan(1f), "피격 번쩍");

            presenter.FinishMotions();
            Assert.That(a.Root.position, Is.EqualTo(home), "돌아옴");
            t.Renderers[0].GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").r, Is.EqualTo(1f).Within(1e-4f), "번쩍 끝");
        }

        // 죽은 유닛은 가라앉는 연출 뒤 지움 — 시뮬레이션 쪽으로는 이미 사라짐 (TryGetView false)
        [Test]
        public void Motion_DeathPlaysThenDestroys()
        {
            var presenter = CreateEntityPresenter();
            var id = new EntityId(4);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(2, 2), "player"));
            _bus.Flush();
            presenter.TryGetView(id, out var view);
            var root = view.Root.gameObject;

            _bus.Publish(new ProjectM.Units.UnitDiedEvent(id, ProjectM.Units.Faction.Enemy));
            _bus.Publish(new EntityRemovedEvent(id));
            _bus.Flush();
            Assert.That(presenter.TryGetView(id, out _), Is.False);
            Assert.That(presenter.DyingCount, Is.EqualTo(1));
            presenter.Advance(0.2f);
            Assert.That(root.transform.localScale.y, Is.LessThan(1f), "작아지는 중");

            presenter.FinishMotions();
            Assert.That(presenter.DyingCount, Is.EqualTo(0));
            Assert.That(root == null, Is.True, "연출 뒤 지움");
        }

        // 서는 높이: 올라가는 계단 위 유닛은 발밑이 올라감 (3D 모듈일 때만)
        [Test]
        public void Motion_StandHeightOnStairs()
        {
            _tileVisuals.SetModule(TileType.StairsUp, TestModule("StairsUp", 0.57f));
            _tileVisuals.SetStandHeight(TileType.StairsUp, 0.3f);
            var tiles = CreateTilemapPresenter();
            tiles.ConfigureWalls(true);
            var map = new GridMap(3, 1);
            map.Set(new GridPos(0, 0), TileType.Floor);
            map.Set(new GridPos(1, 0), TileType.StairsUp);
            _bus.Publish(new MapLoadedEvent(map));
            _bus.Flush();

            var go = new GameObject("Entities");
            go.transform.SetParent(_root.transform, false);
            var presenter = go.AddComponent<EntityPresenter>();
            presenter.Configure(_grid, _catalog, tiles);
            Assert.That(presenter.StandPoint(new GridPos(1, 0)) - presenter.CellCenter(new GridPos(1, 0)), Is.EqualTo(new Vector3(0f, 0.3f, 0f)));
            Assert.That(presenter.StandPoint(new GridPos(0, 0)), Is.EqualTo(presenter.CellCenter(new GridPos(0, 0))), "바닥은 0");
        }

        [Test]
        public void Fog_PaintsByVisibility_RevealAllHidesFogOnly()
        {
            var fogGo = new GameObject("Fog");
            fogGo.transform.SetParent(_root.transform, false);
            var fogMap = fogGo.AddComponent<Tilemap>();
            var fogRenderer = fogGo.AddComponent<TilemapRenderer>();
            var fog = fogGo.AddComponent<FogPresenter>();
            fog.Configure(fogMap, _floorTile);
            fog.Bind(_bus);

            _bus.Publish(new MapLoadedEvent(new GridMap(3, 1)));
            _bus.Publish(Visibility(3, 1, new GridPos(0, 0)));
            _bus.Flush();
            Assert.That(fogMap.GetTile(new Vector3Int(0, 0, 0)), Is.Null, "보이는 칸은 안개 없음");
            Assert.That(fogMap.GetTile(new Vector3Int(2, 0, 0)), Is.Not.Null, "안 본 칸은 안개");

            fog.RevealAll = true;
            Assert.That(fogRenderer.enabled, Is.False);
            _bus.Publish(Visibility(3, 1, new GridPos(2, 0)));
            _bus.Flush();
            Assert.That(fogMap.GetTile(new Vector3Int(2, 0, 0)), Is.Null, "밝힌 동안에도 안개는 계속 갱신");
            fog.RevealAll = false;
            Assert.That(fogRenderer.enabled, Is.True);
        }
    }
}
