using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICharacter
{

}

public interface IObject
{

}

//private struct _Character
//{

//}
//public struct Character
//{
//    get
//    {
//        return _Character;
//    }
//    private set
//    {
//        _Character = value;
//    }
//}

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager instance;



    private void Awake()
    {
        instance = this;
    }
}
