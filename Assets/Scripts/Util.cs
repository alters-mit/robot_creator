using System.Linq;
using UnityEngine;


/// <summary>
/// Utility class.
/// </summary>
public static class Util
{
    /// <summary>
    /// Convert a .urdf xyz string to a Vector3 position in Unity coordinates.
    /// </summary>
    /// <param name="xyz">A string of xyz coordinates in a .urdf file.</param>
    public static Vector3 xyzToVector3(this string xyz)
    {
        float[] coordinates = xyz.Split(' ').Select(q => float.Parse(q)).ToArray();
        return new Vector3(-coordinates[1], coordinates[2], coordinates[0]);
    }


    /// <summary>
    /// Convert a .urdf rpy string to Vector3 Euler angles in Unity coordinates.
    /// </summary>
    /// <param name="rpy">A string of rpy coordinates in a .urdf file.</param>
    public static Vector3 rpyToVector3(this string rpy)
    {
        float[] coordinates = rpy.Split(' ').Select(q => float.Parse(q)).ToArray();
        return new Vector3(
            coordinates[1] * Mathf.Rad2Deg,
            -coordinates[2] * Mathf.Rad2Deg,
            -coordinates[0] * Mathf.Rad2Deg);
    }


    /// <summary>
    /// Convert a .urdf xyz axis string to a Vector3 position in Unity coordinates.
    /// </summary>
    /// <param name="axis">A string of axis coordinates in a .urdf file.</param>
    public static Vector3 axisToVector3(this string axis)
    {
        float[] a = axis.Split(' ').Select(q => float.Parse(q)).ToArray();
        return new Vector3(a[0], a[1], a[2]);
    }


    /// <summary>
    /// Transform and align a child to a parent object.
    /// </summary>
    /// <param name="transform">(this)</param>
    /// <param name="parent">The parent object.</param>
    public static void SetParentAndAlign(this Transform transform, Transform parent)
    {
        Vector3 localPosition = transform.localPosition;
        Quaternion localRotation = transform.localRotation;
        transform.parent = parent;
        transform.position = transform.parent.position + localPosition;
        transform.rotation = transform.parent.rotation * localRotation;
    }
}