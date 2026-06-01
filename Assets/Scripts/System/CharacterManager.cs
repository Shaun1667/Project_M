using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public interface ICharacter
{
    void CalHP(float damage);
}


public class CharacterManager : MonoBehaviour
{
    public static CharacterManager instance;

    public static CharacterManager Instance
    {
        get
        {
            return instance;
        }
    }

    public struct Character
    {
        public float HP;

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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
