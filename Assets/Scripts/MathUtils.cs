using System.Collections.Generic;
using UnityEngine;

public static class MathUtils
{
    /// <summary>
    /// Return a new Vector3 with all NaNs replaced with 0
    /// </summary>
    public static Vector3 ReplaceNaN(Vector3 a)
    {
        return new Vector3(ReplaceNaN(a.x), ReplaceNaN(a.y), ReplaceNaN(a.z));
    }

    /// <summary>
    /// Calculate the arithmetic mean on a given subset of the sample
    /// </summary>
    public static Vector3 MeanVector(IEnumerable<Vector3> points)
    {
        int length = 0;
        Vector3 sum = Vector3.zero;
        foreach (Vector3 a in points)
        {
            sum += a;
            length++;
        }

        return sum / length;
    }
    /// <summary>
    /// Finds the vector with the median magnitude and returns it
    /// </summary>
    public static Vector3 MedianMagnitudeVector(IEnumerable<Vector3> points)
    {
        List<Vector3> pointList = new List<Vector3>(points);
        pointList.Sort((a, b) => a.magnitude.CompareTo(b.magnitude));

        int medianIndex = pointList.Count / 2;
        return pointList[medianIndex];
    }

    /// <summary>
    /// Returns 0 if parameter is NaN or Infinity, otherwise returns the parameter
    /// </summary>
    private static float ReplaceNaN(float a)
    {
        return float.IsNaN(a) || float.IsInfinity(a) ? 0f : a;
    }
}
