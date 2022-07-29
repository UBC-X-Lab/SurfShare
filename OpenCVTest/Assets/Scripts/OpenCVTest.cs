using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using OpenCvSharp;
#endif

public class OpenCVTest : MonoBehaviour
{
    Material myMaterial;
    Texture2D originalTex;
    Texture2D contouredTex;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start");
        myMaterial = this.GetComponent<Renderer>().material;
        originalTex = (Texture2D) myMaterial.mainTexture;
        byte[] imageData = originalTex.GetRawTextureData();
#if UNITY_WSA && !UNITY_EDITOR
        Mat image = new Mat(originalTex.height, originalTex.width, MatType.CV_8UC3, imageData);
        Mat greyImage = image.CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(127, 255, ThresholdTypes.Binary);

        Point[][] contours = greyImage.FindContoursAsArray(RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

        // filter contours that are too small
        List<Point[]> res_con = new List<Point[]>();
        for (int i = 0; i < contours.Length; i++)
        {
            if (Cv2.ContourArea(contours[i]) > 2500)
            {
                // contour approx
                double eps = 0.01 * Cv2.ArcLength(contours[i], true);
                res_con.Add(Cv2.ApproxPolyDP(contours[i], eps, true));
                Debug.Log(res_con[res_con.Count - 1].Length);
                Debug.Log(Cv2.ContourArea(contours[i]));
            }
        }
        
        Cv2.DrawContours(image, res_con, -1, new Scalar(0, 255, 0), 3);
        //byte[] resImageData = new byte[imageData.Length];

        //image.GetArray(out resImageData);
        contouredTex = new Texture2D(originalTex.width, originalTex.height, originalTex.format, mipChain: false);
        contouredTex.LoadRawTextureData(image.Data, imageData.Length);
#endif
        myMaterial.mainTexture = contouredTex;
        contouredTex.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
