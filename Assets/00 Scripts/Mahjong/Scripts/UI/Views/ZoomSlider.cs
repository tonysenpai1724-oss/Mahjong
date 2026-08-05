using MahjongOut3D.CameraSystem;
using MahjongOut3D.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D
{
    /// <summary>
    /// Drives orbit-camera zoom from a UI slider where the middle position is neutral on setup.
    /// Dragging upward zooms in, dragging downward zooms out, and the handle stays where the player leaves it.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public sealed class ZoomSlider : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private CameraManager cameraManager;
        [SerializeField, Min(1f)] private float zoomDeltaScale = 120f;

        private bool isUpdatingSilently;
        private float previousSliderValue = 0.5f;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }

            if (cameraManager == null)
            {
                cameraManager = FindFirstObjectByType<CameraManager>(FindObjectsInactive.Exclude);
            }

            ConfigureSlider();
        }

        private void OnEnable()
        {
            if (slider != null)
            {
                slider.onValueChanged.AddListener(HandleSliderValueChanged);
            }

            ResetToCenter();
        }

        private void OnDisable()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(HandleSliderValueChanged);
            }
        }

        public void SyncWithCamera()
        {
            ResetToCenter();
        }

        private void ConfigureSlider()
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            SetSliderValueWithoutNotify(0.5f);
        }

        private void HandleSliderValueChanged(float sliderValue)
        {
            if (isUpdatingSilently)
            {
                return;
            }

            OrbitCameraController orbitCamera = GetOrbitCamera();
            if (orbitCamera == null)
            {
                previousSliderValue = sliderValue;
                return;
            }

            float sliderDelta = sliderValue - previousSliderValue;
            previousSliderValue = sliderValue;

            if (Mathf.Abs(sliderDelta) <= Mathf.Epsilon)
            {
                return;
            }

            orbitCamera.Zoom(sliderDelta * zoomDeltaScale);
        }

        private OrbitCameraController GetOrbitCamera()
        {
            if (cameraManager == null)
            {
                cameraManager = FindFirstObjectByType<CameraManager>(FindObjectsInactive.Exclude);
            }

            return cameraManager != null ? cameraManager.OrbitCameraController : null;
        }

        private void ResetToCenter()
        {
            previousSliderValue = 0.5f;
            SetSliderValueWithoutNotify(previousSliderValue);
        }

        private void SetSliderValueWithoutNotify(float value)
        {
            if (slider == null)
            {
                return;
            }

            isUpdatingSilently = true;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            isUpdatingSilently = false;
        }
    }
}
