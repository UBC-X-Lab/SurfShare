using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using DelaunatorSharp.Unity.Extensions;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Mirror;

public class OpenCVTest : MonoBehaviour
{
    static Material myMaterial;
    static Texture2D originalTex;
    static Texture2D contouredTex;

    public GameObject BaseMesh;

    public bool Spawn = false;

    public bool createKinematic;

    static public readonly UnityEngine.Object spawn_lock = new UnityEngine.Object();

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start");
        myMaterial = this.GetComponent<Renderer>().material;
        originalTex = (Texture2D)myMaterial.mainTexture;
    }

    // Update is called once per frame
    void Update()
    {
        if (Spawn)
        {
            test();
            Spawn = false;
        }
    }

    void test()
    {

        byte[] imageData = originalTex.GetRawTextureData();

        Mat image = new Mat(originalTex.height, originalTex.width, MatType.CV_8UC3, imageData);

        Mat greyImage = image.CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(127, 255, ThresholdTypes.Binary); // pay attention to the import setting for the texture

        Point[][] contours;
        HierarchyIndex[] hierarchy;

        greyImage.FindContours(out contours, out hierarchy, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);

        List<List<Point[]>> objects = new List<List<Point[]>>(); // stores groups of contours, each group of List<Point[]> represents an object; in each group, the first is the external contour, and the rest are holes
        
        for (int i = 0; i < hierarchy.Length; i++)
        {
            HierarchyIndex hier = hierarchy[i];
            // find the external contour that is big enough
            if (hier.Parent == -1 && Cv2.ContourArea(contours[i]) > 2500)
            {
                List<Point[]> new_object = new List<Point[]>();
                
                // contour approx
                double eps = 0.005 * Cv2.ArcLength(contours[i], true);
                new_object.Add(Cv2.ApproxPolyDP(contours[i], eps, true));

                // now find all the holes that are big enough in this object
                if (hier.Child != -1)
                {
                    // add the first hole
                    if (Cv2.ContourArea(contours[hier.Child]) > 1000)
                    {
                        // contour approx
                        eps = 0.005 * Cv2.ArcLength(contours[hier.Child], true);
                        new_object.Add(Cv2.ApproxPolyDP(contours[hier.Child], eps, true));
                    }
                    HierarchyIndex hier_hole = hierarchy[hier.Child];

                    // add the rest holes
                    while (hier_hole.Next != -1)
                    {
                        if (Cv2.ContourArea(contours[hier_hole.Next]) > 1000)
                        {
                            // contour approx
                            eps = 0.005 * Cv2.ArcLength(contours[hier_hole.Next], true);
                            new_object.Add(Cv2.ApproxPolyDP(contours[hier_hole.Next], eps, true));
                        }
                        hier_hole = hierarchy[hier_hole.Next];
                    }
                }

                // add the completed object contours to objects
                objects.Add(new_object);
            }
        }

        foreach (List<Point[]> obj in objects)
        {
            Cv2.DrawContours(image, obj, -1, new Scalar(0, 255, 0), 3); // the contours points are ordered clock-wise
        }

        contouredTex = new Texture2D(originalTex.width, originalTex.height, originalTex.format, mipChain: false);
        contouredTex.LoadRawTextureData(image.Data, imageData.Length);

        myMaterial.mainTexture = contouredTex;
        contouredTex.Apply();

        float image_width = transform.localScale.x;
        float image_height = transform.localScale.y;
        Vector3 image_origin = transform.position - new Vector3(image_width / 2, 0, image_height / 2);

        //create mesh from them
        lock (spawn_lock)
        {
            foreach (List<Point[]> obj in objects)
            {
                // calculate number of vertices
                int ver_count = 0;
                foreach (Point[] contour in obj)
                {
                    ver_count += contour.Length;
                }
                Vector2[] vertices = new Vector2[ver_count];

                // combine all the vertices in the object
                int index = 0;
                foreach (Point[] contour in obj)
                {
                    foreach (Point vertex in contour)
                    {
                        vertices[index] = new Vector2(vertex.X / (float)originalTex.width * image_width, vertex.Y / (float)originalTex.height * image_height);
                    }
                }

                // vertices by hierarchy
                List<Vector2[]> hierarchy_vertices = new List<Vector2[]>();
                foreach (Point[] contour in obj)
                {
                    Vector2[] sub_vertices = new Vector2[contour.Length];
                    for (int i = 0; i < sub_vertices.Length; i++)
                    {
                        sub_vertices[i] = new Vector2(contour[i].X / (float)originalTex.width * image_width, contour[i].Y / (float)originalTex.height * image_height);
                    }
                    hierarchy_vertices.Add(sub_vertices);
                }


                // NetworkClient.localPlayer.gameObject.GetComponent<PlayerControl>().CmdSpawnMesh(hierarchy_vertices, createKinematic);
            }

            //foreach (Point[] con in res_con)
            //{
            //    Vector2[] vertices = new Vector2[con.Length];
            //    for (int i = 0; i < vertices.Length; i++)
            //    {
            //        vertices[i] = new Vector2(con[i].X / (float)originalTex.width * image_width, con[i].Y / (float)originalTex.height * image_height);
            //    }
            //    NetworkClient.localPlayer.gameObject.GetComponent<PlayerControl>().CmdSpawnMesh(vertices, createKinematic);
            //}
        }
    }
}
