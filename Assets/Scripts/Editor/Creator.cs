using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using RosSharp.Urdf.Editor;
using RosSharp;
using RosSharp.Urdf;
using RosSharp.Control;


/// <summary>
/// Create robots from .urdf files.
/// </summary>
public static class Creator
{
    /// <summary>
    /// The directory for the prefab.
    /// </summary>
    private const string DIR_PREFAB = "Assets/prefabs/";


    /// <summary>
    /// Create a prefab from a .urdf file. The .urdf file and its meshes must be already downloaded.
    /// Expected command line arguments:
    /// 
    /// -urdf=path (Path to the .urdf file. Required.)
    /// -immovable=true (Whether or not the robot root is immovable. Options: true, false. Default: true)
    /// -up=y (The up axis. Options: y, z. Default: y)
    /// -convex=vhacd (The convex decomposer. Options: unity, vhacd. Default: vhacd)
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
        // Get the convex decomposer.
        string c = GetStringValue(args, "convex", defaultValue: "vhacd");
        ImportSettings.convexDecomposer convex;
        if (c == "unity")
        {
            convex = ImportSettings.convexDecomposer.unity;
        }
        else if (c == "vhacd")
        {
            convex = ImportSettings.convexDecomposer.vHACD;
        }
        else
        {
            Debug.LogWarning("Invalid -convex value: " + c + " Using vhacd instead.");
            convex = ImportSettings.convexDecomposer.vHACD;
        }

        // Create the prefab.
        ImportSettings settings = new ImportSettings
        {
            choosenAxis = axis,
            convexMethod = convex
        };
        CreatePrefab(urdfPath, settings, immovable);
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

        // Remove ROS garbage.
        GameObject robot = Object.FindObjectOfType<UrdfRobot>().gameObject;
        DestroyAll<RosSharp.Urdf.UrdfJoint> (robot);
        DestroyAll<UrdfInertial>(robot);
        DestroyAll<UrdfVisuals>(robot);
        DestroyAll<UrdfVisual>(robot);
        DestroyAll<UrdfCollisions>(robot);
        DestroyAll<UrdfCollision>(robot);
        DestroyAll<RosSharp.Urdf.UrdfLink>(robot);
        DestroyAll<UrdfRobot>(robot);
        DestroyAll<Controller>(robot);

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
            a.anchorPosition = Vector3.zero;
            // Set the root articulation body as the root object.
            if (a.isRoot)
            {
                a.immovable = immovable;
            }
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
        foreach (UrdfPlugins plugins in robot.GetComponentsInChildren<UrdfPlugins>())
        {
            Object.DestroyImmediate(plugins.gameObject);
        }

        // Create the prefab directory if it doesn't exist.
        string absolutePrefabPath = Path.Combine(Application.dataPath, "prefabs");
        if (!Directory.Exists(absolutePrefabPath))
        {
            Directory.CreateDirectory(absolutePrefabPath);
        }

        // Create the prefab.
        PrefabUtility.SaveAsPrefabAsset(robot, DIR_PREFAB + robot.name + ".prefab");
        // Destroy the object in the scene.
       // Object.DestroyImmediate(robot);
    }


    /// <summary>
    /// Destroy all components of type T on the robot.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="go">The robot.</param>
    private static void DestroyAll<T>(GameObject go)
        where T: MonoBehaviour
    {
        foreach (T t in go.GetComponentsInChildren<T>())
        {
            Object.DestroyImmediate(t);
        }
    }
}