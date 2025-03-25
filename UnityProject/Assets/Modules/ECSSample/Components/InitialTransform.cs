using Unity.Entities;
using Unity.Mathematics;

namespace ECSSample
{
    /// <summary>
    /// Component that holds the initial transformation matrix for an entity.
    /// </summary>
    public struct InitialTransform : IComponentData
    {
        /// <summary>
        /// The initial transformation matrix.
        /// </summary>
        public float4x4 Value;
    }
}