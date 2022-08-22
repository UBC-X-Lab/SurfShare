using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

public class Main : MonoBehaviour
{
    public static bool meshCreation = false;
    public static List<Point[]> res_con = new List<Point[]>(); // contour position on the image
    public static List<Vector3[]> res_con_world = new List<Vector3[]>(); // contour position in the world
    public static readonly object res_con_lock = new object();
    public GameObject BaseMesh;

    //private int frame_width = 640;
    //private int frame_height = 360;

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
                Vector3 X_Axis = FrameHandler.corners[1] - FrameHandler.corners[0];
                Vector3 Y_Axis = FrameHandler.corners[2] - FrameHandler.corners[0];
                Vector3 heightNormal = Vector3.Normalize(Vector3.Cross(X_Axis, Y_Axis));

                //create mesh from them
                for (int i = 0; i <  res_con.Count; i++)
                {
                    Point[] con = res_con[i];
                    Vector2[] vertices = new Vector2[con.Length];
                    for (int j = 0; j < vertices.Length; j++)
                    {
                        Debug.Log("X:" + con[j].X + ", Y:" + con[j].Y);
                        vertices[j] = new Vector2(con[j].X, con[j].Y);
                    }
                    GameObject obj = Instantiate(BaseMesh);
                    //obj.transform.position = FrameHandler.corners[2];
                    obj.GetComponent<MeshFilter>().mesh = MeshCreator.CreateMesh(vertices, res_con_world[i], heightNormal);
                    obj.GetComponent<MeshRenderer>().enabled = true;
                }
                res_con.Clear();
                res_con_world.Clear();
                Debug.Log("Mesh Created!");
            }
        }
    }

    public void OnMeshCreation()
    {
        meshCreation = true;
    }
}

