using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class ClickManager : MonoBehaviour
{
    public Camera maincamera;
    public Grid tilemap;
    Vector3 mouseposition;


    void Update()
    {
        Click();
    }
    void Click()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseposition = Input.mousePosition;
            mouseposition = maincamera.ScreenToWorldPoint(mouseposition);

            RaycastHit2D hit = Physics2D.Raycast(mouseposition, transform.forward, 1000);
            Debug.DrawRay(mouseposition, transform.forward * 10, Color.green, 0.3f);

            if (hit)
            {
                print(hit.transform.GetComponent<Tilemap>().WorldToCell(hit.point));
                CheckTile(hit.transform.GetComponent<Tilemap>().WorldToCell(hit.point));
            }
        }
    }

    void CheckTile(Vector3 check)
    {
        
    }
    
    void Selected()
    {

    }
}
