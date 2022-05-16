using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

public class Utility
{
    /// <summary>
    /// Map 'value' from between 'fromMin' and 'fromMax' to 'toMin' and 'toMax'
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fromMin"></param>
    /// <param name="fromMax"></param>
    /// <param name="toMin"></param>
    /// <param name="toMax"></param>
    /// <returns></returns>
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }
    /// <summary>
    /// Find the scalar projection of 'a' onto 'b' (this is the proportion of a that aligns with b)
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static float ScalarProjection(Vector3 a, Vector3 b)
    {
        return Vector3.Dot(a, b) / b.magnitude;
    }
    /// <summary>
    /// Return the maximum of 'a' and 'b' based on their absolute values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Vector3 MaxByAbsolute(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Mathf.Abs(a.x) > Mathf.Abs(b.x) ? a.x : b.x,
            Mathf.Abs(a.y) > Mathf.Abs(b.y) ? a.y : b.y,
            Mathf.Abs(a.z) > Mathf.Abs(b.z) ? a.z : b.z
            );
    }
    /// <summary>
    /// return the minimum of 'a' and 'b' based on their absolute values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Vector3 MinByAbsolute(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Mathf.Abs(a.x) < Mathf.Abs(b.x) ? a.x : b.x,
            Mathf.Abs(a.y) < Mathf.Abs(b.y) ? a.y : b.y,
            Mathf.Abs(a.z) < Mathf.Abs(b.z) ? a.z : b.z
            );
    }
    public static float MinByAbsolute(float a, float b)
    {
        return Mathf.Abs(a) < Mathf.Abs(b) ? a : b;
    }
}
