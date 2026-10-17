using System.Collections.Generic;
using ProjectM.Items;
using ProjectM.Units;
using UnityEditor;
using UnityEngine;

namespace ProjectM.Editor.Debugging
{
    /// <summary>
    /// 개발자 디버그 창 (메뉴 ProjectM/Debug/Debug Window). 플레이 모드에서 씬의 GameRoot에 붙어 기능을 쓴다.
    /// 기능을 추가할 때는 DrawXxx 섹션 메서드를 하나 만들고 OnGUI에서 부른다.
    /// 디버그 기능은 가능하면 표시만 바꾸고, 게임 상태를 바꾸는 기능은 섹션 제목에 [상태 변경]을 붙인다 (시드 재현 · 저장에 영향).
    /// </summary>
    public sealed class DebugWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("ProjectM/Debug/Debug Window")]
        public static void Open()
        {
            var window = GetWindow<DebugWindow>();
            window.titleContent = new GUIContent("ProjectM Debug");
            window.minSize = new Vector2(260, 160);
            window.Show();
        }

        // 플레이 중 정보(틱 · 체력)를 계속 갱신
        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서 사용한다. Main.unity를 플레이한다.", MessageType.Info);
                return;
            }

            var root = FindAnyObjectByType<GameRoot>();
            if (root == null || root.World == null)
            {
                EditorGUILayout.HelpBox("씬에 시작된 GameRoot가 없다.", MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawInfo(root);
            EditorGUILayout.Space();
            DrawView(root);
            EditorGUILayout.Space();
            DrawInventory(root);
            EditorGUILayout.Space();
            DrawMana(root);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>[상태 변경] 마나 (T-2): 버튼 하나로 플레이어 마나를 최대치까지 채운다 (Mana.Restore — HUD · 로그는 평소 경로 그대로).</summary>
        private static void DrawMana(GameRoot root)
        {
            EditorGUILayout.LabelField("[상태 변경] 마나", EditorStyles.boldLabel);
            if (!root.World.Units.TryGet(root.PlayerId, out var player))
            {
                EditorGUILayout.HelpBox("플레이어가 없다.", MessageType.None);
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(player.Mana >= player.MaxMana))
            {
                if (!GUILayout.Button($"마나 최대로 채우기 ({player.Mana}/{player.MaxMana})")) return;
                ProjectM.Magic.Mana.Restore(root.World, player, player.MaxMana - player.Mana);
                root.World.Events.Flush(); // 틱 밖 변경 → HUD에 바로 보이게
            }
        }

        private int _spellPick;
        private int _itemPick;

        /// <summary>
        /// [상태 변경] 인벤토리 · 빠른 칸 (3-2a): 주문 · 아이템 넣기 / 빼기, 빠른 칸마다 등록할 항목 고르기.
        /// 주문 · 아이템은 게임 중에 얻는다 (Phase 6) — 그 전까지 테스트용. 게임 상태(World)를 바꾸므로 시드 재현 · 저장에 영향이 있다.
        /// </summary>
        private void DrawInventory(GameRoot root)
        {
            EditorGUILayout.LabelField("[상태 변경] 인벤토리 · 빠른 칸", EditorStyles.boldLabel);
            if (!root.World.Units.TryGet(root.PlayerId, out var player) || root.Spells == null || root.Items == null)
            {
                EditorGUILayout.HelpBox("플레이어가 없다.", MessageType.None);
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                _spellPick = AddRow("주문 넣기", root.Spells.Ids, _spellPick, id => InventoryRef.Spell(id), root, player);
                _itemPick = AddRow("아이템 넣기", root.Items.Ids, _itemPick, id => InventoryRef.Item(id), root, player);

                // 인벤토리 목록 (빈 칸 제외) + 빼기
                var slots = player.InventorySlots;
                int filled = 0;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].IsEmpty) continue;
                    filled++;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"칸 {i + 1}", slots[i].ToString());
                        if (GUILayout.Button("빼기", GUILayout.Width(48)))
                        {
                            Inventory.RemoveAt(root.World, player, i);
                            root.World.Events.Flush();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                EditorGUILayout.LabelField("인벤토리", $"{filled}/{slots.Length}칸");

                // 빠른 칸: (빈 칸) + 인벤토리 항목
                var entries = new List<InventoryRef>();
                foreach (var e in slots)
                    if (!e.IsEmpty && !entries.Contains(e)) entries.Add(e);
                var options = new string[entries.Count + 1];
                options[0] = "(빈 칸)";
                for (int i = 0; i < entries.Count; i++) options[i + 1] = entries[i].ToString();
                for (int slot = 0; slot < player.QuickSlots.Length; slot++)
                {
                    int index = entries.IndexOf(player.QuickSlots[slot]) + 1;
                    int picked = EditorGUILayout.Popup($"빠른 칸 {slot + 1}", index, options);
                    if (picked == index) continue;
                    Inventory.AssignQuickSlot(root.World, player, slot, picked == 0 ? InventoryRef.Empty : entries[picked - 1]);
                    root.World.Events.Flush(); // 틱 밖 변경 → HUD에 바로 보이게
                }
            }
        }

        private static int AddRow(string label, IReadOnlyList<string> ids, int pick, System.Func<string, InventoryRef> make, GameRoot root, Unit player)
        {
            if (ids.Count == 0) return 0;
            using (new EditorGUILayout.HorizontalScope())
            {
                var names = new string[ids.Count];
                for (int i = 0; i < ids.Count; i++) names[i] = ids[i];
                pick = EditorGUILayout.Popup(label, Mathf.Clamp(pick, 0, ids.Count - 1), names);
                if (GUILayout.Button("넣기", GUILayout.Width(48)))
                {
                    if (Inventory.TryAdd(root.World, root.Spells, root.Items, player, make(ids[pick])) < 0)
                        Debug.LogWarning("[DebugWindow] 넣지 못함 — 인벤토리가 꽉 찼거나 이미 있는 주문");
                    root.World.Events.Flush();
                }
            }
            return pick;
        }

        private static void DrawInfo(GameRoot root)
        {
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.SelectableLabel($"시드 {(long)root.Seed}", GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.LabelField("틱", root.World.Scheduler.CurrentTick.ToString());
                if (root.World.Units.TryGet(root.PlayerId, out var player))
                    EditorGUILayout.LabelField("플레이어", $"{player.Pos}  HP {player.Hp}/{player.MaxHp}  MP {player.Mana}/{player.MaxMana}"
                        + string.Concat(player.Statuses.ConvertAll(s => $"  {s.Type} {s.Remaining}")));
                else
                    EditorGUILayout.LabelField("플레이어", root.PlayerDead ? "사망 (R로 새 판)" : "없음");

                int enemies = 0;
                foreach (var unit in root.World.Units.ToSortedList())
                    if (unit.Faction == Faction.Enemy) enemies++;
                EditorGUILayout.LabelField("적", enemies.ToString());
            }
        }

        private static void DrawView(GameRoot root)
        {
            EditorGUILayout.LabelField("보기 (표시만 바꿈)", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                bool reveal = EditorGUILayout.ToggleLeft(
                    new GUIContent("맵 전체 밝히기", "안개를 숨기고 모든 적을 표시한다. 탐험 기록 · 적 AI는 그대로."),
                    root.DebugRevealAll);
                if (reveal != root.DebugRevealAll) root.DebugRevealAll = reveal;
            }
        }
    }
}
