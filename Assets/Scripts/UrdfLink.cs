using System.Xml;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;


/// <summary>
/// Link data from a node in a .urdf file.
/// </summary>
public struct UrdfLink
{
    /// <summary>
    /// The path to the mesh file.
    /// </summary>
    public string meshPath;
    /// <summary>
    /// The mass of the object.
    /// </summary>
    public float mass;
    /// <summary>
    /// The name of the object.
    /// </summary>
    public string name;
    /// <summary>
    /// If true, this link has a mesh.
    /// </summary>
    public bool hasMesh;
    /// <summary>
    /// The local position of the visual mesh and the colliders.
    /// </summary>
    public Vector3 meshPosition;
    /// <summary>
    /// The local rotation in Unity Euler angles of the visual mesh and the colliders.
    /// </summary>
    public Vector3 meshRotation;
    /// <summary>
    /// If true, this is a camera link.
    /// </summary>
    public bool camera;


    public UrdfLink(XmlNode node)
    {
        name = node.Attributes["name"].Value;
        XmlNode inertial = node.SelectSingleNode("inertial");
        if (inertial == null)
        {
            mass = 0;
            camera = true;
            hasMesh = false;
            meshPath = "";
            meshPosition = default;
            meshRotation = default;
            return;
        }
        camera = false;
        mass = float.Parse(inertial.SelectSingleNode("mass").Attributes["value"].Value);
        XmlNode visual = node.SelectSingleNode("visual");
        if (visual == null)
        {
            hasMesh = false;
            meshPath = "";
            meshPosition = default;
            meshRotation = default;
            return;
        }
        XmlNode mesh = visual.SelectSingleNode("geometry/mesh");
        // Some nodes don't have meshes.
        hasMesh = mesh != null;
        if (hasMesh)
        {
            meshPath = mesh.Attributes["filename"].Value;
            // Get the position of the mesh.
            XmlNode meshOrigin = visual.SelectSingleNode("origin");
            meshPosition = meshOrigin.Attributes["xyz"].Value.xyzToVector3();
            meshRotation = meshOrigin.Attributes["rpy"].Value.rpyToVector3();
        }
        else
        {
            meshPath = "";
            meshPosition = default;
            meshRotation = default;
        }
    }
}


/// <summary>
/// Comparer for UrdfLink.
/// </summary>
public struct UrdfLinkComparer: IEqualityComparer<UrdfLink>
{
    public bool Equals(UrdfLink a, UrdfLink b)
    {
        return a.name == b.name && a.meshPath == b.meshPath;
    }


    public int GetHashCode(UrdfLink a)
    {
        return a.GetHashCode();
    }
}