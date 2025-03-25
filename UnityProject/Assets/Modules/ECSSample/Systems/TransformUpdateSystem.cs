using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECSSample
{
    /// <summary>
    /// System that updates the transformation matrices for entities.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TransformUpdateSystem : SystemBase
    {
        /// <summary>
        /// Persistent NativeArray that stores computed matrices.
        /// </summary>
        public NativeArray<Matrix4x4> MatricesNativeArray;

        private int matrixBufferSize = 8192;
        private readonly int resizeIncrement = 4096;
        private readonly int resizeBuffer = 1024;

        // Query for all relevant entities.
        private EntityQuery query;

        /// <summary>
        /// Stores the actual count of processed instances in the last update.
        /// </summary>
        public int LastInstanceCount { get; private set; }

        public JobHandle DependencyHandle => Dependency;

        /// <summary>
        /// Called when the system is created.
        /// Initializes the entity query and the native array buffer.
        /// </summary>
        protected override void OnCreate()
        {
            // Create a query for entities with the required components.
            query = GetEntityQuery(
                ComponentType.ReadOnly<LocalTransform>()
            );

            // Initialize the buffer.
            MatricesNativeArray = new NativeArray<Matrix4x4>(matrixBufferSize, Allocator.Persistent);
        }

        /// <summary>
        /// Called when the system is destroyed.
        /// Disposes the NativeArray if it has been created.
        /// </summary>
        protected override void OnDestroy()
        {
            if (MatricesNativeArray.IsCreated)
                MatricesNativeArray.Dispose();
        }

        /// <summary>
        /// Updates the entity transformations and schedules the corresponding jobs.
        /// </summary>
        protected override void OnUpdate()
        {
            // Calculate the count of entities matching the query.
            int instanceCount = query.CalculateEntityCount();
            LastInstanceCount = instanceCount; // Store the actual instance count.

            if (instanceCount == 0)
                return;

            // Resize the buffer if required.
            if (instanceCount > matrixBufferSize - resizeBuffer)
            {
                ResizeBuffer(instanceCount);
            }

            // Asynchronously retrieve the LocalTransform data.
            var transformListAsync = query.ToComponentDataListAsync<LocalTransform>(Allocator.TempJob, out JobHandle transformJobHandle);
            // Use the deferred variant so the update job automatically depends on transformJobHandle.
            var transformsArray = transformListAsync.AsDeferredJobArray();

            // Schedule a job to compute the matrices.
            var updateJob = new UpdateMatricesJob
            {
                Transforms = transformsArray,
                Matrices = MatricesNativeArray
            };

            // Determine the batch size (experimentally adjusted, e.g., instanceCount / 30)
            int batchSize = (int)math.ceil(instanceCount / 30f);
            JobHandle updateJobHandle = updateJob.ScheduleBatch(instanceCount, batchSize, transformJobHandle);

            // Chain the dispose operation so that the temporary array is only freed after the job is completed.
            JobHandle disposeHandle = transformListAsync.Dispose(updateJobHandle);

            // Instead of waiting synchronously, include the dependency in the system's Dependency,
            // so later systems (e.g., the GPU update system) can wait for it.
            Dependency = JobHandle.CombineDependencies(Dependency, disposeHandle);
        }

        /// <summary>
        /// Resizes the matrix buffer based on the current instance count.
        /// </summary>
        /// <param name="instanceCount">The current number of instances.</param>
        private void ResizeBuffer(int instanceCount)
        {
            int oldSize = matrixBufferSize;
            matrixBufferSize = Mathf.Max(matrixBufferSize + resizeIncrement, instanceCount + resizeBuffer);
            if (MatricesNativeArray.IsCreated)
                MatricesNativeArray.Dispose();
            MatricesNativeArray = new NativeArray<Matrix4x4>(matrixBufferSize, Allocator.Persistent);

            Debug.LogWarning($"Matrix Buffer resized: Old Size = {oldSize}, New Size = {matrixBufferSize}");
        }
    }
}