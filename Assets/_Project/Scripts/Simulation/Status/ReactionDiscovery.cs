using System;
using ProjectM.Core;
using ProjectM.Units;

namespace ProjectM.Status
{
    /// <summary>
    /// 반응 발견 (3-59, 결정 2026-11-25 — 판마다 경험치 + 영구 도감): 플레이어 주문이 이 판에서 처음 일으킨 반응 그룹(같은 이름 줄 묶음)을
    /// RunState.DiscoveredReactions에 남기고 ReactionConfig.DiscoveryExp만큼 경험치를 준다. 판 밖 도감은 Runtime(ReactionCodexStore)이
    /// ReactionDiscoveredEvent를 보고 따로 쓴다 (판 밖 데이터는 스냅샷에 넣지 않음). ReactionEngine.Resolve만 부른다. 상태를 갖지 않는다.
    /// </summary>
    public static class ReactionDiscovery
    {
        /// <returns>처음 발견이면 true.</returns>
        public static bool Record(World world, ReactionBook book, ReactionDef reaction, EntityId casterId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (reaction == null || !casterId.IsValid || casterId != world.Scheduler.PlayerId) return false;
            if (!world.Run.AddDiscoveredReaction(reaction.Group)) return false;
            int exp = 0;
            int reward = book?.Config.DiscoveryExp ?? 0;
            if (reward > 0 && world.ProgressionRules != null && world.Units.TryGet(casterId, out var player) && player.IsAlive)
                exp = Progression.GainExp(world, player, reward, world.ProgressionRules);
            world.Events.Publish(new ReactionDiscoveredEvent(reaction.Group, reaction.Id, exp));
            return true;
        }
    }

    /// <summary>이 판에서 처음 일으킨 반응 (3-59). Group = 도감 키, ReactionId = 이름을 찾을 줄, Exp = 받은 경험치 (최대 레벨이면 0).</summary>
    public sealed class ReactionDiscoveredEvent : ISimEvent
    {
        public string Group { get; }
        public string ReactionId { get; }
        public int Exp { get; }

        public ReactionDiscoveredEvent(string group, string reactionId, int exp)
        {
            Group = group;
            ReactionId = reactionId;
            Exp = exp;
        }
    }
}
