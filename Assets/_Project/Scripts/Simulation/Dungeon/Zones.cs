using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>구역의 적 한 줄 (3-17a): 종류 + 뽑기 무게.</summary>
    public readonly struct ZoneEnemy
    {
        public UnitDef Def { get; }
        public int Weight { get; }

        public ZoneEnemy(UnitDef def, int weight)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
            Weight = weight;
        }
    }

    /// <summary>
    /// 구역 하나 (3-17a, 결정 2026-11-04 — 5구역 · 25층, SPD): 층 범위 · 적 표 · 세기 · 보스.
    /// 보스가 있으면 구역 마지막 층이 보스 층이다. 수치는 ZoneCatalog.asset (BALANCE.md 20번).
    /// </summary>
    public sealed class ZoneDef
    {
        public string Name { get; }
        public int FirstFloor { get; }
        public int LastFloor { get; }
        /// <summary>세기를 적용한 적 표 (StatPercent).</summary>
        public IReadOnlyList<ZoneEnemy> Enemies { get; }
        public int StatPercent { get; }
        /// <summary>세기를 적용한 보스. null이면 보스 층 없음.</summary>
        public UnitDef Boss { get; }
        /// <summary>층 꾸미기 웅덩이의 지형 (3-18 — 얼음 동굴 = 빙판). 기본 물.</summary>
        public TerrainEffectType PuddleTerrain { get; }

        public ZoneDef(string name, int firstFloor, int lastFloor, IReadOnlyList<ZoneEnemy> enemies, int statPercent = 100, UnitDef boss = null,
            TerrainEffectType puddleTerrain = TerrainEffectType.Water)
        {
            if (puddleTerrain == TerrainEffectType.None) throw new ArgumentException("웅덩이 지형이 없다.", nameof(puddleTerrain));
            if (firstFloor < 1 || lastFloor < firstFloor) throw new ArgumentOutOfRangeException(nameof(lastFloor), "층 범위가 잘못됐다.");
            if (statPercent < 1) throw new ArgumentOutOfRangeException(nameof(statPercent));
            if (enemies == null || enemies.Count == 0) throw new ArgumentException("구역 적 표가 비어 있다.", nameof(enemies));
            int total = 0;
            var scaled = new List<ZoneEnemy>();
            foreach (var e in enemies)
            {
                total += e.Weight;
                scaled.Add(new ZoneEnemy(e.Def.Scaled(statPercent), e.Weight));
            }
            if (total <= 0) throw new ArgumentException("구역 적 무게 합이 0이다.", nameof(enemies));
            Name = name ?? string.Empty;
            FirstFloor = firstFloor;
            LastFloor = lastFloor;
            StatPercent = statPercent;
            Enemies = scaled;
            Boss = boss?.Scaled(statPercent);
            PuddleTerrain = puddleTerrain;
        }
    }

    /// <summary>구역 표 (3-17a). 구역은 1층부터 빈틈없이 이어진다. 상태가 없다 (규칙 6).</summary>
    public sealed class ZoneBook
    {
        private readonly List<ZoneDef> _zones;

        public IReadOnlyList<ZoneDef> Zones => _zones;
        /// <summary>마지막 구역의 마지막 층 = 전체 층 수.</summary>
        public int FloorCount => _zones[_zones.Count - 1].LastFloor;

        public ZoneBook(IReadOnlyList<ZoneDef> zones)
        {
            if (zones == null || zones.Count == 0) throw new ArgumentException("구역이 없다.", nameof(zones));
            _zones = new List<ZoneDef>(zones);
            int next = 1;
            foreach (var z in _zones)
            {
                if (z.FirstFloor != next) throw new ArgumentException($"구역 '{z.Name}'은 {next}층에서 시작해야 한다.", nameof(zones));
                next = z.LastFloor + 1;
            }
        }

        /// <summary>그 층의 구역. 마지막 구역보다 깊으면 마지막 구역.</summary>
        public ZoneDef ZoneOf(int floor)
        {
            foreach (var z in _zones)
                if (floor <= z.LastFloor) return z;
            return _zones[_zones.Count - 1];
        }

        public bool IsBossFloor(int floor)
        {
            var z = ZoneOf(floor);
            return z.Boss != null && floor == z.LastFloor;
        }

        /// <summary>보스 층의 보스, 아니면 null.</summary>
        public UnitDef BossOf(int floor) => IsBossFloor(floor) ? ZoneOf(floor).Boss : null;

        /// <summary>그 층의 적 한 종류 (무게, 표 순서 고정).</summary>
        public UnitDef PickEnemy(int floor, IRng rng)
        {
            var z = ZoneOf(floor);
            int total = 0;
            foreach (var e in z.Enemies) total += e.Weight;
            int roll = rng.Range(0, total);
            foreach (var e in z.Enemies)
            {
                if (roll < e.Weight) return e.Def;
                roll -= e.Weight;
            }
            return z.Enemies[z.Enemies.Count - 1].Def;
        }
    }

    /// <summary>중간 보스를 쓰러뜨려 봉인된 계단이 열렸다 (3-17a, 로그).</summary>
    public sealed class StairsUnsealedEvent : ISimEvent
    {
        public GridPos Pos { get; }

        public StairsUnsealedEvent(GridPos pos)
        {
            Pos = pos;
        }
    }
}
