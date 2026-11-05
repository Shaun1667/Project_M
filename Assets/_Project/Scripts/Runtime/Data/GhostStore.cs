using System;
using System.Collections.Generic;
using System.IO;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.Units;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 지난 회차 플레이어 기록 파일 (3-23b, 결정 2026-11-05 — 기록 하나). 판 밖에 남는 첫 저장 데이터라 World 스냅샷과 따로,
    /// Application.persistentDataPath/ghost.json에 JsonUtility로 쓴다. 쓰기는 임시 파일 뒤 바꾸기(원자적). 읽다 실패하면 기록 없음으로 본다.
    /// 시뮬레이션 값(GhostRecord) ↔ 직렬화 모양(Dto) 바꾸기만 — 규칙은 GhostBoss.
    /// </summary>
    public static class GhostStore
    {
        public const string FileName = "ghost.json";
        public const int FormatVersion = 1;

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        [Serializable]
        public sealed class SpellDto
        {
            public string Id;
            public Element Element;
            public SpellShape Shape;
            public int Range, Damage, ManaCost, Radius;
            public StatusType AppliesStatus;
            public int StatusChancePercent;
            public TerrainEffectType LeavesTerrain;
            public int TerrainDurationTicks;
            public bool TriggersReactions;
            public int HitCount, MaxTargets, UnseenDamagePercent;
        }

        [Serializable]
        public sealed class Dto
        {
            public int Version = FormatVersion;
            public string DisplayName, VisualId;
            public int Level, MaxHp, AttackDamage, SightRange, MaxMana, Accuracy, Evasion, Strength;
            public bool HasWeapon;
            public string WeaponId;
            public int WeaponMin, WeaponMax, WeaponAccuracy, WeaponCost, WeaponReach, WeaponUpgrade, WeaponStrength, WeaponSurprise, WeaponDefense, WeaponHits;
            public bool HasArmor;
            public string ArmorId;
            public int ArmorTier, ArmorUpgrade, ArmorStrength, ArmorMin, ArmorMax, ArmorPenalty, ArmorDivisor;
            public List<SpellDto> Spells = new List<SpellDto>();
        }

        public static Dto ToDto(GhostRecord r)
        {
            var d = new Dto
            {
                DisplayName = r.DisplayName, VisualId = r.VisualId, Level = r.Level, MaxHp = r.MaxHp, AttackDamage = r.AttackDamage,
                SightRange = r.SightRange, MaxMana = r.MaxMana, Accuracy = r.Accuracy, Evasion = r.Evasion, Strength = r.Strength,
            };
            if (!r.Weapon.IsEmpty)
            {
                var w = r.Weapon;
                d.HasWeapon = true; d.WeaponId = w.ItemId; d.WeaponMin = w.MinDamage; d.WeaponMax = w.MaxDamage; d.WeaponAccuracy = w.AccuracyPercent;
                d.WeaponCost = w.AttackCostTicks; d.WeaponReach = w.Reach; d.WeaponUpgrade = w.Upgrade; d.WeaponStrength = w.StrengthRequirement;
                d.WeaponSurprise = w.SurpriseMinPercent; d.WeaponDefense = w.Defense; d.WeaponHits = w.HitsPerAttack;
            }
            if (!r.Armor.IsEmpty)
            {
                var a = r.Armor;
                d.HasArmor = true; d.ArmorId = a.ItemId; d.ArmorTier = a.Tier; d.ArmorUpgrade = a.Upgrade; d.ArmorStrength = a.StrengthRequirement;
                d.ArmorMin = a.DefenseMin; d.ArmorMax = a.DefenseMax; d.ArmorPenalty = a.DeficitPenalty; d.ArmorDivisor = a.DefenseDivisor;
            }
            foreach (var s in r.Spells)
                d.Spells.Add(new SpellDto
                {
                    Id = s.Id, Element = s.Element, Shape = s.Shape, Range = s.Range, Damage = s.Damage, ManaCost = s.ManaCost, Radius = s.Radius,
                    AppliesStatus = s.AppliesStatus, StatusChancePercent = s.StatusChancePercent, LeavesTerrain = s.LeavesTerrain,
                    TerrainDurationTicks = s.TerrainDurationTicks, TriggersReactions = s.TriggersReactions,
                    HitCount = s.HitCount, MaxTargets = s.MaxTargets, UnseenDamagePercent = s.UnseenDamagePercent,
                });
            return d;
        }

        /// <summary>Dto → 기록. 값이 잘못됐으면 예외 (Load가 받아 기록 없음으로).</summary>
        public static GhostRecord FromDto(Dto d)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            if (d.Version != FormatVersion) throw new InvalidDataException($"지원하지 않는 기록 버전 {d.Version}");
            var weapon = d.HasWeapon
                ? new EquippedWeapon(d.WeaponId, d.WeaponMin, d.WeaponMax, d.WeaponAccuracy, d.WeaponCost, d.WeaponReach, d.WeaponUpgrade, d.WeaponStrength,
                    d.WeaponSurprise, d.WeaponDefense, Math.Max(1, d.WeaponHits))
                : EquippedWeapon.Empty;
            var armor = d.HasArmor
                ? new EquippedArmor(d.ArmorId, d.ArmorTier, d.ArmorUpgrade, d.ArmorStrength, d.ArmorMin, d.ArmorMax, d.ArmorPenalty, d.ArmorDivisor)
                : EquippedArmor.Empty;
            var spells = new List<SpellDef>();
            foreach (var s in d.Spells ?? new List<SpellDto>())
                spells.Add(new SpellDef(s.Id, s.Element, s.Shape, s.Range, s.Damage, s.ManaCost, s.Radius, s.AppliesStatus, s.StatusChancePercent,
                    s.LeavesTerrain, s.TerrainDurationTicks, s.TriggersReactions, hitCount: Math.Max(1, s.HitCount), maxTargets: s.MaxTargets,
                    unseenDamagePercent: s.UnseenDamagePercent));
            return new GhostRecord(d.DisplayName, d.VisualId, d.Level, d.MaxHp, d.AttackDamage, d.SightRange, d.MaxMana,
                d.Accuracy, d.Evasion, d.Strength, weapon, armor, spells);
        }

        public static string ToJson(GhostRecord r) => JsonUtility.ToJson(ToDto(r), true);
        public static GhostRecord FromJson(string json) => FromDto(JsonUtility.FromJson<Dto>(json));

        /// <summary>쓴다 (임시 파일에 쓴 뒤 바꾸기 — 쓰다 끊겨도 이전 기록이 깨지지 않게).</summary>
        public static void Save(GhostRecord record, string path = null)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            path = path ?? DefaultPath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, ToJson(record));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>읽는다. 없거나 깨졌으면 null (깨진 파일은 경고만 — 게임은 기록 없이).</summary>
        public static GhostRecord Load(string path = null)
        {
            path = path ?? DefaultPath;
            if (!File.Exists(path)) return null;
            try
            {
                return FromJson(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GhostStore] 기록을 읽지 못했다 — 없는 것으로 봄: {e.Message}");
                return null;
            }
        }

        public static void Delete(string path = null)
        {
            path = path ?? DefaultPath;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
