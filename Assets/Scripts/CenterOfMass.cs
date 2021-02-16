using UnityEngine;


namespace TDW.Robotics
{
    /// <summary>
    /// Set the center of mass of an ArticulationBody at runtime.
    /// </summary>
    [RequireComponent(typeof(ArticulationBody))]
    public class CenterOfMass : MonoBehaviour
    {
        /// <summary>
        /// The center of mass offset from the pivot position.
        /// </summary>
        public Vector3 centerOfMass = Vector3.zero;


        private void Awake()
        {
            GetComponent<ArticulationBody>().centerOfMass = centerOfMass;
        }
    }
}