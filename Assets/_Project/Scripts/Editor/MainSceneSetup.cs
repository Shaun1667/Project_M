using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectM.Editor
{
    /// <summary>
    /// Main.unity와 비주얼 에셋을 구성한다. 몇 번 실행해도 결과가 같다 (있으면 찾아 쓰고, 연결만 다시 한다).
    /// 메뉴: ProjectM/Setup/Main Scene. 편집 모드에서만 실행한다.
    /// </summary>
    public static class MainSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string VisualsDir = "Assets/_Project/Data/Visuals";
        private const string TileVisualSetPath = VisualsDir + "/TileVisualSet.asset";
        private const string EntityCatalogPath = VisualsDir + "/EntityVisualCatalog.asset";
        private const string GameConfigPath = "Assets/_Project/Data/Config/GameConfig.asset";

        // 그레이박스: tile.png 슬라이스 0 = 연회색, 1 = 진회색
        private const string FloorTilePath = "Assets/_Project/Art/Greybox/TilePalette/tile_0.asset";
        private const string WallTilePath = "Assets/_Project/Art/Greybox/TilePalette/tile_1.asset";
        private const string PlayerSpritePath = "Assets/_Project/Art/Greybox/player.png";

        [MenuItem("ProjectM/Setup/Main Scene")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[MainSceneSetup] 플레이 중에는 실행하지 않는다.");
                return;
            }

            // 씬을 먼저 연다. OpenScene이 쓰지 않는 에셋을 메모리에서 내리므로, 그 전에 불러온 에셋 참조는 무효가 될 수 있다.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var tileVisuals = EnsureTileVisualSet();
            var catalog = EnsureEntityCatalog();
            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            if (config == null)
            {
                Debug.LogError("[MainSceneSetup] GameConfig 에셋 없음: " + GameConfigPath);
                return;
            }

            var gridGo = FindOrCreate("Grid", null);
            var grid = GetOrAdd<Grid>(gridGo);

            var terrainGo = FindOrCreate("Terrain", gridGo.transform);
            var tilemap = GetOrAdd<Tilemap>(terrainGo);
            GetOrAdd<TilemapRenderer>(terrainGo).sortingOrder = 0;

            var entitiesGo = FindOrCreate("Entities", null);
            var entityPresenter = GetOrAdd<EntityPresenter>(entitiesGo);
            SetRefs(entityPresenter, ("_grid", grid), ("_catalog", catalog));

            var tilemapPresenter = GetOrAdd<TilemapPresenter>(terrainGo);
            SetRefs(tilemapPresenter, ("_tilemap", tilemap), ("_visuals", tileVisuals));

            var cameraGo = GameObject.FindWithTag("MainCamera");
            CameraController cameraController = null;
            if (cameraGo != null) cameraController = GetOrAdd<CameraController>(cameraGo);
            else Debug.LogWarning("[MainSceneSetup] MainCamera 태그 카메라 없음");

            var rootGo = FindOrCreate("GameRoot", null);
            var root = GetOrAdd<GameRoot>(rootGo);
            SetRefs(root,
                ("_config", config),
                ("_tilemapPresenter", tilemapPresenter),
                ("_entityPresenter", entityPresenter),
                ("_camera", cameraController));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MainSceneSetup] 완료: " + ScenePath);
        }

        private static TileVisualSet EnsureTileVisualSet()
        {
            var set = LoadOrCreate<TileVisualSet>(TileVisualSetPath);
            set.Set(TileType.Floor, AssetDatabase.LoadAssetAtPath<TileBase>(FloorTilePath));
            set.Set(TileType.Wall, AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath));
            EditorUtility.SetDirty(set);
            return set;
        }

        private static EntityVisualCatalog EnsureEntityCatalog()
        {
            var catalog = LoadOrCreate<EntityVisualCatalog>(EntityCatalogPath);
            catalog.Set("player", AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath), Color.white);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        private static GameObject FindOrCreate(string name, Transform parent)
        {
            if (parent != null)
            {
                var child = parent.Find(name);
                if (child != null) return child.gameObject;
            }
            else
            {
                foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (go.name == name) return go;
                }
            }

            var created = new GameObject(name);
            if (parent != null) created.transform.SetParent(parent, false);
            return created;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"[MainSceneSetup] {target.GetType().Name}.{field} 필드 없음");
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
