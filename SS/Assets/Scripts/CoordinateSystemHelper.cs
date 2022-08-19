using System;
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
            System.Numerics.Vector3 HLCoordinate = NumericsConversionExtensions.ToSystem(unityPosition); // In seated, standing, and room scale the Unity origin and HL origin are the same
#if ENABLE_WINMD_SUPPORT
            var HLWorldOrigin = Marshal.GetObjectForIUnknown(WindowsMREnvironment.OriginSpatialCoordinateSystem) as SpatialCoordinateSystem;
#endif
            System.Numerics.Matrix4x4? transformToFrame = HLWorldOrigin.TryGetTransformTo(frameCoordinateSystem);
            if (transformToFrame.HasValue){
                System.Numerics.Vector3 frameCoordinate = System.Numerics.Vector3.Transform(HLCoordinate, transformToFrame.Value);
                Point point = videoMediaFrame.CameraIntrinsics.ProjectOntoFrame(frameCoordinate); // add reference here in case of error message

                // the hole camera results in flipped image, so flip back!
                point.X = cameraFrameWidth - point.X;
                point.Y = cameraFrameHeight - point.Y;

                return point;
            }
            else
            {
                Debug.Log("Transformation Matrix returned null!");
                return null;
            }
        }

        // project many to frame
//        public static void GetManyFramePosition(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, System.Numerics.Vector3[] HLCoordinates, 
//            System.Numerics.Vector3[] frameCoordinates, Point[] target_points, int cameraFrameWidth, int cameraFrameHeight)
//        {
//#if ENABLE_WINMD_SUPPORT
//            var HLWorldOrigin = Marshal.GetObjectForIUnknown(WindowsMREnvironment.OriginSpatialCoordinateSystem) as SpatialCoordinateSystem;
//#endif
//            System.Numerics.Matrix4x4? transformToFrame = HLWorldOrigin.TryGetTransformTo(frameCoordinateSystem);
//            if (transformToFrame.HasValue)
//            {
//                for (int i = 0; i < HLCoordinates.Length; i++)
//                {
//                    frameCoordinates[i] = System.Numerics.Vector3.Transform(HLCoordinates[i], transformToFrame.Value);
//                }
//                videoMediaFrame.CameraIntrinsics.ProjectManyOntoFrame(frameCoordinates, target_points);

//                // flip back
//                for (int i = 0; i < target_points.Length; i++)
//                {
//                    target_points[i].X = cameraFrameWidth - target_points[i].X;
//                    target_points[i].Y = cameraFrameHeight - target_points[i].Y;
//                }
//            }
//            else
//            {
//                Debug.Log("Transformation Matrix returned null!");
//            }
//        }
    }

    public unsafe static class FrameProcessor
    {
        public static byte* target_frame;
        public static int targetWidth = 480;
        public static int targetHeight = 270;
        private static byte[] first_frame;
        private static bool is_first_frame = true;
        // private static bool enable_bg_subtraction = false;

        // masking with projection utlities
        public static List<System.Numerics.Vector3[]> world_coors = new List<System.Numerics.Vector3[]>();
        public static List<Point[]> camera_coors = new List<Point[]>();
        public static List<System.Numerics.Vector3[]> frame_coors = new List<System.Numerics.Vector3[]>();
        public static List<Point[]> target_points = new List<Point[]>();

        private static bool world_system_defined = false;
        private static System.Numerics.Vector3 world_origin; // those are constants once defined;
        private static System.Numerics.Vector3 world_X_Axis;
        private static System.Numerics.Vector3 world_Y_Axis;

        public static unsafe void AddPoints(byte* frameData, int X, int Y, BitmapPlaneDescription bufferLayout) // bgra8
        {
            for (int i = (0 > Y - 10 ? 0 : Y - 10); i < (bufferLayout.Height < Y + 10 ? bufferLayout.Height : Y + 10); i++)
            {
                for (int j = (0 > X - 10 ? 0 : X - 10); j < (bufferLayout.Width < X + 10 ? bufferLayout.Width : X + 10); j++)
                {
                    byte* color = stackalloc byte[] { 0, 0, 255, 255 }; // bgra
                    setPixel(frameData, i, j, color, bufferLayout);
                }
            }
        }

        // camera image masking without considering the camera projection (operate on originaly frameData)
        public static void NaiveMasking(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, byte* camera_frame, int camera_start_index, int camera_width, int camera_height)
        {
            if (FrameHandler.corners.Count < 4)
            {
                Debug.Log("This function should only be called after the frame corners are defined!");
                return;
            }

            // get the corner coordinates on the camera frame
            Point* corners = stackalloc Point[3]; // we only need the first 3 corners to calculate the axes
            for (int corner_index = 0; corner_index < 3; corner_index++)
            {
                Vector3 corner = FrameHandler.corners[corner_index];
                Point? corner_on_frame = CoordinateSystemHelper.GetFramePosition(frameCoordinateSystem, videoMediaFrame, corner, camera_width, camera_height);
                if (corner_on_frame.HasValue)
                {
                    // FrameProcessor.addPoints(dataInBytes, (int) corner_on_frame.Value.X, (int) corner_on_frame.Value.Y, bufferLayout);
                    corners[corner_index] = corner_on_frame.Value;
                }
                else
                {
                    Debug.Log("Frame corner projection failed!");
                }
            }

            Point camera_origin = corners[0];
            Point camera_X_Axis = corners[1];
            camera_X_Axis.X -= camera_origin.X; camera_X_Axis.Y -= camera_origin.Y;
            camera_X_Axis.X /= targetWidth; camera_X_Axis.Y /= targetWidth; // pre-copmute the multiplier
            Point camera_Y_Axis = corners[2];
            camera_Y_Axis.X -= camera_origin.X; camera_Y_Axis.Y -= camera_origin.Y;
            camera_Y_Axis.X /= targetHeight; camera_Y_Axis.Y /= targetHeight; // pre-compute the multiplier


            //---- Avoid heap allocation and function calls
            //---- Use native functions instead of Unity functions as much as possible

            // initialize utilities at a larger scope
            Point camera_coor = new Point();
            Point* neighbors = stackalloc Point[4];
            byte* neighbor_colors = stackalloc byte[16]; // 4 neighbors * 4 channels

            // iterate through and set the pixels in the target frame (note: Y is i, X is j)
            for (int Y = 0; Y < targetHeight; Y++)
            {
                for (int X = 0; X < targetWidth; X++)
                {
                    // initialize camera coordinates
                    camera_coor.X = camera_X_Axis.X * X + camera_Y_Axis.X * Y + camera_origin.X;
                    camera_coor.Y = camera_X_Axis.Y * X + camera_Y_Axis.Y * Y + camera_origin.Y;

                    if (camera_coor.X < 0 || camera_coor.X > camera_width - 1 || camera_coor.Y < 0 || camera_coor.Y > camera_height - 1) // if outside the original camera frame
                    {
                        // since we are overwriting the same frame, just skip
                        // Debug.Log("out of FOV!");
                        continue;
                    }
                    else // inside the original camera frame
                    {
                        // Debug.Log("Inside of FOV!");
                        neighbors[0].X = Math.Floor(camera_coor.X); neighbors[0].Y = Math.Floor(camera_coor.Y);
                        neighbors[1].X = Math.Ceiling(camera_coor.X); neighbors[1].Y = Math.Floor(camera_coor.Y);
                        neighbors[2].X = Math.Floor(camera_coor.X); neighbors[2].Y = Math.Ceiling(camera_coor.Y);
                        neighbors[3].X = Math.Ceiling(camera_coor.X); neighbors[3].Y = Math.Ceiling(camera_coor.Y);

                        for (int color_index = 0; color_index < 4; color_index++)
                        {
                            byte* pixel_color = camera_frame + (camera_start_index + camera_width * 4 * (int)neighbors[color_index].Y + 4 * (int)neighbors[color_index].X);
                            neighbor_colors[color_index * 4] = pixel_color[0];
                            neighbor_colors[color_index * 4 + 1] = pixel_color[1];
                            neighbor_colors[color_index * 4 + 2] = pixel_color[2];
                            neighbor_colors[color_index * 4 + 3] = pixel_color[3];
                        }
                        double x_scale = camera_coor.X - neighbors[0].X; double y_scale = camera_coor.Y - neighbors[0].Y;
                        byte* target_color = Color_bililerp(neighbor_colors, (float)x_scale, (float)y_scale); // get the target color from color_bililerp, TODO

                        // set the pixel onto the target frame
                        int i = Y; int j = X;
                        target_frame[targetWidth * 4 * i + 4 * j] = target_color[0];
                        target_frame[targetWidth * 4 * i + 4 * j + 1] = target_color[1];
                        target_frame[targetWidth * 4 * i + 4 * j + 2] = target_color[2];
                        target_frame[targetWidth * 4 * i + 4 * j + 3] = target_color[3];
                        // setPixel(target_frame, i, j, target_color, targetWidth * 4);
                    }
                }
            }
        }

        // camera image masking considering the camera projection
        public static void ProjectionMasking(SpatialCoordinateSystem frameCoordinateSystem, VideoMediaFrame videoMediaFrame, byte* camera_frame, int camera_start_index, int camera_width, int camera_height)
        {
            if (FrameHandler.corners.Count < 4)
            {
                Debug.Log("This function should only be called after the frame corners are defined!");
                return;
            }

            // get the X_Axis and Y_Axis of the region to crop in the world (constant, only initialize once)
            if (!world_system_defined)
            {
                // we directly use the HL system
                world_origin = NumericsConversionExtensions.ToSystem(FrameHandler.corners[0]);
                world_X_Axis = (NumericsConversionExtensions.ToSystem(FrameHandler.corners[1]) - world_origin) / targetWidth;
                world_Y_Axis = (NumericsConversionExtensions.ToSystem(FrameHandler.corners[2]) - world_origin) / targetHeight;

                // all world coordinates are constants, fill in
                for (int Y = 0; Y < targetHeight; Y++)
                {
                    for (int X = 0; X < targetWidth; X++)
                    {
                        world_coors[Y][X] = world_X_Axis * X + world_Y_Axis * Y + world_origin;
                    }
                }
                world_system_defined = true;
            }

#if ENABLE_WINMD_SUPPORT
            // this could also be hoisted to world system defined
            var HLWorldOrigin = Marshal.GetObjectForIUnknown(WindowsMREnvironment.OriginSpatialCoordinateSystem) as SpatialCoordinateSystem;
#endif
            System.Numerics.Matrix4x4? transformToFrame = HLWorldOrigin.TryGetTransformTo(frameCoordinateSystem);
            if (!transformToFrame.HasValue)
            {
                Debug.Log("Transformation Matrix returned null!");
                return;
            }

            // get the projected camera coordinates
            for (int Y = 0; Y < targetHeight; Y++)
            {
                // get the current row of projected camera coordinates, in place modifying target_points[Y]
                // CoordinateSystemHelper.GetManyFramePosition(frameCoordinateSystem, videoMediaFrame, world_coors[Y], frame_coors[Y], target_points[Y], camera_width, camera_height);
                for (int X = 0; X < targetWidth; X++)
                {
                    frame_coors[Y][X] = System.Numerics.Vector3.Transform(world_coors[Y][X], transformToFrame.Value);
                }
                videoMediaFrame.CameraIntrinsics.ProjectManyOntoFrame(frame_coors[Y], target_points[Y]);

                // flip back
                for (int X = 0; X < targetWidth; X++)
                {
                    target_points[Y][X].X = camera_width - target_points[Y][X].X;
                    target_points[Y][X].Y = camera_height - target_points[Y][X].Y;
                }
            }

            Point* neighbors = stackalloc Point[4];
            byte* neighbor_colors = stackalloc byte[16]; // 4 neighbors * 4 channels

            // iterate through and set the pixels in the target frame (note: Y is i, X is j)
            for (int Y = 0; Y < targetHeight; Y++)
            {
                for (int X = 0; X < targetWidth; X++)
                {
                    Point camera_coor = camera_coors[Y][X];
                    if (camera_coor.X < 0 || camera_coor.X > camera_width - 1 || camera_coor.Y < 0 || camera_coor.Y > camera_height - 1) // if outside the original camera frame
                    {
                        // since we are overwriting the same frame, just skip
                        continue;
                    }
                    else // inside the original camera frame
                    {
                        neighbors[0].X = Math.Floor(camera_coor.X); neighbors[0].Y = Math.Floor(camera_coor.Y);
                        neighbors[1].X = Math.Ceiling(camera_coor.X); neighbors[1].Y = Math.Floor(camera_coor.Y);
                        neighbors[2].X = Math.Floor(camera_coor.X); neighbors[2].Y = Math.Ceiling(camera_coor.Y);
                        neighbors[3].X = Math.Ceiling(camera_coor.X); neighbors[3].Y = Math.Ceiling(camera_coor.Y);

                        for (int color_index = 0; color_index < 4; color_index++)
                        {
                            byte* pixel_color = camera_frame + (camera_start_index + camera_width * 4 * (int)neighbors[color_index].Y + 4 * (int)neighbors[color_index].X);
                            neighbor_colors[color_index * 4] = pixel_color[0];
                            neighbor_colors[color_index * 4 + 1] = pixel_color[1];
                            neighbor_colors[color_index * 4 + 2] = pixel_color[2];
                            neighbor_colors[color_index * 4 + 3] = pixel_color[3];
                        }

                        double x_scale = camera_coor.X - neighbors[0].X; double y_scale = camera_coor.Y - neighbors[0].Y;
                        byte* target_color = Color_bililerp(neighbor_colors, (float)x_scale, (float)y_scale);

                        // set the pixel onto the target frame
                        int i = Y; int j = X;
                        target_frame[targetWidth * 4 * i + 4 * j] = target_color[0];
                        target_frame[targetWidth * 4 * i + 4 * j + 1] = target_color[1];
                        target_frame[targetWidth * 4 * i + 4 * j + 2] = target_color[2];
                        target_frame[targetWidth * 4 * i + 4 * j + 3] = target_color[3];
                    }
                }
            }

            // take first frame for local background subtraction
            if (is_first_frame)
            {
                // save the first frame as the background
                first_frame = new byte[targetWidth * targetHeight * 4];
#if ENABLE_WINMD_SUPPORT
                Marshal.Copy((IntPtr)target_frame, first_frame, 0, first_frame.Length);
#endif
                Debug.Log("first frame saved as background!");
                is_first_frame = false;
            }
        }

        private static void Bg_subtraction(int targetWidth, int targetHeight)
        {
            for (int i = 0; i < targetHeight; i++)
            {
                for (int j = 0; j < targetWidth; j++)
                {
                    byte[] current_color = GetPixel(target_frame, i, j, targetWidth * 4);
                    byte[] bg_color = GetPixel(first_frame, i, j, targetWidth * 4);

                    bool close_enough = true;
                    for (int k = 0; k < 3; k++)
                    {
                        if (Mathf.Abs((int)current_color[k] - (int)bg_color[k]) > 10)
                        {
                            close_enough = false;
                            break;
                        }
                    }

                    if (close_enough)
                    {
                        byte* result_color = stackalloc byte[] { 0, 0, 0, 255 }; // nah, just set to black
                        SetPixel(target_frame, i, j, result_color, targetWidth * 4);
                    }
                }
            }
        }

        // bililerp color
        // scale is the original vector subtracted by the integer part
        private static byte* Color_bililerp(byte* neighbor_colors, float x_scale, float y_scale)
        {
            byte* result_color = stackalloc byte[4];
            result_color[3] = 255;
            int channel = 0;
            while (channel < 3)
            {
                result_color[channel] = Bililerp(neighbor_colors[channel], neighbor_colors[4 + channel], neighbor_colors[8 + channel], neighbor_colors[12 + channel], x_scale, y_scale);
                channel++;
            }
            return result_color;
        }

        // handles the bililerp for each color channel
        private static byte Bililerp(byte LT, byte RT, byte LB, byte RB, float x_scale, float y_scale)
        {
            // lerp horizontally
            float top_row = LT * (1 - x_scale) + RT * x_scale;
            float bottom_row = LB * (1 - x_scale) + RB * x_scale;

            // lerp vertically
            float result = top_row * (1 - y_scale) + bottom_row * y_scale;
            return (byte) result;
        }

        // Get a bgra8 pixel from an unsafe frame array
        private static byte[] GetPixel(byte* frameData, int i, int j, BitmapPlaneDescription bufferLayout) // returns a byte storing bgra
        {
            return new byte[]{frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j],
                              frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1],
                              frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2],
                              frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3]};
        }

        // Get a bgra8 pixel from a managed frame array
        private static byte[] GetPixel(byte[] frameData, int i, int j, int stride)
        {
            return new byte[]{frameData[stride * i + 4 * j],
                              frameData[stride * i + 4 * j + 1],
                              frameData[stride * i + 4 * j + 2],
                              frameData[stride * i + 4 * j + 3]};
        }

        // Get a bgra8 pixel from an unsafe frame array without bufferlayout
        private static byte[] GetPixel(byte* frameData, int i, int j, int stride)
        {
            return new byte[]{frameData[stride * i + 4 * j],
                              frameData[stride * i + 4 * j + 1],
                              frameData[stride * i + 4 * j + 2],
                              frameData[stride * i + 4 * j + 3]};
        }

        // Set a bgra8 pixel on an unsafe frame array with bufferlayout (may just zero?)
        private static void setPixel(byte* frameData, int i, int j, byte* color, BitmapPlaneDescription bufferLayout)
        {
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j] = color[0];
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = color[1];
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = color[2];
            frameData[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = color[3];
        }

        // Set a bgra8 pixel on an unsafe frame array without bufferlayout
        private static void SetPixel(byte* frameData, int i, int j, byte* color, int stride)
        {
            frameData[stride * i + 4 * j] = color[0];
            frameData[stride * i + 4 * j + 1] = color[1];
            frameData[stride * i + 4 * j + 2] = color[2];
            frameData[stride * i + 4 * j + 3] = color[3];
        }

        // Set a bgra8 pixel on a managed frame array
        private static void SetPixel(byte[] framedata, int i, int j, byte* color, int stride)
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

