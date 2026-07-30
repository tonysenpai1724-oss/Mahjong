using MahjongOut3D.Core;
using MahjongOut3D.CameraSystem;
using MahjongOut3D.GameplayInput;
using MahjongOut3D.Utilities;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Owns the active gameplay camera and routes orbit input into the camera controller.
    /// </summary>
    public sealed class CameraManager : ManagerBehaviour
    {
        [SerializeField] private OrbitCameraController orbitCameraController;

        /// <summary>
        /// Gets the active runtime camera.
        /// </summary>
        public Camera ActiveCamera { get; private set; }

        /// <summary>
        /// Gets the active orbit camera controller.
        /// </summary>
        public OrbitCameraController OrbitCameraController => orbitCameraController;

        /// <summary>
        /// Gets the bootstrap order for the camera manager.
        /// </summary>
        public override int InitializationOrder => 40;

        /// <summary>
        /// Initializes the orbit camera and subscribes to input-driven camera events.
        /// </summary>
        protected override void OnInitialize()
        {
            if (orbitCameraController == null)
            {
                orbitCameraController = GetComponentInChildren<OrbitCameraController>(true);
            }

            if (orbitCameraController == null)
            {
                MahjongRuntimeLogger.LogWarning("CameraManager could not find an OrbitCameraController in its hierarchy.");
                return;
            }

            orbitCameraController.Initialize(Context);
            SetActiveCamera(orbitCameraController.ManagedCamera);
            Context.EventBus.Subscribe<OrbitDragInputEvent>(HandleOrbitDragged);
            Context.EventBus.Subscribe<ZoomInputEvent>(HandleZoomChanged);
        }

        /// <summary>
        /// Unsubscribes from input-driven camera events and clears runtime references.
        /// </summary>
        protected override void OnShutdown()
        {
            Context.EventBus.Unsubscribe<OrbitDragInputEvent>(HandleOrbitDragged);
            Context.EventBus.Unsubscribe<ZoomInputEvent>(HandleZoomChanged);

            if (orbitCameraController != null)
            {
                orbitCameraController.Shutdown();
            }

            ActiveCamera = null;
        }

        /// <summary>
        /// Updates the active runtime camera reference.
        /// </summary>
        /// <param name="cameraInstance">Camera to register as active.</param>
        public void SetActiveCamera(Camera cameraInstance)
        {
            ActiveCamera = cameraInstance;
        }

        /// <summary>
        /// Updates the orbit focus target transform.
        /// </summary>
        /// <param name="focusTarget">Transform that the camera should orbit around.</param>
        public void SetFocusTarget(Transform focusTarget)
        {
            orbitCameraController?.SetFocusTarget(focusTarget);
        }

        /// <summary>
        /// Updates the orbit fallback focus point.
        /// </summary>
        /// <param name="focusPoint">World-space point to orbit around.</param>
        public void SetFocusPoint(Vector3 focusPoint)
        {
            orbitCameraController?.SetFocusPoint(focusPoint);
        }

        /// <summary>
        /// Routes drag input into the orbit camera controller.
        /// </summary>
        /// <param name="eventData">Drag input payload.</param>
        private void HandleOrbitDragged(OrbitDragInputEvent eventData)
        {
            orbitCameraController?.Rotate(eventData.ScreenDelta);
        }

        /// <summary>
        /// Routes zoom input into the orbit camera controller.
        /// </summary>
        /// <param name="eventData">Zoom input payload.</param>
        private void HandleZoomChanged(ZoomInputEvent eventData)
        {
            orbitCameraController?.Zoom(eventData.Delta);
        }
    }
}
