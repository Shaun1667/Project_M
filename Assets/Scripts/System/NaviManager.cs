using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaviManager : MonoBehaviour
{
    public static NaviManager instance;

    private MapTile[,] nowmap;
    private Vector3Int startpos, endpos;
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

    public void GetMap(MapTile[,] tile)
    {
        nowmap = tile;
    }

    public void GetSEPos(Vector3Int pos)
    {
        startpos = pos;
        endpos = pos;
    }
}
