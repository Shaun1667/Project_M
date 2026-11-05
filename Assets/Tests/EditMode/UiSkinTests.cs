using NUnit.Framework;
using ProjectM.Magic;
using ProjectM.Status;
using ProjectM.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.Tests
{
    /// <summary>G-18 UI 그림 표: 상태 · 속성 아이콘 조회, 9-slice 틀 두께 맞춤, 층 표시판 글자.</summary>
    public class UiSkinTests
    {
        private Texture2D _texture;
        private UiSkin _skin;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(64, 64);
            _skin = ScriptableObject.CreateInstance<UiSkin>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_skin);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void StatusAndElementIcons_LookUpByType_SetReplacesSameKey()
        {
            var a = Sprite.Create(_texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            var b = Sprite.Create(_texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            _skin.SetStatusIcon(StatusType.Burn, a);
            _skin.SetStatusIcon(StatusType.Burn, b);
            _skin.SetElementIcon(Element.Neutral, a);
            Assert.That(_skin.StatusIconOf(StatusType.Burn), Is.SameAs(b), "같은 상태는 바꿈");
            Assert.That(_skin.StatusIconOf(StatusType.Wet), Is.Null, "없으면 null — 예전 색 칸");
            Assert.That(_skin.ElementIconOf(Element.Neutral), Is.SameAs(a));
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void ApplyFrame_ScalesSourceBorderToScreenBorder()
        {
            var framed = Sprite.Create(_texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            var go = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var image = go.GetComponent<Image>();
            Assert.That(UiSkin.ApplyFrame(image, framed, 8f), Is.True);
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(2f).Within(0.001f), "원본 16 px → 화면 8 px");
            Assert.That(UiSkin.ApplyFrame(image, null, 8f), Is.False, "그림이 없으면 그대로");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(framed);
        }

        [Test]
        public void FloorPlaque_Text() => Assert.That(HudText.FloorPlaque(7), Is.EqualTo("7층"));
    }
}
