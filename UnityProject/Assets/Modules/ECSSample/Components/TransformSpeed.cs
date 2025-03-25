using Unity.Entities;

namespace ECSSample
{

    /// <summary>
    /// The transform speed of the entity.
    /// </summary>
    public struct TransformSpeed : IComponentData
    {
        /// <summary>
        /// The transform speed factor.
        /// </summary>
        public float Value;
    }
}