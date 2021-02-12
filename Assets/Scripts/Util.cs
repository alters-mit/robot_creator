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