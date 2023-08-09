using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms.Impl;

public class FrameHandler : MonoBehaviour
{
    // use below to draw lines
    public GameObject lineSample; 
    public static List<LineRenderer> lineRenderers;
    // Corners of the frame, make them adjustable this time
    public static List<Vector3> corners; // left top, right top, left bottom, right bottom
                                         // (maybe we should just forbid users from starting with bottom borders...)
    public static readonly object frame_corner_lock = new object();
    public static bool corners_updated = false;
    // private List<LineRenderer> remoteLines;

    // surface stats
    //private Vector3 sf_origin;
    //private Vector3 sf_xAxis;
    //private Vector3 sf_yAxis;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderers = new List<LineRenderer>();
        // remoteLines = new List<LineRenderer>();
        corners = new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        // is in cursor drawing state
        if (corners.Count == 1)
        {
            lineRenderers[0].SetPosition(1, HandEventsHandler.handray_cursor_position);
            //if (RemoteSpaceControl.remoteSet && !HandEventsHandler.handray_cursor_position.Equals(corners[0]))
            //{
            //    Vector3 direction = (HandEventsHandler.handray_cursor_position - corners[0]).normalized;
            //    this.remoteLines[0].SetPosition(1, corners[0] + RemoteSpaceControl.remoteWidth * direction);
            //}
        }
        else if (corners.Count == 2)
        {
            Vector3 firstBorderVec = corners[1] - corners[0]; // AB
            Vector3 cursorVec = HandEventsHandler.handray_cursor_position - corners[0]; // AC
            float cos_alpha = Vector3.Dot(firstBorderVec, cursorVec) / (firstBorderVec.magnitude * cursorVec.magnitude);
            Vector3 middle_point = firstBorderVec * ((cursorVec.magnitude * cos_alpha) / firstBorderVec.magnitude); // AM
            Vector3 target_vec = cursorVec - middle_point; // MC
            lineRenderers[1].SetPosition(1, corners[0] + target_vec);
            lineRenderers[2].SetPosition(1, corners[1] + target_vec);

            // remote frame indicator (full)
            //if (RemoteSpaceControl.remoteSet && target_vec.magnitude > 0)
            //{
            //    Vector3 direction = target_vec.normalized;
            //    this.remoteLines[1].SetPosition(1, corners[0] + RemoteSpaceControl.remoteHeight * direction);
            //    this.remoteLines[2].SetPosition(0, this.remoteLines[0].GetPosition(1));
            //    this.remoteLines[2].SetPosition(1, this.remoteLines[0].GetPosition(1) + RemoteSpaceControl.remoteHeight * direction);
            //    this.remoteLines[3].SetPosition(0, corners[0] + RemoteSpaceControl.remoteHeight * direction);
            //    this.remoteLines[3].SetPosition(1, this.remoteLines[0].GetPosition(1) + RemoteSpaceControl.remoteHeight * direction);
            //}
        }
        
    }

    public void SetLocalPinched()
    {
        Debug.Log("Setup Local Frame Corner!");
        int cornersDetermined = corners.Count;
        if (cornersDetermined == 0) // first corner
        {
            lock (frame_corner_lock) 
            {
                corners.Add(HandEventsHandler.handray_cursor_position);
            }
            LineRenderer firstBorder = this.CreateNewLine(corners[0], corners[0], "firstBorder");
            lineRenderers.Add(firstBorder);

            // remote frame indicator, start with just the first line (just to avoid surface finding)
            //LineRenderer firstRemoteBorder = this.CreateNewLine(corners[0], corners[0], "firstRemoteBorder");
            //firstRemoteBorder.SetColors(new Color(0, 1, 0, 0.5f), new Color(0, 1, 0, 0.5f));
            //this.remoteLines.Add(firstRemoteBorder);
        }
        else if (cornersDetermined == 1)
        {
            lock (frame_corner_lock)
            {
                corners.Add(HandEventsHandler.handray_cursor_position);
            }
            LineRenderer secondBorder = this.CreateNewLine(corners[0], corners[0], "secondBorder"); // second border is the left border
            LineRenderer thirdBorder = this.CreateNewLine(corners[1], corners[1], "thirdBorder"); // rightborder
            lineRenderers.Add(secondBorder);
            lineRenderers.Add(thirdBorder);

            // remote frame indicator, display the full frame
            //if (RemoteSpaceControl.remoteSet)
            //{
            //    LineRenderer secondRemoteBorder = this.CreateNewLine(corners[0], corners[0], "secondRemoteBorder"); // second border is the left border
            //    LineRenderer thirdRemoteBorder = this.CreateNewLine(corners[0], corners[0], "thirdRemoteBorder"); // rightborder
            //    LineRenderer fourthRemoteBorder = this.CreateNewLine(corners[0], corners[0], "fourthRemoteBorder"); // last boder
            //    secondRemoteBorder.SetColors(new Color(0, 1, 0, 0.5f), new Color(0, 1, 0, 0.5f));
            //    thirdRemoteBorder.SetColors(new Color(0, 1, 0, 0.5f), new Color(0, 1, 0, 0.5f));
            //    fourthRemoteBorder.SetColors(new Color(0, 1, 0, 0.5f), new Color(0, 1, 0, 0.5f));
            //    this.remoteLines.Add(secondRemoteBorder);
            //    this.remoteLines.Add(thirdRemoteBorder);
            //    this.remoteLines.Add(fourthRemoteBorder);
            //}
        }
        else if (cornersDetermined == 2) // Done!
        {
            lock (frame_corner_lock) { 
                corners.Add(lineRenderers[1].GetPosition(1)); // left bottom corner - second border
                corners.Add(lineRenderers[2].GetPosition(1)); // right bottom corner - third border
            }
            LineRenderer fourthBorder = CreateNewLine(corners[2], corners[3], "fourthBorder");
            lineRenderers.Add(fourthBorder);

            // Destroy remote frame indicator if exists
            //for (int i = 0; i < remoteLines.Count; i++)
            //{
            //    Destroy(remoteLines[i].gameObject);
            //}
            //remoteLines.Clear();

            //Debug.Log(corners[0].ToString() + " ; " + corners[1].ToString() + " ; " + corners[2].ToString() + " ; " + corners[3].ToString());
        }
    }

    // turn local portal reposition on and off
    public void ToggleLocalPortalReposition()
    {
        Vector3 X_Axis = corners[1] - corners[0];
        Vector3 Y_Axis = corners[2] - corners[0];
        Vector3 heightNormal = Vector3.Normalize(Vector3.Cross(X_Axis, Y_Axis));

        if (GetComponent<BoxCollider>().enabled)
        {
            GetComponent<BoxCollider>().enabled = false;
            transform.position -= 0.03f * heightNormal;
        }
        else
        {
            GetComponent<BoxCollider>().enabled = true;

            transform.position += 0.03f * heightNormal;
        }
    }

    private LineRenderer CreateNewLine(Vector3 start, Vector3 end, string name, float width=0.005f)
    {
        GameObject newLine = Object.Instantiate(this.lineSample, transform);
        newLine.name = name;
        LineRenderer newLineRenderer = newLine.GetComponent<LineRenderer>();
        //newLineRenderer.enabled = true;
        newLineRenderer.SetWidth(width, width);
        newLineRenderer.positionCount = 2;
        newLineRenderer.SetPosition(0, start);
        newLineRenderer.SetPosition(1, end);
        return newLineRenderer;
    }
}
