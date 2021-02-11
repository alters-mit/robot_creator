using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;


/// <summary>
/// Import the ROS package if it doesn't already exist.
/// </summary>
public static class RosImporter
{
    /// <summary>
    /// Check if the URDF package has been added to the project. If not, add it.
    /// </summary>
    public static void Import()
    {
        // List all of the packages.
        ListRequest packages = Client.List(offlineMode: true);
        while (packages.Status == StatusCode.InProgress)
        {

        }
        Debug.Assert(packages.Status == StatusCode.Success, "Failed to get a list of packages.");
        // Check if the URDF importer is already installed.
        bool importerInstalled = false;
        foreach (PackageInfo package in packages.Result)
        {
            if (package.assetPath == "Packages/com.unity.robotics.urdf-importer")
            {
                importerInstalled = true;
                Debug.Log("URDF importer already installed.");
                break;
            }
        }
        // Import the ROS package.
        if (!importerInstalled)
        {
            AddRequest getPackage = Client.Add("https://github.com/Unity-Technologies/URDF-Importer.git#v0.1.2");
            while (getPackage.Status == StatusCode.InProgress)
            {

            }
            if (getPackage.Status == StatusCode.Success)
            {
                Debug.Log("Added the URDF importer package.");
            }
            else
            {
                Debug.LogError("Failed to add the URDF importer package.");
            }
        }
    }
}