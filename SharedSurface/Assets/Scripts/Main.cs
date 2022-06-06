using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraFrameUtilities;

public class Main : MonoBehaviour
{
    private bool conversion_complete = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // only print once: when corners are determined and conversion has not
        //if (!this.conversion_complete && this.gameObject.GetComponent<FrameHandler>().corners.Count == 4)
        //{
        //    Debug.Log("Conversion Starts!");
        //    foreach (Vector3 corner in this.gameObject.GetComponent<FrameHandler>().corners)
        //    {
        //        // System.Numerics.Vector3 corner_on_frame = CoordinateSystemHelper.GetFramePosition()
        //    }
        //    Debug.Log("Conversion Completed!");
        //}
    }
}

