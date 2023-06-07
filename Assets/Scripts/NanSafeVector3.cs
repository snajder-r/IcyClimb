using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtils
{

    public static Vector3 ReplaceNaN(Vector3 a)
    {
        return new Vector3(ReplaceNaN(a.x), ReplaceNaN(a.y), ReplaceNaN(a.z));
    }

    private static float ReplaceNaN(float a)
    {
        if (float.IsNaN(a) || float.IsInfinity(a)) return 0f;
        return a;
    }
}
