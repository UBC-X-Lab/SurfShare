using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Windows.Perception.Spatial;
using Microsoft.MixedReality.Toolkit.WindowsMixedReality;
using UnityEngine.XR.WindowsMR;

namespace CoordinateConversion
{
    public static class CoordinateSystemHelper
    {
        // from unity coordinates to frame coordinate. Note the taken argument is WinRT's definition for SpatialCoordinateSystem
        public static System.Numerics.Vector3? GetFramePosition(SpatialCoordinateSystem frameCoordinateSystem, Vector3 unityPosition)
        {
            // convert Unity Coordinates to Hololens Coordinates (left-handed to right handed)
            System.Numerics.Vector3 HLCoordinate = NumericsConversionExtensions.ToSystem(unityPosition); // Is the hololens world origin the same as unity origin? For now we don't need rotation
            System.Numerics.Matrix4x4? transformToFrame = WindowsMixedRealityUtilities.SpatialCoordinateSystem.TryGetTransformTo(frameCoordinateSystem);
            if (transformToFrame.HasValue)
            {
                System.Numerics.Vector3 frameCoordinate = System.Numerics.Vector3.Transform(HLCoordinate, transformToFrame.Value);
                Debug.Log("X:" + frameCoordinate.X + ", Y:" + frameCoordinate.Y + ", Z:" + frameCoordinate.Z);
                return frameCoordinate;
            }
            else
            {
                Debug.Log("Transformation Matrix returned null!");
                return null;
            }
        }
    }
}

