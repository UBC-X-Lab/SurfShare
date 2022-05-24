// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
//using System.Diagnostics;
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

#if ENABLE_WINMD_SUPPORT
using global::Windows.Graphics.Holographic;
#endif

#if UNITY_WSA && !UNITY_EDITOR
using global::Windows.Devices.Enumeration;
using global::Windows.UI.Xaml.Media.Imaging;
using global::Windows.Media.MediaProperties;
using global::Windows.Graphics.Imaging;
using global::Windows.UI.Core;
using global::Windows.Foundation;
using global::Windows.Media.Capture.Frames;
using global::Windows.Media.Capture;
using global::Windows.Media.Core;
using global::Windows.Media;
using global::Windows.Media.Devices;
using global::Windows.Media.Audio;
using global::Windows.ApplicationModel.Core;
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
        //private var colorFrameSource;
        private MediaFrameReader mediaFrameReader;
        private SoftwareBitmap backBuffer;
        private bool taskRunning = false;
#endif

        protected override async void OnEnable() // potentially callable as an async function
        {
            Debug.Log("Enabled");
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
                    Debug.Log("Try requesting media access");
                    UnityEngine.WSA.Application.InvokeOnUIThread(() => RequestAccessAsync(), waitUntilDone: true);
                    Debug.Log("Try Creating Media Frame Reader");
                    UnityEngine.WSA.Application.InvokeOnUIThread(() => CreateMediaFrameReader(), waitUntilDone: true);
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
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // dispose of the frame queue?
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
            Debug.Log("Try requesting media access (In Task)");
            // On UWP the app must have the "webcam" capability, and the user must allow webcam
            // access. So check that access before trying to initialize the WebRTC library, as this
            // may result in a popup window being displayed the first time, which needs to be accepted
            // before the camera can be accessed by WebRTC.

            // select frame sources and frame source groups
            var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

            foreach (var sourceGroup in frameSourceGroups)
            {
                foreach (var sourceInfo in sourceGroup.SourceInfos)
                {
                    // this identifies a webcam: videoPreview (VideoRecording?) + color suggests a webcam that provides colored frames
                    if (sourceInfo.MediaStreamType == MediaStreamType.VideoPreview
                        && sourceInfo.SourceKind == MediaFrameSourceKind.Color)
                    {
                        colorSourceInfo = sourceInfo;
                        break;
                    }
                }
                if (colorSourceInfo != null)
                {
                    selectedGroup = sourceGroup;
                    break;
                }
            }

            mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings()
            {
                SourceGroup = selectedGroup,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                PhotoCaptureSource = PhotoCaptureSource.VideoPreview
            };
            await mediaCapture.InitializeAsync(settings);
            Debug.Log("Request Access Success");
        }

        /// <summary>
        /// Initialize the MediaFrameReader
        /// </summary>
        private async Task CreateMediaFrameReader()
        {
            Debug.Log("Try Creating Media Frame Reader (In Task)");
            //Set the preferred format for the frame source
            var colorFrameSource = mediaCapture.FrameSources[colorSourceInfo.Id];
            var preferredFormat = colorFrameSource.SupportedFormats.Where(format =>
            {
                return format.VideoFormat.Width >= 960
                && format.Subtype == MediaEncodingSubtypes.Argb32;
            }).FirstOrDefault();

            if (preferredFormat == null)
            {
                // Our desired format is not supported
                Debug.Log("Our desired format is not supported");
                return;
            }
            await colorFrameSource.SetFormatAsync(preferredFormat);

            // creating the media Frame Reader
            mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(colorFrameSource, MediaEncodingSubtypes.Argb32);
            mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
            await mediaFrameReader.StartAsync();
            Debug.Log("Create Media Frame Reader Success");
        }

        /// <summary>
        /// Frame Handler at frame arrival
        /// </summary>
        private void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            Debug.Log("FrameArrived");

            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

            if (softwareBitmap != null)
            {
                if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                // Swap the processed frame to _backBuffer and dispose of the unused image.
                //softwareBitmap = Interlocked.Exchange(ref backBuffer, softwareBitmap);
                //softwareBitmap?.Dispose();

                //// In documentation this is invoked in the UI thread. Am I doing this correctly?
                //if (taskRunning)
                //{
                //    return;
                //}
                //taskRunning = true;

                // Keep draining frames from the backbuffer until the backbuffer is empty. (why would this hold multiple bitmaps?)
                //SoftwareBitmap latestBitmap;
                // converting the bitmap to a byte buffer
                using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write)) // Read Mode?
                {
                    using (var reference = buffer.CreateReference())
                    {
                        unsafe
                        {
                            byte* dataInBytes;
                            uint capacity;
                            ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
                             
                            BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);

                            // Enqueue a frame in the internal frame queue. This will make a copy
                            // of the frame into a pooled buffer owned by the frame queue.
                            var frame = new Argb32VideoFrame
                            {
                                data = (IntPtr)dataInBytes,
                                stride = softwareBitmap.PixelWidth * 4,
                                width = (uint)softwareBitmap.PixelWidth,
                                height = (uint)softwareBitmap.PixelHeight
                            };
                            _frameQueue.Enqueue(frame); 
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
                }
                softwareBitmap.Dispose();
            }

            mediaFrameReference.Dispose();
            Debug.Log("FrameProcessed");
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

