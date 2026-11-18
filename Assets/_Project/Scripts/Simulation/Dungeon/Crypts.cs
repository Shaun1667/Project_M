using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Items;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>지하 묘지 수치 (3-47, SPD Crypt). 값은 FloorDecorConfigAsset에서 — 기준은 BALANCE.md 16번.</summary>
    public sealed class CryptConfig
    {
        /// <summary>관을 열면 깨어나는 지킴이 (적 표 ID = 모습 ID) · 수.</summary>
        public string GuardianId { get; }
        public int GuardianCount { get; }
        /// <summary>지킴이 세기 % = 100 + 층마다 × (층 − 1) — UnitDef.Scaled.</summary>
        public int GuardianPercentPerFloor { get; }
        /// <summary>보상 갑옷 (낮은 티어부터) — FloorsPerArmor 층마다 한 단계씩.</summary>
        public IReadOnlyList<string> Armors { get; }
        public int FloorsPerArmor { get; }
        /// <summary>보상 갑옷 강화 최소 ~ 최대.</summary>
        public int UpgradeMin { get; }
        public int UpgradeMax { get; }

        public CryptConfig(string guardianId, int guardianCount, int guardianPercentPerFloor, IReadOnlyList<string> armors, int floorsPerArmor,
            int upgradeMin, int upgradeMax)
        {
            if (string.IsNullOrEmpty(guardianId)) throw new ArgumentException("지킴이 ID가 비어 있다", nameof(guardianId));
            if (guardianCount < 0) throw new ArgumentOutOfRangeException(nameof(guardianCount));
            if (guardianPercentPerFloor < 0) throw new ArgumentOutOfRangeException(nameof(guardianPercentPerFloor));
            if (armors == null || armors.Count == 0) throw new ArgumentException("보상 갑옷이 없다", nameof(armors));
            if (floorsPerArmor < 1) throw new ArgumentOutOfRangeException(nameof(floorsPerArmor));
            if (upgradeMin < 0 || upgradeMax < upgradeMin) throw new ArgumentOutOfRangeException(nameof(upgradeMax), "0 ≤ 최소 ≤ 최대");
            GuardianId = guardianId;
            GuardianCount = guardianCount;
            GuardianPercentPerFloor = guardianPercentPerFloor;
            Armors = armors;
            FloorsPerArmor = floorsPerArmor;
            UpgradeMin = upgradeMin;
            UpgradeMax = upgradeMax;
        }

        /// <summary>그 층의 보상 갑옷 ID.</summary>
        public string ArmorFor(int floor) => Armors[Math.Min(Armors.Count - 1, Math.Max(0, (floor - 1) / FloorsPerArmor))];

        /// <summary>그 층의 지킴이 세기 %.</summary>
        public int GuardianPercent(int floor) => 100 + GuardianPercentPerFloor * Math.Max(0, floor - 1);
    }

    /// <summary>지하 묘지 규칙 연결 (3-47): 수치 · 지킴이 기본 수치 · 적 AI. World.SetCryptRules로 넣는다 — 규칙 연결이라 저장하지 않는다.</summary>
    public sealed class CryptRules
    {
        public CryptConfig Config { get; }
        public UnitDef Guardian { get; }
        public ICommandSource Ai { get; }

        public CryptRules(CryptConfig config, UnitDef guardian, ICommandSource ai)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Guardian = guardian ?? throw new ArgumentNullException(nameof(guardian));
            Ai = ai ?? throw new ArgumentNullException(nameof(ai));
        }
    }

    /// <summary>
    /// 지하 묘지 관 열기 (3-47, 결정 2026-11-18 — SPD Crypt): 옆(8방향) 관(TileType.Tomb)을 열면 빈 관(OpenTomb)이 되고,
    /// 그 층 티어의 갑옷(강화 UpgradeMin ~ Max)이 연 유닛 발밑에 떨어지며(곧 주움), 관 둘레 빈 칸에 지킴이(원혼)가 깨어나 연 유닛을 안다.
    /// 갑옷 · 강화는 열 때 정한다(RngStream.Loot) — 관 안에 미리 둔 상태가 없다.
    /// </summary>
    public static class Crypts
    {
        public static bool Open(World world, Unit unit, GridPos tomb)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null || !unit.IsAlive || !world.Map.InBounds(tomb) || world.Map.Get(tomb) != TileType.Tomb) return false;
            if (GridMovement.Chebyshev(unit.Pos, tomb) != 1) return false;
            world.SetTile(tomb, TileType.OpenTomb);
            var rules = world.CryptRules;
            string armor = null;
            int upgrade = 0, spawned = 0;
            if (rules != null)
            {
                var rng = world.Rng.Get(RngStream.Loot);
                var config = rules.Config;
                armor = config.ArmorFor(world.Floor);
                upgrade = config.UpgradeMin + rng.Range(0, config.UpgradeMax - config.UpgradeMin + 1);
                world.FloorItems.Add(unit.Pos, armor, upgrade: upgrade);
                world.Events.Publish(new FloorItemPlacedEvent(unit.Pos, armor, Identification.VisualOf(world, armor)));

                var def = rules.Guardian.Scaled(config.GuardianPercent(world.Floor));
                foreach (var dir in GridMovement.Directions)
                {
                    if (spawned >= config.GuardianCount) break;
                    var p = tomb + dir;
                    if (!world.Map.IsWalkable(p) || world.Units.TryGetAt(p, out _)) continue;
                    var guardian = UnitSpawner.SpawnEnemy(world, def, p, rules.Ai);
                    guardian.LastSeenTargetPos = unit.Pos;
                    spawned++;
                }
            }
            world.Events.Publish(new TombOpenedEvent(tomb, armor, upgrade, spawned));
            return true;
        }
    }

    /// <summary>관 열기 (3-47, 차례 1): 옆 관을 연다 (Crypts.Open). 관이 아니거나 이미 열렸으면 차례만 쓴다.</summary>
    public sealed class OpenTombCommand : ICommand
    {
        private readonly World _world;

        public GridPos Tomb { get; }
        public int CostTicks { get; }

        public OpenTombCommand(World world, GridPos tomb, int costTicks)
        {
            if (costTicks < 1) throw new ArgumentOutOfRangeException(nameof(costTicks));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            Tomb = tomb;
            CostTicks = costTicks;
        }

        public void Execute(in TickContext context)
        {
            if (_world.Units.TryGet(context.Actor, out var unit)) Crypts.Open(_world, unit, Tomb);
        }
    }

    /// <summary>관을 열었다 (3-47 — 로그). ArmorId = 떨어진 갑옷(규칙이 없으면 null) · Upgrade · Guardians = 깨어난 지킴이 수.</summary>
    public sealed class TombOpenedEvent : ISimEvent
    {
        public GridPos Pos { get; }
        public string ArmorId { get; }
        public int Upgrade { get; }
        public int Guardians { get; }

        public TombOpenedEvent(GridPos pos, string armorId, int upgrade, int guardians)
        {
            Pos = pos;
            ArmorId = armorId;
            Upgrade = upgrade;
            Guardians = guardians;
        }
    }
}
