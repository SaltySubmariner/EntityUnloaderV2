using OfflineUnload.Models;
using UnityEngine;

namespace OfflineUnload.Services
{
    internal static class ModelConvert
    {
        public static SavedVector3 ToSaved(this Vector3 v) => new SavedVector3 { X = v.x, Y = v.y, Z = v.z };
        public static Vector3 ToUnity(this SavedVector3 v) => new Vector3(v.X, v.Y, v.Z);
        public static SavedQuaternion ToSaved(this Quaternion q) => new SavedQuaternion { X = q.x, Y = q.y, Z = q.z, W = q.w };
        public static Quaternion ToUnity(this SavedQuaternion q) => new Quaternion(q.X, q.Y, q.Z, q.W);
        public static SavedColor32 ToSaved(this Color32 c) => new SavedColor32 { R = c.r, G = c.g, B = c.b, A = c.a };
        public static Color32 ToUnity(this SavedColor32 c) => new Color32(c.R, c.G, c.B, c.A);
    }
}
