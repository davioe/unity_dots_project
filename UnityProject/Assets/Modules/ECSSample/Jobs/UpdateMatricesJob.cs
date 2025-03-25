using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECSSample
{
    /// <summary>
    /// Job that updates transformation matrices based on LocalTransform components.
    /// </summary>
    [BurstCompile]
    public struct UpdateMatricesJob : IJobParallelForBatch
    {
        /// <summary>
        /// Array containing the LocalTransform data for entities.
        /// </summary>
        [ReadOnly]
        public NativeArray<LocalTransform> Transforms;

        /// <summary>
        /// Array where calculated transformation matrices are stored.
        /// </summary>
        [WriteOnly]
        public NativeArray<Matrix4x4> Matrices;

        /// <summary>
        /// Executes this job over a batch of entities.
        /// </summary>
        /// <param name="startIndex">The starting index of the batch.</param>
        /// <param name="batchSize">The number of entities in the batch.</param>
        public void Execute(int startIndex, int batchSize)
        {
            int endIndex = startIndex + batchSize;
            for (int i = startIndex; i < endIndex; i++)
            {
                // Retrieve the LocalTransform data for the current entity.
                LocalTransform t = Transforms[i];
                float3 position = t.Position;
                quaternion rotation = t.Rotation;
                float uniformScale = t.Scale;
                float3 scale = new float3(uniformScale); // Convert uniform scale to a float3.

                // Calculate the transformation matrix based on the LocalTransform data.
                Matrices[i] = float4x4.TRS(position, rotation, scale);
            }
        }
    }
}