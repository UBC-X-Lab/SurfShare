using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    GameObject lineStart;
    GameObject lineEnd;
    public GameObject lineSample;
    GameObject newLine;
    // Start is called before the first frame update
    void Start()
    {
        lineStart = this.gameObject;
        lineEnd = GameObject.Find("End");
        newLine = Object.Instantiate(lineSample);
        newLine.name = "newLine";
        newLine.GetComponent<LineRenderer>().enabled = true;
        newLine.GetComponent<LineRenderer>().useWorldSpace = true;
        newLine.GetComponent<LineRenderer>().positionCount = 3;
        newLine.GetComponent<LineRenderer>().SetPosition(0, lineStart.transform.position);
        newLine.GetComponent<LineRenderer>().SetPosition(1, lineEnd.transform.position);
        newLine.GetComponent<LineRenderer>().SetPosition(2, lineEnd.transform.position + Vector3.right * 5);
        newLine.GetComponent<LineRenderer>().SetWidth(0.5f, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
