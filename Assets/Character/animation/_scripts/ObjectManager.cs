using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    [SerializeField]
    GameObject prefrab;
    int initial = 20;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        prewarm();
   GetFromPool();

    }
   public List<GameObject> pool = new List<GameObject>();
    // create a method to initialize gameobjects 
    //create a list 
    GameObject CreateObject()
    {
        GameObject go = Instantiate(prefrab);
        go.SetActive(false);

        return go;
    }
    void prewarm()
    {
        for (int i = 0; i< initial; i++)
        {
            GameObject go = CreateObject();
            pool.Add(go);
            go.transform.parent=transform;

        }
    }
 


    // creat emehto to  get object from pool 
    GameObject GetFromPool()
    {
        GameObject go = null;
        foreach(var item in pool)
        {
            if (!item.activeInHierarchy)
            {
                item.SetActive(true);
                item.transform.parent = null;

                return item;

            }
          
        }
        Debug.LogError("null object not enough ites in pool - please increase pool size");
        return null;
    }
    // create mehtod to put back the boject in th pool
    void ReturnPool()
    {
        foreach (var item in pool)
        {
            if (item.activeInHierarchy)
            {
                item.SetActive(false);
            

            }

        }
        Debug.LogError("null object not enough ites in pool - please increase pool size");
       
    }
}

    // Update is called once per frame
    
    
