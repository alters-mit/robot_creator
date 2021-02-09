using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using TDW.Robotics;


/// <summary>
/// Create a robot prefab.
/// </summary>
public static class PrefabCreator
{
    /// <summary>
    /// The directory for the prefab.
    /// </summary>
    private const string DIR_PREFAB = "Assets/prefabs/";


    /// <summary>
    /// Test prefab creation with the Sawyer robot.
    /// </summary>
    public static void TestSawyer()
    {
        string path = Path.Combine(Application.dataPath, "robots/sawyer/sawyer.urdf");
        UrdfToPrefab(path, true);
    }


    /// <summary>
    /// Test prefab creation with the Baxter robot.
    /// </summary>
    public static void TestBaxter()
    {
        string path = Path.Combine(Application.dataPath, "robots/baxter/baxter.urdf");
        UrdfToPrefab(path, true);
    }


    /// <summary>
    /// Create a robot prefab from a .urdf file.
    /// </summary>
    /// <param name="path">The absolute path to the .urdf file.</param>
    /// <param name="immovable">If true, this robot is immovable.</param>
    private static void UrdfToPrefab(string path, bool immovable)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(path);
        XmlNode root = doc.SelectSingleNode("robot");
        string robotName = root.Attributes["name"].Value;

        // Create materials and store their paths.
        string materialDirectory = "robots/" + robotName + "/materials";
        string absoluteMaterialDirectory = Path.Combine(Application.dataPath, materialDirectory);
        if (!Directory.Exists(absoluteMaterialDirectory))
        {
            Directory.CreateDirectory(absoluteMaterialDirectory);
        }
        Dictionary<string, string> materials = new Dictionary<string, string>();
        Material defaultMaterial = GetDefaultMaterial();
        string defaultMaterialPath = "Assets/" + materialDirectory + "/default.mat";
        AssetDatabase.CreateAsset(defaultMaterial, defaultMaterialPath);
        materials.Add("default", defaultMaterialPath);
        XmlNodeList materialNodes = root.SelectNodes("material");
        for (int i = 0; i < materialNodes.Count; i++)
        {
            float[] rgba = materialNodes[i].SelectSingleNode("color").Attributes["rgba"].
                Value.Split(' ').Select(q => float.Parse(q)).ToArray();
            Material material = GetDefaultMaterial();
            material.color = new Color(rgba[0], rgba[1], rgba[2], rgba[3]);
            string materialName = materialNodes[i].Attributes["name"].Value;
            string materialPath = "Assets/" + materialDirectory + "/" + materialName + ".mat";
            AssetDatabase.CreateAsset(material, materialPath);
            materials.Add(materialName, materialPath);
        }

        XmlNodeList linkNodes = root.SelectNodes("link");
        // Get all of the links. Key = the name of the link.
        Dictionary<string, UrdfLink> links = new Dictionary<string, UrdfLink>();
        for (int i = 0; i < linkNodes.Count; i++)
        {
            UrdfLink link = new UrdfLink(linkNodes[i]);
            links.Add(link.name, link);
        }
        
        // Get each of the joints.
        XmlNodeList jointNodes = root.SelectNodes("joint");
        HashSet<string> children = new HashSet<string>();
        Dictionary<UrdfLink, UrdfJoint> joints = new Dictionary<UrdfLink, UrdfJoint>(new UrdfLinkComparer());
        for (int i = 0; i < jointNodes.Count; i++)
        {
            UrdfJoint joint = new UrdfJoint(jointNodes[i]);
            // Match the joint to its parent link.
            joints.Add(links[joint.child], joint);
            children.Add(joint.child);
        }

        // Get the root object.
        Queue<string> nodes = new Queue<string>();
        string rootName = links.Keys.Where(li => !children.Contains(li)).First();
        // Create objects from each node.
        nodes.Enqueue(rootName);
        while (nodes.Count > 0)
        {
            GameObject go = new GameObject();
            string name = nodes.Dequeue();
            go.name = name;
            UrdfLink link = links[name];
            if (joints.ContainsKey(link))
            {
                UrdfJoint joint = joints[link];

                //go.transform.SetParentAndAlign(GameObject.Find(joint.parent).transform);
                go.transform.parent = GameObject.Find(joint.parent).transform;
                go.transform.localPosition = joint.position;
                go.transform.localEulerAngles = joint.rotation;

                if (link.hasMesh)
                {
                    // Get the mesh.
                    string p = Path.Combine("Assets/robots/", robotName, link.meshPath);
                    p = Regex.Replace(p, "package://", "");

                    Vector3 prefabEulerAngles = AssetDatabase.LoadAssetAtPath<GameObject>(p).transform.eulerAngles;
                    GameObject mesh = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(p));

                    mesh.transform.SetParentAndAlign(go.transform);
                    mesh.transform.localPosition = link.meshPosition;
                    mesh.transform.localEulerAngles = new Vector3(-90, 0, 90) - link.meshRotation;
                    mesh.name = "visual";

                    // Get the colliders.
                    string collidersPath = p.Substring(0, p.Length - 4) + ".obj";
                    GameObject colliders = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(collidersPath));
                    colliders.transform.parent = go.transform;
                    colliders.transform.localPosition = link.meshPosition;

                    if (prefabEulerAngles.Equals(Vector3.zero))
                    {
                        colliders.transform.localEulerAngles = mesh.transform.localEulerAngles;
                    }
                    else
                    {
                        colliders.transform.localEulerAngles = new Vector3(0, 90, 0);
                    }

                    colliders.name = "colliders";
                    foreach (MeshFilter mf in colliders.GetComponentsInChildren<MeshFilter>())
                    {
                        MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = true;
                        Object.DestroyImmediate(mf);
                    }

                    p = Path.Combine("robots/", robotName, "meshes", link.meshPath);

                    ArticulationBody a = go.AddComponent<ArticulationBody>();
                    a.mass = link.mass;
                    a.jointType = joint.type;
                    // Assign drive values.
                    foreach (DriveAxis da in joint.axes)
                    {
                        ArticulationDrive drive;
                        if (da == DriveAxis.x)
                        {
                            drive = a.xDrive;
                        }
                        else if (da == DriveAxis.y)
                        {
                            drive = a.yDrive;
                        }
                        else
                        {
                            drive = a.zDrive;
                        }
                        drive.forceLimit = joint.velocityLimit;
                        drive.stiffness = 1000;
                        drive.damping = 180;
                        if (joint.hasLimits)
                        {
                            drive.lowerLimit = joint.lowerLimit;
                            drive.upperLimit = joint.upperLimit;
                        }
                        if (da == DriveAxis.x)
                        {
                            a.xDrive = drive;
                        }
                        else if (da == DriveAxis.y)
                        {
                            a.yDrive = drive;
                        }
                        else
                        {
                            a.zDrive = drive;
                        }
                    }
                    if (a.isRoot)
                    {
                        a.immovable = immovable;
                    }

                    // Set the materials.
                    foreach (MeshRenderer mr in mesh.GetComponentsInChildren<MeshRenderer>())
                    {
                        string m;
                        if (!materials.ContainsKey(link.material))
                        {
                            m = "default";
                        }
                        else
                        {
                            m = link.material;
                        }
                        mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materials[m]);
                    }
                }
            }
            // Get the children.
            foreach (string k in links.Keys)
            {
                if (!joints.ContainsKey(links[k]) || joints[links[k]].name == name)
                {
                    continue;
                }
                if (joints[links[k]].parent == name)
                {
                    nodes.Enqueue(joints[links[k]].child);
                }
            }
        }

        // Rename the robot.
        GameObject robot = GameObject.Find(rootName);
        robot.name = robotName;

        // Create the prefab directory if it doesn't exist.
        string absolutePrefabPath = Path.Combine(Application.dataPath, "prefabs");
        if (!Directory.Exists(absolutePrefabPath))
        {
            Directory.CreateDirectory(absolutePrefabPath);
        }

        // Remove garbage objects.
        foreach (Light light in robot.GetComponentsInChildren<Light>())
        {
            Object.DestroyImmediate(light.gameObject);
        }
        foreach (Camera camera in robot.GetComponentsInChildren<Camera>())
        {
            Object.DestroyImmediate(camera.gameObject);
        }


        // Create the prefab.
        PrefabUtility.SaveAsPrefabAsset(robot, DIR_PREFAB + robot.name + ".prefab");
        Object.DestroyImmediate(robot);
    }


    /// <summary>
    /// Returns the default material for this robot.
    /// </summary>
    private static Material GetDefaultMaterial()
    {
        Material material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Metallic", 0.75f);
        material.SetFloat("_Glossiness", 0.75f);
        material.color = new Color(0.33f, 0.33f, 0.33f, 0.0f);
        return material;
    }
}