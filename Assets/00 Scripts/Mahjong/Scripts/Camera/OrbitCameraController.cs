using MahjongOut3D.Core;
using MahjongOut3D.Data;
using MahjongOut3D.Utilities;
using UnityEngine;

namespace MahjongOut3D.CameraSystem
{
    /// <summary>
    /// Controls a smooth orbit camera around a focus point for the Mahjong puzzle block.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OrbitCameraController : MonoBehaviour
    {
        private const float FallbackDefaultDistance = 10f;
        private const float FallbackRotationSpeed = 0.2f;
        private const float FallbackZoomSpeed = 3f;
        private const float FallbackZoomInputScale = 0.02f;
        private const float FallbackMinZoomDistance = 6f;
        private const float FallbackMaxZoomDistance = 18f;
        private const float FallbackDefaultPitch = 25f;
        private const float FallbackMinPitch = -15f;
        private const float FallbackMaxPitch = 70f;
        private const float FallbackRotationInertia = 0.88f;
        private const float FallbackRotationSmoothing = 0.08f;
        private const float FallbackZoomSmoothing = 0.2f;
        private const float FallbackFocusSmoothing = 0.12f;

        [Header("References")]
        [SerializeField] private Camera managedCamera;
        [SerializeField] private OrbitCameraSettings settings;
        [SerializeField] private Transform focusTarget;

        [Header("Fallback Focus")]
        [SerializeField] private Vector3 focusPoint = Vector3.zero;
        [SerializeField] private Vector3 focusOffset = Vector3.zero;

        [Header("Initial Orbit")]
        [SerializeField] private float initialYaw;

        private bool isInitialized;
        private float targetYaw;
        private float targetPitch;
        private float currentYaw;
        private float currentPitch;
        private float targetDistance;
        private float currentDistance;
        private float yawSmoothVelocity;
        private float pitchSmoothVelocity;
        private float distanceSmoothVelocity;
        private float focusXSmoothVelocity;
        private float focusYSmoothVelocity;
        private float focusZSmoothVelocity;
        private Vector2 inertialRotationVelocity;
        private Vector3 currentFocusPoint;
        private int lastDragFrame = -1;

        /// <summary>
        /// Gets the camera driven by this orbit controller.
        /// </summary>
        public Camera ManagedCamera => managedCamera;

        /// <summary>
        /// Gets the current focus point in world space.
        /// </summary>
        public Vector3 CurrentFocusPoint => currentFocusPoint;

        /// <summary>
        /// Initializes the orbit camera runtime state.
        /// </summary>
        /// <param name="context">Shared game context.</param>
        public void Initialize(GameContext context)
        {
            if (isInitialized)
            {
                return;
            }

            if (managedCamera == null)
            {
                managedCamera = GetComponent<Camera>();
            }

            if (managedCamera == null)
            {
                MahjongRuntimeLogger.LogWarning("OrbitCameraController could not find a Camera component. It will drive its own transform only.");
            }

            if (settings == null)
            {
                MahjongRuntimeLogger.LogWarning("OrbitCameraController has no OrbitCameraSettings assigned. Falling back to built-in defaults.");
            }

            targetYaw = initialYaw;
            currentYaw = targetYaw;

            targetPitch = Mathf.Clamp(GetDefaultPitch(), GetMinPitch(), GetMaxPitch());
            currentPitch = targetPitch;
            targetDistance = Mathf.Clamp(GetDefaultZoomDistance(), GetMinZoomDistance(), GetMaxZoomDistance());
            currentDistance = targetDistance;

            currentFocusPoint = ResolveDesiredFocusPoint();
            ApplyTransform(true);
            isInitialized = true;
        }

        /// <summary>
        /// Clears transient runtime motion state.
        /// </summary>
        public void Shutdown()
        {
            isInitialized = false;
            inertialRotationVelocity = Vector2.zero;
            yawSmoothVelocity = 0f;
            pitchSmoothVelocity = 0f;
            distanceSmoothVelocity = 0f;
            focusXSmoothVelocity = 0f;
            focusYSmoothVelocity = 0f;
            focusZSmoothVelocity = 0f;
            lastDragFrame = -1;
        }

        /// <summary>
        /// Applies a rotation input delta expressed in screen pixels.
        /// </summary>
        /// <param name="screenDelta">Pointer movement delta in screen pixels.</param>
        public void Rotate(Vector2 screenDelta)
        {
            if (!isInitialized)
            {
                return;
            }

            float deltaTime = Mathf.Max(GetDeltaTime(), 0.0001f);
            Vector2 scaledDelta = screenDelta * GetRotationSpeed();

            targetYaw += scaledDelta.x;
            targetPitch = Mathf.Clamp(targetPitch - scaledDelta.y, GetMinPitch(), GetMaxPitch());

            inertialRotationVelocity = scaledDelta / deltaTime;
            lastDragFrame = Time.frameCount;
        }

        /// <summary>
        /// Applies a signed zoom delta and clamps the orbit distance.
        /// </summary>
        /// <param name="zoomDelta">Signed zoom delta from pinch or wheel input.</param>
        public void Zoom(float zoomDelta)
        {
            if (!isInitialized)
            {
                return;
            }

            float scaledZoom = zoomDelta * GetZoomSpeed() * GetZoomInputScale();
            targetDistance = Mathf.Clamp(targetDistance - scaledZoom, GetMinZoomDistance(), GetMaxZoomDistance());
        }

        /// <summary>
        /// Updates the orbit focus target at runtime.
        /// </summary>
        /// <param name="target">Transform that the camera should orbit around.</param>
        public void SetFocusTarget(Transform target)
        {
            focusTarget = target;
        }

        /// <summary>
        /// Updates the fallback orbit focus point used when no focus target transform exists.
        /// </summary>
        /// <param name="worldPoint">World-space point to orbit around.</param>
        public void SetFocusPoint(Vector3 worldPoint)
        {
            focusPoint = worldPoint;
        }

        /// <summary>
        /// Advances the smooth orbit camera simulation.
        /// </summary>
        private void LateUpdate()
        {
            if (!isInitialized)
            {
                return;
            }

            ApplyRotationInertia();
            ApplyTransform(false);
        }

        /// <summary>
        /// Applies decaying inertial rotation when the player stops dragging.
        /// </summary>
        private void ApplyRotationInertia()
        {
            if (lastDragFrame == Time.frameCount)
            {
                return;
            }

            if (inertialRotationVelocity.sqrMagnitude <= 0.0001f)
            {
                inertialRotationVelocity = Vector2.zero;
                return;
            }

            float deltaTime = GetDeltaTime();
            targetYaw += inertialRotationVelocity.x * deltaTime;
            targetPitch = Mathf.Clamp(targetPitch - (inertialRotationVelocity.y * deltaTime), GetMinPitch(), GetMaxPitch());

            float decay = Mathf.Pow(Mathf.Clamp01(GetRotationInertia()), deltaTime * 60f);
            inertialRotationVelocity *= decay;
        }

        /// <summary>
        /// Applies smoothed orbit position and rotation to the driven camera transform.
        /// </summary>
        /// <param name="snap">True to snap immediately instead of smoothing.</param>
        private void ApplyTransform(bool snap)
        {
            Vector3 desiredFocusPoint = ResolveDesiredFocusPoint();

            if (snap)
            {
                currentFocusPoint = desiredFocusPoint;
                currentYaw = targetYaw;
                currentPitch = targetPitch;
                currentDistance = targetDistance;
            }
            else
            {
                float deltaTime = GetDeltaTime();
                currentFocusPoint = SmoothVector(currentFocusPoint, desiredFocusPoint, ref focusXSmoothVelocity, ref focusYSmoothVelocity, ref focusZSmoothVelocity, GetFocusSmoothing(), deltaTime);
                currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawSmoothVelocity, GetRotationSmoothing(), Mathf.Infinity, deltaTime);
                currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchSmoothVelocity, GetRotationSmoothing(), Mathf.Infinity, deltaTime);
                currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceSmoothVelocity, GetZoomSmoothing(), Mathf.Infinity, deltaTime);
            }

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 cameraPosition = currentFocusPoint - (rotation * Vector3.forward * currentDistance);
            Transform drivenTransform = GetDrivenTransform();
            drivenTransform.SetPositionAndRotation(cameraPosition, rotation);
        }

        /// <summary>
        /// Resolves the desired focus point from the current focus target or fallback point.
        /// </summary>
        /// <returns>World-space point the camera should orbit around.</returns>
        private Vector3 ResolveDesiredFocusPoint()
        {
            if (focusTarget != null)
            {
                return focusTarget.position + focusOffset;
            }

            return focusPoint + focusOffset;
        }

        /// <summary>
        /// Smooths a vector using per-axis damping.
        /// </summary>
        /// <param name="current">Current vector.</param>
        /// <param name="target">Target vector.</param>
        /// <param name="xVelocity">Smoothed x-axis velocity.</param>
        /// <param name="yVelocity">Smoothed y-axis velocity.</param>
        /// <param name="zVelocity">Smoothed z-axis velocity.</param>
        /// <param name="smoothTime">Smoothing duration.</param>
        /// <param name="deltaTime">Simulation delta time.</param>
        /// <returns>Smoothed vector value.</returns>
        private static Vector3 SmoothVector(Vector3 current, Vector3 target, ref float xVelocity, ref float yVelocity, ref float zVelocity, float smoothTime, float deltaTime)
        {
            return new Vector3(
                Mathf.SmoothDamp(current.x, target.x, ref xVelocity, smoothTime, Mathf.Infinity, deltaTime),
                Mathf.SmoothDamp(current.y, target.y, ref yVelocity, smoothTime, Mathf.Infinity, deltaTime),
                Mathf.SmoothDamp(current.z, target.z, ref zVelocity, smoothTime, Mathf.Infinity, deltaTime));
        }

        /// <summary>
        /// Gets the active time step selected by the orbit camera settings.
        /// </summary>
        /// <returns>Current simulation delta time.</returns>
        private float GetDeltaTime()
        {
            if (settings != null && settings.UseUnscaledTime)
            {
                return Time.unscaledDeltaTime;
            }

            return Time.deltaTime;
        }

        /// <summary>
        /// Gets the transform driven by the orbit camera.
        /// </summary>
        /// <returns>Driven transform instance.</returns>
        private Transform GetDrivenTransform()
        {
            return managedCamera != null ? managedCamera.transform : transform;
        }

        /// <summary>
        /// Gets the configured default zoom distance.
        /// </summary>
        /// <returns>Default zoom distance.</returns>
        private float GetDefaultZoomDistance()
        {
            return settings != null ? settings.DefaultZoomDistance : FallbackDefaultDistance;
        }

        /// <summary>
        /// Gets the configured rotation speed.
        /// </summary>
        /// <returns>Rotation speed value.</returns>
        private float GetRotationSpeed()
        {
            return settings != null ? settings.RotationSpeed : FallbackRotationSpeed;
        }

        /// <summary>
        /// Gets the configured zoom speed.
        /// </summary>
        /// <returns>Zoom speed value.</returns>
        private float GetZoomSpeed()
        {
            return settings != null ? settings.ZoomSpeed : FallbackZoomSpeed;
        }

        /// <summary>
        /// Gets the configured zoom input scale.
        /// </summary>
        /// <returns>Zoom input scale value.</returns>
        private float GetZoomInputScale()
        {
            return settings != null ? settings.ZoomInputScale : FallbackZoomInputScale;
        }

        /// <summary>
        /// Gets the configured minimum zoom distance.
        /// </summary>
        /// <returns>Minimum zoom distance.</returns>
        private float GetMinZoomDistance()
        {
            return settings != null ? settings.MinZoomDistance : FallbackMinZoomDistance;
        }

        /// <summary>
        /// Gets the configured maximum zoom distance.
        /// </summary>
        /// <returns>Maximum zoom distance.</returns>
        private float GetMaxZoomDistance()
        {
            return settings != null ? settings.MaxZoomDistance : FallbackMaxZoomDistance;
        }

        /// <summary>
        /// Gets the configured default pitch.
        /// </summary>
        /// <returns>Default pitch angle.</returns>
        private float GetDefaultPitch()
        {
            return settings != null ? settings.DefaultPitch : FallbackDefaultPitch;
        }

        /// <summary>
        /// Gets the configured minimum pitch.
        /// </summary>
        /// <returns>Minimum pitch angle.</returns>
        private float GetMinPitch()
        {
            return settings != null ? settings.MinPitch : FallbackMinPitch;
        }

        /// <summary>
        /// Gets the configured maximum pitch.
        /// </summary>
        /// <returns>Maximum pitch angle.</returns>
        private float GetMaxPitch()
        {
            return settings != null ? settings.MaxPitch : FallbackMaxPitch;
        }

        /// <summary>
        /// Gets the configured rotation inertia factor.
        /// </summary>
        /// <returns>Rotation inertia factor.</returns>
        private float GetRotationInertia()
        {
            return settings != null ? settings.RotationInertia : FallbackRotationInertia;
        }

        /// <summary>
        /// Gets the configured rotation smoothing duration.
        /// </summary>
        /// <returns>Rotation smoothing duration.</returns>
        private float GetRotationSmoothing()
        {
            return settings != null ? settings.RotationSmoothing : FallbackRotationSmoothing;
        }

        /// <summary>
        /// Gets the configured zoom smoothing duration.
        /// </summary>
        /// <returns>Zoom smoothing duration.</returns>
        private float GetZoomSmoothing()
        {
            return settings != null ? settings.ZoomSmoothing : FallbackZoomSmoothing;
        }

        /// <summary>
        /// Gets the configured focus smoothing duration.
        /// </summary>
        /// <returns>Focus smoothing duration.</returns>
        private float GetFocusSmoothing()
        {
            return settings != null ? settings.FocusSmoothing : FallbackFocusSmoothing;
        }

        /// <summary>
        /// Draws the focus point in the Scene view for setup convenience.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 gizmoPoint = focusTarget != null ? focusTarget.position + focusOffset : focusPoint + focusOffset;
            Gizmos.DrawWireSphere(gizmoPoint, 0.2f);
        }
    }
}
