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
        public static Point? GetFramePosition(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, Vector3 unityPosition, int cameraFrameWidth, int cameraFrameHeight)
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
                point.X = cameraFrameWidth - point.X;
                point.Y = cameraFrameHeight - point.Y;

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

        // project many to frame
        public static Point[] GetManyFramePosition(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, Vector3[] unityPositions, int cameraFrameWidth, int cameraFrameHeight)
        {
            System.Numerics.Vector3[] HLCoordinates = new System.Numerics.Vector3[unityPositions.Length];
            for (int i = 0; i < unityPositions.Length; i++)
            {
                HLCoordinates[i] = NumericsConversionExtensions.ToSystem(unityPositions[i]);
            }
#if ENABLE_WINMD_SUPPORT
            var HLWorldOrigin = Marshal.GetObjectForIUnknown(WindowsMREnvironment.OriginSpatialCoordinateSystem) as SpatialCoordinateSystem;
#endif
            System.Numerics.Matrix4x4? transformToFrame = HLWorldOrigin.TryGetTransformTo(frameCoordinateSystem);
            if (transformToFrame.HasValue)
            {
                System.Numerics.Vector3[] frameCoordinates = new System.Numerics.Vector3[HLCoordinates.Length];
                for (int i = 0; i < HLCoordinates.Length; i++)
                {
                    frameCoordinates[i] = System.Numerics.Vector3.Transform(HLCoordinates[i], transformToFrame.Value);
                }
                Point[] points = new Point[HLCoordinates.Length];
                videoMediaFrame.CameraIntrinsics.ProjectManyOntoFrame(frameCoordinates, points);

                // flip back
                for (int i = 0; i < points.Length; i++)
                {
                    points[i].X = cameraFrameWidth - points[i].X;
                    points[i].Y = cameraFrameHeight - points[i].Y;
                }
                return points;
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
                    byte[] color = new byte[] { 0, 0, 255, 255 }; // bgra
                    setPixel(frameData, i, j, color, bufferLayout);
                }
            }
        }

        // camera image masking without considering the camera projection (operate on originaly frameData)
        public static unsafe void naiveMasking(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, byte* camera_frame, byte* target_frame, BitmapPlaneDescription bufferLayout, int targetWidth, int targetHeight)
        {
            if (FrameHandler.corners.Count < 4)
            {
                Debug.Log("This function should only be called after the frame corners are defined!");
                return;
            }

            // get the corner coordinates on the camera frame
            Point?[] corners = { null, null, null, null };
            for (int corner_index = 0; corner_index < FrameHandler.corners.Count; corner_index++)
            {
                Vector3 corner = FrameHandler.corners[corner_index];
                Point? corner_on_frame = CoordinateSystemHelper.GetFramePosition(frameCoordinateSystem, videoMediaFrame, corner, bufferLayout.Width, bufferLayout.Height);
                if (corner_on_frame.HasValue)
                {
                    // FrameProcessor.addPoints(dataInBytes, (int) corner_on_frame.Value.X, (int) corner_on_frame.Value.Y, bufferLayout);
                    corners[corner_index] = corner_on_frame;
                }
            }

            // get the X_Axis and Y_Axis of the region to crop
            Vector2 camera_origin = Point2Vector2(corners[0]);
            Vector2 camera_X_Axis = Point2Vector2(corners[1]) - Point2Vector2(corners[0]);
            Vector2 camera_Y_Axis = Point2Vector2(corners[2]) - Point2Vector2(corners[0]);

            //byte[] camera_frame = new byte[bufferLayout.Stride * bufferLayout.Height];
            //Marshal.Copy((System.IntPtr) frameData, camera_frame, bufferLayout.StartIndex, bufferLayout.Stride * bufferLayout.Height); // will this work?

            // iterate through and set the pixels in the target frame (note: Y is i, X is j)
            for (int Y = 0; Y < targetHeight; Y++)
            {
                for (int X = 0; X < targetWidth; X++)
                {
                    Vector2 camera_coor = camera_X_Axis * (X / targetWidth) + camera_Y_Axis * (Y / targetHeight) + camera_origin;
                    if (camera_coor.x < 0 || camera_coor.x > bufferLayout.Width || camera_coor.y < 0 || camera_coor.y > bufferLayout.Height) // if outside the original camera frame
                    {
                        int i = Y; int j = X;
                        byte[] prev_pixel = getPixel(prev_target_frame, i, j, targetWidth * 4);
                        setPixel(target_frame, i, j, prev_pixel, targetWidth * 4);
                    }
                    else // inside the original camera frame
                    {
                        Vector2[] neighbors = new Vector2[] { new Vector2(Mathf.Floor(camera_coor.x), Mathf.Floor(camera_coor.y)),
                                                              new Vector2(Mathf.Ceil(camera_coor.x), Mathf.Floor(camera_coor.y)),
                                                              new Vector2(Mathf.Floor(camera_coor.x), Mathf.Ceil(camera_coor.y)),
                                                              new Vector2(Mathf.Ceil(camera_coor.x), Mathf.Ceil(camera_coor.y))};
                        List<byte[]> neighbor_colors = new List<byte[]>();
                        for (int color_index = 0; color_index < 4; color_index++)
                        {
                            byte[] pixel_color = getPixel(camera_frame, (int) neighbors[color_index].y, (int) neighbors[color_index].x, bufferLayout);
                            neighbor_colors.Add(pixel_color);
                        }
                        Vector2 scale = new Vector2(camera_coor.x - Mathf.Floor(camera_coor.x), camera_coor.y - Mathf.Floor(camera_coor.y));
                        byte[] target_color = color_bililerp(neighbor_colors[0], neighbor_colors[1], neighbor_colors[2], neighbor_colors[3], scale); // get the target color from color_bililerp, TODO
                        
                        // set the pixel onto the target frame
                        int i = Y; int j = X;
                        setPixel(target_frame, i, j, target_color, targetWidth * 4);
                    }
                }
            }

            // copy the target frame to the previous frame
            prev_target_frame = new byte[targetWidth * targetHeight * 4];
            Marshal.Copy((System.IntPtr)target_frame, prev_target_frame, 0, prev_target_frame.Length);
        }

        // camera image masking considering the camera projection
        public static unsafe void projectionMasking(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, byte* camera_frame, byte* target_frame, BitmapPlaneDescription bufferLayout, int targetWidth, int targetHeight)
        {
            if (FrameHandler.corners.Count < 4)
            {
                Debug.Log("This function should only be called after the frame corners are defined!");
                return;
            }

            // get the X_Axis and Y_Axis of the region to crop in the world
            Vector3 world_origin = FrameHandler.corners[0];
            Vector3 world_X_Axis = FrameHandler.corners[1] - FrameHandler.corners[0];
            Vector3 world_Y_Axis = FrameHandler.corners[2] - FrameHandler.corners[0];

            // get the projected camera coordinates
            List<Point[]> camera_coors = new List<Point[]>();
            for (int Y = 0; Y < targetHeight; Y++)
            {
                Vector3[] current_world_row = new Vector3[targetWidth]; // the current row of world coordinates
                for (int X = 0; X < targetWidth; X++)
                {
                    current_world_row[X] = world_X_Axis * (X / targetWidth) + world_Y_Axis * (Y / targetHeight) + world_origin;
                }

                // get the current row of projected camera coordinates
                Point[] current_camera_row = CoordinateSystemHelper.GetManyFramePosition(frameCoordinateSystem, videoMediaFrame, current_world_row, bufferLayout.Width, bufferLayout.Height);
                camera_coors.Add(current_camera_row);
            }

            // iterate through and set the pixels in the target frame (note: Y is i, X is j)
            for (int Y = 0; Y < targetHeight; Y++)
            {
                for (int X = 0; X < targetWidth; X++)
                {
                    Vector2 camera_coor = Point2Vector2(camera_coors[Y][X]);
                    if (camera_coor.x < 0 || camera_coor.x > bufferLayout.Width || camera_coor.y < 0 || camera_coor.y > bufferLayout.Height) // if outside the original camera frame
                    {
                        int i = Y; int j = X;
                        byte[] prev_pixel = getPixel(prev_target_frame, i, j, targetWidth * 4);
                        setPixel(target_frame, i, j, prev_pixel, targetWidth * 4);
                    }
                    else // inside the original camera frame
                    {
                        Vector2[] neighbors = new Vector2[] { new Vector2(Mathf.Floor(camera_coor.x), Mathf.Floor(camera_coor.y)),
                                                              new Vector2(Mathf.Ceil(camera_coor.x), Mathf.Floor(camera_coor.y)),
                                                              new Vector2(Mathf.Floor(camera_coor.x), Mathf.Ceil(camera_coor.y)),
                                                              new Vector2(Mathf.Ceil(camera_coor.x), Mathf.Ceil(camera_coor.y))};
                        List<byte[]> neighbor_colors = new List<byte[]>();
                        for (int color_index = 0; color_index < 4; color_index++)
                        {
                            byte[] pixel_color = getPixel(camera_frame, (int)neighbors[color_index].y, (int)neighbors[color_index].x, bufferLayout);
                            neighbor_colors.Add(pixel_color);
                        }
                        Vector2 scale = new Vector2(camera_coor.x - Mathf.Floor(camera_coor.x), camera_coor.y - Mathf.Floor(camera_coor.y));
                        byte[] target_color = color_bililerp(neighbor_colors[0], neighbor_colors[1], neighbor_colors[2], neighbor_colors[3], scale); // get the target color from color_bililerp, TODO

                        // set the pixel onto the target frame
                        int i = Y; int j = X;
                        setPixel(target_frame, i, j, target_color, targetWidth * 4);
                    }
                }
            }

            // copy the target frame to the previous frame
            prev_target_frame = new byte[targetWidth * targetHeight * 4];
            Marshal.Copy((System.IntPtr)target_frame, prev_target_frame, 0, prev_target_frame.Length);
        }

        // bililerp color
        // scale is the original vector subtracted by the integer part
        private static byte[] color_bililerp(byte[] C_LT, byte[] C_RT, byte[] C_LB, byte[] C_RB, Vector2 scale)
        {
            byte[] result_color = new byte[] { 0, 0, 0, 255 }; // don't care about the alpha channel
            int channel = 0;
            while (channel < 3)
            {
                switch (channel)
                {
                    case 0:
                        result_color[0] = bililerp(C_LT[0], C_RT[0], C_LB[0], C_RB[0], scale);
                        break;
                    case 1:
                        result_color[1] = bililerp(C_LT[1], C_RT[1], C_LB[1], C_RB[1], scale);
                        break;
                    case 2:
                        result_color[2] = bililerp(C_LT[2], C_RT[2], C_LB[2], C_RB[2], scale);
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
        private static byte bililerp(byte LT, byte RT, byte LB, byte RB, Vector2 scale)
        {
            // lerp horizontally
            float top_row = LT * (1 - scale.x) + RT * scale.x;
            float bottom_row = LB * (1 - scale.x) + RB * scale.x;

            // lerp vertically
            float result = top_row * (1 - scale.y) + bottom_row * scale.y;
            return (byte) result;
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
        private unsafe static void setPixel(byte* frameData, int i, int j, byte[] color, BitmapPlaneDescription bufferLayout)
        {
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j] = color[0];
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = color[1];
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = color[2];
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = color[3];
        }

        // Set a bgra8 pixel on an unsafe frame array without bufferlayout
        private unsafe static void setPixel(byte* frameData, int i, int j, byte[] color, int stride)
        {
            frameData[stride * i + 4 * j] = color[0];
            frameData[stride * i + 4 * j + 1] = color[1];
            frameData[stride * i + 4 * j + 2] = color[2];
            frameData[stride * i + 4 * j + 3] = color[3];
        }

        // Set a bgra8 pixel on a managed frame array
        private static void setPixel(byte[] framedata, int i, int j, int stride, byte[] color)
        {
            framedata[stride * i + 4 * j] = color[0];
            framedata[stride * i + 4 * j + 1] = color[1];
            framedata[stride * i + 4 * j + 2] = color[2];
            framedata[stride * i + 4 * j + 3] = color[3];
        }

        // pointer to vector2
        private static Vector2 Point2Vector2(Point point)
        {
            return new Vector2((float)point.X, (float)point.Y);
        }

        // pointer to vector2
        private static Vector2 Point2Vector2(Point? point)
        {
            return new Vector2((float)point.Value.X, (float)point.Value.Y);
        }
    }
#endif
}

