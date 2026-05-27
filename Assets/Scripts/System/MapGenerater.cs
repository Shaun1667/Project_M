using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerater : MonoBehaviour
{
    public static MapGenerater instance;

    delegate int Extensive<T>(T count);
    Extensive<int> PICK;

    private float _subseed = 3141592653; //시드값을 11진수 이상 사용 고려해볼것
    public int mapX;
    public int mapY;
    int Obstacle_count;

    //struct MapTile
    //{
    //public int ICharacter
    //}


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
        instance = this;
        SetDel();
    }

    void SetDel()
    {
        PICK = (count) => { return Random.Range(0, count); };
    }

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

    struct MapTile
    {
        public int type;
        public int who;
    }

    private void Start()
    {
        NewGame(); //테스트용
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
        //장애물 갯수
        Obstacle_count = mapX / 10 * mapY / 10;
        for (int i= 0; i < Obstacle_count; i++)
        {

        }
    }    

}
