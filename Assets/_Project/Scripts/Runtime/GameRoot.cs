using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Dungeon;
using ProjectM.Input;
using ProjectM.Items;
using ProjectM.Magic;
using ProjectM.Presentation;
using ProjectM.Status;
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
        [Tooltip("마지막 층(보스 층)의 임시 보스 (3-4b)")] [SerializeField] private UnitDefAsset _bossDef;
        [SerializeField] private SpawnConfigAsset _spawnConfig;
        [SerializeField] private MagicConfigAsset _magicConfig;
        [SerializeField] private SpellCatalogAsset _spellCatalog;
        [SerializeField] private StatusCatalogAsset _statusCatalog;
        [SerializeField] private TerrainCatalogAsset _terrainCatalog;
        [SerializeField] private ReactionCatalogAsset _reactionCatalog;
        [SerializeField] private ItemCatalogAsset _itemCatalog;
        [SerializeField] private FusionCatalogAsset _fusionCatalog;
        [Tooltip("마법서 배치 수치 (3-5). 비우면 바닥에 마법서를 놓지 않는다")] [SerializeField] private LootConfigAsset _lootConfig;
        [Tooltip("경험치 · 레벨업 수치 (3-6). 비우면 경험치를 주지 않는다")] [SerializeField] private ProgressionConfigAsset _progressionConfig;
        [Tooltip("물약 · 주문서 수치 · 겉모습 (3-9). 비우면 식별 · 무작위 배치 없음")] [SerializeField] private ConsumableConfigAsset _consumableConfig;
        [Tooltip("고를 수 있는 캐릭터 (2026-10-21). 비우거나 창이 없으면 고르지 않고 Player.asset 모습으로 시작")] [SerializeField] private CharacterCatalogAsset _characters;
        [SerializeField] private CharacterSelectPresenter _characterSelect;
        [SerializeField] private TilemapPresenter _tilemapPresenter;
        [Tooltip("2D 픽셀 맵 (P-3a). 있으면 3D TilemapPresenter 대신 이것을 쓴다")] [SerializeField] private PixelMapPresenter _pixelMap;
        [SerializeField] private EntityPresenter _entityPresenter;
        [Tooltip("시야 안개. 비우면 안개 없이 전부 보인다")]
        [SerializeField] private FogPresenter _fogPresenter;
        [Tooltip("지형 효과 오버레이. 비우면 지형 효과가 화면에 안 보인다")]
        [SerializeField] private TerrainOverlayPresenter _terrainOverlay;
        [Tooltip("3D 이펙트 (G-3). 비우면 이펙트 없이 색 덮개만")] [SerializeField] private FxPresenter _fx;
        [Tooltip("바닥 아이템 · 마법서 (3-5). 비우면 바닥 아이템이 화면에 안 보인다")] [SerializeField] private FloorItemPresenter _floorItems;
        [SerializeField] private PlayerInputController _input;
        [SerializeField] private HudPresenter _hud;
        [Tooltip("결과 화면 (3-4b)")] [SerializeField] private ResultPresenter _result;
        [SerializeField] private CameraController _camera;

        [Header("시드 — 0이면 매번 새로 정한다. 같은 맵을 다시 보려면 콘솔에 찍힌 시드를 넣는다")]
        [SerializeField] private long _fixedSeed;

        public GameConfig Config { get; private set; }
        public MagicConfig Magic { get; private set; }
        public SpellBook Spells { get; private set; }
        public StatusBook Statuses { get; private set; }
        public TerrainBook Terrains { get; private set; }
        public ReactionBook Reactions { get; private set; }
        public ItemBook Items { get; private set; }
        public FusionBook Fusions { get; private set; }
        public World World { get; private set; }
        public DungeonFloor Floor { get; private set; }
        /// <summary>이번 판의 시드 (디버그 창 · 콘솔 표시용).</summary>
        public ulong Seed { get; private set; }
        public EntityId PlayerId => _playerId;
        public bool PlayerDead => _playerDead;
        /// <summary>판이 끝났다 (승리 · 패배, 3-4b) — R로 새 판.</summary>
        public bool RunEnded => _runEnded;

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
                if (_tilemapPresenter != null) _tilemapPresenter.RevealAll = value; // G-1: 3D 벽
                if (_fx != null) _fx.RevealAll = value; // G-3: 이펙트
                if (_floorItems != null) _floorItems.RevealAll = value; // 3-5: 바닥 마법서
            }
        }

        private EntityId _playerId;
        private Unit _player;
        private bool _playerDead;
        private bool _runEnded;
        private HudText _hudText;

        private void Awake()
        {
            if (_config == null || _dungeonConfig == null || _playerDef == null || _enemyDef == null || _bossDef == null || _result == null || _spawnConfig == null || _magicConfig == null
                || _spellCatalog == null || _statusCatalog == null || _terrainCatalog == null || _reactionCatalog == null || _itemCatalog == null || _fusionCatalog == null || (_tilemapPresenter == null && _pixelMap == null) || _entityPresenter == null || _input == null || _hud == null)
            {
                Debug.LogError("[GameRoot] 참조가 비어 있다. 메뉴 ProjectM/Setup/Main Scene을 실행한다.", this);
                enabled = false;
                return;
            }

            Config = _config.ToConfig();
            Magic = _magicConfig.ToConfig();
            Statuses = _statusCatalog.ToBook();
            Terrains = _terrainCatalog.ToBook(Statuses);
            Reactions = _reactionCatalog.ToBook(Statuses);
            Spells = _spellCatalog.ToBook(Statuses, Terrains, Reactions);
            Items = _itemCatalog.ToBook();
            Fusions = _fusionCatalog.ToBook(Statuses, Terrains);
            foreach (var id in _playerDef.ToDef().StartingSpells)
            {
                if (!Spells.TryGet(id, out _))
                    Debug.LogWarning($"[GameRoot] 시작 주문 '{id}'가 SpellCatalog에 없다 — 쓸 수 없다.", this);
            }
            _hudText = new HudText(_spellCatalog.DisplayNames(), _reactionCatalog.DisplayNames(), _itemCatalog.DisplayNames(), _fusionCatalog.DisplayNames());
            _input.Text = _hudText;
            _input.HintChanged += _hud.SetHint;
            // 2026-10-20: 이동 · 공격 · 주문 연출이 끝날 때까지 다음 행동(= 다음 틱)을 받지 않는다
            _input.PresentationBusy = () => _entityPresenter.IsAnimating || (_fx != null && _fx.IsPlaying);
        }

        private void OnDestroy()
        {
            if (_input != null && _hud != null) _input.HintChanged -= _hud.SetHint;
        }

        private void Start()
        {
            if (Config == null) return;
            BeginCharacterSelect();
        }

        /// <summary>지난 판에 고른 캐릭터 (다음 선택 창의 처음 칸). 저장 대상 아님 — 판의 캐릭터는 플레이어 유닛의 모습 ID에 남는다.</summary>
        private int _characterIndex;

        /// <summary>판을 시작하기 전에 캐릭터를 고른다 (결정 2026-10-21 — 처음 · R 새 판마다). 목록이나 창이 없으면 바로 시작.</summary>
        private void BeginCharacterSelect()
        {
            var list = _characters != null ? _characters.Characters : null;
            if (list == null || list.Count == 0 || _characterSelect == null)
            {
                StartNewRun(null);
                return;
            }
            var portraits = new System.Collections.Generic.List<Sprite>(list.Count);
            var illustrations = new System.Collections.Generic.List<Sprite>(list.Count);
            var names = new System.Collections.Generic.List<string>(list.Count);
            foreach (var c in list)
            {
                portraits.Add(c.Portrait);
                illustrations.Add(c.Illustration);
                names.Add(string.IsNullOrEmpty(c.DisplayName) ? c.Id : c.DisplayName);
            }
            _characterSelect.Open(portraits, names, _characterIndex, index =>
            {
                _characterIndex = index;
                StartNewRun(list[index].VisualId);
            }, illustrations);
        }

        private void Update()
        {
            // 입력만 읽는다 — 규칙은 틱으로만 진행한다.
            var keyboard = Keyboard.current;
            if (_runEnded && keyboard != null && keyboard.rKey.wasPressedThisFrame && (_characterSelect == null || !_characterSelect.IsOpen))
                BeginCharacterSelect();
        }

        /// <param name="visualId">고른 캐릭터의 모습 ID (null이면 Player.asset 그대로).</param>
        private void StartNewRun(string visualId)
        {
            ulong seed = _fixedSeed != 0 ? (ulong)_fixedSeed : (ulong)System.DateTime.UtcNow.Ticks;
            Seed = seed;
            Debug.Log($"[GameRoot] 시드 {(long)seed}");

            if (World != null)
            {
                World.Events.Unsubscribe<EntityMovedEvent>(OnEntityMoved);
                World.Events.Unsubscribe<UnitDiedEvent>(OnUnitDied);
                World.Events.Unsubscribe<RunEndedEvent>(OnRunEnded);
            }
            _result.Hide();

            var dungeonConfig = _dungeonConfig.ToConfig();
            var generator = new DungeonGenerator(dungeonConfig);
            DungeonFloor floor = null;
            World = World.Create(seed, rng =>
            {
                floor = generator.Generate(rng, stairsUp: false, stairsDown: dungeonConfig.FloorCount > 1);
                return floor.Map;
            });
            Floor = floor;
            var sight = new ShadowcastSightRule();
            var progression = _progressionConfig != null ? _progressionConfig.ToConfig() : null;
            var vision = World.InstallCoreSystems(sight, Magic, Statuses, Terrains, progression);
            var consumableConfig = _consumableConfig != null ? _consumableConfig.ToConfig() : ConsumableConfig.Default;
            var consumables = new ConsumableRules(Items, consumableConfig, Statuses, Terrains, progression, sight); // 3-9 물약 · 주문서 효과

            // 3-4a: 적 재생성(⑤) · 계단 사용(틱 밖 — 입력이 부른다)
            var spawn = _spawnConfig.ToConfig();
            var enemyDef = _enemyDef.ToDef();
            var enemyAi = new MeleeChaserAi(World, Config, sight);
            World.Scheduler.AddSystem(TickPhase.ManaRegen, new EnemyRespawnSystem(World, spawn, enemyDef, enemyAi, sight, dungeonConfig.FloorCount));
            var loot = _lootConfig != null ? _lootConfig.ToConfig() : null;
            if (loot != null) World.Scheduler.AddSystem(TickPhase.ManaRegen, new ItemPickupSystem(World, Items)); // 3-5: 걸어 들어가면 줍기
            var stairs = new FloorTransition(World, generator, dungeonConfig.FloorCount, enemyDef, spawn, (w, id) => enemyAi, vision, _bossDef.ToDef(), loot);

            if (_pixelMap != null) _pixelMap.Bind(World.Events); // P-3a 2D 맵 — 3D 맵은 묶지 않음
            else _tilemapPresenter.Bind(World.Events);
            _entityPresenter.Bind(World.Events);
            if (_fogPresenter != null) _fogPresenter.Bind(World.Events);
            if (_terrainOverlay != null) _terrainOverlay.Bind(World.Events);
            if (_fx != null) _fx.Bind(World.Events); // G-3: 엔티티 모습 다음에 (상태 효과가 유닛에 붙음)
            if (_floorItems != null) _floorItems.Bind(World.Events);
            World.Events.Subscribe<EntityMovedEvent>(OnEntityMoved);
            World.Events.Subscribe<UnitDiedEvent>(OnUnitDied);
            World.Events.Subscribe<RunEndedEvent>(OnRunEnded);

            World.Events.Publish(new MapLoadedEvent(World.Map));
            var playerDef = _playerDef.ToDef();
            if (!string.IsNullOrEmpty(visualId)) playerDef = playerDef.WithVisualId(visualId); // 2026-10-21 캐릭터 선택 — 지금은 외형만
            var player = FloorPopulator.Populate(World, Floor.Start, playerDef, enemyDef, spawn, enemyAi,
                Magic.QuickSlotCount, Magic.InventorySlotCount);
            _playerId = player.Id;
            var startingArmor = playerDef.StartingArmor;
            if (startingArmor != null && !Armors.Wear(World, Items, player, startingArmor)) // 3-10a: 천 갑옷을 입고 시작 (SPD)
                Debug.LogWarning($"[GameRoot] 시작 갑옷 '{startingArmor}'가 ItemCatalog에 없다.", this);
            _player = player;
            _playerDead = false;
            _runEnded = false;
            if (loot != null)
            {
                // 3-5: 한 판의 마법서 배치 계획(1~마지막 전 층) → 1층 몫을 놓는다 (Loot 난수 — 맵 · 적 배치는 그대로)
                Loot.PlanSpellbooks(World, Items, loot, dungeonConfig.FloorCount);
                Loot.PlanWeapons(World, Items, loot, dungeonConfig.FloorCount); // 3-7a: 책 계획 뒤에 붙인다
                Loot.PlanConsumables(World, Items, loot, dungeonConfig.FloorCount); // 3-7b: 강화 주문서 · 힘 물약 (무기 계획 뒤)
                Loot.PlanRandomConsumables(World, Items, consumableConfig, dungeonConfig.FloorCount); // 3-9: 층마다 무작위 물약 · 주문서
                Loot.PlanArmors(World, Items, loot, dungeonConfig.FloorCount); // 3-10a: 갑옷 (앞 계획 뒤)
            }
            Identification.AssignLooks(World, Items, consumableConfig); // 3-9: 판마다 겉모습 (배치 계획 모두 뒤 — 앞 계획은 그대로)
            if (loot != null)
            {
                Loot.PlaceFloorItems(World, Floor.Start, loot);
            }
            vision.Refresh(); // 첫 시야 — 이후는 매 틱 ⑥ 단계에서

            _hudText.Run = World.Run; // 3-9: 모르는 물약 · 주문서는 겉모습 이름으로
            _hud.Bind(World.Events, player, Spells, _hudText, progression);
            _input.Bind(World, Config, new TurnClock(World.Scheduler, World.PlayerInput), sight, Spells, new BresenhamTargetingRule(), Items, Fusions, stairs, consumables);
            World.Events.Flush();

            if (_camera != null)
            {
                _camera.SetPanBounds(World.Map.Width, World.Map.Height); // 둘러보기 범위 = 맵
                _camera.SnapToTarget(_entityPresenter.CellCenter(Floor.Start));
            }
        }

        private void OnEntityMoved(EntityMovedEvent e)
        {
            if (e.Id != _playerId || _camera == null) return;
            if (e.From == e.To) // 층 이동 (3-4a: 제자리 이동 이벤트)
            {
                _camera.SetPanBounds(World.Map.Width, World.Map.Height);
                _camera.SnapToTarget(_entityPresenter.CellCenter(e.To));
            }
            else _camera.FollowTarget(_entityPresenter.CellCenter(e.To)); // G-2b: 걸음은 부드럽게
        }

        private void OnUnitDied(UnitDiedEvent e)
        {
            if (e.Id != _playerId) return;
            _playerDead = true;
            Debug.Log("[GameRoot] 플레이어 사망 — R로 새 판");
        }

        /// <summary>판이 끝남 (3-4b): 입력을 멈추고 결과 화면. R로 새 판.</summary>
        private void OnRunEnded(RunEndedEvent e)
        {
            _runEnded = true;
            _input.Unbind();
            _result.Show(ResultModel.From(World, Seed, _player, _hudText));
            Debug.Log($"[GameRoot] 판 끝: {e.Outcome} — R로 새 판");
        }
    }
}
