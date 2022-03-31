using UnityEngine;
using System.Collections.Generic;

public class WorldCursor : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    public GameObject lineSample;

    private List<LineRenderer> lineRenderers;

    private List<Vector3> corners;

    //int cornersDetermined = 0; // should have different behavior when different numbers of corners are determined, or maybe directly use a list of lines?

    // Use this for initialization
    void Start()
    {
        // Grab the mesh renderer that's on the same object as this script.
        meshRenderer = this.gameObject.GetComponentInChildren<MeshRenderer>();
        lineRenderers = new List<LineRenderer>();
        corners = new List<Vector3>();
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
            meshRenderer.enabled = true;

            // Move the cursor to the point where the raycast hit.
            this.transform.position = hitInfo.point;

            // Rotate the cursor to hug the surface of the hologram.
            this.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
        }
        else
        {
            meshRenderer.enabled = false;
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
            // TODO
            this.corners.Add(this.lineRenderers[1].GetPosition(1)); // left bottom corner - second border
            this.corners.Add(this.lineRenderers[2].GetPosition(1)); // right bottom corner - third border
            LineRenderer fourthBorder = CreateNewLine(this.corners[2], this.corners[3], "fourthBorder");
            this.lineRenderers.Add(fourthBorder);
        }
    }

    private LineRenderer CreateNewLine(Vector3 start, Vector3 end, string name)
    {
        GameObject newLine = Object.Instantiate(this.lineSample);
        newLine.name = name;
        LineRenderer newLineRenderer = newLine.GetComponent<LineRenderer>();
        newLineRenderer.enabled = true;
        newLineRenderer.SetWidth(0.02f, 0.02f); 
        newLineRenderer.positionCount = 2;
        newLineRenderer.SetPosition(0, start);
        newLineRenderer.SetPosition(1, end);
        return newLineRenderer;
    }

    //GameObject lineStart;
    //GameObject lineEnd;
    //public GameObject lineSample;
    //GameObject newLine;
    //// Start is called before the first frame update
    //void Start()
    //{
    //    lineStart = this.gameObject;
    //    lineEnd = GameObject.Find("End");
    //    newLine = Object.Instantiate(lineSample);
    //    newLine.name = "newLine";
    //    newLine.GetComponent<LineRenderer>().enabled = true;
    //    newLine.GetComponent<LineRenderer>().useWorldSpace = true;
    //    newLine.GetComponent<LineRenderer>().positionCount = 3;
    //    newLine.GetComponent<LineRenderer>().SetPosition(0, lineStart.transform.position);
    //    newLine.GetComponent<LineRenderer>().SetPosition(1, lineEnd.transform.position);
    //    newLine.GetComponent<LineRenderer>().SetPosition(2, lineEnd.transform.position + Vector3.right * 5);
    //    newLine.GetComponent<LineRenderer>().SetWidth(0.5f, 0.5f);
    //}
}