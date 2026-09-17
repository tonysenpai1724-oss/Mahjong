using MahjongOut3D.Core;
using MahjongOut3D.Gameplay;
using MahjongOut3D.GameplayInput;
using MahjongOut3D.Utilities;
using UnityEngine;
using TigerForge;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Bridges raw touch input into gameplay events and controls when input is active.
    /// </summary>
    public sealed class InputManager : ManagerBehaviour
    {
        [SerializeField] private TouchInputSource inputSource;

        /// <summary>
        /// Gets a value indicating whether runtime input is enabled.
        /// </summary>
        public bool IsInputEnabled { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the input manager.
        /// </summary>
        public override int InitializationOrder => 80;

        /// <summary>
        /// Connects the configured input source and starts listening for game flow changes.
        /// </summary>
        protected override void OnInitialize()
        {
            if (inputSource == null)
            {
                inputSource = GetComponentInChildren<TouchInputSource>(true);
            }

            if (inputSource == null)
            {
                MahjongRuntimeLogger.LogWarning("InputManager could not find a TouchInputSource in its hierarchy.");
            }

            if (inputSource != null)
            {
                inputSource.TileTapped += HandleTileTapped;
                inputSource.OrbitDragged += HandleOrbitDragged;
                inputSource.ZoomChanged += HandleZoomChanged;
                inputSource.ScreenActivity += HandleScreenActivity;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.GameFlowStateChanged += HandleGameFlowStateChanged;
            }
            EventManager.StartListening(Constant.ON_GAME_STATE_CHANGE, HandleGameplayStateChanged);
            RefreshInputEnabled();
        }

        /// <summary>
        /// Disconnects the raw input source and clears active subscriptions.
        /// </summary>
        protected override void OnShutdown()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.GameFlowStateChanged -= HandleGameFlowStateChanged;
            }

            EventManager.StopListening(Constant.ON_GAME_STATE_CHANGE, HandleGameplayStateChanged);

            if (inputSource != null)
            {
                inputSource.TileTapped -= HandleTileTapped;
                inputSource.OrbitDragged -= HandleOrbitDragged;
                inputSource.ZoomChanged -= HandleZoomChanged;
                inputSource.ScreenActivity -= HandleScreenActivity;
                inputSource.SetInputEnabled(false);
            }

            IsInputEnabled = false;
        }

        /// <summary>
        /// Updates the runtime input state.
        /// </summary>
        /// <param name="isEnabled">New input enabled state.</param>
        public void SetInputEnabled(bool isEnabled)
        {
            if (IsInputEnabled == isEnabled)
            {
                return;
            }

            IsInputEnabled = isEnabled;

            if (inputSource != null)
            {
                inputSource.SetInputEnabled(IsInputEnabled);
            }

            Context?.EventBus.Publish(new InputEnabledChangedEvent(IsInputEnabled));
        }

        /// <summary>
        /// Enables gameplay gestures only while the game is in the gameplay state.
        /// </summary>
        /// <param name="eventData">Published game flow state change.</param>
        private void HandleGameFlowStateChanged(GameFlowStateChangedEvent eventData)
        {
            RefreshInputEnabled();
        }

        private void HandleGameplayStateChanged()
        {
            RefreshInputEnabled();
        }

        private void RefreshInputEnabled()
        {
            GameManager gameManager = GameManager.Instance;
            bool isGameplayFlow = gameManager != null && gameManager.CurrentFlowState == GameFlowState.Gameplay;
            bool isGameplayRunning = GameplayManager.Instance == null || GameplayManager.Instance.State == EGamePlayState.Running;
            SetInputEnabled(isGameplayFlow && isGameplayRunning);
        }

        /// <summary>
        /// Republishes tile tap input through the shared event bus.
        /// </summary>
        /// <param name="eventData">Tap input payload.</param>
        private void HandleScreenActivity(ScreenActivityInputEvent eventData)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            Context.EventBus.Publish(eventData);
        }

        private void HandleTileTapped(TileTapInputEvent eventData)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            Context.EventBus.Publish(eventData);
        }

        /// <summary>
        /// Republishes orbit drag input through the shared event bus.
        /// </summary>
        /// <param name="eventData">Drag input payload.</param>
        private void HandleOrbitDragged(OrbitDragInputEvent eventData)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            Context.EventBus.Publish(eventData);
        }

        /// <summary>
        /// Republishes zoom input through the shared event bus.
        /// </summary>
        /// <param name="eventData">Zoom input payload.</param>
        private void HandleZoomChanged(ZoomInputEvent eventData)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            Context.EventBus.Publish(eventData);
        }
    }
}
