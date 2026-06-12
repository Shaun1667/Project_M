using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;


[System.Serializable]
public class MapTile
{
    public MapTile(int _type, int _x, int _y) { type = _type; x = _x; y = _y; }

    public int type;    //0 null, 1 땅, 2 벽
    public int state;   //0 빈칸, 1 차있음

    public MapTile ParentNode;

    // G : 시작으로부터 이동했던 거리, H : |가로|+|세로| 장애물 무시하여 목표까지의 거리, F : G + H
    public int x, y, G, H;
    public int F { get { return G + H; } }
}

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
    //List<List<int>> mappoints;
    List<MapTile> mappoints;


    public Sprite tileSprite;
    public Tilemap tilemap;
    public TileBase[] groundbase;

    public Vector2Int bottomLeft, topRight, startPos, targetPos;
    public List<MapTile> FinalNodeList;

    int sizeX, sizeY;

    MapTile StartNode, TargetNode, CurNode;
    MapTile[,] Wholemap;
    List<MapTile> OpenList, ClosedList;


    List<List<MapTile>> Room_list;

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
        //MapPrinter(MapMaker());
        MapMaker();
        
        PathFinding();
        //StartCoroutine(MapPrinter(Wholemap));
        //StartCoroutine(MapPrinter(Wholemap));
    }

    public void MapButton()
    {
        StartCoroutine(MapPrinter(Wholemap));
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

    private void MapMaker()
    {
        Room_count = mapX / 10 + mapY / 10;
        Room_maxsize = mapX / 5 + mapY / 5;
        //맵 크기와 방 갯수
        Room_minsize = 7;
        Wholemap = new MapTile[mapX, mapY];   //전체 맵

        for (int i = 0; i < mapX; i++)  // 값 초기화
        {
            for (int j = 0; j < mapY; j++)
            {
                Wholemap[i, j] = new MapTile(0, i, j);  // type=0(null), x=i, y=j
            }
        }

        //각각 방과 방 기준점
        Room_list = new List<List<MapTile>>();
        //mappoints = new List<List<MapTile>>();
        mappoints = new List<MapTile>();

        int pickx, picky, picksizex, picksizey;
        for (int i = 0; i < Room_count; i++)
        {
            Room_list.Add(new List<MapTile>());
            //mappoints.Add(new List<MapTile>());
            mappoints.Add(null);

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


                if (Wholemap[pickx, picky].type != 2)    //첫 스타트 지점이 벽인지 확인
                {
                    for (int j = 0; j < picksizex; j++)
                    {
                        for (int k = 0; k < picksizey; k++)
                        {
                            if (pickx + j < mapX && picky + k < mapY)
                            {

                                Wholemap[pickx + j, picky + k].type = 1;
                                //Wholemap[pickx + j, picky + k].x = pickx + j;
                                //Wholemap[pickx + j, picky + k].y = picky + k;



                                Room_list[i].Add(Wholemap[pickx + j, picky + k]);

                                if (picky + k + 1 >= mapY)
                                {
                                    break;
                                }
                                //}
                            }
                        }
                        //if (pickx + j == picksizey || pickx + j + 1 >= mapX)
                        //{

                        //}
                        if (pickx + j + 1 >= mapX)
                        {
                            break;
                        }
                    }
                    break;
                }
            }
        }

        for (int i = 0; i < Room_list.Count; i++)
        {
            int ran = PICK(Room_list[i].Count);


            //mappoints[i].x = Room_list[i][ran].x;
            //mappoints[i].y = Room_list[i][ran].y;
            mappoints[i] = Room_list[i][ran];

        }


    }

    IEnumerator MapPrinter(MapTile[,] map)
    //private void MapPrinter(MapTile[,] map)
    {
        // 특정 좌표(예: 0, 0, 0)의 타일 가져오기
        mappos.z = 0;
        //TileBase tile;
        for (int i = 0; i < mapX; i++)
        {
            for (int j = 0; j < mapY; j++)
            {
                if (map[i, j].type == 1)
                {
                    mappos.x = i;
                    mappos.y = j;
                    //tile = tilemap.GetTile(mappos);
                    tilemap.SetTile(mappos, groundbase[0]);
                    yield return YieldCache.Seconds001;
                }

            }
        }


    }
    public void PathFinding()
    {

        for (int i = 0; i < mappoints.Count - 1; i++)
        {
            // 시작과 끝 노드, 열린리스트와 닫힌리스트, 마지막리스트 초기화
            StartNode = mappoints[i];
            TargetNode = mappoints[i + 1];

            OpenList = new List<MapTile>() { StartNode };
            ClosedList = new List<MapTile>();
            FinalNodeList = new List<MapTile>();

            while (OpenList.Count > 0)
            {
                // 열린리스트 중 가장 F가 작고 F가 같다면 H가 작은 걸 현재노드로 하고 열린리스트에서 닫힌리스트로 옮기기
                CurNode = OpenList[0];
                for (int j = 1; j < OpenList.Count; j++)
                    if (OpenList[j].F <= CurNode.F && OpenList[j].H < CurNode.H) CurNode = OpenList[j];

                OpenList.Remove(CurNode);
                ClosedList.Add(CurNode);


                // 마지막
                if (CurNode == TargetNode)
                {
                    MapTile TargetCurNode = TargetNode;
                    while (TargetCurNode != StartNode)
                    {
                        FinalNodeList.Add(TargetCurNode);
                        TargetCurNode = TargetCurNode.ParentNode;
                    }
                    FinalNodeList.Add(StartNode);
                    FinalNodeList.Reverse();

                    //for (int j = 0; j < FinalNodeList.Count; j++) print(j + "번째는 " + FinalNodeList[j].x + ", " + FinalNodeList[j].y);
                    for (int j = 0; j < FinalNodeList.Count; j++)
                    {
                        Wholemap[FinalNodeList[j].x, FinalNodeList[j].y].type = 1;
                    }
                    break;
                }

                // ↑ → ↓ ←
                OpenListAdd(CurNode.x, CurNode.y + 1);
                OpenListAdd(CurNode.x + 1, CurNode.y);
                OpenListAdd(CurNode.x, CurNode.y - 1);
                OpenListAdd(CurNode.x - 1, CurNode.y);
            }
        }


    }

    void OpenListAdd(int checkX, int checkY)
    {
        // 상하좌우 범위를 벗어나지 않고, 벽이 아니면서, 닫힌리스트에 없다면
        if (checkX >= bottomLeft.x && checkX < mapX && checkY >= bottomLeft.y && checkY < mapY && Wholemap[checkX, checkY].type != 2 && !ClosedList.Contains(Wholemap[checkX, checkY]))
        {


            // 이웃노드에 넣고, 직선은 10, 대각선은 14비용
            MapTile NeighborNode = Wholemap[checkX, checkY];
            int MoveCost = CurNode.G + (CurNode.x - checkX == 0 || CurNode.y - checkY == 0 ? 10 : 14);


            // 이동비용이 이웃노드G보다 작거나 또는 열린리스트에 이웃노드가 없다면 G, H, ParentNode를 설정 후 열린리스트에 추가
            if (MoveCost < NeighborNode.G || !OpenList.Contains(NeighborNode))
            {
                NeighborNode.G = MoveCost;
                NeighborNode.H = (Mathf.Abs(NeighborNode.x - TargetNode.x) + Mathf.Abs(NeighborNode.y - TargetNode.y)) * 10;
                NeighborNode.ParentNode = CurNode;

                OpenList.Add(NeighborNode);
            }
        }
    }
}
