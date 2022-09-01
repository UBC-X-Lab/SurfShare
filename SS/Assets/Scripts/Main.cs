using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
//using Microsoft.MixedReality.Toolkit.UI;
//using Microsoft.MixedReality.Toolkit.Input;
using Mirror;

public class Main : MonoBehaviour
{

    public static bool meshCreation = false;

    public static bool toggleExtrusion = true;

    public static bool WebRTCSetupComplete = false;

    public bool KinematicCreation = false;

    public static List<Point[]> res_con = new List<Point[]>(); // contour position on the image
    public static List<Vector3[]> res_con_world = new List<Vector3[]>(); // contour position in the world
    public static List<Color> mesh_colors = new List<Color>();
    public static readonly object res_con_lock = new object();
    
    public GameObject RemoteObject;
    // public GameObject RemoteVideoPlayer;

    //public bool bgs = true;
    static public bool bgs_on = true;

    //private int frame_width = 640;
    //private int frame_height = 360;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // bgs_on = bgs;
        // process mesh creation
        lock (res_con_lock)
        {
            if (res_con.Count > 0 && FrameHandler.corners.Count == 4)
            {
                //Vector3 X_Axis = FrameHandler.corners[1] - FrameHandler.corners[0];
                //Vector3 Y_Axis = FrameHandler.corners[2] - FrameHandler.corners[0];
                //Vector3 heightNormal = Vector3.Normalize(Vector3.Cross(X_Axis, Y_Axis));

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

                    Vector3 X_Axis = FrameHandler.corners[1] - FrameHandler.corners[0];
                    Vector3 Y_Axis = FrameHandler.corners[2] - FrameHandler.corners[0];
                    Vector3 heightNormal = Vector3.Normalize(Vector3.Cross(X_Axis, Y_Axis));

                    NetworkClient.localPlayer.gameObject.GetComponent<PlayerController>().SpawnMesh(vertices, res_con_world[i], heightNormal, mesh_colors[i], KinematicCreation);
                }
                res_con.Clear();
                res_con_world.Clear();
                mesh_colors.Clear();
                Debug.Log("Meshes Created!");
            }
        }
    }

    public void OnMeshCreation()
    {
        if (RemoteSpaceControl.STATE == RemoteSpaceControl.PLACE_COMPLETE)
        {
            meshCreation = true;
            KinematicCreation = false;
        }
    }

    public void OnKinematicCreation()
    {
        if (RemoteSpaceControl.STATE == RemoteSpaceControl.PLACE_COMPLETE)
        {
            meshCreation = true;
            KinematicCreation = true;
        }
    }

    public void OnExtrusionToggle()
    {
        toggleExtrusion = !toggleExtrusion;
        Debug.Log("Extrusion mode:" + toggleExtrusion);
    }

    public void OnToggleBackgroundSubtraction()
    {
        if (bgs_on)
        {
            Debug.Log("BGS OFF");
            bgs_on = false;
            // RemoteVideoPlayer.GetComponent<Renderer>().material = RemoteVideoPlayer.GetComponent<CustomVideoRenderer>().nonBSVideoMaterial;
        }
        else
        {
            Debug.Log("BGS ON");
            bgs_on = true;
            // RemoteVideoPlayer.GetComponent<Renderer>().material = RemoteVideoPlayer.GetComponent<CustomVideoRenderer>().videoMaterial;
        }
    }
}

