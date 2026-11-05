using System.IO;
using ProjectM.Presentation;
using UnityEditor;
using UnityEngine;

namespace ProjectM.Editor
{
    /// <summary>
    /// G-17 아이템 아이콘 (2026-11-05, ART_ASSETS ★ 3차 29 — 결정: 모델 렌더 아이콘부터): ItemVisualCatalog에서 모델이 있는 줄을 모두
    /// 바닥에 놓인 모습 그대로(눕힘 여부 같음) 게임 카메라 각도에서 찍어 Art/Icons3D/&lt;키&gt;.png(투명 배경)로 만들고 Icon3D에 연결한다.
    /// 3D 그래픽이면(MainSceneSetup.Pixel2D = false) 카탈로그가 3D 아이콘을 먼저 쓴다 — 인벤토리 · 빠른 칸. 픽셀 그림(Icon)은 그대로 둔다.
    /// Varco Models · Consumables 다음에 실행. 여러 번 실행해도 같음.
    /// </summary>
    public static class ItemIconSetup
    {
        private const string CatalogPath = "Assets/_Project/Data/Visuals/ItemVisualCatalog.asset";
        private const string IconDir = "Assets/_Project/Art/Icons3D";
        /// <summary>아이콘 크기 (px) · 가장자리 여백 비율 · 카메라 내려보는 각도 (게임 카메라와 비슷하게).</summary>
        private const int IconSize = 128;
        private const float Margin = 0.08f;
        private const float CameraPitch = 50f;
        private const float CameraYaw = 20f;
        /// <summary>찍는 곳 — 씬의 다른 물체와 겹치지 않게 멀리.</summary>
        private static readonly Vector3 StagePosition = new Vector3(0f, -500f, 5000f);

        [MenuItem("ProjectM/Setup/Item Icons")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[ItemIconSetup] 플레이 중에는 실행하지 않는다.");
                return;
            }
            var catalog = AssetDatabase.LoadAssetAtPath<ItemVisualCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError("[ItemIconSetup] 카탈로그 없음: " + CatalogPath);
                return;
            }
            Directory.CreateDirectory(IconDir);

            var stage = new GameObject("IconStage");
            stage.transform.position = StagePosition;
            var lightGo = new GameObject("Key");
            lightGo.transform.SetParent(stage.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var camGo = new GameObject("IconCamera");
            camGo.transform.SetParent(stage.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.enabled = false;
            var rt = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            int made = 0;
            try
            {
                foreach (var (id, prefab, upright) in new System.Collections.Generic.List<(string, GameObject, bool)>(catalog.Models())) // 복사 — 연결하면서 표가 바뀜
                {
                    var holder = new GameObject("Item");
                    holder.transform.SetParent(stage.transform, false);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    model.transform.SetParent(holder.transform, false);
                    model.transform.localRotation = upright ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f); // FloorItemPresenter와 같은 모습
                    var renderers = model.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0)
                    {
                        Object.DestroyImmediate(holder);
                        continue;
                    }
                    Frame(cam, renderers);
                    var png = RenderTransparent(cam, rt);
                    Object.DestroyImmediate(holder);

                    string path = $"{IconDir}/{id}.png";
                    File.WriteAllBytes(path, png);
                    AssetDatabase.ImportAsset(path);
                    if (AssetImporter.GetAtPath(path) is TextureImporter importer
                        && (importer.textureType != TextureImporterType.Sprite || importer.alphaIsTransparency == false || importer.mipmapEnabled))
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        importer.SaveAndReimport();
                    }
                    catalog.SetIcon3D(id, AssetDatabase.LoadAssetAtPath<Sprite>(path));
                    made++;
                }
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(stage);
            }
            catalog.SetUse3DIcons(!MainSceneSetup.Pixel2D);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ItemIconSetup] 아이콘 {made}개: {IconDir}");
        }

        /// <summary>카메라를 게임 각도로 두고 모델 전체가 여백을 두고 꽉 차게 정사영 크기를 맞춘다.</summary>
        private static void Frame(Camera cam, Renderer[] renderers)
        {
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            cam.transform.rotation = rotation;
            cam.transform.position = b.center - rotation * Vector3.forward * (b.extents.magnitude * 4f + 1f);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = b.extents.magnitude * 8f + 2f;
            // 바운딩 상자 꼭짓점을 카메라 평면에 비춰 가로 · 세로 중 큰 쪽에 맞춤
            float half = 0f;
            var inv = Quaternion.Inverse(rotation);
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var local = inv * (corner - b.center);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
            half = Mathf.Max(max.x - min.x, max.y - min.y) * 0.5f;
            var offset = rotation * new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            cam.transform.position += offset;
            cam.orthographicSize = Mathf.Max(0.01f, half / (1f - 2f * Margin));
        }

        /// <summary>
        /// 검은 배경 · 흰 배경으로 두 번 찍어 알파를 구한다 (URP 후처리가 배경 알파를 덮어써도 됨): a = 1 − (흰 − 검), 색 = 검 ÷ a.
        /// </summary>
        private static byte[] RenderTransparent(Camera cam, RenderTexture rt)
        {
            var black = Capture(cam, rt, Color.black);
            var white = Capture(cam, rt, Color.white);
            var outTex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            var b = black.GetPixels();
            var w = white.GetPixels();
            var result = new Color[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                float a = 1f - Mathf.Clamp01(((w[i].r - b[i].r) + (w[i].g - b[i].g) + (w[i].b - b[i].b)) / 3f);
                result[i] = a > 0.001f ? new Color(Mathf.Clamp01(b[i].r / a), Mathf.Clamp01(b[i].g / a), Mathf.Clamp01(b[i].b / a), a) : Color.clear;
            }
            outTex.SetPixels(result);
            outTex.Apply();
            var png = outTex.EncodeToPNG();
            Object.DestroyImmediate(black);
            Object.DestroyImmediate(white);
            Object.DestroyImmediate(outTex);
            return png;
        }

        private static Texture2D Capture(Camera cam, RenderTexture rt, Color background)
        {
            cam.backgroundColor = background;
            cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            return tex;
        }
    }
}
