using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ObjectPool : MonoBehaviour
{
    //public static ObjectPool Instance;

    //[SerializeField]
    //private GameObject BulletPrefeb;
    //private Queue<Bullet> BulletQueue = new Queue<Bullet>();



    //private void Awake()
    //{
    //    DontDestroyOnLoad(gameObject);
    //    Instance = this;
    //    InitializeBullet(45);
    //}

    //private Bullet CreateNewBullet()
    //{
    //    var newBull = Instantiate(BulletPrefeb, transform).GetComponent<Bullet>();
    //    newBull.gameObject.SetActive(false);
    //    return newBull;
    //}

    //private void InitializeBullet(int count)
    //{
    //    for (int i = 0; i < count; i++)
    //    {
    //        BulletQueue.Enqueue(CreateNewBullet());
    //    }
    //}

    //private void InitializeBullet_Impact(int count)
    //{
    //    for (int i = 0; i < count; i++)
    //    {
    //        Bullet_ImpactQueue.Enqueue(CreateNewBullet_Impact());
    //    }
    //}

    //public static Bullet GetBullet()
    //{
    //    if (Instance.BulletQueue.Count > 0)
    //    {
    //        var obj = Instance.BulletQueue.Dequeue();
    //        obj.transform.SetParent(null);
    //        obj.gameObject.SetActive(true);
    //        return obj;
    //    }
    //    else
    //    {
    //        var newObj = Instance.CreateNewBullet();
    //        newObj.transform.SetParent(null);
    //        newObj.gameObject.SetActive(true);
    //        return newObj;
    //    }

    //}


    //public static void ReturnBullet(Bullet bullet)
    //{
    //    bullet.gameObject.SetActive(false);
    //    bullet.transform.SetParent(Instance.transform);
    //    Instance.BulletQueue.Enqueue(bullet);
    //}
}

   