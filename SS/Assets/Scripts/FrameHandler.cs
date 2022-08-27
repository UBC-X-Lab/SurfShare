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

    public GameObject remoteFrame;
    private List<LineRenderer> remoteLines;

    // surface stats
    private Vector3 sf_origin;
    private Vector3 sf_xAxis;
    private Vector3 sf_yAxis;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderers = new List<LineRenderer>();
        remoteLines = new List<LineRenderer>();
        corners = new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        // is in cursor drawing state
        if (corners.Count == 1)
        {
            lineRenderers[0].SetPosition(1, HandEventsHandler.handray_cursor_position);
            if (RemoteSpaceControl.remoteSet && !HandEventsHandler.handray_cursor_position.Equals(corners[0]))
            {
                Vector3 direction = (HandEventsHandler.handray_cursor_position - corners[0]).normalized;
                this.remoteLines[0].SetPosition(1, corners[0] + RemoteSpaceControl.remoteWidth * direction);
            }
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

            // remote frame indicator (full)
            if (RemoteSpaceControl.remoteSet && target_vec.magnitude > 0)
            {
                Vector3 direction = target_vec.normalized;
                this.remoteLines[1].SetPosition(1, corners[0] + RemoteSpaceControl.remoteHeight * direction);
                this.remoteLines[2].SetPosition(0, this.remoteLines[0].GetPosition(1));
                this.remoteLines[2].SetPosition(1, this.remoteLines[0].GetPosition(1) + RemoteSpaceControl.remoteHeight * direction);
                this.remoteLines[3].SetPosition(0, corners[0] + RemoteSpaceControl.remoteHeight * direction);
                this.remoteLines[3].SetPosition(1, this.remoteLines[0].GetPosition(1) + RemoteSpaceControl.remoteHeight * direction);
            }
        }

        // I am the second one to setup (TODO)
        if (RemoteSpaceControl.remoteSet && !RemoteSpaceControl.localSet)
        {
            Debug.Log(HandEventsHandler.handray_cursor_orientation * Vector3.up);
            remoteFrame.GetComponent<LineRenderer>().positionCount = 4;
            remoteFrame.GetComponent<LineRenderer>().SetWidth(0.005f, 0.005f);
            remoteFrame.GetComponent<LineRenderer>().SetPosition(0, remoteFrame.GetComponentInParent<Transform>()
                .InverseTransformPoint(HandEventsHandler.handray_cursor_position + HandEventsHandler.handray_cursor_orientation * (RemoteSpaceControl.remoteWidth * new Vector3(1, 0, 0) + RemoteSpaceControl.remoteHeight * new Vector3(0, 1, 0)) / 2));
            remoteFrame.GetComponent<LineRenderer>().SetPosition(1, remoteFrame.GetComponentInParent<Transform>()
                .InverseTransformPoint(HandEventsHandler.handray_cursor_position + HandEventsHandler.handray_cursor_orientation * (RemoteSpaceControl.remoteWidth * new Vector3(-1, 0, 0) + RemoteSpaceControl.remoteHeight * new Vector3(0, 1, 0)) / 2));
            remoteFrame.GetComponent<LineRenderer>().SetPosition(2, remoteFrame.GetComponentInParent<Transform>()
                .InverseTransformPoint(HandEventsHandler.handray_cursor_position + HandEventsHandler.handray_cursor_orientation * (RemoteSpaceControl.remoteWidth * new Vector3(-1, 0, 0) + RemoteSpaceControl.remoteHeight * new Vector3(0, -1, 0)) / 2));
            remoteFrame.GetComponent<LineRenderer>().SetPosition(3, remoteFrame.GetComponentInParent<Transform>()
                .InverseTransformPoint(HandEventsHandler.handray_cursor_position + HandEventsHandler.handray_cursor_orientation * (RemoteSpaceControl.remoteWidth * new Vector3(1, 0, 0) + RemoteSpaceControl.remoteHeight * new Vector3(0, -1, 0)) / 2));
        }
    }

    public void SetLocalPinched()
    {
        // Debug.Log("Pinched");
        int cornersDetermined = corners.Count;
        if (cornersDetermined == 0) // first corner
        {
            corners.Add(HandEventsHandler.handray_cursor_position);
            LineRenderer firstBorder = this.CreateNewLine(corners[0], corners[0], "firstBorder");
            this.lineRenderers.Add(firstBorder);

            // remote frame indicator, start with just the first line (just to avoid surface finding)
            LineRenderer firstRemoteBorder = this.CreateNewLine(corners[0], corners[0], "firstRemoteBorder");
            firstRemoteBorder.SetColors(new Color(0, 1, 0, 0.5f), new Color(0, 1, 0, 0.5f));
            this.remoteLines.Add(firstRemoteBorder);
        }
        else if (cornersDetermined == 1)
        {
            corners.Add(HandEventsHandler.handray_cursor_position);
            LineRenderer secondBorder = this.CreateNewLine(corners[0], corners[0], "secondBorder"); // second border is the left border
            LineRenderer thirdBorder = this.CreateNewLine(corners[1], corners[1], "thirdBorder"); // rightborder
            this.lineRenderers.Add(secondBorder);
            this.lineRenderers.Add(thirdBorder);

            // remote frame indicator, display the full frame
            if (RemoteSpaceControl.remoteSet)
            {
                LineRenderer secondRemoteBorder = this.CreateNewLine(corners[0], corners[0], "secondRemoteBorder"); // second border is the left border
                LineRenderer thirdRemoteBorder = this.CreateNewLine(corners[0], corners[0], "thirdRemoteBorder"); // rightborder
                LineRenderer fourthRemoteBorder = this.CreateNewLine(corners[0], corners[0], "fourthRemoteBorder"); // last boder
                secondRemoteBorder.SetColors(new Color(0, 1, 0, 0.5f), new Color(0, 1, 0, 0.5f));
                thirdRemoteBorder.SetColors(new Color(0, 1, 0, 0.5f), new Color(0, 1, 0, 0.5f));
                fourthRemoteBorder.SetColors(new Color(0, 1, 0, 0.5f), new Color(0, 1, 0, 0.5f));
                this.remoteLines.Add(secondRemoteBorder);
                this.remoteLines.Add(thirdRemoteBorder);
                this.remoteLines.Add(fourthRemoteBorder);
            }
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

            // Destroy remote frame indicator if exists
            for (int i = 0; i < remoteLines.Count; i++)
            {
                Destroy(remoteLines[i].gameObject);
            }
            remoteLines.Clear();
        }
    }

    private LineRenderer CreateNewLine(Vector3 start, Vector3 end, string name, float width=0.005f)
    {
        GameObject newLine = Object.Instantiate(this.lineSample);
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
