using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Microsoft.Windows.Perception.Spatial;
using Microsoft.MixedReality.Toolkit.WindowsMixedReality;

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

        // from unity coordinates to frame coordinate. Note the taken argument is WinRT's definition for SpatialCoordinateSystem
        public static Point? GetFramePosition(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, Vector3 unityPosition)
        {
            // convert Unity Coordinates to Hololens Coordinates (left-handed to right handed)
            Debug.Log("Unity Position - " + "X:" + unityPosition.x + ", Y:" + unityPosition.y + ", Z:" + unityPosition.z);
            System.Numerics.Vector3 HLCoordinate = NumericsConversionExtensions.ToSystem(unityPosition); // Is the hololens world origin the same as unity origin? For now we don't need rotation
            System.Numerics.Matrix4x4? transformToFrame = WindowsMixedRealityUtilities.SpatialCoordinateSystem.TryGetTransformTo(frameCoordinateSystem);
            if (transformToFrame.HasValue){
                System.Numerics.Vector3 frameCoordinate = System.Numerics.Vector3.Transform(HLCoordinate, transformToFrame.Value);
                Debug.Log("Frame Coordinates: X:" + frameCoordinate.X + ", Y:" + frameCoordinate.Y + ", Z:" + frameCoordinate.Z);
                Point point = videoMediaFrame.CameraIntrinsics.ProjectOntoFrame(frameCoordinate);
                Debug.Log("Pixel Coordinates: X:" + point.X + ", Y:" + point.Y);
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
        public static unsafe void addPoints(byte* FrameData, int X, int Y, BitmapPlaneDescription bufferLayout)
        {
            for (int i = (0 > Y - 25 ? 0 : Y - 25); i < (bufferLayout.Height < Y + 25 ? bufferLayout.Height : Y + 25); i++)
            {
                for (int j = (0 > X - 25 ? 0 : X - 25); j < (bufferLayout.Width < X + 25 ? bufferLayout.Width : X + 25); j++)
                {
                    FrameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = 255;
                    FrameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = 0;
                    FrameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = 0;
                    FrameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = 255;
                }
            }
        }

        // Fill-in the BGRA plane
        //BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0); // use this as a backup?
        //for (int i = 0; i < bufferLayout.Height; i++)
        //{
        //    for (int j = 0; j < bufferLayout.Width; j++)
        //    {

        //        byte value = (byte)((float)j / bufferLayout.Width * 255);
        //        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = value;
        //        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = value;
        //        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = value;
        //        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = (byte)255;
        //    }
        //}
    }
#endif
}

