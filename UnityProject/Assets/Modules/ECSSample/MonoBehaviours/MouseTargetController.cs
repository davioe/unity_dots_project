using Unity.Entities;
using UnityEngine;

namespace ECSSample
{
  /// <summary>
    /// Controls the target position using mouse input.
    /// The target position is smoothed over time and stored in the TargetData singleton component.
    /// </summary>
    public class MouseTargetController : MonoBehaviour
    {
        /// <summary>
        /// The effective radius around the target.
        /// </summary>
        public float Radius = 10f;

        /// <summary>
        /// Smoothing factor between 0 and 1, where lower values result in slower transitions.
        /// </summary>
        [Tooltip("Smoothing factor between 0 and 1, where lower values result in slower transitions.")]
        public float smoothing = 0.1f;

        private Camera cam;
        // Holds the smoothly updated target position.
        private Vector3 currentTargetPosition;

        /// <summary>
        /// Initializes the camera reference and subscribes to the update observable to continuously update the target position.
        /// </summary>
        private void Start()
        {
            // Retrieve the main camera.
            cam = Camera.main;
            currentTargetPosition = Vector3.zero;
        }

        private void Update() => UpdateTargetPosition();

        /// <summary>
        /// Updates the target position based on the current mouse position.
        /// Converts the mouse screen coordinates into world space (assumed on the y = 0 plane),
        /// smoothly interpolates the position, and updates the ECS TargetData component.
        /// </summary>
        private void UpdateTargetPosition()
        {
            // Convert the mouse position to a ray from the main camera.
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            // Define a plane at y=0.
            var plane = new Plane(Vector3.up, Vector3.zero);

            // If the ray intersects the plane...
            if (plane.Raycast(ray, out float distance))
            {
                // Get the desired target position from the ray at the intersection point.
                Vector3 desiredTargetPosition = ray.GetPoint(distance);
                // Smoothly interpolate between the current and desired positions.
                currentTargetPosition = Vector3.Lerp(currentTargetPosition, desiredTargetPosition, smoothing);

                // Retrieve the EntityManager from the default world.
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                // Create an entity query to locate the singleton TargetData component.
                var query = entityManager.CreateEntityQuery(typeof(TargetData));
                Entity targetEntity;

                // If no singleton exists, create a new one.
                if (query.IsEmptyIgnoreFilter)
                {
                    targetEntity = entityManager.CreateEntity(typeof(TargetData));
                }
                else
                {
                    // Otherwise, retrieve the existing singleton entity.
                    targetEntity = query.GetSingletonEntity();
                }

                // Create a new TargetData with the updated target position and effective radius.
                var targetData = new TargetData
                {
                    Position = currentTargetPosition,
                    Radius = Radius
                };

                // Update the TargetData component of the singleton entity.
                entityManager.SetComponentData(targetEntity, targetData);
            }
        }
    }
}