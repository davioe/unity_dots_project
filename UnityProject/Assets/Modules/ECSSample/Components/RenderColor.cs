using Unity.Entities;
using Unity.Mathematics;

namespace ECSSample
{
    /// <summary>
    /// Component to store the color of an entity for rendering.
    /// </summary>
    public struct RenderColor : IComponentData
    {
        public float3 Value;
    }
}