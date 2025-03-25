using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECSSample
{
    /// <summary>
    /// System that updates entity transforms based on target data.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TransformSystem : SystemBase
    {
        /// <summary>
        /// Updates the transforms of entities based on the proximity to a target.
        /// </summary>
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            const float snapThreshold = 0.2f;

            // Attempt to retrieve the singleton TargetData component.
            if (!SystemAPI.TryGetSingleton(out TargetData target))
            {
                return;
            }

            // Pre-calculations outside the loop to avoid repeated expensive operations.
            float3 targetPos = target.Position;
            float targetRadiusSq = target.Radius * target.Radius;
            float deltaSpeed = deltaTime; // Base value for transform speed; pre-calculate if needed multiple times.

            // Process each entity.
            Entities.ForEach((ref LocalTransform transform, ref InitialTransform initTransform, in TransformSpeed transformSpeed) =>
            {
                // Calculate squared distance to avoid expensive square root calculation.
                float distSq = math.lengthsq(transform.Position - targetPos);

                // Calculate multipliers once per iteration.
                float speedDelta = transformSpeed.Value * deltaTime;
                float snapSpeedDelta = transformSpeed.Value * deltaTime * 5f;

                if (distSq < targetRadiusSq)
                {
                    // Rotate smoothly towards the target.
                    float3 direction = math.normalize(targetPos - transform.Position);
                    quaternion desiredRotation = quaternion.LookRotationSafe(direction, math.up());
                    transform.Rotation = math.slerp(transform.Rotation, desiredRotation, speedDelta);
                    // Move and scale towards the target.
                    transform.Position = math.lerp(transform.Position, targetPos, speedDelta);
                    transform.Scale = math.lerp(transform.Scale, 0.1f, speedDelta);
                }
                else
                {
                    // Compute initial position.
                    float3 initPos = initTransform.Value.Translation();
                    if (math.distance(transform.Position, initPos) >= snapThreshold)
                    {
                        // Smoothly return to the initial transform.
                        transform.Position = math.lerp(transform.Position, initPos, snapSpeedDelta);
                        transform.Rotation = math.slerp(transform.Rotation, initTransform.Value.Rotation(), snapSpeedDelta);
                        transform.Scale = math.lerp(transform.Scale, initTransform.Value.Scale().x, snapSpeedDelta);
                    }
                    else
                    {
                        // Snap directly to the initial transform values.
                        transform.Position = initPos;
                        transform.Rotation = initTransform.Value.Rotation();
                        transform.Scale = initTransform.Value.Scale().x;
                    }
                }
            }).ScheduleParallel();
        }
    }
}