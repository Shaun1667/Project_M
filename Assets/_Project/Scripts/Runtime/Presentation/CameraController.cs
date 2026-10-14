using UnityEngine;

namespace ProjectM.Presentation
{
    /// <summary>
    /// 카메라를 대상 위치로 즉시 옮긴다. 표현 레이어 전용 (기존 Assets/Scripts/System/CameraController.cs 이전).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _depth = -10f;

        public void SnapToTarget(Vector3 targetWorldPos)
        {
            targetWorldPos.z = _depth;
            transform.position = targetWorldPos;
        }

        public void SnapToTarget(Transform target)
        {
            SnapToTarget(target.position);
        }
    }
}
