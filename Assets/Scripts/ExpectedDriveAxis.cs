using UnityEngine;


namespace TDW.Robotics
{
    /// <summary>
    /// Revolute and prismatic ArticulationBodies have a single drive but there isn't a direct way to know which one it is.
    /// This component will tell the CachedArticulationBody which drive to expect.
    /// </summary>
    public class ExpectedDriveAxis : MonoBehaviour
    {
        /// <summary>
        /// The expected drive axis of a revolute or prismatic joint.
        /// </summary>
        public DriveAxis axis = DriveAxis.x;
    }
}