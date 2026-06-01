using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IObject
{

}



public class ObjectManager : MonoBehaviour
{
    public static ObjectManager instance;

    public static ObjectManager Instance
    {
        get
        {
            return instance;
        }
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
    }
}
