using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class Automatic : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform left;
    public Transform right;
    public Vector3 starting_left;
    public Vector3 starting_right;
    public Vector3 end_left ;
    public Vector3 end_right ;
    public bool isopen = false;
    public bool isclosed;
    float t;

    void Start()
    {
        starting_left = left.localPosition;
        starting_right = right.localPosition;
        end_left = new Vector3(0, 0, 3);
        end_right = new Vector3(0, 0, -3);
    }

    // Update is called once per frame
    void Update()
    {
        if (isopen)
        {
            t += Time.deltaTime *0.5f;
            t=Mathf.Clamp01(t);
            left.localPosition = Vector3.Lerp(starting_left, end_left, t);
            right.localPosition = Vector3.Lerp(starting_right, end_right, t);

        }
        else if(isclosed){
            left.localPosition = Vector3.Lerp( end_left, starting_left, t);
            right.localPosition =Vector3.Lerp( end_right,starting_right,t);
        }


    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            
           isopen = true;
            isclosed = false;
        }
        

    }
    void OnTriggerExit(Collider other)
    {
       isclosed=true;
        isopen = false;
    }
}
