using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

public class Main : MonoBehaviour
{
    public static bool meshCreation = false;
    public static List<Point[]> res_con = new List<Point[]>();
    public static readonly object res_con_lock = new object();
    public GameObject BaseMesh;

    private int targetWidth = 640;
    private int targetHeight = 360;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // process mesh creation
        lock (res_con_lock)
        {
            if (res_con.Count > 0 && FrameHandler.corners.Count == 4)
            {
                Vector3 X_Axis = FrameHandler.corners[3] - FrameHandler.corners[2];
                Vector3 Y_Axis = FrameHandler.corners[0] - FrameHandler.corners[2]; // image origin bottom left corner?
                //create mesh from them
                foreach (Point[] con in res_con)
                {
                    Vector2[] vertices = new Vector2[con.Length];
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = new Vector2(con[i].X / (float)targetWidth * X_Axis.magnitude, con[i].Y / (float)targetHeight * Y_Axis.magnitude);
                    }
                    GameObject obj = Instantiate(BaseMesh);
                    obj.transform.position = FrameHandler.corners[2];
                    obj.GetComponent<MeshFilter>().mesh = MeshCreator.CreateMesh(vertices);
                    obj.GetComponent<MeshRenderer>().enabled = true;
                }
                res_con.Clear();
                Debug.Log("Mesh Created!");
            }
        }
    }

    public void OnMeshCreation()
    {
        meshCreation = true;
    }
}

