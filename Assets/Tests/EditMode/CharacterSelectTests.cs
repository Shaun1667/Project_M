using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Data;
using ProjectM.UI;
using ProjectM.Units;
using UnityEditor;

namespace ProjectM.Tests
{
    /// <summary>캐릭터 선택 (결정 2026-10-21 — 판마다 고름, 지금은 외형만): 고르기 · 확정 · 모습만 바꾼 유닛 정의 · 목록 에셋.</summary>
    public class CharacterSelectTests
    {
        [Test]
        public void Model_MoveStopsAtEnds_ConfirmReportsSelectedAndCloses()
        {
            var model = new CharacterSelectModel();
            var confirmed = new List<int>();
            model.Confirmed += confirmed.Add;
            model.Open(2, initial: 0);
            Assert.That((model.IsOpen, model.Selected), Is.EqualTo((true, 0)));
            model.Move(-1);
            Assert.That(model.Selected, Is.EqualTo(0), "왼쪽 끝");
            model.Move(1);
            model.Move(1);
            Assert.That(model.Selected, Is.EqualTo(1), "오른쪽 끝");
            model.Confirm();
            Assert.That(confirmed, Is.EqualTo(new[] { 1 }));
            Assert.That(model.IsOpen, Is.False);
            model.Confirm();
            Assert.That(confirmed.Count, Is.EqualTo(1), "닫힌 뒤에는 확정 안 됨");
        }

        [Test]
        public void Model_OpenRemembersLastChoice_SelectByMouse()
        {
            var model = new CharacterSelectModel();
            model.Open(2, initial: 1);
            Assert.That(model.Selected, Is.EqualTo(1), "지난 판에 고른 캐릭터에서 시작");
            model.Select(0);
            Assert.That(model.Selected, Is.EqualTo(0));
            model.Select(5);
            Assert.That(model.Selected, Is.EqualTo(0), "없는 칸은 무시");
            model.Open(2, initial: 9);
            Assert.That(model.Selected, Is.EqualTo(1), "범위 밖이면 끝 칸");
        }

        [Test]
        public void UnitDef_WithVisualId_ChangesOnlyLook()
        {
            var def = new UnitDef("player", 20, 3, 8, 10, new[] { "fire_basic" }, accuracy: 10, evasion: 5, expReward: 0, expMaxLevel: 0, strength: 10, startingArmor: "armor_cloth");
            var hero = def.WithVisualId("hero_blond");
            Assert.That(hero.VisualId, Is.EqualTo("hero_blond"));
            Assert.That((hero.MaxHp, hero.AttackDamage, hero.SightRange, hero.MaxMana, hero.Accuracy, hero.Evasion, hero.Strength, hero.StartingArmor),
                Is.EqualTo((def.MaxHp, def.AttackDamage, def.SightRange, def.MaxMana, def.Accuracy, def.Evasion, def.Strength, def.StartingArmor)));
            Assert.That(hero.StartingSpells, Is.EqualTo(def.StartingSpells));
        }

        // 검산: 목록 에셋 — 두 캐릭터, 모습 ID가 EntityVisualCatalog에 있고 초상이 있음
        [Test]
        public void ProjectAssets_TwoCharacters_WithPortraitAndVisual()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalogAsset>("Assets/_Project/Data/Units/CharacterCatalog.asset");
            Assert.That(catalog, Is.Not.Null, "CharacterCatalog.asset 없음 — 메뉴 ProjectM/Setup/Main Scene 실행");
            var visuals = AssetDatabase.LoadAssetAtPath<ProjectM.Presentation.EntityVisualCatalog>("Assets/_Project/Data/Visuals/EntityVisualCatalog.asset");
            var ids = new List<string>();
            foreach (var c in catalog.Characters)
            {
                ids.Add(c.Id);
                Assert.That(c.Portrait, Is.Not.Null, c.Id);
                Assert.That(visuals.TryGet(c.VisualId, out _), Is.True, c.VisualId);
            }
            Assert.That(ids, Is.EqualTo(new[] { "hero_blond", "hero_black" }));
        }
    }
}
