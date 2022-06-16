using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WindowsMR;
using System.Runtime.InteropServices;
// using Microsoft.Windows.Perception.Spatial;
// using Microsoft.MixedReality.Toolkit.WindowsMixedReality;

#if UNITY_WSA && !UNITY_EDITOR
using global::Windows.Perception.Spatial;
using global::Windows.Media.Capture.Frames;
using global::Windows.Foundation;
using global::Windows.Graphics.Imaging;
# endif

namespace CameraFrameUtilities
{
#if UNITY_WSA && !UNITY_EDITOR
    public static class CoordinateSystemHelper
    {
        private static int count = 0;

        // from unity coordinates to frame coordinate. Note the taken argument is WinRT's definition for SpatialCoordinateSystem
        public static Point? GetFramePosition(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, Vector3 unityPosition, BitmapPlaneDescription bufferLayout)
        {
            // convert Unity Coordinates to Hololens Coordinates (left-handed to right handed)
            // Debug.Log("Unity Position - " + "X:" + unityPosition.x + ", Y:" + unityPosition.y + ", Z:" + unityPosition.z);
            System.Numerics.Vector3 HLCoordinate = NumericsConversionExtensions.ToSystem(unityPosition); // Is the hololens world origin the same as unity origin? For now we don't need rotation
#if ENABLE_WINMD_SUPPORT
            var HLWorldOrigin = Marshal.GetObjectForIUnknown(WindowsMREnvironment.OriginSpatialCoordinateSystem) as SpatialCoordinateSystem;
#endif
            System.Numerics.Matrix4x4? transformToFrame = HLWorldOrigin.TryGetTransformTo(frameCoordinateSystem);
            if (transformToFrame.HasValue){
                System.Numerics.Vector3 frameCoordinate = System.Numerics.Vector3.Transform(HLCoordinate, transformToFrame.Value);
                Point point = videoMediaFrame.CameraIntrinsics.ProjectOntoFrame(frameCoordinate);

                // the hole camera results in flipped image, so flip back!
                point.X = bufferLayout.Width - point.X;
                point.Y = bufferLayout.Height - point.Y;

                // log results
                //count += 1;
                //if (count % 40 == 0)
                //{
                //    Debug.Log("HLCoordinate: X:" + HLCoordinate.X + ", Y:" + HLCoordinate.Y + ", Z:" + HLCoordinate.Z); // this always stay the same
                //    Debug.Log("Frame Coordinates: X:" + frameCoordinate.X + ", Y:" + frameCoordinate.Y + ", Z:" + frameCoordinate.Z); // this moves according to the head position and direction
                //    Debug.Log("Pixel Coordinates: X:" + point.X + ", Y:" + point.Y);
                //    count = 0;
                //}
                return point;
            }
            else
            {
                Debug.Log("Transformation Matrix returned null!");
                return null;
            }
        }
    }

    public static class FrameProcessor
    {
        private static byte[] prev_target_frame; // managed byte array storing the previous frame
        public static unsafe void addPoints(byte* frameData, int X, int Y, BitmapPlaneDescription bufferLayout) // bgra8
        {
            for (int i = (0 > Y - 10 ? 0 : Y - 10); i < (bufferLayout.Height < Y + 10 ? bufferLayout.Height : Y + 10); i++)
            {
                for (int j = (0 > X - 10 ? 0 : X - 10); j < (bufferLayout.Width < X + 10 ? bufferLayout.Width : X + 10); j++)
                {
                    Color color = new Color(1, 0, 0, 1);
                    setPixel(frameData, i, j, color, bufferLayout);
                }
            }
        }

        // camera image masking without considering the camera projection (operate on originaly frameData)
        public static unsafe void naiveMasking(byte* camera_frame, byte* target_frame, Point?[] corners, BitmapPlaneDescription bufferLayout, int targetWidth, int targetHeight)
        {
            // get the X_Axis and Y_Axis of the region to crop
            Vector2 camera_X_Axis = new Vector2((float)(corners[1].Value.X - corners[0].Value.X), (float)(corners[1].Value.Y - corners[0].Value.Y));
            Vector2 camera_Y_Axis = new Vector2((float)(corners[2].Value.X - corners[0].Value.X), (float)(corners[2].Value.Y - corners[0].Value.Y));

            //byte[] camera_frame = new byte[bufferLayout.Stride * bufferLayout.Height];
            //Marshal.Copy((System.IntPtr) frameData, camera_frame, bufferLayout.StartIndex, bufferLayout.Stride * bufferLayout.Height); // will this work?

            // iterate through and set the pixels in the target frame (note: Y is i, X is j)
            for (int Y = 0; Y < targetHeight; Y++)
            {
                for (int X = 0; X < targetWidth; X++)
                {
                    Vector2 camera_coor = camera_X_Axis * (X / targetWidth) + camera_Y_Axis * (Y / targetHeight);
                    if (camera_coor.x < 0 || camera_coor.x > bufferLayout.Width || camera_coor.y < 0 || camera_coor.y > bufferLayout.Height) // if outside the original camera frame
                    {
                        int i = Y; int j = X;
                        setPixel(target_frame, i, j, new Color(0, 0, 0, 0), targetWidth * 4); // for now, set to no color
                    }
                    else // inside the original camera frame
                    {
                        Color target_color; // get the target color from color_bililerp, TODO

                        int i = Y; int j = X;
                        
                    }
                }
            }
        }

        // camera image masking considering the camera projection
        public static unsafe void projectionMasking()
        {
            // to do
        }

        // bililerp color
        // scale is the original vector subtracted by the integer part
        private static Color color_bililerp(Color C_LT, Color C_RT, Color C_LB, Color C_RB, Vector2 scale)
        {
            Color result_color = new Color(0, 0, 0, 1); // don't care about the alpha channel
            int channel = 0;
            while (channel < 3)
            {
                switch (channel)
                {
                    case 0:
                        result_color.r = bililerp(C_LT.r, C_RT.r, C_LB.r, C_RB.r, scale);
                        break;
                    case 1:
                        result_color.g = bililerp(C_LT.g, C_RT.g, C_LB.g, C_RB.g, scale);
                        break;
                    case 2:
                        result_color.b = bililerp(C_LT.b, C_RT.b, C_LB.b, C_RB.b, scale);
                        break;
                    default:
                        Debug.Log("Misterious channel, how did you wind up here?");
                        break;
                }
                channel++;
            }
            return result_color;
        }

        // handles the bililerp for each color channel
        private static float bililerp(float LT, float RT, float LB, float RB, Vector2 scale)
        {
            //TODO
            return 0;
        }

        // Get a bgra8 pixel from an unsafe frame array
        private unsafe static byte[] getPixel(byte* frameData, int i, int j, BitmapPlaneDescription bufferLayout) // returns a byte storing bgra
        {
            return new byte[]{frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j],
                              frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1],
                              frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2],
                              frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3]};
        }

        // Get a bgra8 pixel from a managed frame array
        private static byte[] getPixel(byte[] frameData, int i, int j, int stride)
        {
            return new byte[]{frameData[stride * i + 4 * j],
                              frameData[stride * i + 4 * j + 1],
                              frameData[stride * i + 4 * j + 2],
                              frameData[stride * i + 4 * j + 3]};
        }

        // Set a bgra8 pixel on an unsafe frame array with bufferlayout (may just zero?)
        private unsafe static void setPixel(byte* frameData, int i, int j, Color color, BitmapPlaneDescription bufferLayout)
        {
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j] = (byte)((int)color.b * 255);
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = (byte)((int)color.g * 255);
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = (byte)((int)color.r * 255);
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = (byte)((int)color.a * 255);
        }

        // Set a bgra8 pixel on an unsafe frame array without bufferlayout
        private unsafe static void setPixel(byte* frameData, int i, int j, Color color, int stride)
        {
            frameData[stride * i + 4 * j] = (byte)((int)color.b * 255);
            frameData[stride * i + 4 * j + 1] = (byte)((int)color.g * 255);
            frameData[stride * i + 4 * j + 2] = (byte)((int)color.r * 255);
            frameData[stride * i + 4 * j + 3] = (byte)((int)color.a * 255);
        }

        // Set a bgra8 pixel on a managed frame array
        private static void setPixel(byte[] framedata, int i, int j, int stride, Color color)
        {
            framedata[stride * i + 4 * j] = (byte)((int)color.b * 255);
            framedata[stride * i + 4 * j + 1] = (byte)((int)color.g * 255);
            framedata[stride * i + 4 * j + 2] = (byte)((int)color.r * 255);
            framedata[stride * i + 4 * j + 3] = (byte)((int)color.a * 255);
        }
    }
#endif
}

