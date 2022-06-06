using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameHandler : MonoBehaviour
{
    // use below to draw lines
    public GameObject lineSample;
    private List<LineRenderer> lineRenderers; 
    
    // Corners of the frame, make them adjustable this time
    public static List<Vector3> corners; // left top, right top, left bottom, right bottom
                                  // (maybe we should just forbid users from starting with bottom borders...)

    // surface stats
    private Vector3 sf_origin;
    private Vector3 sf_xAxis;
    private Vector3 sf_yAxis;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderers = new List<LineRenderer>();
        corners = new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        // is in cursor drawing state
        if (corners.Count == 1)
        {
            lineRenderers[0].SetPosition(1, HandEventsHandler.handray_cursor_position);
        }
        else if (corners.Count == 2)
        {
            Vector3 firstBorderVec = corners[1] - corners[0]; // AB
            Vector3 cursorVec = HandEventsHandler.handray_cursor_position - corners[0]; // AC
            float cos_alpha = Vector3.Dot(firstBorderVec, cursorVec) / (firstBorderVec.magnitude * cursorVec.magnitude);
            Vector3 middle_point = firstBorderVec * ((cursorVec.magnitude * cos_alpha) / firstBorderVec.magnitude); // AM
            Vector3 target_vec = cursorVec - middle_point; // MC
            this.lineRenderers[1].SetPosition(1, corners[0] + target_vec);
            this.lineRenderers[2].SetPosition(1, corners[1] + target_vec);
        }
    }

    public void OnHandRayPinched()
    {
        Debug.Log("Pinched");
        int cornersDetermined = corners.Count;
        if (cornersDetermined == 0) // first corner
        {
            corners.Add(HandEventsHandler.handray_cursor_position);
            LineRenderer firstBorder = this.CreateNewLine(corners[0], corners[0], "firstBorder");
            this.lineRenderers.Add(firstBorder);
        }
        else if (cornersDetermined == 1)
        {
            corners.Add(HandEventsHandler.handray_cursor_position);
            LineRenderer secondBorder = this.CreateNewLine(corners[0], corners[0], "secondBorder"); // second border is the left border
            LineRenderer thirdBorder = this.CreateNewLine(corners[1], corners[1], "thirdBorder"); // rightborder
            this.lineRenderers.Add(secondBorder);
            this.lineRenderers.Add(thirdBorder);
        }
        else if (cornersDetermined == 2) // Done!
        {
            corners.Add(this.lineRenderers[1].GetPosition(1)); // left bottom corner - second border
            corners.Add(this.lineRenderers[2].GetPosition(1)); // right bottom corner - third border
            LineRenderer fourthBorder = CreateNewLine(corners[2], corners[3], "fourthBorder");
            this.lineRenderers.Add(fourthBorder);

            // set surface stats (used for cursor remoting)
            this.sf_origin = corners[2];
            this.sf_xAxis = corners[3] - this.sf_origin;
            this.sf_yAxis = corners[0] - this.sf_origin;
        }
    }

    private LineRenderer CreateNewLine(Vector3 start, Vector3 end, string name)
    {
        GameObject newLine = Object.Instantiate(this.lineSample);
        newLine.name = name;
        LineRenderer newLineRenderer = newLine.GetComponent<LineRenderer>();
        //newLineRenderer.enabled = true;
        newLineRenderer.SetWidth(0.01f, 0.01f);
        newLineRenderer.positionCount = 2;
        newLineRenderer.SetPosition(0, start);
        newLineRenderer.SetPosition(1, end);
        return newLineRenderer;
    }
}
