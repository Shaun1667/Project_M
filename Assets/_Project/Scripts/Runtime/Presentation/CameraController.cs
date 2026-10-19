using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 고각도 3D 카메라 (G-1, 결정 2026-10-18): 대상(바닥 위 칸 중앙)을 아래로 _pitch° 기울여 _distance 떨어진 곳에서 본다.
    /// 걸어서 옮기면 부드럽게 따라가고(FollowTarget, G-2b), 층 이동 · 새 판은 바로 옮긴다(SnapToTarget). 표현 레이어 전용.
    /// 화면 좌표 → 바닥 점은 ScreenToGround — 물리 대신 바닥 평면(y = 0)과의 교점 (규칙 2).
    /// 둘러보기 (2026-10-19 사용자 요청): 마우스 끌기로 맵을 옮겨 보고(BeginDrag · DragTo — 잡은 바닥 점이 커서 밑에 그대로), 플레이어가 움직이면
    /// FollowTarget이 다시 플레이어 쪽으로 부드럽게 돌아온다. 휠 = 확대 · 축소(Zoom — 거리 _minDistance ~ _maxDistance). 둘러보는 범위는 맵 안(SetPanBounds).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Tooltip("아래로 기울인 각도 (°). 90 = 바로 위")] [Range(30f, 90f)] [SerializeField] private float _pitch = 55f;
        [Tooltip("대상까지 거리 (m) — 세로로 보이는 칸 수를 정함")] [Min(1f)] [SerializeField] private float _distance = 16f;
        [Tooltip("원근 시야각 (°)")] [Range(15f, 90f)] [SerializeField] private float _fieldOfView = 35f;
        [Tooltip("따라가기 부드러움 (초, G-2b) — 0이면 바로 옮김")] [Range(0f, 1f)] [SerializeField] private float _followTime = 0.15f;
        [Header("확대 · 축소 (휠)")]
        [Tooltip("가장 가까이 (m)")] [Min(1f)] [SerializeField] private float _minDistance = 8f;
        [Tooltip("가장 멀리 (m)")] [Min(1f)] [SerializeField] private float _maxDistance = 32f;
        [Tooltip("휠 한 칸에 바뀌는 거리 (m)")] [Min(0.1f)] [SerializeField] private float _zoomStep = 2f;

        private static readonly Plane Ground = new Plane(Vector3.up, Vector3.zero);
        private Camera _camera;
        private Vector3 _target;
        private Vector3 _velocity;
        private bool _following;
        private bool _freeLook;
        private Vector3 _dragAnchor;
        private bool _hasBounds;
        private Vector2 _boundsMin, _boundsMax;

        public Camera Camera => _camera != null ? _camera : (_camera = GetComponent<Camera>());
        /// <summary>지금 보는 바닥 점 (따라가는 목표 · 둘러보는 곳).</summary>
        public Vector3 LookTarget => _target;
        /// <summary>대상까지 거리 (m) — 휠로 바뀜.</summary>
        public float Distance => _distance;
        /// <summary>끌어서 둘러보는 중인지 — 플레이어가 움직이면(FollowTarget) 풀린다.</summary>
        public bool IsFreeLook => _freeLook;

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
            _target = targetWorldPos;
            _velocity = Vector3.zero;
            _following = false;
            _freeLook = false;
        }

        /// <summary>대상 쪽으로 부드럽게 따라간다 (G-2b — 유닛 이동 보간과 같은 박자). 층 이동 · 새 판은 SnapToTarget.</summary>
        public void FollowTarget(Vector3 targetWorldPos)
        {
            if (_followTime <= 0f)
            {
                SnapToTarget(targetWorldPos);
                return;
            }
            _target = targetWorldPos;
            _following = true;
            _freeLook = false; // 플레이어가 움직이면 둘러보기를 끝내고 돌아간다 (사용자 요청 2026-10-19)
        }

        // ── 둘러보기 · 확대 축소 (2026-10-19) ──

        /// <summary>둘러볼 수 있는 바닥 범위 (칸 0 ~ 너비 · 높이 m). 새 판 · 층 이동 때 맵 크기로.</summary>
        public void SetPanBounds(float width, float height)
        {
            _hasBounds = true;
            _boundsMin = Vector2.zero;
            _boundsMax = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        /// <summary>끌기 시작: 화면 점 아래 바닥 점을 잡는다. 하늘이면 false.</summary>
        public bool BeginDrag(Vector2 screenPos) => ScreenToGround(screenPos, out _dragAnchor);

        /// <summary>끌기: 잡은 바닥 점이 커서 밑에 오도록 카메라를 옮긴다.</summary>
        public void DragTo(Vector2 screenPos)
        {
            if (!ScreenToGround(screenPos, out var under)) return;
            PanBy(_dragAnchor - under);
        }

        /// <summary>보는 곳을 바닥에서 delta만큼 옮긴다 (둘러보기 — 따라가기를 멈춤). 범위 밖으로는 안 나감.</summary>
        public void PanBy(Vector3 delta)
        {
            delta.y = 0f;
            var target = _target + delta;
            if (_hasBounds)
            {
                target.x = Mathf.Clamp(target.x, _boundsMin.x, _boundsMax.x);
                target.z = Mathf.Clamp(target.z, _boundsMin.y, _boundsMax.y);
            }
            _target = target;
            _following = false;
            _freeLook = true;
            _velocity = Vector3.zero;
            PlaceAtTarget();
        }

        /// <summary>휠: notches &gt; 0 = 확대(가까이), &lt; 0 = 축소(멀리). 보는 곳은 그대로.</summary>
        public void Zoom(float notches)
        {
            float next = Mathf.Clamp(_distance - notches * _zoomStep, _minDistance, Mathf.Max(_minDistance, _maxDistance));
            if (Mathf.Approximately(next, _distance)) return;
            _distance = next;
            if (!_following) PlaceAtTarget(); // 따라가는 중이면 LateUpdate가 새 거리로 맞춘다
        }

        private void PlaceAtTarget()
        {
            var rotation = Quaternion.Euler(_pitch, 0f, 0f);
            transform.rotation = rotation;
            transform.position = _target - rotation * Vector3.forward * _distance;
        }

        private void LateUpdate()
        {
            if (!_following) return;
            var rotation = Quaternion.Euler(_pitch, 0f, 0f);
            var desired = _target - rotation * Vector3.forward * _distance;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _followTime);
            if ((transform.position - desired).sqrMagnitude < 1e-6f)
            {
                transform.position = desired;
                _following = false;
            }
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
