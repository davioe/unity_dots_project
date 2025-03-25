using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ECSSample
{
    /// <summary>
    /// System that handles indirect instanced rendering by retrieving transformation data,
    /// updating compute buffers, and issuing draw calls.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class IndirectInstancedRendererSystem : SystemBase
    {
        /// <summary>
        /// The mesh that will be rendered.
        /// </summary>
        public Mesh Mesh;

        /// <summary>
        /// The material applied to the mesh.
        /// </summary>
        public Material Material;

        // Name of the matrix buffer in the shader.
        private const string MatrixBufferName = "matrixBuffer";

        // Compute buffer that holds transformation matrices.
        private ComputeBuffer matrixBuffer;

        // Name of the color buffer in the shader.
        private const string ColorBufferName = "colorBuffer";

        // Compute buffer that holds color data.
        private ComputeBuffer colorBuffer;

        // Compute buffer that holds the indirect draw arguments.
        private ComputeBuffer argsBuffer;

        // The bounding box used for culling the draw call.
        private Bounds bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));

        // Reference to the TransformUpdateSystem that computes transformation matrices.
        private TransformUpdateSystem transformUpdateSystem;
        private ColorUpdateSystem colorUpdateSystem;

        // Stores the current capacity of the matrix compute buffer.
        private int computeBufferCapacity;

        private readonly uint[] args = new uint[5];

        private int prevInstanceCount = 0;

        /// <summary>
        /// Called when the system is created.
        /// Initializes the reference to the TransformUpdateSystem.
        /// </summary>
        protected override void OnCreate()
        {
            transformUpdateSystem = World.GetOrCreateSystemManaged<TransformUpdateSystem>();
            colorUpdateSystem = World.GetOrCreateSystemManaged<ColorUpdateSystem>();
        }

        /// <summary>
        /// Called when the system starts running.
        /// Initializes the compute buffers and validates required assets.
        /// </summary>
        protected override void OnStartRunning()
        {
            if (Mesh == null || Material == null)
            {
                Debug.LogError("Mesh or Material has not been assigned!");
                Enabled = false;
                return;
            }

            // Cache constant mesh parameters.
            args[0] = Mesh.GetIndexCount(0);
            args[1] = 0; // instance count updates dynamically.
            args[2] = Mesh.GetIndexStart(0);
            args[3] = Mesh.GetBaseVertex(0);
            args[4] = 0;

            computeBufferCapacity = 8192;
            argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            matrixBuffer = new ComputeBuffer(computeBufferCapacity, sizeof(float) * 16, ComputeBufferType.Structured);
            colorBuffer = new ComputeBuffer(computeBufferCapacity, sizeof(float) * 3, ComputeBufferType.Structured);

            argsBuffer.SetData(args);
        }

        /// <summary>
        /// Updates the compute buffers with the latest transformation data and issues the indirect draw call.
        /// </summary>
        protected override void OnUpdate()
        {
            // Combine the dependencies of the TransformUpdateSystem and ColorUpdateSystem.
            JobHandle combinedHandle = JobHandle.CombineDependencies(
                transformUpdateSystem.DependencyHandle,
                colorUpdateSystem.DependencyHandle
            );
            combinedHandle.Complete();

            int instanceCount = transformUpdateSystem.LastInstanceCount;
            if (instanceCount <= 0)
            {
                return;
            }

            // Reallocate buffers only if instanceCount exceeds capacity.
            if (instanceCount > computeBufferCapacity)
            {
                matrixBuffer?.Release();
                colorBuffer?.Release();

                computeBufferCapacity = math.ceilpow2(instanceCount);
                matrixBuffer = new ComputeBuffer(computeBufferCapacity, sizeof(float) * 16, ComputeBufferType.Structured);
                colorBuffer = new ComputeBuffer(computeBufferCapacity, sizeof(float) * 3, ComputeBufferType.Structured);
            }

            // Update the indirect draw arguments if the instance count has changed.
            if (instanceCount != prevInstanceCount)
            {
                args[1] = (uint)instanceCount;
                argsBuffer.SetData(args);
                prevInstanceCount = instanceCount;
            }

            // Update the compute buffers with the latest transformation data.
            NativeArray<Matrix4x4> matrices = transformUpdateSystem.MatricesNativeArray;
            matrixBuffer.SetData(matrices, 0, 0, instanceCount);

            NativeArray<float3> colors = colorUpdateSystem.ColorsNativeArray;
            colorBuffer.SetData(colors, 0, 0, instanceCount);

            // Set the buffers and issue the draw call.
            Material.SetBuffer(MatrixBufferName, matrixBuffer);
            Material.SetBuffer(ColorBufferName, colorBuffer);

            Graphics.DrawMeshInstancedIndirect(Mesh, 0, Material, bounds, argsBuffer);
        }

        /// <summary>
        /// Called when the system is destroyed.
        /// Releases allocated compute buffers.
        /// </summary>
        protected override void OnDestroy()
        {
            matrixBuffer?.Release();
            colorBuffer?.Release();
            argsBuffer?.Release();
        }
    }
}