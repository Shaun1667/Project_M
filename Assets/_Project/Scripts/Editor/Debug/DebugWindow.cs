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
            EditorGUILayout.EndScrollView();
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
