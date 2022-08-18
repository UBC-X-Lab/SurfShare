﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using DelaunatorSharp.Unity.Extensions;

public class OpenCVTest : MonoBehaviour
{
    static Material myMaterial;
    static Texture2D originalTex;
    static Texture2D contouredTex;

    

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start");
        myMaterial = this.GetComponent<Renderer>().material;
        originalTex = (Texture2D)myMaterial.mainTexture;
        test();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void test()
    {

        byte[] imageData = originalTex.GetRawTextureData();

        //Debug.Log(1);
        Mat image = new Mat(originalTex.height, originalTex.width, MatType.CV_8UC3, imageData);
        //Debug.Log(2);
        Mat greyImage = image.CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(127, 255, ThresholdTypes.Binary); // pay attention to the import setting for the texture
        //Debug.Log(3);
        Point[][] contours = greyImage.FindContoursAsArray(RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
        //Debug.Log(4);
        // filter contours that are too small
        List<Point[]> res_con = new List<Point[]>();
        for (int i = 0; i < contours.Length; i++)
        {
            if (Cv2.ContourArea(contours[i]) > 2500)
            {
                // contour approx
                double eps = 0.01 * Cv2.ArcLength(contours[i], true);
                res_con.Add(Cv2.ApproxPolyDP(contours[i], eps, true));
                //Debug.Log(res_con[res_con.Count - 1].Length);
                //Debug.Log(Cv2.ContourArea(contours[i]));
            }
        }

        Cv2.DrawContours(image, res_con, -1, new Scalar(0, 255, 0), 3); // the contours points are ordered clock-wise
        //foreach (Point point in res_con[0])
        //{
        //    Debug.Log(point.X + "," + point.Y);
        //}

        //byte[] resImageData = new byte[imageData.Length];

        //image.GetArray(out resImageData);
        contouredTex = new Texture2D(originalTex.width, originalTex.height, originalTex.format, mipChain: false);
        contouredTex.LoadRawTextureData(image.Data, imageData.Length);

        myMaterial.mainTexture = contouredTex;
        contouredTex.Apply();

        float image_width = transform.localScale.x;
        float image_height = transform.localScale.y;
        Vector3 image_origin = transform.position - new Vector3(image_width / 2, 0, image_height / 2);

        //create mesh from them
        foreach (Point[] con in res_con)
        {
            Vector2[] vertices = new Vector2[con.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector2(con[i].X / (float) originalTex.width * image_width, con[i].Y / (float) originalTex.height * image_height);
            }
            GameObject obj = Instantiate(GameObject.Find("Object"));
            obj.transform.position = image_origin;
            obj.GetComponent<UpdateMesh>().enabled = true;
            obj.GetComponent<MeshFilter>().mesh = MeshCreator.CreateMesh(vertices);
        }
    }
}
