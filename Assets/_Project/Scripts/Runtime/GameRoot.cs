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
        [Tooltip("구역 표 (3-17a) — 있으면 층마다 적 종류 · 보스 층을 여기서. 없으면 위 적 · 보스")] [SerializeField] private ZoneCatalogAsset _zoneCatalog;
        [Tooltip("구역 분위기 (3-17c) — 비우면 없음")] [SerializeField] private ZoneThemePresenter _zoneTheme;
        [SerializeField] private SpawnConfigAsset _spawnConfig;
        [SerializeField] private MagicConfigAsset _magicConfig;
        [SerializeField] private SpellCatalogAsset _spellCatalog;
        [SerializeField] private StatusCatalogAsset _statusCatalog;
        [SerializeField] private TerrainCatalogAsset _terrainCatalog;
        [SerializeField] private ReactionCatalogAsset _reactionCatalog;
        [SerializeField] private ItemCatalogAsset _itemCatalog;
        [SerializeField] private FusionCatalogAsset _fusionCatalog;
        [Tooltip("마법서 배치 수치 (3-5). 비우면 바닥에 마법서를 놓지 않는다")] [SerializeField] private LootConfigAsset _lootConfig;
        [Tooltip("층 꾸미기 수치 (3-11a — 풀 · 물 웅덩이). 비우면 꾸미지 않는다")] [SerializeField] private FloorDecorConfigAsset _floorDecorConfig;
        [Tooltip("함정 표 (3-12a). 비우면 함정을 놓지 않는다")] [SerializeField] private TrapCatalogAsset _trapCatalog;
        [Tooltip("허기 · 음식 (3-14). 비우면 허기 없음")] [SerializeField] private HungerConfigAsset _hungerConfig;
        [Tooltip("경험치 · 레벨업 수치 (3-6). 비우면 경험치를 주지 않는다")] [SerializeField] private ProgressionConfigAsset _progressionConfig;
        [Tooltip("물약 · 주문서 수치 · 겉모습 (3-9). 비우면 식별 · 무작위 배치 없음")] [SerializeField] private ConsumableConfigAsset _consumableConfig;
        [Tooltip("고를 수 있는 캐릭터 (2026-10-21). 비우거나 창이 없으면 고르지 않고 Player.asset 모습으로 시작")] [SerializeField] private CharacterCatalogAsset _characters;
        [SerializeField] private CharacterSelectPresenter _characterSelect;
        [Tooltip("타이틀 화면 (3-27 — 이어하기 · 게임 시작 → 모드 · 시드). 비우면 바로 캐릭터 선택 (턴제 · 무작위 시드)")] [SerializeField] private TitlePresenter _title;
        [Tooltip("직업 · 형태 표 (3-21b). 비우면 형태 선택 없음")] [SerializeField] private ClassCatalogAsset _classCatalog;
        [SerializeField] private TilemapPresenter _tilemapPresenter;
        [Tooltip("2D 픽셀 맵 (P-3a). 있으면 3D TilemapPresenter 대신 이것을 쓴다")] [SerializeField] private PixelMapPresenter _pixelMap;
        [SerializeField] private EntityPresenter _entityPresenter;
        [Tooltip("시야 안개. 비우면 안개 없이 전부 보인다")]
        [SerializeField] private FogPresenter _fogPresenter;
        [Tooltip("지형 효과 오버레이. 비우면 지형 효과가 화면에 안 보인다")]
        [SerializeField] private TerrainOverlayPresenter _terrainOverlay;
        [SerializeField] private TrapPresenter _traps;
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
        /// <summary>직업 · 형태 표 (3-21b). 없으면 null.</summary>
        public ClassBook Classes { get; private set; }
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
            _baseSpells = Spells; // 3-23b: 판마다 그림자 보스 주문을 더한 표를 여기서 다시 만든다
            Items = _itemCatalog.ToBook();
            Fusions = _fusionCatalog.ToBook(Statuses, Terrains);
            Classes = _classCatalog != null ? _classCatalog.ToBook() : null; // 3-21b
            foreach (var id in _playerDef.ToDef().StartingSpells)
            {
                if (!Spells.TryGet(id, out _))
                    Debug.LogWarning($"[GameRoot] 시작 주문 '{id}'가 SpellCatalog에 없다 — 쓸 수 없다.", this);
            }
            _hudText = new HudText(_spellCatalog.DisplayNames(), _reactionCatalog.DisplayNames(), _itemCatalog.DisplayNames(), _fusionCatalog.DisplayNames());
            _hudText.FormNames = FormNames(); // 3-21b
            _hudText.TalentNames = _classCatalog != null ? _classCatalog.TalentNames() : null; // 3-21c
            _workbench = _floorDecorConfig != null ? _floorDecorConfig.ToWorkbench() : null; // 3-28b 마법 제작대 수치
            _input.Text = _hudText;
            _input.HintChanged += _hud.SetHint;
            _input.InfoChanged += _hud.SetInfo;
            // 2026-10-20: 이동 · 공격 · 주문 연출이 끝날 때까지 다음 행동(= 다음 틱)을 받지 않는다
            _input.PresentationBusy = () => _entityPresenter.IsAnimating || (_fx != null && _fx.IsPlaying)
                || (_characterSelect != null && _characterSelect.IsOpen) || (_title != null && _title.IsOpen); // 3-21b: 형태 선택 창이 열려 있으면 행동을 받지 않음 · 3-27 타이틀
        }

        private void OnDestroy()
        {
            if (_input != null && _hud != null) _input.HintChanged -= _hud.SetHint;
            if (_input != null && _hud != null) _input.InfoChanged -= _hud.SetInfo;
        }

        private void Start()
        {
            if (Config == null) return;
            if (_title != null)
            {
                _title.Model.ContinueChosen += () => Defer(ContinueRun);
                _title.Model.StartChosen += (mode, seedText) => Defer(() =>
                {
                    _pendingMode = mode;
                    _pendingSeed = SeedInput.TryParse(seedText, out var s) ? s : (ulong?)null;
                    BeginCharacterSelect();
                });
                _title.Model.QuitChosen += Quit;
            }
            OpenTitle();
        }

        // ── 타이틀 · 모드 · 시드 (3-27, 결정 2026-11-05) ──

        private RunMode _pendingMode;
        private ulong? _pendingSeed;
        private System.Action _deferred;
        private int _deferredFrame;
        private RealTimeClock _realTime; // 실시간 판이면 시계 (턴제면 null)
        private bool _paused;
        private long _savedFlush = -1; // 자동 저장: 마지막으로 저장한 SimEventBus.FlushCount

        /// <summary>확정한 프레임의 키(Enter)가 다음 창에 또 읽히지 않게 다음 프레임에 연다.</summary>
        private void Defer(System.Action action)
        {
            _deferred = action;
            _deferredFrame = Time.frameCount;
        }

        /// <summary>타이틀을 연다 (게임 처음 · 판이 끝난 뒤 R). 타이틀이 없으면 바로 턴제 · 무작위 시드로 캐릭터 선택.</summary>
        private void OpenTitle()
        {
            if (_title == null)
            {
                _pendingMode = RunMode.TurnBased;
                _pendingSeed = null;
                BeginCharacterSelect();
                return;
            }
            _title.Open(RunSaveStore.Exists());
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>저장에서 이어하기 (3-27). 저장이 깨졌으면 타이틀로.</summary>
        private void ContinueRun()
        {
            var snapshot = RunSaveStore.Load();
            if (snapshot == null)
            {
                RunSaveStore.Delete();
                OpenTitle();
                return;
            }
            try
            {
                BeginRun(snapshot, null, null, null, snapshot.Mode, snapshot.RngSeed);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameRoot] 저장을 불러오지 못했다 — 지우고 타이틀로: {ex}");
                RunSaveStore.Delete();
                OpenTitle();
            }
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
                _characterName = string.IsNullOrEmpty(list[index].DisplayName) ? list[index].Id : list[index].DisplayName; // 3-23b 기록 이름
                StartNewRun(list[index].VisualId, list[index].StartingSpells, list[index].ClassId); // 3-21a: 루멘 = 빛의 창 기술 2개 · 3-21b 직업
            }, illustrations, onCancel: _title != null ? () => Defer(OpenTitle) : (System.Action)null); // 3-27 Esc = 타이틀로
        }

        private void Update()
        {
            // 입력만 읽는다 — 규칙은 틱으로만 진행한다.
            if (_deferred != null && Time.frameCount > _deferredFrame)
            {
                var action = _deferred;
                _deferred = null;
                action();
                return;
            }
            var keyboard = Keyboard.current;
            bool titleOpen = _title != null && _title.IsOpen;
            if (_runEnded && keyboard != null && keyboard.rKey.wasPressedThisFrame && !titleOpen && (_characterSelect == null || !_characterSelect.IsOpen))
                OpenTitle(); // 3-27: 판이 끝나면 타이틀로 (모드 · 시드를 다시 고름)
            else if (!_runEnded && World != null && keyboard != null && keyboard.tKey.wasPressedThisFrame
                     && _characterSelect != null && !_characterSelect.IsOpen && !_entityPresenter.IsAnimating)
                OpenTalents(); // 3-21c 특성 창
            else if (_realTime != null && !_runEnded && keyboard != null && keyboard.pKey.wasPressedThisFrame)
            {
                _paused = !_paused; // 3-27 실시간 일시정지
                _hud.SetHint(_paused ? HudText.Paused : string.Empty);
            }

            // 3-27 실시간: 1초마다 1틱 — 일시정지 · 창(캐릭터 · 형태 · 특성 · 배낭 · 타이틀)이 열려 있으면 멈춤
            if (_realTime != null && !_runEnded && !_paused && !titleOpen && (_characterSelect == null || !_characterSelect.IsOpen) && !_input.Inventory.IsOpen)
            {
                _realTime.Update(Time.deltaTime);
                if (_realTime.LastTicks > 0) World.Events.Flush();
            }
        }

        /// <summary>3-27 자동 저장 (결정 2026-11-05 — 매 행동 · 매 틱): 이번 프레임에 화면에 보낸 이벤트가 있었으면(= 무언가 바뀜) 저장. 판이 끝났으면 안 함.</summary>
        private void LateUpdate() => AutoSave();

        private void OnApplicationQuit() => AutoSave();

        private void OnApplicationPause(bool paused)
        {
            if (paused) AutoSave();
        }

        private void AutoSave()
        {
            if (World == null || _runEnded || _playerDead || !_playerId.IsValid || World.Events.FlushCount == _savedFlush) return;
            try
            {
                RunSaveStore.Save(World);
                _savedFlush = World.Events.FlushCount;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameRoot] 자동 저장 실패: {ex.Message}");
                _savedFlush = World.Events.FlushCount; // 같은 상태로 매 프레임 다시 시도하지 않게
            }
        }

        /// <param name="visualId">고른 캐릭터의 모습 ID (null이면 Player.asset 그대로).</param>
        /// <param name="characterSpells">고른 캐릭터(직업)의 시작 주문 (3-21a). null이면 없음.</param>
        /// <param name="classId">고른 캐릭터의 직업 ID (3-21b — RunState.ClassId). null이면 직업 없음.</param>
        private void StartNewRun(string visualId, System.Collections.Generic.IReadOnlyList<string> characterSpells = null, string classId = null)
        {
            // 3-27: 시드 = 타이틀 입력 칸 → 인스펙터 고정 시드 → 지금 시각
            ulong seed = _pendingSeed ?? (_fixedSeed != 0 ? (ulong)_fixedSeed : (ulong)System.DateTime.UtcNow.Ticks);
            RunSaveStore.Delete(); // 새 판 = 지난 저장은 버림 (저장은 하나)
            BeginRun(null, visualId, characterSpells, classId, _pendingMode, seed);
        }

        /// <summary>
        /// 판을 연결한다 (3-27): snapshot이 null이면 새 판(seed로 1층 생성 · 플레이어 · 적 · 배치 계획), 있으면 저장에서 복원(World.Restore → 시스템 · 규칙을 다시 연결 → 화면에 지금 층 전체 알림).
        /// 규칙 연결(시스템 · 적 · 아이템 · 직업 규칙 · 계단 · 입력)은 두 경우 같은 코드.
        /// </summary>
        private void BeginRun(WorldSnapshot snapshot, string visualId, System.Collections.Generic.IReadOnlyList<string> characterSpells, string classId,
            RunMode mode, ulong seed)
        {
            bool restoring = snapshot != null;
            _paused = false;
            Seed = seed;
            Debug.Log($"[GameRoot] 시드 {(long)seed}");
            // 3-23b: 지난 회차 기록(있으면 마지막 보스 = 그 플레이어) · 그 보스 주문을 더한 판 주문 표
            _ghost = GhostStore.Load();
            Spells = _baseSpells;
            if (_ghost != null)
            {
                var defs = new System.Collections.Generic.List<ProjectM.Magic.SpellDef>();
                foreach (var id in _baseSpells.Ids) if (_baseSpells.TryGet(id, out var def)) defs.Add(def);
                defs.AddRange(GhostBoss.SpellsWithPrefix(_ghost));
                Spells = new ProjectM.Magic.SpellBook(defs, _baseSpells.Statuses, _baseSpells.Terrains, _baseSpells.Reactions);
                Debug.Log($"[GameRoot] 지난 회차 기록: {_ghost.DisplayName} (레벨 {_ghost.Level}) — 마지막 보스가 된다");
            }

            if (World != null)
            {
                World.Events.Unsubscribe<EntityMovedEvent>(OnEntityMoved);
                World.Events.Unsubscribe<EntitySpawnedEvent>(OnEntitySpawned);
                World.Events.Unsubscribe<UnitDiedEvent>(OnUnitDied);
                World.Events.Unsubscribe<RunEndedEvent>(OnRunEnded);
                World.Events.Unsubscribe<ArtifactObtainedEvent>(OnArtifactObtained);
                World.Events.Unsubscribe<StairsUnsealedEvent>(OnStairsUnsealed); // 3-21b
                World.Events.Unsubscribe<LevelUpEvent>(OnLevelUp); // 3-21c
            }
            _result.Hide();

            var dungeonConfig = _dungeonConfig.ToConfig();
            var generator = new DungeonGenerator(dungeonConfig);
            var sight = new ShadowcastSightRule();
            // 적 · 아군 AI: 복원(World.Restore)은 새 World를 만들며 AI를 물어 오므로 처음 물을 때 그 World로 만든다 (상태 없음 — 규칙 6)
            var enemiesBook = _zoneCatalog != null ? _zoneCatalog.ToEnemyBook() : null; // 3-17b 적 종류 행동
            if (enemiesBook != null && _ghost != null) enemiesBook = enemiesBook.With(GhostBoss.Traits(_ghost)); // 3-23b 그림자 보스 행동
            MeleeChaserAi enemyAi = null;
            AllyAi allyAi = null;
            void EnsureAi(World w)
            {
                if (enemyAi != null) return;
                enemyAi = new MeleeChaserAi(w, Config, sight, Statuses, enemiesBook, Spells, new BresenhamTargetingRule()); // 3-13a: 투명 · 3-17b 행동
                allyAi = new AllyAi(w, Config, sight); // 3-13b 분신
            }
            System.Func<World, EntityId, ICommandSource> aiFactory = (w, id) =>
            {
                EnsureAi(w);
                return !w.Units.TryGet(id, out var u) ? enemyAi : u.Faction == Faction.Ally ? allyAi
                    : u.Faction == Faction.Neutral ? new ProjectM.Dungeon.IdleNeutralAi() : (ICommandSource)enemyAi; // 떠났던 층의 분신 · 상인
            };
            if (restoring)
            {
                World = World.Restore(snapshot, aiFactory);
                Floor = null;
            }
            else
            {
                DungeonFloor floor = null;
                World = World.Create(seed, rng =>
                {
                    floor = generator.Generate(rng, stairsUp: false, stairsDown: dungeonConfig.FloorCount > 1);
                    return floor.Map;
                });
                Floor = floor;
                World.Run.Mode = mode; // 3-27: 판 시작 때 고정 (스냅샷 v27)
            }
            EnsureAi(World);
            var progression = _progressionConfig != null ? _progressionConfig.ToConfig() : null;
            var hunger = _hungerConfig != null ? _hungerConfig.ToConfig() : null; // 3-14
            var vision = World.InstallCoreSystems(sight, Magic, Statuses, Terrains, progression, _terrainCatalog.ToFireSpread(), hunger); // 3-11b 불 번짐 · 3-14 허기
            var lootRules = _lootConfig != null ? _lootConfig.ToConfig() : null;
            // 3-14 · 3-15a: 바닥 고기 요리 · 적이 고기 · 금화 떨어뜨리기 (규칙 연결)
            World.SetItemRules((element, cells) => { if (hunger != null) ProjectM.Units.Hunger.TransformFloorItems(World, Items, element, cells); },
                dead =>
                {
                    if (hunger != null) ProjectM.Units.Hunger.DropFood(World, hunger, dead);
                    if (lootRules != null) Loot.DropGold(World, lootRules, dead);
                    if (lootRules != null && World.Run.BossId.IsValid && dead.Id == World.Run.BossId) Loot.DropBossRelic(World, Items, lootRules, dead.Pos); // 3-28a 구역 보스 보상
                },
                pos => { if (lootRules != null) Loot.DropSeed(World, lootRules, pos); }); // 3-16b
            var consumableConfig = _consumableConfig != null ? _consumableConfig.ToConfig() : ConsumableConfig.Default;
            var consumables = new ConsumableRules(Items, consumableConfig, Statuses, Terrains, progression, sight, allyAi, Spells); // 3-9 물약 · 주문서 효과

            // 3-4a: 적 재생성(⑤) · 계단 사용(틱 밖 — 입력이 부른다)
            var spawn = _spawnConfig.ToConfig();
            var enemyDef = _enemyDef.ToDef();
            var zones = _zoneCatalog != null ? _zoneCatalog.ToBook() : null; // 3-17a
            _zones = zones;
            if (zones != null && zones.FloorCount != dungeonConfig.FloorCount)
                Debug.LogWarning($"[GameRoot] 구역 표의 층 수 {zones.FloorCount} ≠ DungeonGenConfig 층 수 {dungeonConfig.FloorCount}", this);
            var kindRng = World.Rng.Get(RngStream.EnemyKind);
            if (enemiesBook != null) World.SetEnemyRules(new EnemyRules(enemiesBook, Spells, Statuses, Terrains));
            _unitVisuals.Clear();
            _enemyNames = _zoneCatalog != null ? _zoneCatalog.DisplayNames() : new System.Collections.Generic.Dictionary<string, string>();
            if (_ghost != null) _enemyNames[_ghost.VisualId] = _ghost.DisplayName; // 3-23b "루멘의 그림자 #12"
            HudText.EnemyName = id => _unitVisuals.TryGetValue(id, out var v) && _enemyNames.TryGetValue(v, out var n) ? n : null;
            World.Scheduler.AddSystem(TickPhase.ManaRegen, new EnemyRespawnSystem(World, spawn, enemyDef, enemyAi, sight, dungeonConfig.FloorCount, zones));
            var loot = _lootConfig != null ? _lootConfig.ToConfig() : null;
            if (loot != null) World.Scheduler.AddSystem(TickPhase.ManaRegen, new ItemPickupSystem(World, Items)); // 3-5: 걸어 들어가면 줍기
            var decor = _floorDecorConfig != null ? _floorDecorConfig.ToConfig() : null;
            var traps = _trapCatalog != null ? _trapCatalog.ToBook(Spells) : null; // 3-12a
            var search = _trapCatalog != null ? _trapCatalog.ToSearch() : null; // 3-12b 찾기
            if (search != null) World.Scheduler.AddSystem(TickPhase.ManaRegen, new PassiveSearchSystem(World, search)); // ⑤ 저절로 발견 (발동보다 먼저)
            if (traps != null) World.Scheduler.AddSystem(TickPhase.ManaRegen, new TrapSystem(World, traps, Spells)); // ⑤ 밟으면 발동
            Terrains.TryGet(TerrainEffectType.Water, out var water); // 3-11a: 물 웅덩이 = 사라지지 않는 물 지형
            var stairs = new FloorTransition(World, generator, dungeonConfig.FloorCount, enemyDef, spawn, aiFactory, vision, _bossDef.ToDef(), loot,
                decor, water, traps, _floorDecorConfig != null ? _floorDecorConfig.ToShop() : null, Items, zones, Terrains, // 3-15c 상점 · 3-17a 구역 · 3-18 구역별 웅덩이
                _ghost, _ghost != null && _zoneCatalog != null
                    ? GhostBoss.ToUnitDef(_ghost, _zoneCatalog.GhostHpPercent, _zoneCatalog.GhostExpReward, _zoneCatalog.GhostExpMaxLevel) : null, // 3-23b 그림자 보스
                _workbench); // 3-28b 작업실

            if (_pixelMap != null) _pixelMap.Bind(World.Events); // P-3a 2D 맵 — 3D 맵은 묶지 않음
            else _tilemapPresenter.Bind(World.Events);
            _entityPresenter.Bind(World.Events);
            if (_fogPresenter != null) _fogPresenter.Bind(World.Events);
            if (_terrainOverlay != null) _terrainOverlay.Bind(World.Events);
            if (_traps != null) _traps.Bind(World.Events);
            if (_fx != null) _fx.Bind(World.Events); // G-3: 엔티티 모습 다음에 (상태 효과가 유닛에 붙음)
            if (_floorItems != null) _floorItems.Bind(World.Events);
            if (_zoneTheme != null) _zoneTheme.Bind(World.Events, 1); // 3-17c
            World.Events.Subscribe<EntityMovedEvent>(OnEntityMoved);
            World.Events.Subscribe<EntitySpawnedEvent>(OnEntitySpawned); // 3-17b 로그의 적 이름
            World.Events.Subscribe<UnitDiedEvent>(OnUnitDied);
            World.Events.Subscribe<RunEndedEvent>(OnRunEnded);
            World.Events.Subscribe<ArtifactObtainedEvent>(OnArtifactObtained); // 3-23b 모래시계 → 기록
            World.Events.Subscribe<StairsUnsealedEvent>(OnStairsUnsealed); // 3-21b: 선택 층 보스 처치 → 형태 선택
            World.Events.Subscribe<LevelUpEvent>(OnLevelUp); // 3-21c: 특성 점수 안내
            World.SetClassRules(Classes); // 3-21c: 특성 효과 조회 (규칙 연결 — 저장 안 함)
            World.SetRelicRules(Items); // 3-28a: 유물 효과 조회 (규칙 연결 — 저장 안 함)
            _input.WorkbenchRequested = OpenWorkbench; // 3-28b 제작대에 부딪힘 → 창
            var consumableConfigForRun = consumableConfig;

            Unit player;
            if (restoring)
            {
                // 3-27 이어하기: 저장된 판 그대로 — 화면에 지금 층 전체를 알린다
                if (!PlayerActions.TryGetPlayer(World, out player)) throw new System.InvalidOperationException("저장에 플레이어가 없다.");
                _characterName = CharacterNameOf(player.VisualId);
                FloorTransition.AnnounceRestored(World);
                _playerId = player.Id;
                _player = player;
                _playerDead = false;
                _runEnded = false;
            }
            else
            {
                player = SpawnNewRun(visualId, characterSpells, classId, decor, water, traps, zones, kindRng, spawn, enemyDef, enemyAi, hunger, loot, dungeonConfig, consumableConfigForRun);
            }
            vision.Refresh(); // 첫 시야 — 이후는 매 틱 ⑥ 단계에서

            _hudText.Run = World.Run; // 3-9: 모르는 물약 · 주문서는 겉모습 이름으로
            _hud.Bind(World.Events, player, Spells, _hudText, progression);
            // 3-27: 판의 시계 — 턴제(행동 = 틱) / 실시간(1초 = 1틱, 플레이어 즉시 행동)
            ITickClock clock;
            if (World.Run.Mode == RunMode.RealTime)
            {
                _realTime = new RealTimeClock(World.Scheduler, Config.RealtimeTickIntervalSeconds);
                clock = _realTime;
            }
            else
            {
                _realTime = null;
                clock = new TurnClock(World.Scheduler, World.PlayerInput);
            }
            _input.Bind(World, Config, clock, sight, Spells, new BresenhamTargetingRule(), Items, Fusions, stairs, consumables, search, hunger, _floorDecorConfig != null ? _floorDecorConfig.ToShop() : null);
            World.Events.Flush();
            _savedFlush = -1; // 새 판 · 이어하기 바로 저장
            if (restoring) _hud.SetHint(HudText.RunContinued);
            else if (_realTime != null) _hud.SetHint(HudText.RealTimeStarted);

            if (_camera != null)
            {
                _camera.SetPanBounds(World.Map.Width, World.Map.Height); // 둘러보기 범위 = 맵
                _camera.SnapToTarget(_entityPresenter.CellCenter(player.Pos));
            }
            if (restoring) OpenFormSelectIfReady(bossDefeated: !HasSealedStairs()); // 형태를 고르기 전에 껐으면 다시 묻는다
        }

        /// <summary>캐릭터 모습 ID → 캐릭터 이름 (3-23b 기록 이름 — 이어하기에서 다시 찾음).</summary>
        private string CharacterNameOf(string visualId)
        {
            if (_characters != null && _characters.Characters != null)
                foreach (var c in _characters.Characters)
                    if (c.VisualId == visualId) return string.IsNullOrEmpty(c.DisplayName) ? c.Id : c.DisplayName;
            return visualId;
        }

        private bool HasSealedStairs()
        {
            var map = World.Map;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.Get(new GridPos(x, y)) == TileType.SealedStairs) return true;
            return false;
        }

        /// <summary>새 판 (3-27 이전 StartNewRun 그대로): 1층 꾸미기 → 플레이어 · 적 → 직업 · 갑옷 · 식량 → 배치 계획 · 1층 아이템. 플레이어를 돌려준다.</summary>
        private Unit SpawnNewRun(string visualId, System.Collections.Generic.IReadOnlyList<string> characterSpells, string classId,
            FloorDecorConfig decor, TerrainDef water, TrapBook traps, ProjectM.Dungeon.ZoneBook zones, IRng kindRng, SpawnConfig spawn, UnitDef enemyDef,
            ICommandSource enemyAi, HungerConfig hunger, LootConfig loot, DungeonGenConfig dungeonConfig, ConsumableConfig consumableConfig)
        {
            World.Events.Publish(new MapLoadedEvent(World.Map));
            if (decor != null) FloorDecor.Apply(World, Floor, decor, water, traps); // 3-11a · 3-12a: MapLoaded 뒤에 (화면이 지우지 않게), 적 배치 전
            var playerDef = _playerDef.ToDef();
            if (!string.IsNullOrEmpty(visualId)) playerDef = playerDef.WithVisualId(visualId); // 2026-10-21 캐릭터 선택 — 외형
            if (characterSpells != null)
            {
                foreach (var id in characterSpells)
                    if (!Spells.TryGet(id, out _)) Debug.LogWarning($"[GameRoot] 캐릭터 시작 주문 '{id}'가 SpellCatalog에 없다.", this);
                playerDef = playerDef.WithExtraStartingSpells(characterSpells); // 3-21a 직업 시작 기술 (빠른 칸 앞쪽)
            }
            Unit player;
            if (zones != null) // 3-17a: 1층 적 종류도 구역 표에서
            {
                player = UnitSpawner.SpawnPlayer(World, playerDef, Floor.Start, Magic.QuickSlotCount, Magic.InventorySlotCount);
                FloorPopulator.PopulateEnemies(World, Floor.Start, () => zones.PickEnemy(1, kindRng), spawn, enemyAi);
            }
            else player = FloorPopulator.Populate(World, Floor.Start, playerDef, enemyDef, spawn, enemyAi,
                Magic.QuickSlotCount, Magic.InventorySlotCount);
            _playerId = player.Id;
            ClassForms.SetClass(World, classId); // 3-21b: 형태는 선택 층 보스 뒤에
            var startingArmor = playerDef.StartingArmor;
            if (startingArmor != null && !Armors.Wear(World, Items, player, startingArmor)) // 3-10a: 천 갑옷을 입고 시작 (SPD)
                Debug.LogWarning($"[GameRoot] 시작 갑옷 '{startingArmor}'가 ItemCatalog에 없다.", this);
            if (hunger?.StartingFood != null && Items.TryGet(hunger.StartingFood, out _))
                Inventory.TryAdd(World, Spells, Items, player, InventoryRef.Item(hunger.StartingFood)); // 3-14: 식량 1개로 시작 (SPD)
            _player = player;
            _playerDead = false;
            _runEnded = false;
            if (loot != null)
            {
                // 3-5: 한 판의 마법서 배치 계획(1~마지막 전 층) → 1층 몫을 놓는다 (Loot 난수 — 맵 · 적 배치는 그대로)
                // 3-17a: 한 번만 놓는 것(책 · 무기 · 갑옷 · 강화)은 앞 층(LootPlanFloors)에만 — 계획 함수는 '층 수 − 1'개 층에 나눈다
                int planFloors = zones != null && _zoneCatalog.LootPlanFloors > 0 ? System.Math.Min(dungeonConfig.FloorCount, _zoneCatalog.LootPlanFloors + 1) : dungeonConfig.FloorCount;
                Loot.PlanSpellbooks(World, Items, loot, planFloors);
                Loot.PlanWeapons(World, Items, loot, planFloors); // 3-7a: 책 계획 뒤에 붙인다
                Loot.PlanConsumables(World, Items, loot, planFloors); // 3-7b: 강화 주문서 · 힘 물약 (무기 계획 뒤)
                Loot.PlanRandomConsumables(World, Items, consumableConfig, dungeonConfig.FloorCount); // 3-9: 층마다 무작위 물약 · 주문서
                Loot.PlanArmors(World, Items, loot, planFloors); // 3-10a: 갑옷 (앞 계획 뒤)
                if (hunger != null) Loot.PlanFood(World, Items, hunger, dungeonConfig.FloorCount); // 3-14: 층마다 식량 1 + 가끔 빵 (앞 계획 뒤)
            }
            Identification.AssignLooks(World, Items, consumableConfig); // 3-9: 판마다 겉모습 (배치 계획 모두 뒤 — 앞 계획은 그대로)
            if (loot != null) Loot.PlanRelics(World, Items, loot, dungeonConfig.FloorCount); // 3-28a: 유물 (겉모습 뒤 — 앞 계획 · 겉모습은 그대로)
            if (loot != null)
            {
                Loot.PlaceFloorItems(World, Floor.Start, loot);
                Loot.PlaceGold(World, Floor.Start, loot); // 3-15a
                Loot.PlaceThrown(World, Floor.Start, loot); // 3-16a
            }
            return player;
        }

        /// <summary>3-17b: 엔티티 → 모습 ID (로그의 적 이름 — 죽은 뒤에도 남겨 둠, 판마다 비움).</summary>
        private readonly System.Collections.Generic.Dictionary<EntityId, string> _unitVisuals = new System.Collections.Generic.Dictionary<EntityId, string>();
        private System.Collections.Generic.Dictionary<string, string> _enemyNames = new System.Collections.Generic.Dictionary<string, string>();
        private ProjectM.Magic.SpellBook _baseSpells; // 3-23b 데이터 주문 표 (판마다 그림자 주문을 더함)
        private GhostRecord _ghost; // 3-23b 이 판의 지난 회차 기록 (없으면 null)
        private ProjectM.Dungeon.ZoneBook _zones;
        private string _characterName; // 3-23b 기록 이름 ("루멘의 그림자")

        /// <summary>3-23b: 시간의 모래시계를 얻으면 지금 플레이어를 기록으로 남긴다 (기록 하나 — 덮어씀).</summary>
        private void OnArtifactObtained(ArtifactObtainedEvent e)
        {
            if (_player == null || e.Unit != _playerId) return;
            try
            {
                var record = GhostBoss.Capture(World, _player, Spells, HudText.GhostName(_characterName ?? _player.VisualId));
                GhostStore.Save(record);
                Debug.Log($"[GameRoot] 기록을 남겼다: {record.DisplayName} (레벨 {record.Level}, 주문 {record.Spells.Count}) — {GhostStore.DefaultPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameRoot] 기록을 남기지 못했다: {ex.Message}");
            }
        }

        private void OnEntitySpawned(EntitySpawnedEvent e) => _unitVisuals[e.Id] = e.VisualId;

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
            // 3-23b: 그림자 보스를 쓰러뜨리면 기록이 사라진다 (다음 판부터 마지막 보스는 원래 보스) — 결정 2026-11-05 "처치할 때까지"
            if (_ghost != null && World.Run.BossId.IsValid && e.Id == World.Run.BossId && _zones != null && World.Floor == _zones.LastBossFloor)
            {
                GhostStore.Delete();
                _ghost = null;
                _hud.SetHint(HudText.GhostDefeated);
            }
            if (e.Id != _playerId) return;
            _playerDead = true;
            Debug.Log("[GameRoot] 플레이어 사망 — R로 새 판");
        }

        /// <summary>형태 ID → 표시 이름 (ClassCatalog, HUD 로그용 — 3-21b).</summary>
        private System.Collections.Generic.Dictionary<string, string> FormNames()
        {
            var names = new System.Collections.Generic.Dictionary<string, string>();
            if (_classCatalog == null) return names;
            foreach (var c in _classCatalog.Classes)
                if (c.Forms != null)
                    foreach (var f in c.Forms) if (!string.IsNullOrEmpty(f.Id)) names[f.Id] = string.IsNullOrEmpty(f.DisplayName) ? f.Id : f.DisplayName;
            return names;
        }

        /// <summary>
        /// 3-21b: 선택 층 보스를 쓰러뜨려 봉인 계단이 열리면, 아직 형태를 안 고른 직업이면 형태 선택 창을 연다 (캐릭터 선택 창을 같이 씀).
        /// 고르면 틱 밖 변경(ClassForms.Choose) 뒤 이벤트를 비운다. 창이 열린 동안 입력은 PresentationBusy로 막힌다.
        /// </summary>
        private void OnStairsUnsealed(StairsUnsealedEvent e) => OpenFormSelectIfReady(bossDefeated: true);

        /// <summary>형태를 고를 때면 형태 선택 창 (봉인 계단 열림 · 3-27 이어하기 — 고르기 전에 껐을 때).</summary>
        private void OpenFormSelectIfReady(bool bossDefeated)
        {
            if (_characterSelect == null || Classes == null || _player == null) return;
            if (ClassForms.CanChoose(World, Classes, bossDefeated) != ChooseFormResult.Chosen) return;
            if (!_classCatalog.TryGetClass(World.Run.ClassId, out var entry) || entry.Forms == null || entry.Forms.Count == 0) return;
            var names = new System.Collections.Generic.List<string>();
            var details = new System.Collections.Generic.List<string>();
            foreach (var f in entry.Forms)
            {
                names.Add(string.IsNullOrEmpty(f.DisplayName) ? f.Id : f.DisplayName);
                details.Add(f.Description);
            }
            var world = World;
            _characterSelect.Open(null, names, 0, index =>
            {
                if (World != world || _player == null) return; // 그새 새 판
                ClassForms.Choose(World, Classes, Spells, _player, entry.Forms[index].Id);
                World.Events.Flush();
            }, title: HudText.FormSelectTitle, hint: HudText.FormSelectHint, details: details);
        }

        /// <summary>남은 특성 점수 (모든 단계 — 2단계는 형태를 고른 뒤에만 셈).</summary>
        private int UnspentTalents()
        {
            if (Classes == null || _player == null) return 0;
            int total = Talents.Unspent(World, Classes, _player, 1);
            if (World.Run.FormId != null) total += Talents.Unspent(World, Classes, _player, 2);
            return total;
        }

        private void OnLevelUp(LevelUpEvent e)
        {
            if (e.Unit != _playerId) return;
            int points = UnspentTalents();
            if (points > 0) _hud.SetHint(HudText.TalentPointsReady(points));
        }

        /// <summary>
        /// 3-21c 특성 창 (T): 점수가 남은 가장 낮은 단계의 찍을 수 있는 특성을 카드로 (캐릭터 선택 창을 같이 씀, Esc 닫기).
        /// 고르면 1점(틱 밖 — Talents.Spend) 뒤 이벤트를 비우고, 점수가 남았으면 다시 연다.
        /// </summary>
        private void OpenTalents()
        {
            if (Classes == null || _player == null || !_classCatalog.TryGetClass(World.Run.ClassId, out var entry) || entry.Talents == null) return;
            int tier = Talents.Unspent(World, Classes, _player, 1) > 0 ? 1
                : World.Run.FormId != null && Talents.Unspent(World, Classes, _player, 2) > 0 ? 2 : 0;
            if (tier == 0)
            {
                _hud.SetHint(HudText.NoTalentPoints);
                return;
            }
            var ids = new System.Collections.Generic.List<string>();
            var names = new System.Collections.Generic.List<string>();
            var details = new System.Collections.Generic.List<string>();
            foreach (var t in entry.Talents)
            {
                if (t.Tier != tier || (!string.IsNullOrEmpty(t.FormId) && t.FormId != World.Run.FormId)) continue;
                ids.Add(t.Id);
                names.Add($"{(string.IsNullOrEmpty(t.DisplayName) ? t.Id : t.DisplayName)}  {World.Run.TalentPoints(t.Id)}/{t.MaxPoints}");
                details.Add(t.Description);
            }
            if (ids.Count == 0) return;
            var world = World;
            _characterSelect.Open(null, names, 0, index =>
            {
                if (World != world || _player == null) return;
                var result = Talents.Spend(World, Classes, _player, ids[index]);
                World.Events.Flush();
                if (result == SpendTalentResult.Maxed) _hud.SetHint(HudText.TalentMaxed);
                if (UnspentTalents() > 0) OpenTalents();
            }, title: HudText.TalentSelectTitle(tier, Talents.Unspent(World, Classes, _player, tier)), hint: HudText.TalentSelectHint,
               details: details, onCancel: () => { });
        }

        // ── 마법 제작대 (3-28b, 결정 2026-11-05): 만들기 · 강화 · 분해 — 카드 창(형태 · 특성 창과 같음), 모두 틱 밖 ──

        private WorkbenchConfig _workbench;

        private System.Collections.Generic.List<string> RelicIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            foreach (var id in Items.Ids) if (Relics.IsRelic(Items, id)) ids.Add(id);
            return ids;
        }

        private string RelicEffectsText(string id, int upgrade)
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var e in Relics.EffectsOf(Items, id)) lines.Add(HudText.RelicEffect(e, 1 + upgrade));
            return string.Join("\n", lines);
        }

        /// <summary>제작대 첫 창: 만들기 · 강화 · 분해 (Esc 닫기).</summary>
        private void OpenWorkbench()
        {
            if (_characterSelect == null || _workbench == null || _player == null || _runEnded) return;
            var names = new System.Collections.Generic.List<string> { HudText.WorkbenchCraft, HudText.WorkbenchUpgrade, HudText.WorkbenchDisassemble };
            var details = new System.Collections.Generic.List<string>
            {
                HudText.WorkbenchCraftDetail(_workbench.CraftCost),
                HudText.WorkbenchUpgradeDetail(_workbench.UpgradeCosts),
                HudText.WorkbenchDisassembleDetail(_workbench.DisassembleGain),
            };
            var world = World;
            _characterSelect.Open(null, names, 0, index =>
            {
                if (World != world || _player == null) return;
                if (index == 0) OpenWorkbenchCraft();
                else if (index == 1) OpenWorkbenchUpgrade();
                else OpenWorkbenchDisassemble();
            }, title: HudText.WorkbenchTitle(World.Run.Ether), hint: HudText.WorkbenchHint, details: details, onCancel: () => { });
        }

        private void OpenWorkbenchCraft()
        {
            var ids = RelicIds();
            if (ids.Count == 0) return;
            var names = new System.Collections.Generic.List<string>();
            var details = new System.Collections.Generic.List<string>();
            foreach (var id in ids)
            {
                names.Add(_hudText.Item(id));
                details.Add(RelicEffectsText(id, 0) + "\n\n" + HudText.EtherCost(_workbench.CraftCost));
            }
            var world = World;
            _characterSelect.Open(null, names, 0, index =>
            {
                if (World != world || _player == null) return;
                AfterWorkbench(Workbench.Craft(World, Items, _workbench, _player, ids[index]));
            }, title: HudText.WorkbenchTitle(World.Run.Ether), hint: HudText.WorkbenchHint, details: details, onCancel: OpenWorkbench);
        }

        /// <summary>강화 대상: 낀 유물(칸) → 배낭 유물(칸 번호) 순.</summary>
        private void OpenWorkbenchUpgrade()
        {
            var targets = new System.Collections.Generic.List<(int slot, int index, InventoryRef relic)>();
            for (int i = 0; i < _player.Relics.Length; i++)
                if (!_player.Relics[i].IsEmpty) targets.Add((i, -1, _player.Relics[i]));
            for (int i = 0; i < _player.InventorySlots.Length; i++)
                if (_player.InventorySlots[i].Kind == InventoryKind.Item && Relics.IsRelic(Items, _player.InventorySlots[i].Id)) targets.Add((-1, i, _player.InventorySlots[i]));
            if (targets.Count == 0)
            {
                _hud.SetHint(HudText.NoRelics);
                OpenWorkbench();
                return;
            }
            var names = new System.Collections.Generic.List<string>();
            var details = new System.Collections.Generic.List<string>();
            foreach (var t in targets)
            {
                names.Add(_hudText.Entry(t.relic) + (t.slot >= 0 ? HudText.RelicWornMark : string.Empty));
                int cost = _workbench.UpgradeCost(t.relic.Upgrade);
                details.Add(cost < 0 ? RelicEffectsText(t.relic.Id, t.relic.Upgrade) + "\n\n" + HudText.RelicMaxUpgrade
                    : $"{RelicEffectsText(t.relic.Id, t.relic.Upgrade)}\n→ {RelicEffectsText(t.relic.Id, t.relic.Upgrade + 1)}\n\n{HudText.EtherCost(cost)}");
            }
            var world = World;
            _characterSelect.Open(null, names, 0, index =>
            {
                if (World != world || _player == null) return;
                var t = targets[index];
                AfterWorkbench(Workbench.Upgrade(World, Items, _workbench, _player, t.slot, t.index));
            }, title: HudText.WorkbenchTitle(World.Run.Ether), hint: HudText.WorkbenchHint, details: details, onCancel: OpenWorkbench);
        }

        private void OpenWorkbenchDisassemble()
        {
            var indices = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _player.InventorySlots.Length; i++)
                if (_player.InventorySlots[i].Kind == InventoryKind.Item && Relics.IsRelic(Items, _player.InventorySlots[i].Id)) indices.Add(i);
            if (indices.Count == 0)
            {
                _hud.SetHint(HudText.NoRelicsInBackpack);
                OpenWorkbench();
                return;
            }
            var names = new System.Collections.Generic.List<string>();
            var details = new System.Collections.Generic.List<string>();
            foreach (var i in indices)
            {
                var r = _player.InventorySlots[i];
                names.Add(_hudText.Entry(r));
                details.Add(RelicEffectsText(r.Id, r.Upgrade) + "\n\n" + HudText.EtherGain(_workbench.DisassembleGain));
            }
            var world = World;
            _characterSelect.Open(null, names, 0, index =>
            {
                if (World != world || _player == null) return;
                AfterWorkbench(Workbench.Disassemble(World, Items, _workbench, _player, indices[index]));
            }, title: HudText.WorkbenchTitle(World.Run.Ether), hint: HudText.WorkbenchHint, details: details, onCancel: OpenWorkbench);
        }

        /// <summary>제작대 결과: 이벤트를 비우고(로그 · 저장), 안 됐으면 이유 — 첫 창으로 돌아간다.</summary>
        private void AfterWorkbench(WorkbenchResult result)
        {
            World.Events.Flush();
            _hud.SetHint(result == WorkbenchResult.Done ? string.Empty : HudText.WorkbenchFailed(result));
            OpenWorkbench();
        }

        /// <summary>판이 끝남 (3-4b): 입력을 멈추고 결과 화면. R로 새 판.</summary>
        private void OnRunEnded(RunEndedEvent e)
        {
            _runEnded = true;
            _realTime = null; // 3-27: 실시간 시계 멈춤
            RunSaveStore.Delete(); // 3-27: 패배 · 승리 = 저장 삭제 (되돌리기 없음)
            _input.Unbind();
            _result.Show(ResultModel.From(World, Seed, _player, _hudText));
            Debug.Log($"[GameRoot] 판 끝: {e.Outcome} — R로 새 판");
        }
    }
}
