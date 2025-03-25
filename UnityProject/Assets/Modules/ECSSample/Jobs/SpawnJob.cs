using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace ECSSample
{
    /// <summary>
    /// Job to spawn entities arranged in a grid formation.
    /// The job instantiates entities and assigns random properties such as transform speed.
    /// </summary>
    [BurstCompile]
    public struct SpawnJob : IJobParallelFor
    {
        /// <summary>
        /// The prototype entity used as a blueprint for each spawned entity.
        /// </summary>
        public Entity Prototype;

        /// <summary>
        /// Total number of entities to spawn.
        /// </summary>
        public int EntityCount;

        /// <summary>
        /// Number of entities in the grid along the X-axis.
        /// </summary>
        public int GridSizeX;

        /// <summary>
        /// Number of entities in the grid along the Y-axis.
        /// </summary>
        public int GridSizeY;

        /// <summary>
        /// Number of entities in the grid along the Z-axis.
        /// </summary>
        public int GridSizeZ;

        /// <summary>
        /// Spacing between entities in the grid.
        /// </summary>
        public float Spacing;

        /// <summary>
        /// Command buffer to record entity instantiation and component setting operations.
        /// </summary>
        public EntityCommandBuffer.ParallelWriter Ecb;

        /// <summary>
        /// Random number generator for randomizing scale and transform speed.
        /// </summary>
        public Random Random;

        /// <summary>
        /// Executes the spawn operation for each entity in parallel.
        /// Calculates the grid position based on the index and sets the entity's components.
        /// </summary>
        /// <param name="index">The index of the current entity to spawn.</param>
        public void Execute(int index)
        {
            // Create a local random number generator based on the global Random instance and the index.
            Random localRandom = new Random(Random.NextUInt() + (uint)index);

            // Instantiate a new entity from the prototype.
            var entity = Ecb.Instantiate(index, Prototype);

            // Calculate 3D grid coordinates based on the index.
            int x = index % GridSizeX;
            int y = (index / GridSizeX) % GridSizeY;
            int z = index / (GridSizeX * GridSizeY);

            // Generate a random scale value using the localRandom.
            float scale = localRandom.NextFloat(0.8f, 1f);

            // Create a local transform component based on the grid position,
            // using identity rotation and the random scale.
            var transform = LocalTransform.FromPositionRotationScale(
                new float3(x * Spacing, y * Spacing, z * Spacing),
                quaternion.identity,
                scale
            );

            // Set the computed LocalTransform component on the entity.
            Ecb.SetComponent(index, entity, transform);

            // Set a random transform speed for the entity using the localRandom.
            Ecb.SetComponent(index, entity, new TransformSpeed { Value = localRandom.NextFloat(0.1f, 1f) });

            // Set the initial transformation using the transformation matrix derived from the local transform.
            Ecb.SetComponent(index, entity, new InitialTransform { Value = transform.ToMatrix() });

            // Set a random RenderColor using the same localRandom.
            Ecb.SetComponent(index, entity, new RenderColor { Value = GenerateRandomColor(localRandom) });
        }

        /// <summary>
        /// Generates a random float3 color using the provided Random instance.
        /// </summary>
        /// <param name="random">The local Random instance.</param>
        /// <returns>A random float3 color.</returns>
        private readonly float3 GenerateRandomColor(Random random)
        {
            return new float3(
                random.NextFloat(0f, 1f),
                random.NextFloat(0f, 1f),
                random.NextFloat(0f, 1f)
            );
        }
    }
}