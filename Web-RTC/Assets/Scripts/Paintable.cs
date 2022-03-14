using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paintable : MonoBehaviour
{
    public GameObject Brush;
    public Camera drawingCam;
    public float BrushSize = 0.1f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = drawingCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                GameObject newBrush = Instantiate(Brush, hit.point + Vector3.up * 0.1f, Quaternion.identity, transform);
                newBrush.transform.localScale = Vector3.one * BrushSize;
            }
        }
    }
}
