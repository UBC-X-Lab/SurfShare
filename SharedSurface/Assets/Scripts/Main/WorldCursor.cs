using UnityEngine;
using System.Collections.Generic;

public class WorldCursor : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    public GameObject lineSample;

    private List<LineRenderer> lineRenderers;

    private List<Vector3> corners;

    //int cornersDetermined = 0; // should have different behavior when different numbers of corners are determined, or maybe directly use a list of lines?

    // remote cursor
    public GameObject remoteCursor;
    private MeshRenderer remoteCursorMesh;

    //public GameObject Brush;
    private GameObject line;
    public GameObject Annotation;

    // data in transmission
    public static Queue<string> buffer = new Queue<string>();

    // my lock
    public static readonly object myLock = new object();

    // surface stats
    private Vector3 sf_origin;
    private Vector3 sf_xAxis;
    private Vector3 sf_yAxis;

    // Use this for initialization
    void Start()
    {
        // Grab the mesh renderer that's on the same object as this script.
        meshRenderer = this.gameObject.GetComponentInChildren<MeshRenderer>();
        lineRenderers = new List<LineRenderer>();
        corners = new List<Vector3>();

        // do the similar to remote cursor
        remoteCursorMesh = this.remoteCursor.GetComponentInChildren<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // Do a raycast into the world based on the user's
        // head position and orientation.
        Vector3 headPosition = Camera.main.transform.position;
        Vector3 gazeDirection = Camera.main.transform.forward;

        RaycastHit hitInfo;

        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
            30.0f, SpatialMapping.PhysicsRaycastMask))
        {
            // If the raycast hit a hologram...
            // Display the cursor mesh.
            this.meshRenderer.enabled = true;

            // Move the cursor to the point where the raycast hit.
            this.transform.position = hitInfo.point;

            // Rotate the cursor to hug the surface of the hologram.
            this.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
        }
        else
        {
            this.meshRenderer.enabled = false;
        }

        // drawing borders
        if (corners.Count == 1)
        {
            lineRenderers[0].SetPosition(1, this.transform.position);
        }else if(corners.Count == 2)
        {
            Vector3 firstBorderVec = this.corners[1] - this.corners[0]; // AB
            Vector3 cursorVec = this.transform.position - this.corners[0]; // AC
            float cos_alpha = Vector3.Dot(firstBorderVec, cursorVec) / (firstBorderVec.magnitude * cursorVec.magnitude);
            Vector3 middle_point = firstBorderVec * ((cursorVec.magnitude * cos_alpha) / firstBorderVec.magnitude); // AM
            Vector3 target_vec = cursorVec - middle_point; // MC
            this.lineRenderers[1].SetPosition(1, corners[0] + target_vec);
            this.lineRenderers[2].SetPosition(1, corners[1] + target_vec);
        }

        // remote cursor control
        if (corners.Count == 4) // the rect has been set!
        {
            this.remoteCursorMesh.enabled = true;

            lock (myLock)
            {
                while (buffer.Count > 0)
                {
                    string remote_cursor_info = buffer.Dequeue();
                    string[] message_parsed = remote_cursor_info.Split(',');

                    float remote_cursor_x = float.Parse(message_parsed[0]); ;
                    float remote_cursor_y = float.Parse(message_parsed[1]); ;
                    int remote_cursor_state = int.Parse(message_parsed[2]); // -1: outside, 0: hover, 1: draw (clicked), 2: clear
                    float remote_cursor_color_r = float.Parse(message_parsed[3]);
                    float remote_cursor_color_g = float.Parse(message_parsed[3]);
                    float remote_cursor_color_b = float.Parse(message_parsed[3]);

                    // placement of cursor
                    this.remoteCursor.transform.rotation = this.transform.rotation;
                    this.remoteCursor.transform.position = remote_cursor_x * this.sf_xAxis + remote_cursor_y * this.sf_yAxis + this.sf_origin;

                    if (remote_cursor_state == 1) // draw
                    {
                        if (this.line == null)
                        {
                            this.line = Instantiate(this.lineSample, this.Annotation.transform);
                            this.line.GetComponent<LineRenderer>().material.color = new Color(remote_cursor_color_r, remote_cursor_color_g, remote_cursor_color_b);
                            this.line.GetComponent<LineRenderer>().SetWidth(0.005f, 0.005f);
                            this.line.tag = "paints";
                        }
                        int pos_index = this.line.GetComponent<LineRenderer>().positionCount;
                        this.line.GetComponent<LineRenderer>().positionCount += 1;
                        this.line.GetComponent<LineRenderer>().SetPosition(pos_index, this.remoteCursor.transform.position);
                    }
                    else if (remote_cursor_state == 2) // clear
                    {
                        GameObject[] paints = GameObject.FindGameObjectsWithTag("paints");
                        foreach (GameObject paint in paints)
                        {
                            Destroy(paint);
                        }
                    }
                    else if (remote_cursor_state == 0) // new line
                    {
                        this.line = null;
                    }
                }
            }
        }
        else
        {
            lock (myLock)
            {
                buffer.Clear(); // just clear it
            }
            this.remoteCursorMesh.enabled = false;
        }
    }

    public void OnTapped()
    {
        Debug.Log("tapped");
        int cornersDetermined = this.corners.Count;
        Debug.Log(cornersDetermined);
        if (cornersDetermined == 0) // first dot
        {
            this.corners.Add(this.transform.position);
            LineRenderer firstBorder = CreateNewLine(this.transform.position, this.transform.position, "firstBorder");
            this.lineRenderers.Add(firstBorder);
        }else if (cornersDetermined == 1)
        {
            this.corners.Add(this.transform.position);
            LineRenderer secondBorder = CreateNewLine(this.corners[0], this.corners[0], "secondBorder"); // second border starts from the original point
            LineRenderer thirdBorder = CreateNewLine(this.transform.position, this.transform.position, "thirdBorder");
            this.lineRenderers.Add(secondBorder);
            this.lineRenderers.Add(thirdBorder);
        }else if (cornersDetermined == 2) // Done!
        {
            this.corners.Add(this.lineRenderers[1].GetPosition(1)); // left bottom corner - second border
            this.corners.Add(this.lineRenderers[2].GetPosition(1)); // right bottom corner - third border
            LineRenderer fourthBorder = CreateNewLine(this.corners[2], this.corners[3], "fourthBorder");
            this.lineRenderers.Add(fourthBorder);

            // set surface stats
            this.sf_origin = this.corners[2];
            this.sf_xAxis = this.corners[3] - this.sf_origin;
            this.sf_yAxis = this.corners[0] - this.sf_origin;
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