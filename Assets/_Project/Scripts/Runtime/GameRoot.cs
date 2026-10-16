using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Presentation;
using UnityEngine;

namespace ProjectM
{
    /// <summary>
    /// 씬 컴포지션 루트. 순수 로직 객체를 만들고 표현 어댑터에 연결한다 (CLAUDE.md: 어댑터는 씬 컴포지션 루트 하나에서 연결).
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameConfigAsset _config;
        [SerializeField] private DungeonGenConfigAsset _dungeonConfig;
        [SerializeField] private TilemapPresenter _tilemapPresenter;
        [SerializeField] private EntityPresenter _entityPresenter;
        [SerializeField] private CameraController _camera;

        [Header("시드 — 0이면 매번 새로 정한다. 같은 맵을 다시 보려면 콘솔에 찍힌 시드를 넣는다")]
        [SerializeField] private long _fixedSeed;

        public GameConfig Config { get; private set; }
        public World World { get; private set; }
        public DungeonFloor Floor { get; private set; }

        private void Awake()
        {
            if (_config == null || _dungeonConfig == null || _tilemapPresenter == null || _entityPresenter == null)
            {
                Debug.LogError("[GameRoot] 참조가 비어 있다. 메뉴 ProjectM/Setup/Main Scene을 실행한다.", this);
                enabled = false;
                return;
            }

            Config = _config.ToConfig();
        }

        private void Start()
        {
            if (Config == null) return;
            StartNewRun();
        }

        private void StartNewRun()
        {
            ulong seed = _fixedSeed != 0 ? (ulong)_fixedSeed : (ulong)System.DateTime.UtcNow.Ticks;
            Debug.Log($"[GameRoot] 시드 {(long)seed}");

            var generator = new DungeonGenerator(_dungeonConfig.ToConfig());
            DungeonFloor floor = null;
            World = World.Create(seed, rng =>
            {
                floor = generator.Generate(rng);
                return floor.Map;
            });
            Floor = floor;

            _tilemapPresenter.Bind(World.Events);
            _entityPresenter.Bind(World.Events);

            // 플레이어는 지금 모습만 둔다. 스케줄러 등록 · 이동은 1-6.
            World.Events.Publish(new MapLoadedEvent(World.Map));
            World.Events.Publish(new EntitySpawnedEvent(World.Ids.Allocate(), Floor.Start, "player"));
            World.Events.Flush();

            if (_camera != null)
                _camera.SnapToTarget(_entityPresenter.CellCenter(Floor.Start));
        }
    }
}
