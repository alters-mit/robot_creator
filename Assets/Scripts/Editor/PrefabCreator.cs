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
    [MenuItem("Tests/Sawyer")]
    public static void SawyerTest()
    {
        string path = Path.Combine(Application.dataPath, "robots/sawyer/sawyer.urdf");
        UrdfToPrefab(path, true);
    }


    private static void UrdfToPrefab(string path, bool immovable)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(path);
        XmlNode root = doc.SelectSingleNode("robot");
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
        string robotName = root.Attributes["name"].Value;
        while (nodes.Count > 0)
        {
            GameObject go = new GameObject();
            string name = nodes.Dequeue();
            go.name = name;
            UrdfLink link = links[name];
            if (joints.ContainsKey(link))
            {
                UrdfJoint joint = joints[link];
                go.transform.parent = GameObject.Find(joint.parent).transform;
                go.transform.localPosition = joint.position;
                go.transform.localEulerAngles = joint.rotation;
                if (link.hasMesh)
                {
                    // Get the mesh.
                    string p = Path.Combine("Assets/robots/", robotName, link.meshPath);
                    p = Regex.Replace(p, "package://", "");

                    GameObject mesh = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(p));
                    mesh.transform.parent = go.transform;
                    mesh.transform.localPosition = link.meshPosition;
                    mesh.transform.localEulerAngles = link.meshRotation;
                    mesh.name = "visual";

                    // Get the colliders.
                    string collidersPath = p.Substring(0, p.Length - 4) + ".obj";

                    GameObject colliders = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(collidersPath));
                    colliders.transform.parent = go.transform;
                    colliders.transform.localPosition = link.meshPosition;
                    colliders.transform.localEulerAngles = link.meshRotation;
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
    }
}