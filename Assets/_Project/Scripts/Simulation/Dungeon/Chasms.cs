using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Status;
using ProjectM.Units;

namespace ProjectM.Dungeon
{
    /// <summary>구덩이 · 추락 수치 (3-51, SPD Chasm). 값은 FloorDecorConfigAsset에서 — 기준은 BALANCE.md 16번.</summary>
    public sealed class ChasmConfig
    {
        /// <summary>추락 피해 = 최대 체력 × 최소 % ~ 최대 % (최소 1).</summary>
        public int FallDamageMinPercent { get; }
        public int FallDamageMaxPercent { get; }

        public ChasmConfig(int fallDamageMinPercent, int fallDamageMaxPercent)
        {
            if (fallDamageMinPercent < 0 || fallDamageMaxPercent < fallDamageMinPercent) throw new ArgumentOutOfRangeException(nameof(fallDamageMaxPercent), "0 ≤ 최소 ≤ 최대");
            FallDamageMinPercent = fallDamageMinPercent;
            FallDamageMaxPercent = fallDamageMaxPercent;
        }
    }

    /// <summary>구덩이 규칙 연결 (3-51): 수치 · 떨어질 수 있는 층인지(다음 층이 보스 · 제단 층이 아니고 마지막 층이 아님). World.SetChasmRules — 저장하지 않는다.</summary>
    public sealed class ChasmRules
    {
        public ChasmConfig Config { get; }
        private readonly Func<int, bool> _canFallFrom;

        public ChasmRules(ChasmConfig config, Func<int, bool> canFallFrom)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _canFallFrom = canFallFrom ?? throw new ArgumentNullException(nameof(canFallFrom));
        }

        public bool CanFallFrom(int floor) => _canFallFrom(floor);
    }

    /// <summary>
    /// 구덩이 · 약한 바닥 방 (3-51, 결정 2026-11-18 — SPD Weak Floor): 구덩이(TileType.Chasm)는 적 · 길 찾기가 피하고, 플레이어는
    /// 부양 중이면 떠서 지나가고(HoverCommand), 아니면 두 번 부딪혀 뛰어들거나 부양이 끝난 채 구덩이 위에 있으면 아래층으로 떨어진다(FloorTransition.TryFall — 틱 밖).
    /// 떨어지면 아래층 아무 빈 맨땅에 내려서고 최대 체력의 일정 %를 잃는다(RngStream.Combat).
    /// </summary>
    public static class Chasms
    {
        public static bool IsLevitating(StatusBook statuses, Unit unit) => StatusEffects.Any(statuses, unit, d => d.Levitates);

        /// <summary>플레이어가 구덩이 위에 떠 있지 않은 채 서 있다 — 떨어져야 한다.</summary>
        public static bool ShouldFall(World world, StatusBook statuses) =>
            PlayerActions.TryGetPlayer(world, out var player) && player.IsAlive
            && world.Map.InBounds(player.Pos) && world.Map.Get(player.Pos) == TileType.Chasm && !IsLevitating(statuses, player);

        /// <summary>이 층에서 떨어질 수 있는지 (규칙이 없으면 못 떨어짐).</summary>
        public static bool CanFall(World world) => world.ChasmRules != null && world.ChasmRules.CanFallFrom(world.Floor);

        /// <summary>추락 피해: 최대 체력 × 최소 ~ 최대 % (내림, 최소 1). 떨어져서 죽지는 않는다 — 체력 1을 남긴다 (틱 밖이라 사망 판정이 바로 돌지 않음).</summary>
        public static int FallDamage(World world, Unit player)
        {
            var c = world.ChasmRules?.Config;
            if (c == null) return 0;
            int min = player.MaxHp * c.FallDamageMinPercent / 100, max = player.MaxHp * c.FallDamageMaxPercent / 100;
            int damage = Math.Max(1, world.Rng.Get(RngStream.Combat).Range(min, max + 1));
            return Math.Max(0, Math.Min(damage, player.Hp - 1));
        }

        /// <summary>내려설 칸: 맨땅 · 빈 칸 중 하나 (y → x 순 목록에서, RngStream.Combat). 없으면 null.</summary>
        public static GridPos? PickLanding(World world, GridMap map, ICollection<GridPos> occupied)
        {
            var candidates = new List<GridPos>();
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (GridMap.IsGroundType(map.Get(p)) && !occupied.Contains(p)) candidates.Add(p);
                }
            if (candidates.Count == 0) return null;
            return candidates[world.Rng.Get(RngStream.Combat).Range(0, candidates.Count)];
        }

        /// <summary>
        /// 약한 바닥 방 (3-51): 방 칸은 입구에서 깊이 순(한 줄 = size칸). 입구 쪽 첫 줄(발판)만 남기고 나머지를 구덩이로, 맨 안쪽 줄 가운데 한 칸(돌기둥)은 바닥으로 남긴다.
        /// 돌려주는 칸 = 그 돌기둥 (보상 자리).
        /// </summary>
        public static List<GridPos> PlaceWeakFloor(World world, List<GridPos> cells)
        {
            int size = (int)Math.Round(Math.Sqrt(cells.Count));
            var pillar = cells[(size - 1) * size + size / 2];
            for (int a = 1; a < size; a++)
                for (int b = 0; b < size; b++)
                {
                    var c = cells[a * size + b];
                    if (c != pillar) world.SetTile(c, TileType.Chasm);
                }
            return new List<GridPos> { pillar };
        }
    }

    /// <summary>부양 중 한 걸음 (3-51, 차례 1): 구덩이 칸으로도 들어간다. 떠 있지 않거나 막혔으면 차례만 쓴다.</summary>
    public sealed class HoverCommand : ICommand
    {
        private readonly World _world;
        private readonly StatusBook _statuses;
        public GridPos To { get; }
        public int CostTicks { get; }

        public HoverCommand(World world, StatusBook statuses, GridPos to, int costTicks)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _statuses = statuses;
            To = to;
            CostTicks = Math.Max(1, costTicks);
        }

        public void Execute(in TickContext context)
        {
            if (!_world.Units.TryGet(context.Actor, out var unit) || !unit.IsAlive || !Chasms.IsLevitating(_statuses, unit)) return;
            if (GridMovement.Chebyshev(unit.Pos, To) != 1 || !_world.Map.InBounds(To) || _world.Units.IsOccupied(To)) return;
            var tile = _world.Map.Get(To);
            if (tile != TileType.Chasm && !GridMap.IsWalkableType(tile)) return;
            var from = unit.Pos;
            _world.Units.Move(unit.Id, To);
            _world.Events.Publish(new EntityMovedEvent(unit.Id, from, To));
        }
    }

    /// <summary>구덩이로 떨어져 아래층에 내려섰다 (로그).</summary>
    public sealed class FellEvent : ISimEvent
    {
        public int Damage { get; }
        public int Floor { get; }

        public FellEvent(int damage, int floor)
        {
            Damage = damage;
            Floor = floor;
        }
    }
}
