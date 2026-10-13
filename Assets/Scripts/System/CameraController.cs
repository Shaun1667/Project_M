using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    public void SnapToTarget(Vector3 targetWorldPos)
    {
        targetWorldPos.z += -10;
        transform.position = targetWorldPos;
    }

    public void SnapToTarget(Transform target)
    {
        SnapToTarget(target.position);
    }
}
