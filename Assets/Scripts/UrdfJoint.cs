using System.Xml;
using UnityEngine;
using TDW.Robotics;


/// <summary>
/// Joint data from a node in a .urdf file.
/// </summary>
public struct UrdfJoint
{
    /// <summary>
    /// Default value for a velocity limit.
    /// </summary>
    private const float DEFAULT_VELOCITY_LIMIT = 2;


    /// <summary>
    /// The name of the joint.
    /// </summary>
    public string name;
    /// <summary>
    /// The local position of the joint in Unity coordinates.
    /// </summary>
    public Vector3 position;
    /// <summary>
    /// The local Euler angles of the joint in Unity coordinates.
    /// </summary>
    public Vector3 rotation;
    /// <summary>
    /// The type of joint.
    /// </summary>
    public ArticulationJointType type;
    /// <summary>
    /// The drive axes.
    /// </summary>
    public DriveAxis[] axes;
    /// <summary>
    /// The name of the parent link.
    /// </summary>
    public string parent;
    /// <summary>
    /// The name of the child link.
    /// </summary>
    public string child;
    /// <summary>
    /// The lower motion limit.
    /// </summary>
    public float lowerLimit;
    /// <summary>
    /// The upper motion limit.
    /// </summary>
    public float upperLimit;
    /// <summary>
    /// If true, this joint has limits.
    /// </summary>
    public bool hasLimits;
    /// <summary>
    /// The velocity limit of the joint.
    /// </summary>
    public float velocityLimit;


    public UrdfJoint(XmlNode node)
    {
        name = node.Attributes["name"].Value;
        parent = node.SelectSingleNode("parent").Attributes["link"].Value;
        child = node.SelectSingleNode("child").Attributes["link"].Value;

        XmlNode origin = node.SelectSingleNode("origin");
        position = origin.Attributes["xyz"].Value.xyzToVector3();
        rotation = origin.Attributes["rpy"].Value.rpyToVector3();

        string t = node.Attributes["type"].Value.ToLower();
        // This is a fixed joint.
        if (t == "fixed")
        {
            type = ArticulationJointType.FixedJoint;
            lowerLimit = 0;
            upperLimit = 0;
            hasLimits = false;
            axes = new DriveAxis[0];
            velocityLimit = 0;
            return;
        }

        if (t == "continuous" || t == "revolute" || t == "prismatic")
        {
            axes = new DriveAxis[1];
            Vector3 axis = node.SelectSingleNode("axis").Attributes["xyz"].Value.axisToVector3();
            // Get the drive axis for a single-axis rotation.
            if (axis.x != 0)
            {
                axes[0] = DriveAxis.x;
            }
            else if (axis.y != 0)
            {
                axes[0] = DriveAxis.y;
            }
            else if (axis.z != 0)
            {
                axes[0] = DriveAxis.z;
            }
            else
            {
                throw new System.Exception("No axis for: " + name);
            }

            // Continuous joints don't have limits.,
            if (t == "continuous")
            {
                hasLimits = false;
                lowerLimit = 0;
                upperLimit = 0;
                type = ArticulationJointType.RevoluteJoint;
                velocityLimit = DEFAULT_VELOCITY_LIMIT;
            }
            // Prismatic and revolute joints have limits.
            else
            {
                hasLimits = true;
                XmlNode limits = node.SelectSingleNode("limit");
                lowerLimit = float.Parse(limits.Attributes["lower"].Value);
                upperLimit = float.Parse(limits.Attributes["upper"].Value);
                velocityLimit = float.Parse(limits.Attributes["velocity"].Value);
                if (t == "revolute")
                {
                    type = ArticulationJointType.RevoluteJoint;
                }
                else
                {
                    type = ArticulationJointType.PrismaticJoint;
                }
            }
        }
        // Floating joints don't have limits and have 3 drive axes.
        else if (t == "floating")
        {
            type = ArticulationJointType.SphericalJoint;
            lowerLimit = 0;
            upperLimit = 0;
            hasLimits = false;
            axes = new DriveAxis[] { DriveAxis.x, DriveAxis.y, DriveAxis.z };
            velocityLimit = DEFAULT_VELOCITY_LIMIT;
        }
        // TODO planar joints.
        else
        {
            throw new System.Exception("Joint type not supported: " + t);
        }
    }
}