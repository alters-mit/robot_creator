using System.Linq;
using System.Collections.Generic;
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


    /// <summary>
    /// Returns all descendants of a parent transform with a given name.
    /// </summary>
    /// <param name="aName">Name of the descendant.</param>
    public static HashSet<Transform> FindDeepChildren(this Transform aParent, string aName)
    {
        HashSet<Transform> children = new HashSet<Transform>();
        foreach (Transform t in aParent.GetComponentsInChildren<Transform>())
        {
            if (t.name == aName)
            {
                children.Add(t);
            }
        }
        return children;
    }


    /// <summary>
    /// Returns a descendants of a parent transform with a given name.
    /// </summary>
    /// <param name="aName">Name of the descendant.</param>
    public static Transform FindDeepChild(this Transform aParent, string aName)
    {
        foreach (Transform t in aParent.GetComponentsInChildren<Transform>())
        {
            if (t.name == aName)
            {
                return t;
            }
        }
        return null;
    }


    /// <summary>
    /// Destroy all objects attached to the root object of a given name.
    /// </summary>
    /// <param name="go">(this)</param>
    /// <param name="name">The name of the object(s) to destroy.</param>
    public static void DestroyAllObjects(this GameObject go, string name)
    {
        foreach (Transform t in go.transform.FindDeepChildren(name))
        {
            Object.DestroyImmediate(t.gameObject);
        }
    }



    /// <summary>
    /// Destroy all GameObjects with a component of type T attached to a root object.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="go">The root object.</param>
    public static void DestroyAllComponents<T>(this GameObject go)
        where T : Component
    {
        foreach (T t in go.GetComponentsInChildren<T>())
        {
            Object.DestroyImmediate(t.gameObject);
        }
    }


    /// <summary>
    /// Destroy all components of type T attached to children of a root object.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="go">The roobt object.</param>
    public static void DestroyAll<T>(this GameObject go)
        where T : MonoBehaviour
    {
        foreach (T t in go.GetComponentsInChildren<T>())
        {
            Object.DestroyImmediate(t);
        }
    }
}