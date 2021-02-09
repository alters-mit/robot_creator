using System.IO;
using UnityEngine;
using UnityEditor;
using RosSharp.Urdf.Editor;
using RosSharp;
using RosSharp.Urdf;


/// <summary>
/// Create robots from .urdf files.
/// </summary>
public static class Creator
{
    /// <summary>
    /// Test prefab creation with the Baxter robot.
    /// </summary>
    [MenuItem("Tests/Baxter")]
    public static void TestBaxter()
    {
        string path = Path.Combine(Application.dataPath, "robots/baxter/baxter.urdf");
        ImportSettings settings = new ImportSettings
        {
            choosenAxis = ImportSettings.axisType.yAxis,
            convexMethod = ImportSettings.convexDecomposer.vHACD
        };
        CreatePrefab(path, settings, true);
    }


    /// <summary>
    /// Test prefab creation with the Baxter robot.
    /// </summary>
    [MenuItem("Tests/Sawyer")]
    public static void TestSawyer()
    {
        string path = Path.Combine(Application.dataPath, "robots/sawyer/sawyer.urdf");
        ImportSettings settings = new ImportSettings
        {
            choosenAxis = ImportSettings.axisType.yAxis,
            convexMethod = ImportSettings.convexDecomposer.vHACD
        };
        CreatePrefab(path, settings, true);
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
        GameObject robotRoot = Object.FindObjectOfType<UrdfRobot>().gameObject;
        DestroyAll<RosSharp.Urdf.UrdfJoint> (robotRoot);
        DestroyAll<UrdfInertial>(robotRoot);
        DestroyAll<UrdfVisuals>(robotRoot);
        DestroyAll<UrdfVisual>(robotRoot);
        DestroyAll<UrdfCollisions>(robotRoot);
        DestroyAll<UrdfCollision>(robotRoot);
        DestroyAll<RosSharp.Urdf.UrdfLink>(robotRoot);
        DestroyAll<UrdfRobot>(robotRoot);

        // Fix the articulation drives.
        foreach (ArticulationBody a in robotRoot.GetComponentsInChildren<ArticulationBody>())
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
                a.name = robotRoot.name;
                a.transform.parent = null;
            }
        }
        Object.DestroyImmediate(robotRoot.gameObject);
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