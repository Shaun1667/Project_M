using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 입력 → 플레이어 커맨드 변환 규칙 (키보드 · 클릭 이동 공용). 입력 어댑터는 여기서 받은 커맨드를 시계에 넘기기만 한다.
    /// 방향으로 한 칸: 적 진영 유닛이 있으면 공격, 비어 있으면 이동, 벽 · 아군이면 커맨드 없음 (차례를 쓰지 않음).
    /// </summary>
    public static class PlayerActions
    {
        /// <param name="statuses">상태 규칙 (3-13a 신속 — 이동 두 칸). null이면 한 칸.</param>
        public static bool TryStep(World world, GameConfig config, GridPos direction, out ICommand command, ProjectM.Status.StatusBook statuses = null)
        {
            command = null;
            if (!TryGetPlayer(world, out var player)) return false;
            return TryStepTo(world, config, player, player.Pos + direction, out command, player.Pos + direction + direction, statuses);
        }

        /// <summary>인접 칸 to로 이동 또는 공격. 신속이면(statuses) 이동 뒤 next 칸까지 같은 차례에 (3-13a).</summary>
        public static bool TryStepTo(World world, GameConfig config, Unit player, GridPos to, out ICommand command, GridPos? next = null,
            ProjectM.Status.StatusBook statuses = null)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (config == null) throw new ArgumentNullException(nameof(config));
            command = null;
            if (player == null || !player.IsAlive) return false;
            if (!GridMovement.CanStep(world.Map, player.Pos, to)) return false;

            if (world.Units.TryGetAt(to, out var occupant))
            {
                if (!Factions.AreHostile(occupant.Faction, player.Faction) || !occupant.IsAlive) return false; // 3-13b: 아군(분신)은 막힘
                command = new AttackCommand(world, occupant.Id, MeleeWeapon.AttackCost(player, config.DefaultActionCostTicks));
                return true;
            }

            bool fast = next.HasValue && ProjectM.Status.StatusEffects.MoveSteps(statuses, player) >= 2 && !world.Units.IsOccupied(next.Value);
            command = new MoveCommand(world, to, config.DefaultActionCostTicks, fast ? next : null);
            return true;
        }

        /// <summary>
        /// 칸 target의 적을 지금 자리에서 친다 (3-7a 클릭 — 창은 2칸 떨어진 적도). 적이 없거나 닿지 않으면 false (차례를 쓰지 않음).
        /// </summary>
        public static bool TryAttackAt(World world, GameConfig config, Unit player, GridPos target, out ICommand command)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (config == null) throw new ArgumentNullException(nameof(config));
            command = null;
            if (player == null || !player.IsAlive) return false;
            if (!world.Units.TryGetAt(target, out var occupant) || !Factions.AreHostile(occupant.Faction, player.Faction) || !occupant.IsAlive) return false;
            if (!MeleeWeapon.CanReach(world, player, target)) return false;
            command = new AttackCommand(world, occupant.Id, MeleeWeapon.AttackCost(player, config.DefaultActionCostTicks));
            return true;
        }

        public static ICommand Wait(GameConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new WaitCommand(config.DefaultActionCostTicks);
        }

        /// <summary>찾기 (3-12b): 둘레를 확실히 찾는 행동 — 비용은 SearchConfig.</summary>
        public static ICommand Search(World world, ProjectM.Dungeon.SearchConfig config) => new ProjectM.Dungeon.SearchCommand(world, config);

        /// <summary>빠른 찾기 (3-24): 정보 모드에서 E를 한 번 더 — 둘레 QuickRadius 칸 · 비용 QuickCostTicks.</summary>
        public static ICommand QuickSearch(World world, ProjectM.Dungeon.SearchConfig config) => new ProjectM.Dungeon.SearchCommand(world, config, quick: true);

        public static bool TryGetPlayer(World world, out Unit player)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            player = null;
            var id = world.Scheduler.PlayerId;
            return id.IsValid && world.Units.TryGet(id, out player);
        }
    }
}
