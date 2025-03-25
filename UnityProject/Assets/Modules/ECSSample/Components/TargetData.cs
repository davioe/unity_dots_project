using Unity.Entities;
using Unity.Mathematics;

namespace ECSSample
{
    /// <summary>
    /// Component that stores target data used for processing entity transforms.
    /// </summary>
    public struct TargetData : IComponentData
    {
        /// <summary>
        /// The world-space position of the target.
        /// </summary>
        public float3 Position;

        /// <summary>
        /// The radius of the target.
        /// Used for proximity calculations.
        /// </summary>
        public float Radius;
    }
}