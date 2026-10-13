using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject Player;
    private Vector3Int startpos, endpos;

    delegate Vector3 PositionSetter<T>(T pos);
    PositionSetter<Vector3> POSSET;

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

        SetDel();
    }

    void Start()
    {
        MapGenerator.instance.Newmap();
        GetPos();
        SetPlayer();
    }
    void SetDel()
    {
        POSSET = PlusPosition;
    }
    Vector3 PlusPosition(Vector3 pos)
    {
        pos.x += 0.5f;
        pos.y += 0.5f;
        return pos;
    }
    public void GetPos()
    {
        startpos = MapGenerator.instance.startpos;
        endpos = MapGenerator.instance.endpos;
    }
    public void SetPlayer()
    {
        var player = Instantiate(Player, transform);
        player.transform.position = POSSET(startpos);
        CameraController.instance.SnapToTarget(startpos);
    }
    private void PosSetter(Vector3Int pos)
    {

    }
}
