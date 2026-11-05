using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Magic;

namespace ProjectM.Units
{
    /// <summary>
    /// 지난 회차 플레이어 기록 (3-23b, 결정 2026-11-05 — 사용자): 26층 시간의 모래시계를 얻은 순간의 캐릭터 모습 · 능력치 · 든 무기 · 입은 갑옷 ·
    /// 빠른 칸의 주문(특성 · 무기 강화가 반영된 값). 다음 판부터 그 기록을 쓰러뜨릴 때까지 마지막 보스(25층)가 이 기록이 된다.
    /// 판 밖에 남는 데이터라 World(스냅샷)가 아니라 Runtime이 파일로 저장한다 (GhostStore). 상태가 없는 값 객체.
    /// </summary>
    public sealed class GhostRecord
    {
        public string DisplayName { get; }
        public string VisualId { get; }
        public int Level { get; }
        public int MaxHp { get; }
        public int AttackDamage { get; }
        public int SightRange { get; }
        public int MaxMana { get; }
        public int Accuracy { get; }
        public int Evasion { get; }
        public int Strength { get; }
        public EquippedWeapon Weapon { get; }
        public EquippedArmor Armor { get; }
        /// <summary>쓸 주문 (빠른 칸 순서, 값은 특성 · 무기 강화를 반영해 굳힌 것 — 무기 강화 필드는 비움).</summary>
        public IReadOnlyList<SpellDef> Spells { get; }

        public GhostRecord(string displayName, string visualId, int level, int maxHp, int attackDamage, int sightRange, int maxMana,
            int accuracy, int evasion, int strength, EquippedWeapon weapon, EquippedArmor armor, IReadOnlyList<SpellDef> spells)
        {
            if (string.IsNullOrEmpty(visualId)) throw new ArgumentException("모습 ID가 비어 있다.", nameof(visualId));
            if (maxHp < 1 || sightRange < 1 || level < 1) throw new ArgumentOutOfRangeException(nameof(maxHp));
            DisplayName = string.IsNullOrEmpty(displayName) ? visualId : displayName;
            VisualId = visualId;
            Level = level;
            MaxHp = maxHp;
            AttackDamage = Math.Max(0, attackDamage);
            SightRange = sightRange;
            MaxMana = Math.Max(0, maxMana);
            Accuracy = Math.Max(0, accuracy);
            Evasion = Math.Max(0, evasion);
            Strength = Math.Max(0, strength);
            Weapon = weapon;
            Armor = armor;
            Spells = spells ?? Array.Empty<SpellDef>();
        }
    }

    /// <summary>
    /// 지난 회차 플레이어 보스 (3-23b): 기록 만들기(Capture) · 보스 수치(ToUnitDef — 체력만 배율) · 적 행동(Traits — 주문을 빠른 칸 순서로 시도) ·
    /// 판 주문 표에 넣을 주문(SpellsWithPrefix — ID 앞에 "ghost_") · 소환 뒤 무기 · 갑옷(Equip). 상태가 없다.
    /// </summary>
    public static class GhostBoss
    {
        /// <summary>보스 주문 ID 접두어 — 판 주문 표에서 플레이어의 같은 주문과 겹치지 않게 (화면 이름은 접두어를 떼고 찾음).</summary>
        public const string SpellPrefix = "ghost_";

        /// <summary>
        /// 지금 플레이어로 기록을 만든다. 주문 = 빠른 칸 순서의 주문 중 데이터에 있고 뛰어넘기(도약)가 아닌 것 —
        /// 값은 SpellModifiers.For(특성 · 무기 강화 반영)로 굳힌다.
        /// </summary>
        public static GhostRecord Capture(World world, Unit player, SpellBook spells, string displayName)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (player == null) throw new ArgumentNullException(nameof(player));
            var list = new List<SpellDef>();
            var seen = new HashSet<string>();
            for (int i = 0; i < player.QuickSlots.Length; i++)
            {
                var id = player.QuickSlotSpell(i);
                if (id == null || !seen.Add(id) || spells == null || !spells.TryGet(id, out var def)) continue;
                if (def.Shape == SpellShape.Leap) continue; // 이동 기술은 보스가 쓸 곳이 없음
                list.Add(SpellModifiers.For(world, player, def));
            }
            return new GhostRecord(displayName, player.VisualId, player.Level, player.MaxHp, player.AttackDamage, player.SightRange, player.MaxMana,
                player.Accuracy + Talents.AccuracyBonus(world, player), player.Evasion, player.Strength, player.Weapon, player.Armor, list);
        }

        /// <summary>판 주문 표에 넣을 보스 주문 (ID = "ghost_" + 원래 ID, 값은 기록 그대로 · 무기 강화 없음).</summary>
        public static List<SpellDef> SpellsWithPrefix(GhostRecord record)
        {
            var list = new List<SpellDef>();
            foreach (var s in record.Spells)
                list.Add(new SpellDef(SpellPrefix + s.Id, s.Element, s.Shape, s.Range, s.Damage, s.ManaCost, s.Radius, s.AppliesStatus, s.StatusChancePercent,
                    s.LeavesTerrain, s.TerrainDurationTicks, s.TriggersReactions, hitCount: s.HitCount, maxTargets: s.MaxTargets,
                    unseenDamagePercent: s.UnseenDamagePercent));
            return list;
        }

        /// <summary>보스 수치: 체력 × hpPercent% (결정 2026-11-05 — 체력만 3배), 나머지는 기록 그대로. 경험치는 보스 값.</summary>
        public static UnitDef ToUnitDef(GhostRecord record, int hpPercent, int expReward, int expMaxLevel)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            int hp = Math.Max(1, (int)((long)record.MaxHp * hpPercent / 100));
            return new UnitDef(record.VisualId, hp, record.AttackDamage, record.SightRange, record.MaxMana,
                accuracy: record.Accuracy, evasion: record.Evasion, expReward: expReward, expMaxLevel: expMaxLevel, strength: record.Strength);
        }

        /// <summary>적 행동: 주문을 빠른 칸 순서로 시도 (첫 주문 = 원거리 주문, 나머지 = 덧붙은 주문), 거리 벌림 없음 — 붙으면 근접.</summary>
        public static EnemyTraits Traits(GhostRecord record)
        {
            var ids = new List<string>();
            foreach (var s in record.Spells) ids.Add(SpellPrefix + s.Id);
            string first = ids.Count > 0 ? ids[0] : null;
            if (ids.Count > 0) ids.RemoveAt(0);
            return new EnemyTraits(record.VisualId, record.DisplayName, spellId: first, extraSpellIds: ids);
        }

        /// <summary>소환한 보스 유닛에 기록의 무기 · 갑옷을 입힌다.</summary>
        public static void Equip(Unit boss, GhostRecord record)
        {
            if (boss == null || record == null) return;
            boss.Weapon = record.Weapon;
            boss.Armor = record.Armor;
        }
    }
}
