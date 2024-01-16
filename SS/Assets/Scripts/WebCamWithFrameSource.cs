// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
//using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Microsoft.MixedReality.WebRTC;
using Microsoft.MixedReality.WebRTC.Unity;
using OpenCvSharp;


// my name spaces
using CameraFrameUtilities;

// native name spaces
#if ENABLE_WINMD_SUPPORT
using global::Windows.Graphics.Holographic;
#endif

#if UNITY_WSA && !UNITY_EDITOR
using global::Windows.Media.MediaProperties;
using global::Windows.Graphics.Imaging;
using global::Windows.Foundation;
using global::Windows.Media.Capture.Frames;
using global::Windows.Media.Capture;
//using global::Windows.Devices.Enumeration;
//using global::Windows.UI.Core;
//using global::Windows.Media.Core;
//using global::Windows.Media;
//using global::Windows.Media.Devices;
//using global::Windows.Media.Audio;
//using global::Windows.ApplicationModel.Core;
#endif

namespace CustomVideoSources
{
    /// <summary>
    /// Custom video source capturing the Unity scene content as rendered by a given camera,
    /// and sending it as a video track through the selected peer connection.
    /// </summary>
    public class WebCamWithFrameSource : CustomVideoSource<Argb32VideoFrameStorage>
    {

        /// <summary>
        /// Temporary storage for frames requested from MediaFrameReader until consumed by WebRTC.
        /// </summary>
        private VideoFrameQueue<Argb32VideoFrameStorage> _frameQueue = new VideoFrameQueue<Argb32VideoFrameStorage>(3);

#if UNITY_WSA && !UNITY_EDITOR
        private MediaCapture mediaCapture;
        private MediaFrameSourceGroup selectedGroup = null;
        private MediaFrameSourceInfo colorSourceInfo = null;
        private MediaFrameReader mediaFrameReader;
        private bool taskRunning = false;
        //private SoftwareBitmap backBuffer;
#endif

        protected override async void OnEnable() // potentially callable as an async function
        {
            unsafe 
            {
#if ENABLE_WINMD_SUPPORT
                // initialize projection masking utilities
                FrameProcessor.world_coors = (System.Numerics.Vector3*)Marshal.AllocHGlobal(FrameProcessor.targetWidth * FrameProcessor.targetHeight * Marshal.SizeOf(new System.Numerics.Vector3()));
                for (int i = 0; i < FrameProcessor.targetHeight; i++)
                {
                    FrameProcessor.camera_coors[i] = new Windows.Foundation.Point[FrameProcessor.targetWidth];
                    FrameProcessor.frame_coors[i] = new System.Numerics.Vector3[FrameProcessor.targetWidth];
                }
                Debug.Log("Projection masking initialized!");

                // initialize target_frame
                FrameProcessor.target_frame = (byte*)Marshal.AllocHGlobal(FrameProcessor.targetWidth * FrameProcessor.targetHeight * 4);

                for (int i = 0; i < FrameProcessor.targetWidth * FrameProcessor.targetHeight * 4; i++)
                {
                    FrameProcessor.target_frame[i] = 255;
                }
#endif
            }

            // Debug.Log("WebCamWithFrameSourceEnabled");
            // request and enable camera access
#if UNITY_WSA && !UNITY_EDITOR
            // Request UWP access to video capture. The OS may show some popup dialog to the
            // user to request permission. This will succeed only if the user grants permission.
            try
            {
                // Note that the UWP UI thread and the main Unity app thread are always different.
                // https://docs.unity3d.com/Manual/windowsstore-appcallbacks.html
                // We leave the code below as an example of generic handling in case this would be used in
                // some other place, and in case a future version of Unity decided to change that assumption,
                // but currently OnEnable() is always invoked from the main Unity app thread so here the first
                // branch is never taken.
                if (UnityEngine.WSA.Application.RunningOnUIThread())
                {
                    await RequestAccessAsync();
                }
                else
                {
                    UnityEngine.WSA.Application.InvokeOnUIThread(() => RequestAccessAsync(), waitUntilDone: true);
                }
            }
            catch (Exception ex)
            {
                // Log an error and prevent activation
                Debug.LogError($"Video access failure: {ex.Message}.");
                this.enabled = false;
                return;
            }
#endif

            // Create the track source
            // Create the track source
            base.OnEnable();
        }

        protected override async void OnDisable()
        {
#if UNITY_WSA && !UNITY_EDITOR
            await mediaFrameReader.StopAsync();
            mediaFrameReader.FrameArrived -= ColorFrameReader_FrameArrived;
            mediaCapture.Dispose();
            mediaCapture = null;
#endif

#if ENABLE_WINMD_SUPPORT
            unsafe
            {
                Marshal.FreeHGlobal((IntPtr)FrameProcessor.target_frame);
                Marshal.FreeHGlobal((IntPtr)FrameProcessor.world_coors);
            }

#endif
            base.OnDisable();
        }

#if UNITY_WSA && !UNITY_EDITOR
        /// <summary>
        /// Internal UWP helper to ensure device access.
        /// </summary>
        /// <remarks>
        /// This must be called from the main UWP UI thread (not the main Unity app thread).
        /// </remarks>
        private async Task RequestAccessAsync()
        {
            Debug.Log("Try requesting media access");
            // On UWP the app must have the "webcam" capability, and the user must allow webcam
            // access. So check that access before trying to initialize the WebRTC library, as this
            // may result in a popup window being displayed the first time, which needs to be accepted
            // before the camera can be accessed by WebRTC.

            // select frame sources and frame source groups
            var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

            // // Basic way to select source group. Now using video profile instead
            //foreach (var sourceGroup in frameSourceGroups)
            //{
            //    foreach (var sourceInfo in sourceGroup.SourceInfos)
            //    {
            //        // this identifies a webcam: videoPreview (VideoRecording?) + color suggests a webcam that provides colored frames
            //        // 
            //        if (sourceInfo.MediaStreamType == MediaStreamType.VideoRecord && sourceInfo.SourceKind == MediaFrameSourceKind.Color)
            //        {
            //            colorSourceInfo = sourceInfo;
            //            break;
            //        }
            //    }
            //    if (colorSourceInfo != null)
            //    {
            //        selectedGroup = sourceGroup;
            //        break;
            //    }
            //}

            var settings = new MediaCaptureInitializationSettings()
            {
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                PhotoCaptureSource = PhotoCaptureSource.Photo
            };

            bool profile_found = false;

            // Debug.Log("Number of frame source groups:" + frameSourceGroups.Count);
            int possibleSourceGroups = 0;
            foreach (var sourceGroup in frameSourceGroups)
            {
                // Find a device that support AdvancedColorPhoto
                IReadOnlyList<MediaCaptureVideoProfile> profileList = MediaCapture.FindKnownVideoProfiles(sourceGroup.Id, KnownVideoProfile.VideoConferencing);

                if (profileList.Count > 0)
                {
                    // Debug.Log("Profile list length:" + profileList.Count); // 1
                    // Debug.Log(sourceGroup.SourceInfos.Count); // 3
                    List<MediaFrameSourceInfo> sourceInfoList = new List<MediaFrameSourceInfo>();
                    foreach (var sourceInfo in sourceGroup.SourceInfos){
                        if (sourceInfo.SourceKind == MediaFrameSourceKind.Color){
                            //colorSourceInfo = sourceInfo;
                            sourceInfoList.Add(sourceInfo);
                        }
                    }
                    colorSourceInfo = sourceInfoList[0]; // 1280 is in profile 0, 960 is in profile 1
                    
                    // Debug.Log(sourceInfoList.Count); // 2, the one with lower resolution is in the second
                    
                    if (colorSourceInfo != null)
                    {
                        possibleSourceGroups += 1;
                        if (possibleSourceGroups == 1){
                            selectedGroup = sourceGroup;
                            settings.VideoProfile = profileList[0];
                            settings.VideoDeviceId = sourceGroup.Id;
                            profile_found = true;
                        }
                    }
                }
            }

            // Debug.Log("Number of possible source groups:" + possibleSourceGroups);

            if (!profile_found){
                Debug.Log("Specified Profiles were not found!");
                return;
            }

            //Debug.Log(colorSourceInfo);
            //Debug.Log(selectedGroup);

            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(settings);
            Debug.Log("Request Access Success");

            // Initialize the MediaFrameReader
            Debug.Log("Try Creating Media Frame Reader");
            // Set the preferred format for the frame source
            var colorFrameSource = mediaCapture.FrameSources[colorSourceInfo.Id];
            
            // Log all the available formats
            //foreach (var format in colorFrameSource.SupportedFormats)
            //{
            //    Debug.Log(format.VideoFormat.Width);
            //    Debug.Log(format.Subtype);
            //}

            var preferredFormat = colorFrameSource.SupportedFormats.Where(format =>
            {
                return format.VideoFormat.Width == 1280; // change back to 960 (or even 640) if the performance is not good enough
                //&& format.Subtype == MediaEncodingSubtypes.Argb32;
            }).FirstOrDefault();

            Debug.Log("Video width and height set to:" + preferredFormat.VideoFormat.Width + "," + preferredFormat.VideoFormat.Height);
            Debug.Log("MediaEncodingSubtypes set to:" + preferredFormat.Subtype);

            if (preferredFormat == null)
            {
                // Our desired format is not supported
                Debug.Log("Our desired format is not supported");
                return;
            }
            await colorFrameSource.SetFormatAsync(preferredFormat);

            // creating the media Frame Reader
            BitmapSize bitmapSize = new BitmapSize()
            {
                Height = preferredFormat.VideoFormat.Height,
                Width = preferredFormat.VideoFormat.Width
            };

            mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(colorFrameSource, MediaEncodingSubtypes.Nv12, bitmapSize);
            Debug.Log("Create Media Frame Reader Success");

            mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived; // invoked in its own thread?
            await mediaFrameReader.StartAsync();
            Debug.Log("StartAsync");

            // opt in PV camera
            Debug.Log("Trying to Opt-in Rendering from PV camera");
            var display = HolographicDisplay.GetDefault();
            var view = display.TryGetViewConfiguration(HolographicViewConfigurationKind.PhotoVideoCamera);
            if (view != null)
            {
                Debug.Log("Opt-in Rendering from PV camera Success");
                view.IsEnabled = true;
            }
            else
            {
                Debug.Log("Opt-in Rendering from PV camera Failed");
            }

            Debug.Log("Camera Setup Complete");
            Main.WebRTCSetupComplete = true;
        }


        /// <summary>
        /// Frame Handler at frame arrival
        /// </summary>
        private void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            // Debug.Log("FrameArrived");

            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

            if (softwareBitmap != null)
            {
                SoftwareBitmap converted_softwareBitmap;
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    converted_softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    softwareBitmap?.Dispose();
                    // Debug.Log("Convert Success");
                }else
                {
                    converted_softwareBitmap = softwareBitmap;
                }

                if (!taskRunning)
                {
                    taskRunning = true;
                    // converting the bitmap to a byte buffer
                    using (BitmapBuffer buffer = converted_softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write)) // Read Mode?
                    {
                        using (var reference = buffer.CreateReference())
                        {
                            unsafe
                            {
                                byte* dataInBytes;
                                uint capacity;
                                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
                             
                                BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);

                                // corners are set, ready to transmit masked frames
                                if (FrameHandler.corners.Count == 4 && mediaFrameReference.CoordinateSystem != null)
                                {
                                    // FrameProcessor.NaiveMasking(mediaFrameReference.CoordinateSystem, videoMediaFrame, dataInBytes, bufferLayout.StartIndex, bufferLayout.Width, bufferLayout.Height);
                                    FrameProcessor.ProjectionMasking(mediaFrameReference.CoordinateSystem, videoMediaFrame, dataInBytes, bufferLayout.StartIndex, bufferLayout.Width, bufferLayout.Height);

                                    if (Main.meshCreation)
                                    {
                                        Debug.Log("Creating Mesh");
                                        Main.meshCreation = false;
                                        byte* mask = FrameProcessor.Bg_subtraction();
                                        Mat greyImage = new Mat(FrameProcessor.targetHeight, FrameProcessor.targetWidth, MatType.CV_8UC1, (IntPtr)mask);
                                        // Mat greyImage = image.CvtColor(ColorConversionCodes.BGRA2GRAY).Threshold(127, 255, ThresholdTypes.Binary);
                                        // OpenCvSharp.Point[][] contours = greyImage.FindContoursAsArray(RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

                                        OpenCvSharp.Point[][] contours;
                                        HierarchyIndex[] hierarchy;

                                        greyImage.FindContours(out contours, out hierarchy, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);

                                        // filter contours that are too small
                                        lock (Main.res_con_lock)
                                        {
                                            // first group the contours by objects in 2D
                                            List<List<OpenCvSharp.Point[]>> objects = new List<List<OpenCvSharp.Point[]>>(); // stores groups of contours, each group of List<Point[]> represents an object; in each group, the first is the external contour, and the rest are holes
                                            for (int i = 0; i < hierarchy.Length; i++)
                                            {
                                                HierarchyIndex hier = hierarchy[i];
                                                // find the external contour that is big enough
                                                if (hier.Parent == -1 && Cv2.ContourArea(contours[i]) > 1600)
                                                {
                                                    List<OpenCvSharp.Point[]> new_object = new List<OpenCvSharp.Point[]>();

                                                    // contour approx
                                                    double eps = 0.005 * Cv2.ArcLength(contours[i], true);
                                                    new_object.Add(Cv2.ApproxPolyDP(contours[i], eps, true));

                                                    // now find all the holes that are big enough in this object
                                                    if (hier.Child != -1)
                                                    {
                                                        // add the first hole
                                                        if (Cv2.ContourArea(contours[hier.Child]) > 400)
                                                        {
                                                            // contour approx
                                                            eps = 0.01 * Cv2.ArcLength(contours[hier.Child], true);
                                                            new_object.Add(Cv2.ApproxPolyDP(contours[hier.Child], eps, true));
                                                        }
                                                        HierarchyIndex hier_hole = hierarchy[hier.Child];

                                                        // add the rest holes
                                                        while (hier_hole.Next != -1)
                                                        {
                                                            if (Cv2.ContourArea(contours[hier_hole.Next]) > 400)
                                                            {
                                                                // contour approx
                                                                eps = 0.01 * Cv2.ArcLength(contours[hier_hole.Next], true);
                                                                new_object.Add(Cv2.ApproxPolyDP(contours[hier_hole.Next], eps, true));
                                                            }
                                                            hier_hole = hierarchy[hier_hole.Next];
                                                        }
                                                    }

                                                    // add the completed object contours to objects
                                                    objects.Add(new_object);
                                                }
                                            }

                                            // calculate numbder of vertices for each ext and holes and combine all together
                                            foreach (List<OpenCvSharp.Point[]> obj in objects)
                                            {
                                                int ver_count = 0;
                                                int[] vertices_count = new int[obj.Count];
                                                for (int i = 0; i < obj.Count; i++)
                                                {
                                                    ver_count += obj[i].Length;
                                                    Debug.Log("Countour Length " + obj[i].Length);
                                                    vertices_count[i] = obj[i].Length;
                                                }

                                                // combine all the vertices in the object
                                                OpenCvSharp.Point[] vertices = new OpenCvSharp.Point[ver_count];
                                                int index = 0;
                                                foreach (OpenCvSharp.Point[] contour in obj)
                                                {
                                                    foreach (OpenCvSharp.Point vertex in contour)
                                                    {
                                                        vertices[index] = new OpenCvSharp.Point(vertex.X, vertex.Y);
                                                        index++;
                                                    }
                                                }

                                                // then convert the contour objects to windows pixels, voxels in 3D, and calculate the object color
                                                // List<List<Windows.Foundation.Point[]>> object_pixels = new List<List<Windows.Foundation.Point[]>>();
                                                //List<List<Vector3[]>> object_world = new List<List<Vector3[]>>();
                                                //List<Color> object_colors = new List<Color>();

                                                Windows.Foundation.Point[] pixel_pos = new Windows.Foundation.Point[vertices.Length];
                                                Windows.Foundation.Point contour_center = new Windows.Foundation.Point();
                                                for (int i = 0; i < pixel_pos.Length; i++)
                                                {
                                                    pixel_pos[i] = new Windows.Foundation.Point(vertices[i].X, FrameProcessor.targetHeight - 1 - vertices[i].Y);

                                                    // only use extern contour for color calculation
                                                    if (i < vertices_count[0])
                                                    {
                                                        contour_center.X += pixel_pos[i].X;
                                                        contour_center.Y += pixel_pos[i].Y;
                                                    }
                                                }

                                                // calculate color
                                                contour_center.X /= vertices_count[0];
                                                contour_center.Y /= vertices_count[0];

                                                Color mesh_color = new Color(0, 0, 0);
                                                int color_extraction_count = 0;
                                                for (int y = (int)contour_center.Y - 5; y < (int)contour_center.Y + 5; y++)
                                                {
                                                    if (y > FrameProcessor.targetHeight - 1)
                                                    {
                                                        break;
                                                    }

                                                    for (int x = (int)contour_center.X - 5; x < (int)contour_center.X + 5; x++)
                                                    {
                                                        if (x > FrameProcessor.targetWidth - 1)
                                                        {
                                                            break;
                                                        }

                                                        mesh_color.b += FrameProcessor.target_frame[y * FrameProcessor.targetWidth * 4 + x * 4] / 255f;
                                                        mesh_color.g += FrameProcessor.target_frame[y * FrameProcessor.targetWidth * 4 + x * 4 + 1] / 255f;
                                                        mesh_color.r += FrameProcessor.target_frame[y * FrameProcessor.targetWidth * 4 + x * 4 + 2] / 255f;
                                                        color_extraction_count += 1;
                                                    }
                                                }

                                                mesh_color.b /= color_extraction_count;
                                                mesh_color.g /= color_extraction_count;
                                                mesh_color.r /= color_extraction_count;
                                                Debug.Log("Mesh Color is R:" + mesh_color.r + ", G:" + mesh_color.b + ", B:" + mesh_color.b);

                                                Vector3[] contour_world = FrameProcessor.ProjectFrameCoorsToWorld(pixel_pos, mediaFrameReference.CoordinateSystem);

                                                if (contour_world != null)
                                                {
                                                    Main.res_con.Add(vertices);
                                                    Main.res_con_world.Add(contour_world);
                                                    Main.vertices_count.Add(vertices_count);
                                                    Main.mesh_colors.Add(mesh_color);
                                                    // Debug.Log("Contour size:" + Main.res_con[Main.res_con.Count - 1].Length);
                                                    // Debug.Log(Cv2.ContourArea(contours[i]));
                                                }
                                            }



                                            //for (int i = 0; i < contours.Length; i++)
                                            //{
                                            //    if (Cv2.ContourArea(contours[i]) > 1600)
                                            //    {
                                            //        // contour approx
                                            //        double eps = 0.01 * Cv2.ArcLength(contours[i], true);
                                            //        OpenCvSharp.Point[] approx_contour = Cv2.ApproxPolyDP(contours[i], eps, true);
                                            //        // convert to Windows foundataion
                                            //        Windows.Foundation.Point[] pixel_pos = new Windows.Foundation.Point[approx_contour.Length];

                                            //        Windows.Foundation.Point contour_center = new Windows.Foundation.Point();
                                            //        for (int j = 0; j < pixel_pos.Length; j++)
                                            //        {
                                            //            // the opencv image is bottom up while the system image is top down
                                            //            pixel_pos[j] = new Windows.Foundation.Point(approx_contour[j].X, FrameProcessor.targetHeight - 1 - approx_contour[j].Y);
                                            //            contour_center.X += pixel_pos[j].X;
                                            //            contour_center.Y += pixel_pos[j].Y;
                                            //        }

                                            //        // calculate color
                                            //        contour_center.X /= pixel_pos.Length;
                                            //        contour_center.Y /= pixel_pos.Length;

                                            //        Color mesh_color = new Color(0, 0, 0);
                                            //        int color_extraction_count = 0;
                                            //        for (int y = (int) contour_center.Y - 5; y < (int) contour_center.Y + 5; y++)
                                            //        {
                                            //            if (y > FrameProcessor.targetHeight - 1)
                                            //            {
                                            //                break;
                                            //            }

                                            //            for (int x = (int) contour_center.X - 5; x < (int) contour_center.X + 5; x++)
                                            //            {
                                            //                if (x > FrameProcessor.targetWidth - 1)
                                            //                {
                                            //                    break;
                                            //                }

                                            //                mesh_color.b += FrameProcessor.target_frame[y * FrameProcessor.targetWidth * 4 + x * 4] / 255f;
                                            //                mesh_color.g += FrameProcessor.target_frame[y * FrameProcessor.targetWidth * 4 + x * 4 + 1] / 255f;
                                            //                mesh_color.r += FrameProcessor.target_frame[y * FrameProcessor.targetWidth * 4 + x * 4 + 2] / 255f;
                                            //                color_extraction_count += 1;
                                            //            }
                                            //        }

                                            //        mesh_color.b /= color_extraction_count;
                                            //        mesh_color.g /= color_extraction_count;
                                            //        mesh_color.r /= color_extraction_count;
                                            //        Debug.Log("Mesh Color is R:" + mesh_color.r + ", G:" + mesh_color.b + ", B:" + mesh_color.b);


                                            //        Vector3[] contour_world = FrameProcessor.ProjectFrameCoorsToWorld(pixel_pos, mediaFrameReference.CoordinateSystem);
                                            //        if (contour_world != null)
                                            //        {
                                            //            Main.res_con.Add(approx_contour);
                                            //            Main.res_con_world.Add(contour_world);
                                            //            Main.mesh_colors.Add(mesh_color);
                                            //            Debug.Log("Contour size:" + Main.res_con[Main.res_con.Count - 1].Length);
                                            //            Debug.Log(Cv2.ContourArea(contours[i]));
                                            //        }
                                            //    }
                                            //}
                                            //Debug.Log("Number of Meshes to create:" + Main.res_con.Count);
                                        }

                                        // Cv2.DrawContours(image, res_con, -1, new Scalar(0, 255, 0), 3); // the contours points are ordered clock-wise
                                    }

                                    // Enqueue a frame in the internal frame queue. This will make a copy
                                    // of the frame into a pooled buffer owned by the frame queue.
                                    var frame = new Argb32VideoFrame
                                    {
                                        data = (IntPtr)FrameProcessor.target_frame,
                                        stride = FrameProcessor.targetWidth * 4,
                                        width = (uint)FrameProcessor.targetWidth,
                                        height = (uint)FrameProcessor.targetHeight
                                    };
                                    _frameQueue.Enqueue(frame);
                                }
                                else // corners are not set yet, transmit original frames
                                {
                                    if (Main.meshCreation)
                                    {
                                        Main.meshCreation = false;
                                        Debug.Log("Can not do mesh creation in this mode!");
                                    }

                                    var frame = new Argb32VideoFrame
                                    {
                                        data = (IntPtr)dataInBytes,
                                        //stride = converted_softwareBitmap.PixelWidth * 4,
                                        //width = (uint)converted_softwareBitmap.PixelWidth,
                                        //height = (uint)converted_softwareBitmap.PixelHeight
                                        stride = bufferLayout.Stride,
                                        width = (uint)bufferLayout.Width,
                                        height = (uint)bufferLayout.Height
                                    };
                                    _frameQueue.Enqueue(frame);
                                }
                            }
                        }
                    }
                    taskRunning = false;
                }
                converted_softwareBitmap?.Dispose();
            }

            mediaFrameReference.Dispose();
            //Debug.Log("FrameProcessed");
        }
#endif
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        protected override void OnFrameRequested(in FrameRequest request)
        {
            // Debug.Log("On Frame Requested");
            // Try to dequeue a frame from the internal frame queue
            if (_frameQueue.TryDequeue(out Argb32VideoFrameStorage storage))
            {
                //Debug.Log("Got a Frame");
                var frame = new Argb32VideoFrame
                {
                    width = storage.Width,
                    height = storage.Height,
                    stride = (int)storage.Width * 4
                };
                unsafe
                {
                    fixed (void* ptr = storage.Buffer)
                    {
                        // Complete the request with a view over the frame buffer (no allocation)
                        // while the buffer is pinned into memory. The native implementation will
                        // make a copy into a native memory buffer if necessary before returning.
                        frame.data = new IntPtr(ptr);
                        request.CompleteRequest(frame);
                    }
                }

                // Put the allocated buffer back in the pool for reuse
                _frameQueue.RecycleStorage(storage);
            }
            else
            {
                //Debug.Log("Queue was empty");
            }
        }

        /// <summary>
        /// Callback invoked by the command buffer when the scene frame GPU readback has completed
        /// and the frame is available in CPU memory.
        /// </summary>
        /// <param name="request">The completed and possibly failed GPU readback request.</param>
        //private void OnSceneFrameReady(AsyncGPUReadbackRequest request)
        //{
        //    // Read back the data from GPU, if available
        //    if (request.hasError)
        //    {
        //        return;
        //    }
        //    NativeArray<byte> rawData = request.GetData<byte>();
        //    Debug.Assert(rawData.Length >= _readBackWidth * _readBackHeight * 4);
        //    unsafe
        //    {
        //        byte* ptr = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(rawData);

        //        // Enqueue a frame in the internal frame queue. This will make a copy
        //        // of the frame into a pooled buffer owned by the frame queue.
        //        var frame = new Argb32VideoFrame
        //        {
        //            data = (IntPtr)ptr,
        //            stride = _readBackWidth * 4,
        //            width = (uint)_readBackWidth,
        //            height = (uint)_readBackHeight
        //        };
        //        _frameQueue.Enqueue(frame);
        //    }
        //}
    }
}

