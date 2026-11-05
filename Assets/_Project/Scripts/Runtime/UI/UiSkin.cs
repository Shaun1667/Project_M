using System;
using System.Collections.Generic;
using ProjectM.Magic;
using ProjectM.Status;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// UI 그림 표 (G-18, 2026-11-05 — ART_ASSETS ★ 3차 29 · 9번, 결정: Varco 이미지 UI 키트 · 아이콘). 표현 전용 데이터 — 비면 예전 색 칸 그대로.
    /// 틀(창 · 칸 4상태 · 막대 · 표시판)은 9-slice 스프라이트, 아이콘은 상태 · 속성별. 표는 메뉴 ProjectM/Setup/UI Art가 Art/UI에서 채운다.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Visuals/UI Skin", fileName = "UiSkin")]
    public sealed class UiSkin : ScriptableObject
    {
        [Serializable]
        public struct StatusIcon
        {
            public StatusType Type;
            public Sprite Icon;
        }

        [Serializable]
        public struct ElementIcon
        {
            public Element Element;
            public Sprite Icon;
        }

        [Header("틀 (9-slice)")]
        public Sprite Panel;
        public Sprite SlotNormal;
        public Sprite SlotSelected;
        public Sprite SlotEmpty;
        [Tooltip("마나 부족 · 고를 수 없는 칸")] public Sprite SlotDisabled;
        public Sprite BarFrame;
        [Tooltip("층 표시판")] public Sprite Plaque;
        [Header("결과 화면 장식")]
        public Sprite Victory;
        public Sprite Defeat;
        [Header("아이콘")]
        [Tooltip("융합 주문 (속성 아이콘 대신)")] public Sprite FusionIcon;
        [SerializeField] private List<StatusIcon> _statuses = new List<StatusIcon>();
        [SerializeField] private List<ElementIcon> _elements = new List<ElementIcon>();
        [Header("화면에 보이는 틀 두께 (px) — 원본 테두리를 이만큼으로 줄임")]
        [Min(1f)] public float PanelBorder = 22f;
        [Min(1f)] public float SlotBorder = 7f;
        [Min(1f)] public float BarBorder = 9f;
        [Min(1f)] public float PlaqueBorder = 26f;

        public Sprite StatusIconOf(StatusType type)
        {
            foreach (var s in _statuses)
                if (s.Type == type) return s.Icon;
            return null;
        }

        public Sprite ElementIconOf(Element element)
        {
            foreach (var e in _elements)
                if (e.Element == element) return e.Icon;
            return null;
        }

        /// <summary>에디터 Setup · 테스트용.</summary>
        public void SetStatusIcon(StatusType type, Sprite icon)
        {
            for (int i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].Type != type) continue;
                _statuses[i] = new StatusIcon { Type = type, Icon = icon };
                return;
            }
            _statuses.Add(new StatusIcon { Type = type, Icon = icon });
        }

        /// <summary>에디터 Setup · 테스트용.</summary>
        public void SetElementIcon(Element element, Sprite icon)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i].Element != element) continue;
                _elements[i] = new ElementIcon { Element = element, Icon = icon };
                return;
            }
            _elements.Add(new ElementIcon { Element = element, Icon = icon });
        }

        /// <summary>
        /// 이미지에 9-slice 틀을 씌운다: 원본 테두리(px)가 화면에서 borderPx가 되게 pixelsPerUnitMultiplier를 맞춘다. sprite가 없으면 false (예전 색 그대로).
        /// </summary>
        public static bool ApplyFrame(Image image, Sprite sprite, float borderPx)
        {
            if (image == null || sprite == null) return false;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            float source = Mathf.Max(sprite.border.x, sprite.border.y);
            image.pixelsPerUnitMultiplier = source > 0f ? source / Mathf.Max(1f, borderPx) : 1f;
            return true;
        }
    }
}
