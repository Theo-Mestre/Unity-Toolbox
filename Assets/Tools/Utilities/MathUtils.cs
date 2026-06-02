using UnityEngine;

namespace Maths
{
    public static class Constants
    {
        public const float Epsilon = 0.00001f;
    }

    public static class Comparaisons
    {
        static public bool IsEqual(float a, float b, float tolerance = Constants.Epsilon)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }

        static public bool IsEqual(Vector3 a, Vector3 b, float tolerance = Constants.Epsilon)
        {
            return IsEqual(a.x, b.x, tolerance) &&
                   IsEqual(a.y, b.y, tolerance) &&
                   IsEqual(a.z, b.z, tolerance);
        }
    }
}