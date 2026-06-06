using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;


//[System.Serializable]
//public class MapTile
//{
//    public MapTile(int _type, int _x, int _y) { type = _type; x = _x; y = _y; }
    
//    public int type;    //0 벽, 1 땅
//    public int state;   //0 빈칸, 1 차있음
//    public int obstacle;    //장애물

//    public Node ParentNode;

//    // G : 시작으로부터 이동했던 거리, H : |가로|+|세로| 장애물 무시하여 목표까지의 거리, F : G + H
//    public int x, y, G, H;
//    public int F { get { return G + H; } }
//}

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator instance;

    delegate int Extensive<T>(T count);
    Extensive<int> PICK;

    delegate int Extensive2<T>(T count, T count2);
    Extensive2<int> RANGE;

    private float _subseed = 3141592653; //시드값을 11진수 이상 사용 고려해볼것
    public int mapX;
    public int mapY;
    int Room_count;
    int Room_maxsize;
    int Room_minsize;
    Vector3Int mappos; 


    public Sprite tileSprite;
    public Tilemap tilemap;
    public TileBase[] groundbase;


    public struct MapTile
    {
        public int type;    //0 벽, 1 땅
        public int state;   //0 빈칸, 1 차있음
        public int obstacle;    //장애물
    }
    List<MapTile[,]> Room_list;

    public float subSeed
    {
        get { return _subseed; }
    }

    private int _seed;
    public int Seed
    {
        get
        {
            return _seed;
        }
        private set
        {
            _seed = value;
        }
    }

    static class YieldCache
    {
        public static readonly WaitForSeconds Seconds001 = new WaitForSeconds(0.01f);
        public static readonly WaitForSeconds Seconds01 = new WaitForSeconds(0.1f);
        public static readonly WaitForSeconds Seconds02 = new WaitForSeconds(0.2f);
        public static readonly WaitForSeconds Seconds1 = new WaitForSeconds(1);
        public static readonly WaitForEndOfFrame EOF = new WaitForEndOfFrame();

    }

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

    void SetDel()
    {
        PICK = (count) => { return Random.Range(0, count); };
        RANGE = (count, count2) => { return Random.Range(count, count2); };
    }





    private void Start()
    {
        NewGame(); //테스트용
        MapPrinter(MapMaker());
    }

    public void NewGame()
    {
        //새로 사용할 시드
        System.Random randSeed = new System.Random();
        int seed = randSeed.Next();

        Debug.Log(seed);
        Seed = seed;

        Random.InitState(Seed);  //현재 시드 기준으로 고정

    }

    private MapTile[,] MapMaker()
    {
        Room_count = mapX / 10 + mapY / 10;
        Room_maxsize = mapX / 5 + mapY / 5;
        //맵 크기와 방 갯수
        Room_minsize = 7;
        MapTile[,] map = new MapTile[mapX, mapY];
        //전체 맵
        Room_list = new List<MapTile[,]>();
        //방 리스트
        int pickx, picky, picksizex, picksizey;
        for (int i = 0; i < Room_count; i++)
        {
            while (true)
            {
                pickx = PICK(mapX - Room_minsize);
                picky = PICK(mapY - Room_minsize);
                picksizex = PICK(Room_maxsize);
                if (picksizex < Room_minsize)
                {
                    picksizex += Room_minsize;  //최소값 보정
                }
                picksizey = PICK(Room_maxsize);
                if (picksizey < Room_minsize)
                {
                    picksizey += Room_minsize;  //최소값 보정
                }
                
                
                if (map[pickx, picky].type == 0)    //첫 스타트 지점이 벽인지 확인
                {
                    for (int j = 0; j < picksizey; j++)
                    {
                        for (int k = 0; k < picksizex; k++)
                        {
                            if (pickx + k < mapX && picky + j < mapY)
                            {
                                //if (map[pickx + k, picky + j].type == 1)
                                //{
                                //    break;
                                //}
                                //else
                                //{
                                map[picky + j, pickx + k].type = 1;

                                if (pickx + k + 1 >= mapX)
                                {
                                    break;
                                }
                                //}
                            }
                        }
                        if (picky+j == picksizey || picky + j + 1 >= mapY)
                        {

                        }
                        if (picky + j + 1 >= mapY)
                        {
                            break;
                        }
                    }
                    break;
                }
            }
        }
        return map;
    }

    private void MapPrinter(MapTile[,] map)
    {
        // 특정 좌표(예: 0, 0, 0)의 타일 가져오기
        mappos.z = 0;
        //TileBase tile;
        for (int i = 0; i < mapY; i++)
        {
            for (int j = 0; j < mapX; j++)
            {
                if (map[j, i].type == 1)
                {
                    mappos.x = j;
                    mappos.y = i;
                    //tile = tilemap.GetTile(mappos);
                    tilemap.SetTile(mappos, groundbase[0]);
                }

            }
        }



    }

}
