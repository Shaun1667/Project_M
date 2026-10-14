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
            Assert.That(view.sprite, Is.SameAs(_sprite));
            Assert.That(view.transform.position, Is.EqualTo(_grid.GetCellCenterWorld(new Vector3Int(2, 3, 0))));

            _bus.Publish(new EntityMovedEvent(id, new GridPos(2, 3), new GridPos(3, 3)));
            _bus.Flush();
            Assert.That(view.transform.position, Is.EqualTo(_grid.GetCellCenterWorld(new Vector3Int(3, 3, 0))));

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
    }
}
