﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraFrameUtilities
{
    public static class NumericsConversionExtensions
    {
        public static Vector3 ToUnity(this System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, -v.Z);
        public static Quaternion ToUnity(this System.Numerics.Quaternion q) => new Quaternion(q.X, q.Y, -q.Z, -q.W);
        public static Matrix4x4 ToUnity(this System.Numerics.Matrix4x4 m) => new Matrix4x4(
            new Vector4(m.M11, m.M12, -m.M13, m.M14),
            new Vector4(m.M21, m.M22, -m.M23, m.M24),
            new Vector4(-m.M31, -m.M32, m.M33, -m.M34),
            new Vector4(m.M41, m.M42, -m.M43, m.M44));

        public static System.Numerics.Vector3 ToSystem(this Vector3 v) => new System.Numerics.Vector3(v.x, v.y, -v.z);
        public static System.Numerics.Quaternion ToSystem(this Quaternion q) => new System.Numerics.Quaternion(q.x, q.y, -q.z, -q.w);
        public static System.Numerics.Matrix4x4 ToSystem(this Matrix4x4 m) => new System.Numerics.Matrix4x4(
            m.m00, m.m10, -m.m20, m.m30,
            m.m01, m.m11, -m.m21, m.m31,
           -m.m02, -m.m12, m.m22, -m.m32,
            m.m03, m.m13, -m.m23, m.m33);
    }
}
