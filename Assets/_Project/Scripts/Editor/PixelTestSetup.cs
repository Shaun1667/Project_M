using ProjectM.Data;
using ProjectM.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Editor
{
    /// <summary>
    /// 픽셀 그래픽 시험 씬 (P-0, 2026-10-20): Art/Pixel/Test의 64px 이미지를 픽셀 설정(점 필터 · 압축 없음 · 64 PPU)으로 가져오고,
    /// Scenes/PixelTest.unity(정사영 카메라 + Tilemap + PixelTestView)를 다시 만든다. 여러 번 실행해도 결과가 같다. Main 씬 · 빌드 목록은 건드리지 않는다.
    /// </summary>
    public static class PixelTestSetup
    {
        private const string ArtDir = "Assets/_Project/Art/Pixel/Test";
        private const string ScenePath = "Assets/_Project/Scenes/PixelTest.unity";
        private const string MaterialPath = ArtDir + "/SpriteUnlit.mat";
        private const string DungeonConfigPath = "Assets/_Project/Data/Dungeon/DungeonGenConfig.asset";
        private const int Ppu = 64;

        [MenuItem("ProjectM/Setup/Pixel Test Scene")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[PixelTestSetup] 플레이 중에는 실행하지 않는다.");
                return;
            }
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) continue;
                Debug.LogWarning("[PixelTestSetup] 저장하지 않은 씬이 있다 — 저장한 뒤 다시 실행한다 (새 씬을 열면 바뀐 내용이 사라짐).");
                return;
            }
            foreach (var name in new[] { "floor", "wall", "wall_top" }) Import(name, SpriteAlignment.Center);
            foreach (var name in new[] { "player", "mouse", "Heroes/hero_black_red" }) Import(name, SpriteAlignment.BottomCenter);
            foreach (var name in new[] { "sword", "potion", "cloth" }) Import(name, SpriteAlignment.Center);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // 씬을 연 뒤에 에셋을 불러온다 (CLAUDE.md — 새 씬이 안 쓰는 에셋을 내림)
            var material = EnsureMaterial();

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.035f, 0.05f);
            camera.transform.position = new Vector3(0, 0, -10);

            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>().cellSize = Vector3.one;
            var tilemapGo = new GameObject("Tiles");
            tilemapGo.transform.SetParent(gridGo.transform, false);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            var tilemapRenderer = tilemapGo.AddComponent<TilemapRenderer>();
            tilemapRenderer.sharedMaterial = material;
            tilemapRenderer.sortingOrder = -100;

            var viewGo = new GameObject("PixelTest");
            var view = viewGo.AddComponent<PixelTestView>();
            var so = new SerializedObject(view);
            so.FindProperty("_dungeonConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DungeonGenConfigAsset>(DungeonConfigPath);
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_tilemap").objectReferenceValue = tilemap;
            so.FindProperty("_spriteMaterial").objectReferenceValue = material;
            so.FindProperty("_floor").objectReferenceValue = LoadSprite("floor");
            so.FindProperty("_wall").objectReferenceValue = LoadSprite("wall");
            so.FindProperty("_wallTop").objectReferenceValue = LoadSprite("wall_top");
            so.FindProperty("_player").objectReferenceValue = LoadSprite("Heroes/hero_black_red"); // 결정 2026-10-21: 금발 · 검은 코트 · 빨간 눈
            so.FindProperty("_enemy").objectReferenceValue = LoadSprite("mouse");
            var items = so.FindProperty("_items");
            string[] itemNames = { "sword", "potion", "cloth" };
            items.arraySize = itemNames.Length;
            for (int i = 0; i < itemNames.Length; i++) items.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(itemNames[i]);
            so.FindProperty("_pixelsPerUnit").intValue = Ppu;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[PixelTestSetup] 완료: " + ScenePath);
        }

        private static void Import(string name, SpriteAlignment pivot) => ImportPixelSprite($"{ArtDir}/{name}.png", pivot);

        /// <summary>픽셀 스프라이트 가져오기 설정: 점 필터 · 압축 없음 · 64 PPU · 밉맵 없음 (MainSceneSetup 캐릭터 초상도 같이 씀).</summary>
        internal static void ImportPixelSprite(string path, SpriteAlignment pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[PixelTestSetup] 이미지 없음: " + path);
                return;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = Ppu;
            settings.spriteAlignment = (int)pivot;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");

        /// <summary>URP Universal 렌더러에서 2D 조명 없이 그대로 그리는 스프라이트 재질.</summary>
        private static Material EnsureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
