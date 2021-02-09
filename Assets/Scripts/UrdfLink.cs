using System.Xml;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Link data from a node in a .urdf file.
/// </summary>
public struct UrdfLink
{
    /// <summary>
    /// The name of the default material.
    /// </summary>
    private const string DEFAULT_MATERIAL = "default";


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
    /// <summary>
    /// The local position of the joint in Unity coordinates.
    /// </summary>
    public Vector3 position;
    /// <summary>
    /// The local Euler angles of the joint in Unity coordinates.
    /// </summary>
    public Vector3 rotation;
    /// <summary>
    /// The name of the material.
    /// </summary>
    public string material;


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
            position = default;
            rotation = default;
            material = DEFAULT_MATERIAL;
            return;
        }
        XmlNode origin = inertial.SelectSingleNode("origin");
        position = origin.Attributes["xyz"].Value.xyzToVector3();
        rotation = origin.Attributes["rpy"].Value.rpyToVector3();
        camera = false;
        mass = float.Parse(inertial.SelectSingleNode("mass").Attributes["value"].Value);
        XmlNode visual = node.SelectSingleNode("visual");
        if (visual == null)
        {
            hasMesh = false;
            meshPath = "";
            meshPosition = default;
            meshRotation = default;
            material = DEFAULT_MATERIAL;
            return;
        }

        material = visual.SelectSingleNode("material").Attributes["name"].Value;
        XmlNode mesh = visual.SelectSingleNode("geometry/mesh");
        // Some nodes don't have meshes.
        hasMesh = mesh != null;
        if (hasMesh)
        {
            meshPath = mesh.Attributes["filename"].Value;
            // Get the position of the mesh.
            XmlNode meshOrigin = visual.SelectSingleNode("origin");

            float[] xyz = meshOrigin.Attributes["xyz"].Value.Split(' ').Select(q => float.Parse(q)).ToArray();
            meshPosition = new Vector3(-xyz[1], xyz[2], xyz[0]);
            float[] rpy = meshOrigin.Attributes["rpy"].Value.Split(' ').Select(q => float.Parse(q)).ToArray();
            meshRotation = new Vector3(rpy[0] * Mathf.Rad2Deg, rpy[1] * Mathf.Rad2Deg, rpy[2] * Mathf.Rad2Deg);
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