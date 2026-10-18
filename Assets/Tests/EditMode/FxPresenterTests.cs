using NUnit.Framework;
using ProjectM.Core;
using ProjectM.Magic;
using ProjectM.Presentation;
using ProjectM.Status;
using ProjectM.Units;
using UnityEngine;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM.Tests
{
    /// <summary>G-3 이펙트: 이벤트 → 루프 · 한 번 재생 · 투사체 · 시야. 표시 전용이라 규칙은 건드리지 않는다.</summary>
    public class FxPresenterTests
    {
        private GameObject _root;
        private GameObject _prefab;
        private EntityVisualCatalog _visuals;
        private FxCatalog _catalog;
        private SimEventBus _bus;
        private EntityPresenter _entities;
        private FxPresenter _fx;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("FxTestRoot");
            var grid = _root.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            _root.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // G-1: 바닥에 눕힌 Grid

            _prefab = new GameObject("TestFx");
            _prefab.AddComponent<ParticleSystem>();
            _prefab.SetActive(false); // 프리팹 대신 — 복제본만 켠다
            _visuals = ScriptableObject.CreateInstance<EntityVisualCatalog>();
            _catalog = ScriptableObject.CreateInstance<FxCatalog>();
            foreach (var key in new[] { "terrain.FireZone", "status.Burn", "cast.Fire", "projectile.Fire", "impact.Fire", "burst.Poison", "reaction.toxic_ignite" })
                _catalog.Set(new FxCatalog.Entry { Key = key, Prefab = _prefab, Scale = 0.5f, Speed = 1f, Lifetime = key.StartsWith("terrain") || key.StartsWith("status") || key.StartsWith("projectile") ? 0f : 0.5f });

            _bus = new SimEventBus();
            var entitiesGo = new GameObject("Entities");
            entitiesGo.transform.SetParent(_root.transform, false);
            _entities = entitiesGo.AddComponent<EntityPresenter>();
            _entities.Configure(grid, _visuals);
            _entities.Bind(_bus);
            var fxGo = new GameObject("Fx");
            fxGo.transform.SetParent(_root.transform, false);
            _fx = fxGo.AddComponent<FxPresenter>();
            _fx.Configure(_catalog, _entities);
            _fx.Bind(_bus);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_prefab);
            Object.DestroyImmediate(_visuals);
            Object.DestroyImmediate(_catalog);
        }

        private static VisibilityChangedEvent Sight(int width, int height, params GridPos[] visible)
        {
            var seen = new bool[width * height];
            foreach (var p in visible) seen[p.Y * width + p.X] = true;
            return new VisibilityChangedEvent(width, height, seen, (bool[])seen.Clone());
        }

        [Test]
        public void TerrainEffect_PlacesLoop_AndStopsWhenGone()
        {
            var pos = new GridPos(3, 4);
            _bus.Publish(new TerrainChangedEvent(pos, TerrainEffectType.FireZone, 4));
            _bus.Flush();
            Assert.That(_fx.HasTerrainFx(pos, TerrainEffectType.FireZone), Is.True);
            Assert.That(_fx.TerrainFxCount, Is.EqualTo(1));

            _bus.Publish(new TerrainChangedEvent(pos, TerrainEffectType.FireZone, 3)); // 1 줄어듦 — 새로 만들지 않음
            _bus.Flush();
            Assert.That(_fx.TerrainFxCount, Is.EqualTo(1));

            _bus.Publish(new TerrainChangedEvent(pos, TerrainEffectType.FireZone, 0));
            _bus.Flush();
            Assert.That(_fx.HasTerrainFx(pos, TerrainEffectType.FireZone), Is.False);
            _fx.FinishTransients();
            Assert.That(_fx.TransientCount, Is.EqualTo(0), "꺼진 루프는 남은 파티클이 사라진 뒤 지운다");
        }

        [Test]
        public void TerrainWithoutCatalogKey_ShowsNothing()
        {
            _bus.Publish(new TerrainChangedEvent(new GridPos(1, 1), TerrainEffectType.Water, 4)); // 물은 바닥 무늬만
            _bus.Flush();
            Assert.That(_fx.TerrainFxCount, Is.EqualTo(0));
        }

        [Test]
        public void TerrainLoop_HiddenOutsideSight_UnlessRevealAll()
        {
            var seen = new GridPos(1, 1);
            var hidden = new GridPos(4, 4);
            _bus.Publish(Sight(6, 6, seen));
            _bus.Publish(new TerrainChangedEvent(seen, TerrainEffectType.FireZone, 4));
            _bus.Publish(new TerrainChangedEvent(hidden, TerrainEffectType.FireZone, 4));
            _bus.Flush();
            var visibleFx = _fx.transform.Find("terrain.FireZone");
            int active = 0;
            foreach (Transform child in _fx.transform)
                if (child.name == "terrain.FireZone" && child.gameObject.activeSelf) active++;
            Assert.That(visibleFx, Is.Not.Null);
            Assert.That(active, Is.EqualTo(1), "보이는 칸 하나만 켬");

            _fx.RevealAll = true;
            active = 0;
            foreach (Transform child in _fx.transform)
                if (child.name == "terrain.FireZone" && child.gameObject.activeSelf) active++;
            Assert.That(active, Is.EqualTo(2));
        }

        [Test]
        public void Status_AttachesToUnit_AndGoesWithIt()
        {
            var id = new EntityId(5);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(2, 2), "enemy_melee"));
            _bus.Publish(new StatusChangedEvent(id, StatusType.Burn, 3, true));
            _bus.Flush();
            Assert.That(_fx.HasStatusFx(id, StatusType.Burn), Is.True);
            Assert.That(_entities.TryGetView(id, out var view), Is.True);
            Assert.That(view.Root.Find("status.Burn"), Is.Not.Null, "유닛 모습 아래에 붙어 따라다님");

            _bus.Publish(new EntityRemovedEvent(id));
            _bus.Flush();
            Assert.That(_fx.StatusFxCount, Is.EqualTo(0));
        }

        [Test]
        public void StatusRefresh_DoesNotDuplicate_AndWearOffStops()
        {
            var id = new EntityId(6);
            _bus.Publish(new EntitySpawnedEvent(id, new GridPos(1, 1), "enemy_melee"));
            _bus.Publish(new StatusChangedEvent(id, StatusType.Burn, 3, true));
            _bus.Publish(new StatusChangedEvent(id, StatusType.Burn, 3, true));
            _bus.Flush();
            Assert.That(_fx.StatusFxCount, Is.EqualTo(1));
            _bus.Publish(new StatusChangedEvent(id, StatusType.Burn, 0, false));
            _bus.Flush();
            Assert.That(_fx.StatusFxCount, Is.EqualTo(0));
        }

        [Test]
        public void BoltSpell_FliesThenImpacts_AllCleanedUp()
        {
            var caster = new EntityId(1);
            _bus.Publish(new EntitySpawnedEvent(caster, new GridPos(0, 0), "player"));
            var path = new[] { new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0) };
            _bus.Publish(new SpellCastEvent(caster, "fire_basic", Element.Fire, new GridPos(3, 0), new SpellPreview(path, new[] { new GridPos(3, 0) })));
            _bus.Flush();
            Assert.That(_fx.transform.Find("projectile.Fire"), Is.Not.Null, "투사체");
            Assert.That(_fx.transform.Find("impact.Fire"), Is.Null, "도착 전에는 맞는 효과 없음");

            _fx.Advance(0.4f); // 최대 비행 0.35초
            Assert.That(_fx.transform.Find("impact.Fire"), Is.Not.Null, "도착하면 맞는 효과");

            _fx.FinishTransients();
            Assert.That(_fx.TransientCount, Is.EqualTo(0));
            Assert.That(_fx.transform.childCount, Is.EqualTo(0), "한 번 재생 · 투사체는 모두 지움");
        }

        [Test]
        public void ReactionWithCast_WaitsForProjectile()
        {
            var caster = new EntityId(1);
            _bus.Publish(new EntitySpawnedEvent(caster, new GridPos(0, 0), "player"));
            var path = new[] { new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0), new GridPos(4, 0) };
            _bus.Publish(new SpellCastEvent(caster, "fire_basic", Element.Fire, new GridPos(4, 0), new SpellPreview(path, new[] { new GridPos(4, 0) })));
            _bus.Publish(new ReactionTriggeredEvent("toxic_ignite", caster, new GridPos(4, 0), EntityId.None)); // 같은 순간
            _bus.Flush();
            Assert.That(_fx.transform.Find("reaction.toxic_ignite"), Is.Null, "투사체가 도착할 때까지 기다림");
            _fx.Advance(0.4f);
            Assert.That(_fx.transform.Find("reaction.toxic_ignite"), Is.Not.Null);
        }

        [Test]
        public void BurstSpell_UsesBurstKey_AtCenter()
        {
            var caster = new EntityId(1);
            _bus.Publish(new EntitySpawnedEvent(caster, new GridPos(0, 0), "player"));
            var path = new[] { new GridPos(1, 0), new GridPos(2, 0) };
            var area = new[] { new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0), new GridPos(2, 1) };
            _bus.Publish(new SpellCastEvent(caster, "poison_basic", Element.Poison, new GridPos(2, 0), new SpellPreview(path, area)));
            _bus.Flush();
            _fx.Advance(0.4f);
            var burst = _fx.transform.Find("burst.Poison");
            Assert.That(burst, Is.Not.Null, "3×3 = burst 키 (투사체 키가 없어도 바로)");
            Assert.That(burst.position.x, Is.EqualTo(2.5f).Within(0.001f), "중심 = 경로 끝 칸");
            Assert.That(burst.position.z, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void SpellOutOfSight_ShowsNothing()
        {
            var caster = new EntityId(9);
            _bus.Publish(Sight(8, 8, new GridPos(0, 7)));
            _bus.Publish(new EntitySpawnedEvent(caster, new GridPos(5, 5), "enemy_melee"));
            _bus.Publish(new SpellCastEvent(caster, "fire_basic", Element.Fire, new GridPos(6, 5), new SpellPreview(new[] { new GridPos(6, 5) }, new[] { new GridPos(6, 5) })));
            _bus.Flush();
            _fx.Advance(0.4f);
            Assert.That(_fx.transform.childCount, Is.EqualTo(0));
        }

        [Test]
        public void MapLoaded_ClearsEverything()
        {
            _bus.Publish(new TerrainChangedEvent(new GridPos(1, 1), TerrainEffectType.FireZone, 4));
            _bus.Flush();
            _bus.Publish(new MapLoadedEvent(new GridMap(4, 4)));
            _bus.Flush();
            Assert.That(_fx.TerrainFxCount, Is.EqualTo(0));
            Assert.That(_fx.TransientCount, Is.EqualTo(0));
            Assert.That(_fx.transform.childCount, Is.EqualTo(0));
        }

        [Test]
        public void Catalog_SetReplacesSameKey_AndRemoveExcept()
        {
            _catalog.Set(new FxCatalog.Entry { Key = "status.Burn", Prefab = _prefab, Scale = 2f });
            Assert.That(_catalog.TryGet("status.Burn", out var entry), Is.True);
            Assert.That(entry.Scale, Is.EqualTo(2f));
            _catalog.RemoveExcept(new[] { "status.Burn" });
            Assert.That(_catalog.Count, Is.EqualTo(1));
            Assert.That(_catalog.TryGet("terrain.FireZone", out _), Is.False);
        }
    }
}
