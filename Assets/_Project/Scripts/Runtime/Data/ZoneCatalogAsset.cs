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
            [Header("패턴 보스 (3-17d)")]
            [Tooltip("힘 모으기 주문 — 예고 칸 표시 후 다음 차례에 터짐 (사거리 = 주문 Range)")] public string ChargeSpellId;
            [Range(0, 100)] public int ChargePercent;
            [Tooltip("플레이어가 붙으면 순간이동 확률 %")] [Range(0, 100)] public int TeleportPercent;
            [Tooltip("소환할 적 ID (이 표) · 수 · 체력 % 이하에서 한 번")] public string SummonId;
            [Min(0)] public int SummonCount;
            [Range(0, 100)] public int SummonAtHpPercent;
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
            [Tooltip("구역 분위기 (3-17c): 해 색. 알파 0 = 기본 색 그대로")] public Color LightColor;
            [Tooltip("층 꾸미기 웅덩이의 지형 (3-18): None = 물. 얼음 동굴 = 빙판")] public TerrainEffectType PuddleTerrain;
            [Tooltip("층 꾸미기에 풀을 깔지 않음 (3-19 용암 동굴 — 용암이 풀로 번지지 않게)")] public bool NoGrass;
            [Tooltip("제단 구역 (3-23a): 이 유물 아이템을 큰 방에 놓고 적은 없음. 비면 보통 구역")] public string AltarItemId;
        }

        /// <summary>에디터 Setup용 (3-19): 풀 없음을 켠다 (이미 켜져 있으면 false).</summary>
        public bool SetNoGrass(int firstFloor)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.FirstFloor != firstFloor || z.NoGrass) continue;
                z.NoGrass = true;
                _zones[i] = z;
                return true;
            }
            return false;
        }

        /// <summary>에디터 Setup용 (3-18): 웅덩이 지형이 비어 있으면(None) 채운다.</summary>
        public bool SetPuddleTerrainIfNone(int firstFloor, TerrainEffectType type)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.FirstFloor != firstFloor || z.PuddleTerrain != TerrainEffectType.None) continue;
                z.PuddleTerrain = type;
                _zones[i] = z;
                return true;
            }
            return false;
        }

        /// <summary>에디터 Setup용 (3-18): 구역 이름이 from이면 to로 (임시 구역 → 테마 구역).</summary>
        public bool ReplaceName(int firstFloor, string from, string to)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.FirstFloor != firstFloor || z.Name != from) continue;
                z.Name = to;
                _zones[i] = z;
                return true;
            }
            return false;
        }

        /// <summary>에디터 Setup용 (3-18): 구역 적 표가 정확히 from이면 to로 (3~5구역 임시 사막 4종 → 새 구역 적).</summary>
        public bool ReplaceEnemies(int firstFloor, string[] from, params (string id, int weight)[] to)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.FirstFloor != firstFloor || z.Enemies == null || z.Enemies.Count != from.Length) continue;
                for (int k = 0; k < from.Length; k++)
                    if (z.Enemies[k].EnemyId != from[k]) return false;
                z.Enemies = new List<ZoneEnemyRow>();
                foreach (var (id, weight) in to) z.Enemies.Add(new ZoneEnemyRow { EnemyId = id, Weight = weight });
                _zones[i] = z;
                return true;
            }
            return false;
        }

        /// <summary>그 층 구역의 해 색 (3-17c). 정하지 않았으면(알파 0) false.</summary>
        public bool TryGetLightColor(int floor, out Color color)
        {
            color = default;
            for (int i = 0; i < _zones.Count; i++)
            {
                if (floor > _zones[i].LastFloor && i < _zones.Count - 1) continue;
                color = _zones[i].LightColor;
                return color.a > 0f;
            }
            return false;
        }

        /// <summary>에디터 Setup용 (3-17c): 해 색이 비어 있으면 채운다.</summary>
        public bool SetLightColorIfEmpty(int firstFloor, Color color)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                if (z.FirstFloor != firstFloor || z.LightColor.a > 0f) continue;
                z.LightColor = color;
                _zones[i] = z;
                return true;
            }
            return false;
        }

        [SerializeField] private List<Enemy> _enemies = new List<Enemy>();
        [SerializeField] private List<Zone> _zones = new List<Zone>();
        [Tooltip("마법서 · 무기 · 갑옷 · 강화 아이템을 나눠 놓는 층 수 (1~n층) — 아직 채우지 않은 깊은 층에 흩어지지 않게. 0 = 전체")]
        [Min(0)] [SerializeField] private int _lootPlanFloors = 10;

        public int LootPlanFloors => _lootPlanFloors;

        [Header("지난 회차 플레이어 보스 (3-23b — 마지막 보스 층)")]
        [Tooltip("기록 체력 배율 % (결정 2026-11-05 — 체력만 3배)")] [Min(1)] [SerializeField] private int _ghostHpPercent = 300;
        [Min(0)] [SerializeField] private int _ghostExpReward = 50;
        [Min(0)] [SerializeField] private int _ghostExpMaxLevel = 30;

        public int GhostHpPercent => _ghostHpPercent;
        public int GhostExpReward => _ghostExpReward;
        public int GhostExpMaxLevel => _ghostExpMaxLevel;
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
                UnitDef summon = null;
                if (!string.IsNullOrEmpty(e.SummonId))
                {
                    if (!TryGetEnemy(e.SummonId, out var s)) throw new System.ArgumentException($"적 '{e.Id}': 소환 '{e.SummonId}'이 적 표에 없다.");
                    summon = ToDef(s);
                }
                list.Add(new EnemyTraits(e.Id, e.DisplayName, System.Math.Max(1, e.MoveSteps), System.Math.Max(1, e.MoveCostTicks),
                    e.HitStatus, e.HitStatusPercent, e.HurtTerrain, e.SpellId, e.KeepDistance, percents,
                    e.ChargeSpellId, e.ChargePercent, e.TeleportPercent, summon, e.SummonCount, e.SummonAtHpPercent));
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
                if (z.FirstFloor != firstFloor || (z.BossId ?? string.Empty) != (from ?? string.Empty)) continue; // null · 빈 문자열 = 보스 없음
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
                var puddle = z.PuddleTerrain == TerrainEffectType.None ? TerrainEffectType.Water : z.PuddleTerrain;
                zones.Add(new ZoneDef(z.Name, z.FirstFloor, z.LastFloor, enemies, z.StatPercent, boss, puddle, grass: !z.NoGrass, altarItemId: z.AltarItemId));
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
