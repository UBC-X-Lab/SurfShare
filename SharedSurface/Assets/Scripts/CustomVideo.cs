using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Microsoft.MixedReality.WebRTC.Unity
{
    public class CustomVideo : CustomVideoSource<Argb32VideoFrameStorage>
    {
        protected override void OnFrameRequested(in FrameRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
