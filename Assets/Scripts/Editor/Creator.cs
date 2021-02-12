using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using RosSharp.Urdf.Editor;
using RosSharp;
using RosSharp.Urdf;
using RosSharp.Control;
using TDW.Robotics;


/// <summary>
/// Create robots from .urdf files.
/// </summary>
public static class Creator
{
    [MenuItem("Tests/Sawyer")]
    public static void Sawyer()
    {
        string path = Path.Combine(Application.dataPath, "robots/sawyer.urdf");
        // Create the prefab.
        ImportSettings settings = new ImportSettings
        {
            choosenAxis = ImportSettings.axisType.yAxis,
            convexMethod = ImportSettings.convexDecomposer.vHACD
        };
        CreatePrefab(path, settings, true);
    }


    /// <summary>
    /// Create a prefab from a .urdf file. The .urdf file and its meshes must be already downloaded.
    /// Expected command line arguments:
    /// 
    /// -urdf=string_path (Path to the .urdf file. Required.)
    /// -immovable=true (Whether the robot root is immovable. Options: true, false. Default: true)
    /// -up=y (The up axis. Options: y, z. Default: y)
    /// </summary>
    public static void CreatePrefab()
    {
        string[] args = Environment.GetCommandLineArgs();
        string urdfPath = GetStringValue(args, "urdf");
        if (urdfPath == null)
        {
            Debug.LogError("No -urdf argument provided.");
        }
        bool immovable = GetBooleanValue(args, "immovable", defaultValue: true);

        // Get the axis.
        string up = GetStringValue(args, "up");
        ImportSettings.axisType axis;
        if (up == null)
        {
            axis = ImportSettings.axisType.yAxis;  
        }
        else if (up == "y")
        {
            axis = ImportSettings.axisType.yAxis;
        }
        else if (up == "z")
        {
            axis = ImportSettings.axisType.zAxis;
        }
        else
        {
            Debug.LogWarning("Invalid -up value: " + up + " Using y instead.");
            axis = ImportSettings.axisType.yAxis;
        }

        // Create the prefab.
        ImportSettings settings = new ImportSettings
        {
            choosenAxis = axis,
            convexMethod = ImportSettings.convexDecomposer.vHACD
        };
        CreatePrefab(urdfPath, settings, immovable);
    }


    /// <summary>
    /// Create asset bundles from an existing prefab.
    /// Expected command line arguments:
    /// 
    /// -robot=string (The name of the robot; must match the name of the prefab. Required.)
    /// </summary>
    public static void CreateAssetBundles()
    {
        string[] args = Environment.GetCommandLineArgs();
        string name = GetStringValue(args, "robot");
        if (name == null)
        {
            Debug.LogError("Expected -robot argument but didn't find one.");
            return;
        }
        CreateAssetBundles(name);
    }


    /// <summary>
    /// Return the string value of an argument, given this format: -flag=value
    /// </summary>
    /// <param name="args">All environment args.</param>
    /// <param name="flag">The flag.</param>
    /// <param name="defaultValue">The default value.</param>
    private static string GetStringValue(string[] args, string flag, string defaultValue = null)
    {
        if (flag[0] != '-')
        {
            flag = "-" + flag;
        }
        foreach (string arg in args)
        {
            if (arg.StartsWith(flag))
            {
                string[] kv = arg.Split('=');
                return Regex.Replace(kv[1], "'", "");
            }
        }
        return defaultValue;
    }


    /// <summary>
    /// Return the boolean value of an argument, given this format: -flag=value
    /// </summary>
    /// <param name="args">All environment args.</param>
    /// <param name="flag">The flag.</param>
    /// <param name="defaultValue">The default value.</param>
    private static bool GetBooleanValue(string[] args, string flag, bool defaultValue = false)
    {
        string v = GetStringValue(args, flag);
        if (v == null)
        {
            return defaultValue;
        }
        else if (v.ToLower() == "false")
        {
            return false;
        }
        else if (v.ToLower() == "true")
        {
            return true;
        }
        else
        {
            return defaultValue;
        }
    }


    /// <summary>
    /// Create a prefab from a .urdf file.
    /// </summary>
    /// <param name="urdfPath">The path to the .urdf file.</param>
    /// <param name="settings">Import settings for ROS.</param>
    /// <param name="immovable">If true, the root object is immovable.</param>
    private static void CreatePrefab(string urdfPath, ImportSettings settings, bool immovable)
    {
        // Import the robot.
        UrdfRobotExtensions.Create(urdfPath, settings);

        // Remove irrelevant ROS components.
        GameObject robot = Object.FindObjectOfType<UrdfRobot>().gameObject;
        robot.DestroyAll<UrdfJoint>();
        robot.DestroyAll<UrdfInertial>();
        robot.DestroyAll<UrdfVisuals>();
        robot.DestroyAll<UrdfVisual>();
        robot.DestroyAll<UrdfCollisions>();
        robot.DestroyAll<UrdfCollision>();
        robot.DestroyAll<UrdfLink>();
        robot.DestroyAll<UrdfRobot>();
        robot.DestroyAll<Controller>();
        // Destroy unwanted objects.
        robot.DestroyAllComponents<Light>();
        robot.DestroyAllComponents<Camera>();
        robot.DestroyAllComponents<UrdfPlugins>();

        // Save the generated collider meshes.
        string collidersDirectory = Path.Combine(Application.dataPath, "collider_meshes", robot.name);
        if (!Directory.Exists(collidersDirectory))
        {
            Directory.CreateDirectory(collidersDirectory);
        }
        foreach (MeshCollider mc in robot.GetComponentsInChildren<MeshCollider>())
        {
            mc.sharedMesh.name = Random.Range(0, 1000000).ToString();
            AssetDatabase.CreateAsset(mc.sharedMesh, "Assets/collider_meshes/" + robot.name + "/" + mc.sharedMesh.name);
            AssetDatabase.SaveAssets();
        }

        // Load the .urdf file.
        XmlDocument doc = new XmlDocument();
        doc.Load(urdfPath);
        XmlNode xmlRoot = doc.SelectSingleNode("robot");

        // Get the prismatic joints.
        XmlNodeList jointNodes = xmlRoot.SelectNodes("joint");
        // The name of each prismatic joint and its axis.
        Dictionary<string, DriveAxis> prismaticAxes = new Dictionary<string, DriveAxis>();
        for (int i = 0; i < jointNodes.Count; i++)
        {
            string jointType = jointNodes[i].Attributes["type"].Value.ToLower();
            if (jointType == "prismatic")
            {
                string jointChild = jointNodes[i].SelectSingleNode("child").Attributes["link"].Value;
                if (prismaticAxes.ContainsKey(jointChild))
                {
                    continue;
                }
                Vector3 xyz = jointNodes[i].SelectSingleNode("axis").Attributes["xyz"].Value.xyzToVector3();
                DriveAxis driveAxis;
                // Get the drive axis for a single-axis rotation.
                if (xyz.x != 0)
                {
                    driveAxis = DriveAxis.x;
                }
                else if (xyz.y != 0)
                {
                    driveAxis = DriveAxis.y;
                }
                else if (xyz.z != 0)
                {
                    driveAxis = DriveAxis.z;
                }
                else
                {
                    throw new System.Exception("No axis for: " + jointChild);
                }
                prismaticAxes.Add(jointChild, driveAxis);
            }
        }

        // Fix the articulation drives.;
        foreach (ArticulationBody a in robot.GetComponentsInChildren<ArticulationBody>())
        {
            if (a.jointType == ArticulationJointType.RevoluteJoint)
            {
                ArticulationDrive drive = a.xDrive;
                drive.stiffness = 1000;
                drive.damping = 180;
                a.xDrive = drive;
            }
            // Set the prismatic joint's drive values and expected axis.
            else if (a.jointType == ArticulationJointType.PrismaticJoint)
            {
                DriveAxis da = prismaticAxes[a.name];
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
                drive.forceLimit = a.xDrive.forceLimit;
                drive.stiffness = 1000;
                drive.damping = 180;
                drive.lowerLimit = a.xDrive.lowerLimit;
                drive.upperLimit = a.xDrive.upperLimit;
                if (da == DriveAxis.x)
                {
                    a.linearLockX = ArticulationDofLock.LimitedMotion;
                    a.linearLockY = ArticulationDofLock.LockedMotion;
                    a.linearLockZ = ArticulationDofLock.LockedMotion;
                    a.xDrive = drive;
                }
                else if (da == DriveAxis.y)
                {
                    a.linearLockX = ArticulationDofLock.LockedMotion;
                    a.linearLockY = ArticulationDofLock.LimitedMotion;
                    a.linearLockZ = ArticulationDofLock.LockedMotion;
                    a.yDrive = drive;
                }
                else
                {
                    a.linearLockX = ArticulationDofLock.LockedMotion;
                    a.linearLockY = ArticulationDofLock.LockedMotion;
                    a.linearLockZ = ArticulationDofLock.LimitedMotion;
                    a.zDrive = drive;
                }
                ExpectedDriveAxis eda = a.gameObject.AddComponent<ExpectedDriveAxis>();
                eda.axis = da;
            }
            a.anchorPosition = Vector3.zero;
            // Set the root articulation body as the root object.
            if (a.isRoot)
            {
                a.immovable = immovable;
            }
        }

        // Destroy redundant ArticulationBodies.
        ArticulationBody[] badBodies = robot.GetComponentsInChildren<ArticulationBody>().
            Where(a => !a.isRoot && a.mass < 0.01f && a.jointType == ArticulationJointType.FixedJoint &&
            a.GetComponentsInChildren<MeshFilter>().Length == 0).
            ToArray();
        foreach (ArticulationBody b in badBodies)
        {
            Object.DestroyImmediate(b.gameObject);
        }

        // Create the prefab directory if it doesn't exist.
        string absolutePrefabPath = Path.Combine(Application.dataPath, "prefabs");
        if (!Directory.Exists(absolutePrefabPath))
        {
            Directory.CreateDirectory(absolutePrefabPath);
        }

        // Create the prefab.
        PrefabUtility.SaveAsPrefabAsset(robot, "Assets/prefabs/" + robot.name + ".prefab");
    }


    /// <summary>
    /// Create asset bundles from a prefab.
    /// </summary>
    /// <param name="name">The name of the prefab.</param>
    private static void CreateAssetBundles(string name)
    {
        if (name.EndsWith(".prefab"))
        {
            name = Regex.Replace(name, @"\.prefab", "");
        }

        BuildTarget[] targets = new BuildTarget[] {
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneLinux64};
        AssetBundleBuild[] builds = new AssetBundleBuild[]
        {
            new AssetBundleBuild
            {
                assetBundleName = name,
                assetNames = new string[] { Path.Combine("Assets/prefabs", name + ".prefab") }
            }
        };

        string assetBundlesAbsoluteDirectory = Path.Combine(Application.dataPath, "asset_bundles/" + name);
        // Create the directory for all asset bundles.
        if (!Directory.Exists(assetBundlesAbsoluteDirectory))
        {
            Directory.CreateDirectory(assetBundlesAbsoluteDirectory);
        }
        // Create a new asset bundle for each target.
        foreach (BuildTarget target in targets)
        {
            string targetDirectory = Path.Combine(assetBundlesAbsoluteDirectory, target.ToString());
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            // Build the asset bundles.
            BuildPipeline.BuildAssetBundles(targetDirectory,
                builds,
                BuildAssetBundleOptions.None,
                target);
        }
    }
}