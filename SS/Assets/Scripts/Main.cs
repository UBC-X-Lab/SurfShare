using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

public class Main : MonoBehaviour
{
    public static bool meshCreation = false;
    public static bool toggleExtrusion = true;

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
                for (int i = 0; i < res_con.Count; i++)
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
                    Mesh newMesh = MeshCreator.CreateMesh(vertices, res_con_world[i], heightNormal);
                    obj.GetComponent<MeshFilter>().mesh = newMesh;
                    // obj.GetComponent<MeshRenderer>().enabled = true;
                    // obj.AddComponent<MeshCollider>();
                    // obj.GetComponent<MeshCollider>().convex = true;
                    obj.GetComponent<MeshCollider>().sharedMesh = newMesh;
                    // obj.AddComponent<ObjectManipulator>();
                    // obj.AddComponent<NearInteractionGrabbable>();

                    // initiate extrude handle
                    GameObject extrusionHandle = obj.transform.GetChild(0).gameObject;
                    // extrusionHandle.GetComponent<MeshRenderer>().enabled = true;
                    // extrusionHandle.GetComponent<SphereCollider>().enabled = true;
                    // extrusionHandle.AddComponent<ObjectManipulator>();
                    // extrusionHandle.AddComponent<NearInteractionGrabbable>();
                    extrusionHandle.GetComponent<ExtrusionController>().myMesh = newMesh;
                    // extrusionHandle.GetComponent<ExtrusionController>().enabled = true;

                    // get top vertices indices (after optimization)
                    for (int j = 0; j < newMesh.vertexCount; j++)
                    {
                        Vector3 cur_vertex = newMesh.vertices[j];
                        bool isTop = true;
                        for (int k = 0; k < res_con_world[i].Length; k++)
                        {
                            if ((res_con_world[i][k] + 0.015f * Vector3.right).Equals(cur_vertex))
                            {
                                isTop = false;
                                break;
                            }
                        }
                        if (isTop)
                        {
                            extrusionHandle.GetComponent<ExtrusionController>().topVerticesIndices.Add(j);
                            extrusionHandle.transform.localPosition += cur_vertex;
                        }
                    }
                    Debug.Log("Number of top:" + extrusionHandle.GetComponent<ExtrusionController>().topVerticesIndices.Count);
                    Debug.Log("Number of vertices:" + newMesh.vertexCount);
                    extrusionHandle.transform.localPosition /= extrusionHandle.GetComponent<ExtrusionController>().topVerticesIndices.Count;
                    extrusionHandle.transform.localPosition += 0.05f * heightNormal;
                    extrusionHandle.GetComponent<ExtrusionController>().previousPosition = extrusionHandle.transform.localPosition;
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

    public void OnExtrusionToggle()
    {
        toggleExtrusion = !toggleExtrusion;
        Debug.Log("Extrusion mode:" + toggleExtrusion);
    }
}

