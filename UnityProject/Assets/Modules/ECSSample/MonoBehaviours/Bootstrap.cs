using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ECSSample
{
    /// <summary>
    /// Bootstraps the ECS world by creating entities, setting up systems, and initializing rendering.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        /// <summary>
        /// Number of entities along the X-axis of the grid.
        /// </summary>
        public int GridSizeX = 10;

        /// <summary>
        /// Number of entities along the Y-axis of the grid.
        /// </summary>
        public int GridSizeY = 10;

        /// <summary>
        /// Number of entities along the Z-axis of the grid.
        /// </summary>
        public int GridSizeZ = 10;

        /// <summary>
        /// Spacing between entities in the grid.
        /// </summary>
        public float Spacing = 1.5f;

        /// <summary>
        /// Mesh used for rendering the entities.
        /// </summary>
        public Mesh Mesh;

        /// <summary>
        /// Material applied to the entities' mesh.
        /// </summary>
        public Material Material;

        /// <summary>
        /// Initializes the ECS world by creating an entity archetype, scheduling a spawning job,
        /// and setting up the renderer and transform update systems.
        /// </summary>
        void Start()
        {
            // Retrieve the default ECS world and its entity manager.
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            // Create an archetype that includes LocalTransform, TransformSpeed, InitialTransform, and RenderColor components.
            var archetype = entityManager.CreateArchetype(
                typeof(LocalTransform),
                typeof(TransformSpeed),
                typeof(InitialTransform),
                typeof(RenderColor)
            );

            int totalEntities = GridSizeX * GridSizeY * GridSizeZ;

            // Create an EntityCommandBuffer for recording entity commands.
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Create a Random instance to be used for both the job and die Color-Erstellung.
            Random randomSeed = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            // Define and schedule a job to spawn entities arranged in a grid.
            var spawnJob = new SpawnJob
            {
                Prototype = entityManager.CreateEntity(archetype),
                EntityCount = totalEntities,
                GridSizeX = GridSizeX,
                GridSizeY = GridSizeY,
                GridSizeZ = GridSizeZ,
                Spacing = Spacing,
                Ecb = ecb.AsParallelWriter(),
                Random = randomSeed
            };

            // Schedule the spawn job with a batch size of 128 and complete it.
            var jobHandle = spawnJob.Schedule(totalEntities, 128);
            jobHandle.Complete();

            // Retrieve or create the indirect instanced renderer system and assign the mesh/material.
            var rendererSystem = world.GetOrCreateSystemManaged<IndirectInstancedRendererSystem>();
            rendererSystem.Mesh = Mesh;
            rendererSystem.Material = Material;

            // Create the TransformUpdateSystem for handling transformation updates.
            world.CreateSystemManaged<TransformUpdateSystem>();

            // Play back the recorded command buffer to update the entity manager and then dispose it.
            ecb.Playback(entityManager);
            ecb.Dispose();
        }
    }
}