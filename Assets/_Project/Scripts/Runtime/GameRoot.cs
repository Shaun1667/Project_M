using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Input;
using ProjectM.Magic;
using ProjectM.Presentation;
using ProjectM.UI;
using ProjectM.Units;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = ProjectM.Core.EntityId; // Unity 6의 UnityEngine.EntityId와 구분

namespace ProjectM
{
    /// <summary>
    /// 씬 컴포지션 루트. 순수 로직 객체를 만들고 표현 · 입력 어댑터에 연결한다 (CLAUDE.md: 어댑터는 씬 컴포지션 루트 하나에서 연결).
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameConfigAsset _config;
        [SerializeField] private DungeonGenConfigAsset _dungeonConfig;
        [SerializeField] private UnitDefAsset _playerDef;
        [SerializeField] private UnitDefAsset _enemyDef;
        [SerializeField] private SpawnConfigAsset _spawnConfig;
        [SerializeField] private MagicConfigAsset _magicConfig;
        [SerializeField] private SpellCatalogAsset _spellCatalog;
        [SerializeField] private TilemapPresenter _tilemapPresenter;
        [SerializeField] private EntityPresenter _entityPresenter;
        [Tooltip("시야 안개. 비우면 안개 없이 전부 보인다")]
        [SerializeField] private FogPresenter _fogPresenter;
        [SerializeField] private PlayerInputController _input;
        [SerializeField] private HudPresenter _hud;
        [SerializeField] private CameraController _camera;

        [Header("시드 — 0이면 매번 새로 정한다. 같은 맵을 다시 보려면 콘솔에 찍힌 시드를 넣는다")]
        [SerializeField] private long _fixedSeed;

        public GameConfig Config { get; private set; }
        public MagicConfig Magic { get; private set; }
        public SpellBook Spells { get; private set; }
        public World World { get; private set; }
        public DungeonFloor Floor { get; private set; }
        /// <summary>이번 판의 시드 (디버그 창 · 콘솔 표시용).</summary>
        public ulong Seed { get; private set; }
        public EntityId PlayerId => _playerId;
        public bool PlayerDead => _playerDead;

        /// <summary>
        /// 디버그: 맵 전체 밝히기 (안개 숨김 + 모든 엔티티 표시). 표시만 바꾸며 게임 상태 · 시드 재현에는 영향이 없다.
        /// 새 판(R)에도 유지된다. 디버그 창(ProjectM/Debug/Debug Window)에서 켜고 끈다.
        /// </summary>
        public bool DebugRevealAll
        {
            get => _entityPresenter != null && _entityPresenter.RevealAll;
            set
            {
                if (_entityPresenter != null) _entityPresenter.RevealAll = value;
                if (_fogPresenter != null) _fogPresenter.RevealAll = value;
            }
        }

        private EntityId _playerId;
        private bool _playerDead;

        private void Awake()
        {
            if (_config == null || _dungeonConfig == null || _playerDef == null || _enemyDef == null || _spawnConfig == null || _magicConfig == null
                || _spellCatalog == null || _tilemapPresenter == null || _entityPresenter == null || _input == null || _hud == null)
            {
                Debug.LogError("[GameRoot] 참조가 비어 있다. 메뉴 ProjectM/Setup/Main Scene을 실행한다.", this);
                enabled = false;
                return;
            }

            Config = _config.ToConfig();
            Magic = _magicConfig.ToConfig();
            Spells = _spellCatalog.ToBook();
            foreach (var id in _playerDef.ToDef().StartingSpells)
            {
                if (!Spells.TryGet(id, out _))
                    Debug.LogWarning($"[GameRoot] 시작 주문 '{id}'가 SpellCatalog에 없다 — 그 칸은 쓸 수 없다.", this);
            }
            _input.HintChanged += _hud.SetHint;
        }

        private void OnDestroy()
        {
            if (_input != null && _hud != null) _input.HintChanged -= _hud.SetHint;
        }

        private void Start()
        {
            if (Config == null) return;
            StartNewRun();
        }

        private void Update()
        {
            // 입력만 읽는다 — 규칙은 틱으로만 진행한다.
            var keyboard = Keyboard.current;
            if (_playerDead && keyboard != null && keyboard.rKey.wasPressedThisFrame)
                StartNewRun();
        }

        private void StartNewRun()
        {
            ulong seed = _fixedSeed != 0 ? (ulong)_fixedSeed : (ulong)System.DateTime.UtcNow.Ticks;
            Seed = seed;
            Debug.Log($"[GameRoot] 시드 {(long)seed}");

            if (World != null)
            {
                World.Events.Unsubscribe<EntityMovedEvent>(OnEntityMoved);
                World.Events.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            }

            var generator = new DungeonGenerator(_dungeonConfig.ToConfig());
            DungeonFloor floor = null;
            World = World.Create(seed, rng =>
            {
                floor = generator.Generate(rng);
                return floor.Map;
            });
            Floor = floor;
            var sight = new ShadowcastSightRule();
            var vision = World.InstallCoreSystems(sight, Magic);

            _tilemapPresenter.Bind(World.Events);
            _entityPresenter.Bind(World.Events);
            if (_fogPresenter != null) _fogPresenter.Bind(World.Events);
            World.Events.Subscribe<EntityMovedEvent>(OnEntityMoved);
            World.Events.Subscribe<UnitDiedEvent>(OnUnitDied);

            var enemyAi = new MeleeChaserAi(World, Config, sight);
            World.Events.Publish(new MapLoadedEvent(World.Map));
            var player = FloorPopulator.Populate(World, Floor.Start, _playerDef.ToDef(), _enemyDef.ToDef(), _spawnConfig.ToConfig(), enemyAi,
                Magic.SpellSlotCount);
            _playerId = player.Id;
            _playerDead = false;
            vision.Refresh(); // 첫 시야 — 이후는 매 틱 ⑥ 단계에서

            _hud.Bind(World.Events, player, Spells);
            _input.Bind(World, Config, new TurnClock(World.Scheduler, World.PlayerInput), sight, Spells, new BresenhamTargetingRule());
            World.Events.Flush();

            if (_camera != null)
                _camera.SnapToTarget(_entityPresenter.CellCenter(Floor.Start));
        }

        private void OnEntityMoved(EntityMovedEvent e)
        {
            if (e.Id == _playerId && _camera != null)
                _camera.SnapToTarget(_entityPresenter.CellCenter(e.To));
        }

        private void OnUnitDied(UnitDiedEvent e)
        {
            if (e.Id != _playerId) return;
            _playerDead = true;
            _input.Unbind();
            Debug.Log("[GameRoot] 플레이어 사망 — R로 새 판");
        }
    }
}
