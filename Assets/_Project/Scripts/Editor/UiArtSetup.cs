using ProjectM.Magic;
using ProjectM.Presentation;
using ProjectM.Status;
using ProjectM.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectM.Editor
{
    /// <summary>
    /// G-18 UI 그림 (2026-11-05, ART_ASSETS ★ 3차 29 · 9번 — 결정: Varco 이미지 UI 키트 · 아이콘, 범위 1차 + 2차).
    /// 메뉴 ProjectM/Setup/UI Art (멱등, Main Scene 다음): Art/UI/*.png(Varco 시트를 자른 그림)를 스프라이트로 가져오고(틀은 9-slice 테두리),
    /// Data/Visuals/UiSkin.asset을 채운 뒤 Main 씬의 HUD · 인벤토리 · 결과 화면에 UiSkin, 엔티티 모습에 피해 숫자 글꼴을 연결한다.
    /// </summary>
    public static class UiArtSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string ArtDir = "Assets/_Project/Art/UI/";
        private const string SkinPath = "Assets/_Project/Data/Visuals/UiSkin.asset";
        private const string FontPath = "Assets/_Project/Art/Fonts/Galmuri11 SDF.asset";

        /// <summary>틀 그림의 9-slice 테두리 (원본 px: 왼 · 아래 · 오른 · 위) — 자른 그림 크기에 맞춤 (ART_ASSETS 12-11).</summary>
        private static readonly (string File, Vector4 Border)[] Frames =
        {
            ("ui_panel", new Vector4(64, 64, 64, 64)),
            ("ui_slot_normal", new Vector4(48, 48, 48, 48)),
            ("ui_slot_selected", new Vector4(48, 48, 48, 48)),
            ("ui_slot_empty", new Vector4(48, 48, 48, 48)),
            ("ui_slot_disabled", new Vector4(48, 48, 48, 48)),
            ("ui_bar", new Vector4(40, 36, 40, 36)),
            ("ui_plaque", new Vector4(130, 70, 130, 70)),
        };

        private static readonly StatusType[] Statuses =
        {
            StatusType.Burn, StatusType.Slow, StatusType.Poison, StatusType.Paralyze, StatusType.Wet, StatusType.Frozen,
            StatusType.Haste, StatusType.Invisible, StatusType.Levitating, StatusType.MindVision, StatusType.Asleep, StatusType.Terrified,
        };

        private static readonly Element[] Elements =
        {
            Element.Fire, Element.Ice, Element.Lightning, Element.Poison, Element.Earth, Element.Wind, Element.Water, Element.Neutral,
        };

        [MenuItem("ProjectM/Setup/UI Art")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[UiArtSetup] 플레이 중에는 실행하지 않는다.");
                return;
            }
            // 씬을 먼저 연다 — OpenScene이 쓰지 않는 에셋을 내리므로 에셋은 그 뒤에 불러온다 (CLAUDE.md Git · 환경)
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir.TrimEnd('/') }))
                ImportSprite(AssetDatabase.GUIDToAssetPath(guid));

            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UiSkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }
            skin.Panel = Load("ui_panel");
            skin.SlotNormal = Load("ui_slot_normal");
            skin.SlotSelected = Load("ui_slot_selected");
            skin.SlotEmpty = Load("ui_slot_empty");
            skin.SlotDisabled = Load("ui_slot_disabled");
            skin.BarFrame = Load("ui_bar");
            skin.Plaque = Load("ui_plaque");
            skin.Victory = Load("ui_victory");
            skin.Defeat = Load("ui_defeat");
            skin.FusionIcon = Load("spell_Fusion");
            foreach (var s in Statuses) skin.SetStatusIcon(s, Load("status_" + s));
            foreach (var e in Elements) skin.SetElementIcon(e, Load("spell_" + e));
            EditorUtility.SetDirty(skin);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            int linked = 0;
            foreach (var hud in Object.FindObjectsByType<HudPresenter>(FindObjectsInactive.Include)) linked += SetRef(hud, "_skin", skin);
            foreach (var inv in Object.FindObjectsByType<InventoryPresenter>(FindObjectsInactive.Include)) linked += SetRef(inv, "_skin", skin);
            foreach (var res in Object.FindObjectsByType<ResultPresenter>(FindObjectsInactive.Include)) linked += SetRef(res, "_skin", skin);
            foreach (var ent in Object.FindObjectsByType<EntityPresenter>(FindObjectsInactive.Include)) linked += SetRef(ent, "_damageFont", font);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiArtSetup] 완료: UiSkin · 씬 연결 {linked}곳");
        }

        private static Sprite Load(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + file + ".png");
            if (sprite == null) Debug.LogWarning("[UiArtSetup] 그림 없음: " + ArtDir + file + ".png");
            return sprite;
        }

        private static void ImportSprite(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            var border = Vector4.zero;
            foreach (var (name, b) in Frames)
                if (name == file) border = b;
            bool dirty = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency || importer.mipmapEnabled || importer.spriteBorder != border;
            if (!dirty) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static int SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) return 0;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }
    }
}
