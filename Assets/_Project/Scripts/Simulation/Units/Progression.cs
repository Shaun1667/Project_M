using System;
using ProjectM.Core;

namespace ProjectM.Units
{
    /// <summary>
    /// 경험치 · 레벨업 수치 (3-6, SPD 참고 — 결정 2026-10-19). 값은 ProgressionConfigAsset에서 온다 — 기준은 BALANCE.md 12번.
    /// 다음 레벨까지 필요 경험치 = ExpBase + 레벨 × ExpPerLevel (SPD: 5 + 5 × 레벨). 레벨마다 0부터 다시 센다.
    /// </summary>
    public sealed class ProgressionConfig
    {
        public int MaxLevel { get; }
        public int ExpBase { get; }
        public int ExpPerLevel { get; }
        /// <summary>레벨업마다 최대 체력 증가 (현재 체력도 같이 오름).</summary>
        public int HpPerLevel { get; }
        /// <summary>레벨업마다 최대 마나 증가 (현재 마나도 같이 오름).</summary>
        public int ManaPerLevel { get; }
        public int AccuracyPerLevel { get; }
        public int EvasionPerLevel { get; }

        public ProgressionConfig(int maxLevel, int expBase, int expPerLevel, int hpPerLevel, int manaPerLevel, int accuracyPerLevel, int evasionPerLevel)
        {
            if (maxLevel < 1) throw new ArgumentOutOfRangeException(nameof(maxLevel), maxLevel, "최대 레벨은 1 이상이어야 한다.");
            if (expBase < 0) throw new ArgumentOutOfRangeException(nameof(expBase), expBase, "0 이상이어야 한다.");
            if (expPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(expPerLevel), expPerLevel, "0 이상이어야 한다.");
            if (expBase + expPerLevel < 1) throw new ArgumentException("1레벨 필요 경험치가 1 이상이어야 한다.");
            if (hpPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(hpPerLevel), hpPerLevel, "0 이상이어야 한다.");
            if (manaPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(manaPerLevel), manaPerLevel, "0 이상이어야 한다.");
            if (accuracyPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(accuracyPerLevel), accuracyPerLevel, "0 이상이어야 한다.");
            if (evasionPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(evasionPerLevel), evasionPerLevel, "0 이상이어야 한다.");

            MaxLevel = maxLevel;
            ExpBase = expBase;
            ExpPerLevel = expPerLevel;
            HpPerLevel = hpPerLevel;
            ManaPerLevel = manaPerLevel;
            AccuracyPerLevel = accuracyPerLevel;
            EvasionPerLevel = evasionPerLevel;
        }

        /// <summary>level에서 다음 레벨까지 필요한 경험치.</summary>
        public int ExpToNext(int level) => ExpBase + level * ExpPerLevel;
    }

    /// <summary>
    /// 경험치 · 레벨업 (3-6). 적이 죽으면(⑥ 사망 판정 — 누가 죽였든) 살아 있는 플레이어가 그 적의 경험치를 받는다.
    /// 반복 사냥 방지 (SPD): 플레이어 레벨이 적의 ExpMaxLevel보다 높으면 0.
    /// 최대 레벨에서는 경험치를 더 쌓지 않는다.
    /// </summary>
    public static class Progression
    {
        /// <summary>죽은 적 하나의 경험치를 플레이어에게 준다. 준 양 (0이면 이벤트 없음).</summary>
        public static int AwardKill(World world, Unit player, Unit victim, ProgressionConfig config)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (player == null || !player.IsAlive || victim == null || victim.Faction != Faction.Enemy) return 0;
            int amount = player.Level <= victim.ExpMaxLevel ? victim.ExpReward : 0;
            return GainExp(world, player, amount, config);
        }

        /// <summary>경험치를 더하고 필요한 만큼 레벨업한다. 실제로 더한 양.</summary>
        public static int GainExp(World world, Unit unit, int amount, ProgressionConfig config)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "경험치는 0 이상이어야 한다.");
            if (amount == 0 || unit.Level >= config.MaxLevel) return 0;

            unit.Exp += amount;
            while (unit.Level < config.MaxLevel && unit.Exp >= config.ExpToNext(unit.Level))
            {
                unit.Exp -= config.ExpToNext(unit.Level);
                LevelUp(world, unit, config);
            }
            if (unit.Level >= config.MaxLevel) unit.Exp = 0; // 최대 레벨: 남는 경험치 버림 (SPD)
            world.Events.Publish(new ExpGainedEvent(unit.Id, amount, unit.Exp, ExpToNextOrZero(unit, config), unit.Level));
            return amount;
        }

        /// <summary>다음 레벨까지 필요 경험치 (최대 레벨이면 0) — HUD 막대용.</summary>
        public static int ExpToNextOrZero(Unit unit, ProgressionConfig config) =>
            unit.Level >= config.MaxLevel ? 0 : config.ExpToNext(unit.Level);

        private static void LevelUp(World world, Unit unit, ProgressionConfig config)
        {
            unit.Level++;
            unit.MaxHp += config.HpPerLevel;
            unit.Hp = Math.Min(unit.MaxHp, unit.Hp + config.HpPerLevel);
            if (unit.MaxMana > 0 && config.ManaPerLevel > 0) // 마나 없는 유닛은 그대로
            {
                unit.MaxMana += config.ManaPerLevel;
                ProjectM.Magic.Mana.Restore(world, unit, config.ManaPerLevel); // 현재 마나는 마나 규칙 한 곳으로 (ManaChangedEvent)
            }
            unit.Accuracy += config.AccuracyPerLevel;
            unit.Evasion += config.EvasionPerLevel;
            world.Events.Publish(new LevelUpEvent(unit.Id, unit.Level, unit.Hp, unit.MaxHp, unit.Mana, unit.MaxMana, unit.Accuracy, unit.Evasion));
        }
    }

    /// <summary>경험치를 얻었다 (3-6). Exp · ExpToNext는 레벨업 처리 뒤 값, ExpToNext 0 = 최대 레벨.</summary>
    public sealed class ExpGainedEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public int Amount { get; }
        public int Exp { get; }
        public int ExpToNext { get; }
        public int Level { get; }

        public ExpGainedEvent(EntityId unit, int amount, int exp, int expToNext, int level)
        {
            Unit = unit;
            Amount = amount;
            Exp = exp;
            ExpToNext = expToNext;
            Level = level;
        }
    }

    /// <summary>레벨이 올랐다 (3-6). 값은 오른 뒤. 여러 레벨이 한 번에 오르면 레벨마다 하나씩.</summary>
    public sealed class LevelUpEvent : ISimEvent
    {
        public EntityId Unit { get; }
        public int Level { get; }
        public int Hp { get; }
        public int MaxHp { get; }
        public int Mana { get; }
        public int MaxMana { get; }
        public int Accuracy { get; }
        public int Evasion { get; }

        public LevelUpEvent(EntityId unit, int level, int hp, int maxHp, int mana, int maxMana, int accuracy, int evasion)
        {
            Unit = unit;
            Level = level;
            Hp = hp;
            MaxHp = maxHp;
            Mana = mana;
            MaxMana = maxMana;
            Accuracy = accuracy;
            Evasion = evasion;
        }
    }
}
