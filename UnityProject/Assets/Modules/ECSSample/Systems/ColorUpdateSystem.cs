using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ECSSample
{
    /// <summary>
    /// System that extracts color data from entities and gathers it into a NativeArray for further usage (e.g., in the renderer).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ColorUpdateSystem : SystemBase
    {
        /// <summary>
        /// Persistent NativeArray that stores the computed colors.
        /// </summary>
        public NativeArray<float3> ColorsNativeArray;

        /// <summary>
        /// Current capacity of the color buffer.
        /// </summary>
        private int colorBufferCapacity = 8192;

        /// <summary>
        /// Entity Query to capture all entities with the RenderColor component.
        /// </summary>
        private EntityQuery query;

        /// <summary>
        /// Dependency handle for the last job.
        /// </summary>
        public JobHandle DependencyHandle => Dependency;

        protected override void OnCreate()
        {
            // Create a query that retrieves all entities with the RenderColor component.
            query = GetEntityQuery(typeof(RenderColor));

            // Allocate the initial buffer.
            ColorsNativeArray = new NativeArray<float3>(colorBufferCapacity, Allocator.Persistent);
        }

        [BurstCompile]
        private struct CopyColorsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<RenderColor> Colors;
            public NativeArray<float3> PersistentBuffer;

            public void Execute(int index)
            {
                PersistentBuffer[index] = Colors[index].Value;
            }
        }

        protected override void OnUpdate()
        {
            // Calculate the number of entities with RenderColor components.
            int instanceCount = query.CalculateEntityCount();

            // Adjust the capacity of the color buffer based on the instance count.
            int newCapacity = colorBufferCapacity;
            if (instanceCount > colorBufferCapacity)
            {
                newCapacity = (int)math.ceilpow2((uint)instanceCount);
            }
            else if (instanceCount < colorBufferCapacity / 2 && colorBufferCapacity > 8192)
            {
                // Halve the buffer size if the instance count is less than half the current capacity.
                newCapacity = (int)math.ceilpow2((uint)instanceCount);
            }

            // Reallocate the buffer if the capacity has changed.
            if (newCapacity != colorBufferCapacity)
            {
                ColorsNativeArray.Dispose();
                colorBufferCapacity = newCapacity;
                ColorsNativeArray = new NativeArray<float3>(colorBufferCapacity, Allocator.Persistent);
            }

            // Use the query to asynchronously retrieve the RenderColor components.
            var colorsAsync = query.ToComponentDataListAsync<RenderColor>(Allocator.TempJob, out JobHandle asyncHandle);

            // Determine the batch size (experimentally adjusted, e.g., instanceCount / 30)
            int batchSize = (int)math.ceil(instanceCount / 30f);

            // Combine the dependencies of the system with the async job.
            JobHandle combinedHandle = JobHandle.CombineDependencies(Dependency, asyncHandle);

            // Schedule a job to copy the colors into the persistent buffer.
            JobHandle copyJobHandle = new CopyColorsJob
            {
                Colors = colorsAsync.AsDeferredJobArray(),
                PersistentBuffer = ColorsNativeArray
            }.Schedule(instanceCount, batchSize, combinedHandle);

            // Combine the dependencies of the system with the copy job and the dispose operation.
            Dependency = JobHandle.CombineDependencies(copyJobHandle, colorsAsync.Dispose(copyJobHandle));
        }

        protected override void OnDestroy()
        {
            if (ColorsNativeArray.IsCreated)
            {
                ColorsNativeArray.Dispose();
            }
        }
    }
}