using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 고각도 3D 카메라 (G-1, 결정 2026-10-18): 대상(바닥 위 칸 중앙)을 아래로 _pitch° 기울여 _distance 떨어진 곳에서 본다.
    /// 지금은 즉시 옮긴다 (부드럽게 따라가기는 G-2). 표현 레이어 전용.
    /// 화면 좌표 → 바닥 점은 ScreenToGround — 물리 대신 바닥 평면(y = 0)과의 교점 (규칙 2).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Tooltip("아래로 기울인 각도 (°). 90 = 바로 위")] [Range(30f, 90f)] [SerializeField] private float _pitch = 55f;
        [Tooltip("대상까지 거리 (m) — 세로로 보이는 칸 수를 정함")] [Min(1f)] [SerializeField] private float _distance = 16f;
        [Tooltip("원근 시야각 (°)")] [Range(15f, 90f)] [SerializeField] private float _fieldOfView = 35f;

        private static readonly Plane Ground = new Plane(Vector3.up, Vector3.zero);
        private Camera _camera;

        public Camera Camera => _camera != null ? _camera : (_camera = GetComponent<Camera>());

        private void Awake() => Apply();

        /// <summary>카메라 설정(원근 · 시야각)을 맞춘다 — Setup 메뉴도 부른다.</summary>
        public void Apply()
        {
            Camera.orthographic = false;
            Camera.fieldOfView = _fieldOfView;
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public void SnapToTarget(Vector3 targetWorldPos)
        {
            var rotation = Quaternion.Euler(_pitch, 0f, 0f);
            transform.rotation = rotation;
            transform.position = targetWorldPos - rotation * Vector3.forward * _distance;
        }

        public void SnapToTarget(Transform target)
        {
            SnapToTarget(target.position);
        }

        /// <summary>화면 좌표가 가리키는 바닥(y = 0) 위 점. 하늘을 가리키면 false.</summary>
        public bool ScreenToGround(Vector2 screenPos, out Vector3 point) => ScreenToGround(Camera, screenPos, out point);

        public static bool ScreenToGround(Camera camera, Vector2 screenPos, out Vector3 point)
        {
            var ray = camera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            if (Ground.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = default;
            return false;
        }
    }
}
