using System;
using MahjongOut3D.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MahjongOut3D.GameplayInput
{
    /// <summary>
    /// Reads touch and mouse input, then emits gameplay gestures for the Mahjong runtime.
    /// </summary>
    public sealed class TouchInputSource : MonoBehaviour, IInputSource
    {
        private const int InvalidPointerId = int.MinValue;
        private const int MousePointerId = -1;

        [SerializeField] private InputSettings settings;

        private bool isInputEnabled;
        private bool isDragging;
        private bool isPinching;
        private bool ignoreCurrentPointer;
        private int currentPointerId = InvalidPointerId;
        private Vector2 pointerStartPosition;
        private Vector2 pointerLastPosition;
        private float pointerStartTime;
        private float lastPinchDistance;

        /// <summary>
        /// Occurs when the player taps a visible gameplay target.
        /// </summary>
        public event Action<TileTapInputEvent> TileTapped;

        /// <summary>
        /// Occurs when the player drags to rotate the orbit camera.
        /// </summary>
        public event Action<OrbitDragInputEvent> OrbitDragged;

        /// <summary>
        /// Occurs when the player pinches or scrolls to zoom the orbit camera.
        /// </summary>
        public event Action<ZoomInputEvent> ZoomChanged;

        /// <summary>
        /// Enables or disables raw input polling.
        /// </summary>
        /// <param name="isEnabled">True to enable input polling; otherwise false.</param>
        public void SetInputEnabled(bool isEnabled)
        {
            isInputEnabled = isEnabled;
            if (!isInputEnabled)
            {
                ResetPointerTracking();
                ResetPinchTracking();
            }
        }

        /// <summary>
        /// Polls input once per frame and converts it into gameplay gestures.
        /// </summary>
        private void Update()
        {
            if (!isInputEnabled)
            {
                return;
            }

            if (Input.touchCount > 0)
            {
                HandleTouchInput();
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            if (settings == null || settings.EnableMouseSimulationInEditor)
            {
                HandleMouseInput();
            }
#endif
        }

        /// <summary>
        /// Converts mobile multi-touch input into orbit and zoom gestures.
        /// </summary>
        private void HandleTouchInput()
        {
            if (Input.touchCount >= 2)
            {
                HandlePinch(Input.GetTouch(0), Input.GetTouch(1));
                return;
            }

            ResetPinchTracking();
            HandleSingleTouch(Input.GetTouch(0));
        }

        /// <summary>
        /// Tracks a single touch for tap and drag behavior.
        /// </summary>
        /// <param name="touch">Touch to process.</param>
        private void HandleSingleTouch(Touch touch)
        {
            if (ignoreCurrentPointer && touch.fingerId == currentPointerId)
            {
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    ResetPointerTracking();
                }

                return;
            }

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginPointer(touch.position, touch.fingerId, IsPointerBlockedByUi(touch.fingerId));
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    UpdateDrag(touch.position);
                    break;

                case TouchPhase.Ended:
                    CompletePointer(touch.position, touch.fingerId);
                    break;

                case TouchPhase.Canceled:
                    ResetPointerTracking();
                    break;
            }
        }

        /// <summary>
        /// Tracks a two-finger pinch gesture and emits zoom deltas.
        /// </summary>
        /// <param name="firstTouch">First active touch.</param>
        /// <param name="secondTouch">Second active touch.</param>
        private void HandlePinch(Touch firstTouch, Touch secondTouch)
        {
            if (IsPointerBlockedByUi(firstTouch.fingerId) || IsPointerBlockedByUi(secondTouch.fingerId))
            {
                ResetPointerTracking();
                ResetPinchTracking();
                return;
            }

            ResetPointerTracking();

            float currentDistance = Vector2.Distance(firstTouch.position, secondTouch.position);
            if (!isPinching)
            {
                isPinching = true;
                lastPinchDistance = currentDistance;
                return;
            }

            float pinchDelta = currentDistance - lastPinchDistance;
            lastPinchDistance = currentDistance;

            if (settings != null && Mathf.Abs(pinchDelta) < settings.PinchDeltaThresholdPixels)
            {
                return;
            }

            ZoomChanged?.Invoke(new ZoomInputEvent(pinchDelta));
        }

        /// <summary>
        /// Converts mouse input into drag, tap and wheel-zoom gestures for editor testing.
        /// </summary>
        private void HandleMouseInput()
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > Mathf.Epsilon && !IsMouseBlockedByUi())
            {
                ZoomChanged?.Invoke(new ZoomInputEvent(scrollDelta));
            }

            if (Input.GetMouseButtonDown(0))
            {
                BeginPointer(Input.mousePosition, MousePointerId, IsMouseBlockedByUi());
            }

            if (Input.GetMouseButton(0))
            {
                UpdateDrag(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0))
            {
                CompletePointer(Input.mousePosition, MousePointerId);
            }
        }

        /// <summary>
        /// Starts tracking a new pointer gesture.
        /// </summary>
        /// <param name="screenPosition">Pointer start position.</param>
        /// <param name="pointerId">Pointer identifier.</param>
        /// <param name="blockedByUi">True when the pointer started on UI.</param>
        private void BeginPointer(Vector2 screenPosition, int pointerId, bool blockedByUi)
        {
            currentPointerId = pointerId;
            pointerStartPosition = screenPosition;
            pointerLastPosition = screenPosition;
            pointerStartTime = GetCurrentTime();
            isDragging = false;
            ignoreCurrentPointer = blockedByUi;
        }

        /// <summary>
        /// Updates the active pointer and emits orbit drag deltas when the drag threshold is exceeded.
        /// </summary>
        /// <param name="screenPosition">Current pointer position.</param>
        private void UpdateDrag(Vector2 screenPosition)
        {
            if (currentPointerId == InvalidPointerId || ignoreCurrentPointer || isPinching)
            {
                return;
            }

            Vector2 frameDelta = screenPosition - pointerLastPosition;
            float totalDistance = Vector2.Distance(pointerStartPosition, screenPosition);
            float dragThreshold = settings == null ? 0f : settings.DragStartThresholdPixels;

            if (!isDragging && totalDistance >= dragThreshold)
            {
                isDragging = true;
            }

            pointerLastPosition = screenPosition;

            if (!isDragging || frameDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            OrbitDragged?.Invoke(new OrbitDragInputEvent(frameDelta));
        }

        /// <summary>
        /// Completes the active pointer and emits a tile tap when the gesture stayed within tap thresholds.
        /// </summary>
        /// <param name="screenPosition">Pointer end position.</param>
        /// <param name="pointerId">Pointer identifier.</param>
        private void CompletePointer(Vector2 screenPosition, int pointerId)
        {
            if (pointerId != currentPointerId)
            {
                return;
            }

            if (!ignoreCurrentPointer && IsTap(screenPosition))
            {
                TileTapped?.Invoke(new TileTapInputEvent(screenPosition, pointerId));
            }

            ResetPointerTracking();
        }

        /// <summary>
        /// Determines whether the active pointer gesture qualifies as a tap.
        /// </summary>
        /// <param name="screenPosition">Pointer end position.</param>
        /// <returns>True when the gesture is a tap; otherwise false.</returns>
        private bool IsTap(Vector2 screenPosition)
        {
            if (isDragging || isPinching)
            {
                return false;
            }

            float duration = GetCurrentTime() - pointerStartTime;
            float maxTapDuration = settings == null ? 0.25f : settings.MaxTapDurationSeconds;
            float tapThreshold = settings == null ? 18f : settings.TapMoveThresholdPixels;
            float moveDistance = Vector2.Distance(pointerStartPosition, screenPosition);

            return duration <= maxTapDuration && moveDistance <= tapThreshold;
        }

        /// <summary>
        /// Resets the current single-pointer gesture tracking state.
        /// </summary>
        private void ResetPointerTracking()
        {
            currentPointerId = InvalidPointerId;
            pointerStartPosition = Vector2.zero;
            pointerLastPosition = Vector2.zero;
            pointerStartTime = 0f;
            isDragging = false;
            ignoreCurrentPointer = false;
        }

        /// <summary>
        /// Resets the current pinch gesture tracking state.
        /// </summary>
        private void ResetPinchTracking()
        {
            isPinching = false;
            lastPinchDistance = 0f;
        }

        /// <summary>
        /// Gets the current time source configured for input thresholds.
        /// </summary>
        /// <returns>Current runtime time in seconds.</returns>
        private float GetCurrentTime()
        {
            if (settings != null && settings.UseUnscaledTime)
            {
                return Time.unscaledTime;
            }

            return Time.time;
        }

        /// <summary>
        /// Determines whether the current mouse pointer is over a Unity UI element.
        /// </summary>
        /// <returns>True when gameplay input should be blocked by UI.</returns>
        private bool IsMouseBlockedByUi()
        {
            return settings != null
                   && settings.BlockInputWhenPointerOverUi
                   && EventSystem.current != null
                   && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Determines whether the specified touch pointer is over a Unity UI element.
        /// </summary>
        /// <param name="pointerId">Touch pointer identifier.</param>
        /// <returns>True when gameplay input should be blocked by UI.</returns>
        private bool IsPointerBlockedByUi(int pointerId)
        {
            return settings != null
                   && settings.BlockInputWhenPointerOverUi
                   && EventSystem.current != null
                   && EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
