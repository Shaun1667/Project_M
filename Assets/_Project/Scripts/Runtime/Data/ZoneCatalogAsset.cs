using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Dungeon;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 적 종류 표 + 구역 표 (Assets/_Project/Data/Units/ZoneCatalog.asset, 3-17a — 결정 2026-11-04: 5구역 · 25층). 기준은 BALANCE.md 20번.
    /// 적 종류 ID = 유닛 VisualId (모습 · 이름 · 행동을 이 ID로 찾는다). 보스도 적 줄 하나.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Zone Catalog", fileName = "ZoneCatalog")]
    public sealed class ZoneCatalogAsset : ScriptableObject
    {
        [System.Serializable]
        public struct Enemy
        {
            [Tooltip("적 종류 ID = 모습 ID (EntityVisualCatalog)")] public string Id;
            [Tooltip("화면 이름 (로그)")] public string DisplayName;
            [Min(1)] public int MaxHp;
            [Min(0)] public int AttackDamage;
            [Min(1)] public int SightRange;
            [Min(0)] public int Accuracy;
            [Min(0)] public int Evasion;
            [Min(0)] public int ExpReward;
            [Min(0)] public int ExpMaxLevel;
            [Header("행동 (3-17b)")]
            [Tooltip("한 번 이동 칸 (1 · 2 — 0은 1)")] [Range(0, 2)] public int MoveSteps;
            [Tooltip("이동 비용 틱 (0은 1, 2 = 느림)")] [Min(0)] public int MoveCostTicks;
            [Tooltip("근접 명중 시 상태")] public StatusType HitStatus;
            [Range(0, 100)] public int HitStatusPercent;
            [Tooltip("피해를 받으면 자기 칸에 까는 지형")] public TerrainEffectType HurtTerrain;
            [Tooltip("원거리 주문 (SpellCatalog) — 마나를 씀")] public string SpellId;
            [Min(0)] public int MaxMana;
            [Tooltip("플레이어가 이보다 가까우면 물러남 (마나가 있을 때)")] [Min(0)] public int KeepDistance;
            [Tooltip("속성 피해 배수 % (약점 · 저항) — 없는 속성은 100")] public List<ElementPercent> DamagePercents;
        }

        [System.Serializable]
        public struct ElementPercent
        {
            public Element Element;
            [Min(0)] public int Percent;
        }

        [System.Serializable]
        public struct ZoneEnemyRow
        {
            public string EnemyId;
            [Min(0)] public int Weight;
        }

        [System.Serializable]
        public struct Zone
        {
            public string Name;
            [Min(1)] public int FirstFloor;
            [Min(1)] public int LastFloor;
            [Tooltip("적 · 보스 체력 · 피해 · 명중 · 회피 배수 % (100 = 그대로)")] [Min(1)] public int StatPercent;
            public List<ZoneEnemyRow> Enemies;
            [Tooltip("보스 적 ID — 구역 마지막 층이 보스 층. 비면 보스 없음")] public string BossId;
        }

        [SerializeField] private List<Enemy> _enemies = new List<Enemy>();
        [SerializeField] private List<Zone> _zones = new List<Zone>();
        [Tooltip("마법서 · 무기 · 갑옷 · 강화 아이템을 나눠 놓는 층 수 (1~n층) — 아직 채우지 않은 깊은 층에 흩어지지 않게. 0 = 전체")]
        [Min(0)] [SerializeField] private int _lootPlanFloors = 10;

        public int LootPlanFloors => _lootPlanFloors;
        public IReadOnlyList<Enemy> Enemies => _enemies;

        public bool TryGetEnemy(string id, out Enemy enemy)
        {
            foreach (var e in _enemies)
                if (e.Id == id) { enemy = e; return true; }
            enemy = default;
            return false;
        }

        public static UnitDef ToDef(Enemy e) => new UnitDef(e.Id, e.MaxHp, e.AttackDamage, e.SightRange, e.MaxMana,
            accuracy: e.Accuracy, evasion: e.Evasion, expReward: e.ExpReward, expMaxLevel: e.ExpMaxLevel);

        /// <summary>적 종류 행동 표 (3-17b).</summary>
        public EnemyBook ToEnemyBook()
        {
            var list = new List<EnemyTraits>(_enemies.Count);
            foreach (var e in _enemies)
            {
                var percents = new Dictionary<Element, int>();
                if (e.DamagePercents != null) foreach (var p in e.DamagePercents) percents[p.Element] = p.Percent;
                list.Add(new EnemyTraits(e.Id, e.DisplayName, System.Math.Max(1, e.MoveSteps), System.Math.Max(1, e.MoveCostTicks),
                    e.HitStatus, e.HitStatusPercent, e.HurtTerrain, e.SpellId, e.KeepDistance, percents));
            }
            return new EnemyBook(list);
        }

        /// <summary>화면 이름 (VisualId → 이름, HUD 로그용).</summary>
        public Dictionary<string, string> DisplayNames()
        {
            var names = new Dictionary<string, string>();
            foreach (var e in _enemies) if (!string.IsNullOrEmpty(e.DisplayName)) names[e.Id] = e.DisplayName;
            return names;
        }

        /// <summary>에디터 Setup용 (3-17b): 구역 적 표가 예전 자리표시(enemy_melee 하나)뿐이면 rows로 바꾼다.</summary>
        public bool ReplacePlaceholderEnemies(int firstFloor, params (string id, int weight)[] rows)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.FirstFloor != firstFloor) continue;
                if (z.Enemies != null && !(z.Enemies.Count == 1 && z.Enemies[0].EnemyId == "enemy_melee")) return false;
                z.Enemies = new List<ZoneEnemyRow>();
                foreach (var (id, weight) in rows) z.Enemies.Add(new ZoneEnemyRow { EnemyId = id, Weight = weight });
                _zones[i] = z;
                return true;
            }
            return false;
        }

        /// <summary>에디터 Setup용: 구역 보스가 from이면 to로 (임시 보스 → 패턴 보스).</summary>
        public bool ReplaceBoss(int firstFloor, string from, string to)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.FirstFloor != firstFloor || z.BossId != from) continue;
                z.BossId = to;
                _zones[i] = z;
                return true;
            }
            return false;
        }

        /// <summary>에디터 Setup용: 구역 세기가 from이면 to로.</summary>
        public bool ReplaceStatPercent(int firstFloor, int from, int to)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.FirstFloor != firstFloor || z.StatPercent != from) continue;
                z.StatPercent = to;
                _zones[i] = z;
                return true;
            }
            return false;
        }

        public ZoneBook ToBook()
        {
            var zones = new List<ZoneDef>(_zones.Count);
            foreach (var z in _zones)
            {
                var enemies = new List<ZoneEnemy>();
                foreach (var row in z.Enemies ?? new List<ZoneEnemyRow>())
                {
                    if (!TryGetEnemy(row.EnemyId, out var e)) throw new System.ArgumentException($"구역 '{z.Name}': 적 '{row.EnemyId}'이 적 표에 없다.");
                    enemies.Add(new ZoneEnemy(ToDef(e), row.Weight));
                }
                UnitDef boss = null;
                if (!string.IsNullOrEmpty(z.BossId))
                {
                    if (!TryGetEnemy(z.BossId, out var b)) throw new System.ArgumentException($"구역 '{z.Name}': 보스 '{z.BossId}'가 적 표에 없다.");
                    boss = ToDef(b);
                }
                zones.Add(new ZoneDef(z.Name, z.FirstFloor, z.LastFloor, enemies, z.StatPercent, boss));
            }
            return new ZoneBook(zones);
        }

        /// <summary>에디터 Setup용: 같은 ID가 없을 때만 추가 (사용자가 고친 값은 그대로).</summary>
        public bool AddIfMissing(Enemy enemy)
        {
            foreach (var e in _enemies)
                if (e.Id == enemy.Id) return false;
            _enemies.Add(enemy);
            return true;
        }

        /// <summary>에디터 Setup용: 같은 시작 층의 구역이 없을 때만 추가.</summary>
        public bool AddIfMissing(Zone zone)
        {
            foreach (var z in _zones)
                if (z.FirstFloor == zone.FirstFloor) return false;
            _zones.Add(zone);
            _zones.Sort((a, b) => a.FirstFloor.CompareTo(b.FirstFloor));
            return true;
        }
    }
}
