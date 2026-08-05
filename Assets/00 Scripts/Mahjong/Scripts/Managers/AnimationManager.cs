using MahjongOut3D.Core;
using MahjongOut3D.CameraSystem;
using MahjongOut3D.Data;
using MahjongOut3D.TileSystem;
using MahjongOut3D.Utilities;
using System;
using System.Collections;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Coordinates tile motion feedback, hint flashes and camera shake.
    /// </summary>
    public sealed class AnimationManager : ManagerBehaviour
    {
        [SerializeField] private TileAnimationSettings animationSettings;

        private ComponentPool<ParticleSystem> particlePool;
        private MahjongTile activeHintFirstTile;
        private MahjongTile activeHintSecondTile;

        /// <summary>
        /// Gets a value indicating whether a blocking animation is currently running.
        /// </summary>
        public bool IsAnimationLocked { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the animation manager.
        /// </summary>
        public override int InitializationOrder => 90;

        /// <summary>
        /// Initializes the particle pool used for match feedback.
        /// </summary>
        protected override void OnInitialize()
        {
            if (animationSettings != null && animationSettings.MatchParticlePrefab != null)
            {
                particlePool = new ComponentPool<ParticleSystem>(animationSettings.MatchParticlePrefab);
            }
        }

        /// <summary>
        /// Clears pooled animation resources during shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            ClearHintHighlight();
            particlePool?.Clear();
            particlePool = null;
        }

        /// <summary>
        /// Plays a full match-removal sequence for two tiles.
        /// </summary>
        /// <param name="firstTile">First tile to animate.</param>
        /// <param name="secondTile">Second tile to animate.</param>
        /// <param name="onCompleted">Optional callback invoked when the animation completes.</param>
        /// <returns>Running coroutine instance.</returns>
        public Coroutine PlayMatchSequence(MahjongTile firstTile, MahjongTile secondTile, Action onCompleted = null)
        {
            return StartCoroutine(PlayMatchSequenceRoutine(firstTile, secondTile, onCompleted));
        }

        /// <summary>
        /// Plays a short mismatch pause.
        /// </summary>
        /// <param name="onCompleted">Optional callback invoked when the delay completes.</param>
        /// <returns>Running coroutine instance.</returns>
        public Coroutine PlayMismatchDelay(Action onCompleted = null)
        {
            return StartCoroutine(PlayMismatchDelayRoutine(onCompleted));
        }

        /// <summary>
        /// Applies a hint highlight to the supplied pair and keeps it visible until another hint replaces it.
        /// </summary>
        /// <param name="firstTile">First hinted tile.</param>
        /// <param name="secondTile">Second hinted tile.</param>
        /// <returns>Running coroutine instance.</returns>
        public Coroutine PlayHintSequence(MahjongTile firstTile, MahjongTile secondTile)
        {
            return StartCoroutine(PlayHintSequenceRoutine(firstTile, secondTile));
        }

        /// <summary>
        /// Moves a tile into one of the temporary selection tray slots used for gameplay testing.
        /// </summary>
        public Coroutine PlayMoveToTray(MahjongTile tile, int slotIndex, Action onCompleted = null)
        {
            return StartCoroutine(PlayMoveToTrayRoutine(tile, slotIndex, onCompleted));
        }

        /// <summary>
        /// Snaps a tile directly into one of the temporary selection tray slots.
        /// </summary>
        public bool SnapToTray(MahjongTile tile, int slotIndex)
        {
            if (tile == null || !TryGetTraySlotPose(tile, slotIndex, out Vector3 position, out Quaternion rotation))
            {
                return false;
            }

            tile.transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        /// <summary>
        /// Updates the current animation lock state.
        /// </summary>
        /// <param name="isLocked">New animation lock state.</param>
        public void SetAnimationLock(bool isLocked)
        {
            IsAnimationLocked = isLocked;
        }

        /// <summary>
        /// Executes the full match animation and camera shake sequence.
        /// </summary>
        private IEnumerator PlayMatchSequenceRoutine(MahjongTile firstTile, MahjongTile secondTile, Action onCompleted)
        {
            if (firstTile == null || secondTile == null)
            {
                yield break;
            }

            SetAnimationLock(true);

            Vector3 firstStart = firstTile.transform.position;
            Vector3 secondStart = secondTile.transform.position;
            Quaternion firstRotationStart = firstTile.transform.rotation;
            Quaternion secondRotationStart = secondTile.transform.rotation;

            Vector3 cameraRight = GetCameraRight();
            Vector3 cameraForward = GetCameraForward();
            if (cameraForward.sqrMagnitude <= Mathf.Epsilon)
            {
                cameraForward = Vector3.forward;
            }

            cameraForward.Normalize();

            Quaternion uprightCardRotation = GetTrayFacingRotation(cameraForward, Vector3.up);

            float slideDistance = GetMatchSlideDistance();
            float stageOffset = Mathf.Max(0.45f, slideDistance * 0.45f);

            Vector3 impactWorld = GetMatchCenterWorldPosition(firstTile, secondTile);
            Vector3 firstStageWorld = impactWorld - (cameraRight * stageOffset);
            Vector3 secondStageWorld = impactWorld + (cameraRight * stageOffset);

            Quaternion firstStageRotation = uprightCardRotation;
            Quaternion secondStageRotation = uprightCardRotation;

            float impactOffset = Mathf.Max(0.025f, slideDistance * 0.03f);
            Vector3 firstImpactWorld = impactWorld - (cameraRight * impactOffset);
            Vector3 secondImpactWorld = impactWorld + (cameraRight * impactOffset);
            Quaternion firstImpactRotation = uprightCardRotation;
            Quaternion secondImpactRotation = uprightCardRotation;

            float duration = GetMatchDurationSeconds();
            float elapsed = 0f;
            float liftPhaseDuration = duration * 0.6f;
            float collidePhaseDuration = Mathf.Max(0.05f, duration - liftPhaseDuration);

            while (elapsed < liftPhaseDuration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.Clamp01(elapsed / liftPhaseDuration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                firstTile.transform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(firstStart, firstStageWorld, easedT),
                    Quaternion.SlerpUnclamped(firstRotationStart, firstStageRotation, easedT));
                secondTile.transform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(secondStart, secondStageWorld, easedT),
                    Quaternion.SlerpUnclamped(secondRotationStart, secondStageRotation, easedT));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < collidePhaseDuration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.Clamp01(elapsed / collidePhaseDuration);
                float easedT = 1f - Mathf.Pow(1f - t, 4f);

                firstTile.transform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(firstStageWorld, firstImpactWorld, easedT),
                    Quaternion.SlerpUnclamped(firstStageRotation, firstImpactRotation, easedT));
                secondTile.transform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(secondStageWorld, secondImpactWorld, easedT),
                    Quaternion.SlerpUnclamped(secondStageRotation, secondImpactRotation, easedT));
                yield return null;
            }

            PlayMatchParticle(impactWorld);
            PlayCameraShake();

            onCompleted?.Invoke();
            SetAnimationLock(false);
        }

        /// <summary>
        /// Waits a short amount of time before invoking the mismatch callback.
        /// </summary>
        private IEnumerator PlayMismatchDelayRoutine(Action onCompleted)
        {
            SetAnimationLock(true);
            float duration = GetMismatchDelaySeconds();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                yield return null;
            }

            onCompleted?.Invoke();
            SetAnimationLock(false);
        }

        /// <summary>
        /// Applies the current hint highlight and replaces any previous hinted pair.
        /// </summary>
        private IEnumerator PlayHintSequenceRoutine(MahjongTile firstTile, MahjongTile secondTile)
        {
            if (firstTile == null || secondTile == null)
            {
                yield break;
            }

            if (activeHintFirstTile == firstTile && activeHintSecondTile == secondTile)
            {
                firstTile.SetHintHighlighted(true);
                secondTile.SetHintHighlighted(true);
                yield break;
            }

            ClearHintHighlight();

            activeHintFirstTile = firstTile;
            activeHintSecondTile = secondTile;
            firstTile.SetHintHighlighted(true);
            secondTile.SetHintHighlighted(true);
        }

        /// <summary>
        /// Clears the currently active hint highlight pair.
        /// </summary>
        private void ClearHintHighlight()
        {
            if (activeHintFirstTile != null)
            {
                activeHintFirstTile.SetHintHighlighted(false);
            }

            if (activeHintSecondTile != null && activeHintSecondTile != activeHintFirstTile)
            {
                activeHintSecondTile.SetHintHighlighted(false);
            }

            activeHintFirstTile = null;
            activeHintSecondTile = null;
        }

        /// <summary>
        /// Animates a tile from the board into a temporary tray slot near the top of the camera view.
        /// </summary>
        private IEnumerator PlayMoveToTrayRoutine(MahjongTile tile, int slotIndex, Action onCompleted)
        {
            if (tile == null || !TryGetTraySlotPose(tile, slotIndex, out Vector3 targetPosition, out Quaternion targetRotation))
            {
                onCompleted?.Invoke();
                yield break;
            }

            SetAnimationLock(true);

            Vector3 startPosition = tile.transform.position;
            Quaternion startRotation = tile.transform.rotation;
            float duration = GetTrayMoveDurationSeconds();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                tile.transform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(startPosition, targetPosition, easedT),
                    Quaternion.SlerpUnclamped(startRotation, targetRotation, easedT));
                yield return null;
            }

            tile.transform.SetPositionAndRotation(targetPosition, targetRotation);
            onCompleted?.Invoke();
            SetAnimationLock(false);
        }

        /// <summary>
        /// Resolves a temporary tray slot pose relative to the active gameplay camera.
        /// </summary>
        private bool TryGetTraySlotPose(MahjongTile tile, int slotIndex, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (!Context.Services.TryGet(out CameraManager cameraManager) || cameraManager.ActiveCamera == null)
            {
                return false;
            }

            Camera activeCamera = cameraManager.ActiveCamera;
            float x = 0.5f + ((Mathf.Clamp(slotIndex, 0, 3) - 1.5f) * GetTrayViewportSlotSpacing());
            float y = GetTrayViewportY();
            float z = GetTrayTargetDistance(activeCamera, tile);

            position = activeCamera.ViewportToWorldPoint(new Vector3(x, y, z));
            rotation = GetTrayFacingRotation(activeCamera.transform.forward, activeCamera.transform.up);
            return true;
        }

        /// <summary>
        /// Resolves the shared world-space center used by the match animation.
        /// </summary>
        private Vector3 GetMatchCenterWorldPosition(MahjongTile firstTile, MahjongTile secondTile)
        {
            if (!Context.Services.TryGet(out CameraManager cameraManager) || cameraManager.ActiveCamera == null)
            {
                return Vector3.Lerp(firstTile.transform.position, secondTile.transform.position, 0.5f);
            }

            Camera activeCamera = cameraManager.ActiveCamera;
            float viewportX = 0.5f;
            float viewportY = Mathf.Clamp01(GetTrayViewportY() - GetMatchViewportYOffset());
            float firstDepth = GetTrayTargetDistance(activeCamera, firstTile);
            float secondDepth = GetTrayTargetDistance(activeCamera, secondTile);
            float trayDepth = Mathf.Max(firstDepth, secondDepth);
            float targetDepth = Mathf.Max(0.5f, trayDepth - GetMatchDepthOffset());
            return activeCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, targetDepth));
        }

        /// <summary>
        /// Keeps tray tiles from being pulled too close to the camera so their perceived size stays stable.
        /// </summary>
        private float GetTrayTargetDistance(Camera activeCamera, MahjongTile tile)
        {
            return Mathf.Max(0.5f, GetTrayDistanceFromCamera() + GetTrayDistancePadding());
        }

        /// <summary>
        /// Resolves the upright-facing rotation used by tray and match-flight presentation.
        /// </summary>
        private static Quaternion GetTrayFacingRotation(Vector3 cameraForward, Vector3 cameraUp)
        {
            Vector3 resolvedForward = cameraForward.sqrMagnitude > Mathf.Epsilon ? cameraForward.normalized : Vector3.forward;
            Vector3 resolvedUp = cameraUp.sqrMagnitude > Mathf.Epsilon ? cameraUp.normalized : Vector3.up;

            return Quaternion.AngleAxis(180f, -resolvedForward)
                * Quaternion.AngleAxis(90f, -resolvedForward)
                * Quaternion.LookRotation(resolvedUp, -resolvedForward);
        }

        /// <summary>
        /// Gets the local delta time chosen by the animation settings.
        /// </summary>
        private float GetDeltaTime()
        {
            if (animationSettings != null && animationSettings.UseUnscaledTime)
            {
                return Time.unscaledDeltaTime;
            }

            return Time.deltaTime;
        }

        /// <summary>
        /// Spawns a match particle effect at the specified position.
        /// </summary>
        private void PlayMatchParticle(Vector3 worldPosition)
        {
            if (animationSettings == null || animationSettings.MatchParticlePrefab == null)
            {
                return;
            }

            ParticleSystem particle = particlePool != null ? particlePool.Get() : Instantiate(animationSettings.MatchParticlePrefab);
            particle.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            particle.Play();
            StartCoroutine(ReleaseParticleAfter(particle, particle.main.duration + particle.main.startLifetime.constantMax + 0.25f));
        }

        /// <summary>
        /// Returns a pooled particle to the pool after it finishes playing.
        /// </summary>
        private IEnumerator ReleaseParticleAfter(ParticleSystem particle, float delaySeconds)
        {
            float elapsed = 0f;
            while (elapsed < delaySeconds)
            {
                elapsed += GetDeltaTime();
                yield return null;
            }

            if (particlePool != null)
            {
                particlePool.Release(particle);
            }
            else if (particle != null)
            {
                Destroy(particle.gameObject);
            }
        }

        /// <summary>
        /// Requests a short shake on the active orbit camera.
        /// </summary>
        private void PlayCameraShake()
        {
            if (!Context.Services.TryGet(out CameraManager cameraManager))
            {
                return;
            }

            OrbitCameraController orbit = cameraManager.OrbitCameraController;
            if (orbit != null)
            {
                orbit.PlayShake(GetShakeDurationSeconds(), GetShakeAmplitude());
            }
        }

        /// <summary>
        /// Gets the horizontal right direction of the active gameplay camera.
        /// </summary>
        /// <returns>Normalized world-space right vector.</returns>
        private Vector3 GetCameraRight()
        {
            if (Context.Services.TryGet(out CameraManager cameraManager) && cameraManager.ActiveCamera != null)
            {
                Vector3 flattenedRight = Vector3.ProjectOnPlane(cameraManager.ActiveCamera.transform.right, Vector3.up);
                if (flattenedRight.sqrMagnitude > 0.0001f)
                {
                    return flattenedRight.normalized;
                }
            }

            return Vector3.right;
        }

        /// <summary>
        /// Gets the forward view direction of the active gameplay camera projected onto the horizontal plane.
        /// </summary>
        /// <returns>Normalized world-space forward vector.</returns>
        private Vector3 GetCameraForward()
        {
            if (Context.Services.TryGet(out CameraManager cameraManager) && cameraManager.ActiveCamera != null)
            {
                Vector3 flattenedForward = Vector3.ProjectOnPlane(cameraManager.ActiveCamera.transform.forward, Vector3.up);
                if (flattenedForward.sqrMagnitude > 0.0001f)
                {
                    return flattenedForward.normalized;
                }
            }

            return Vector3.forward;
        }

        private float GetMatchDurationSeconds() => animationSettings != null ? animationSettings.MatchDurationSeconds : 0.35f;
        private float GetMatchSlideDistance() => animationSettings != null ? animationSettings.MatchSlideDistance : 1.25f;
        private float GetMatchRotationDegrees() => animationSettings != null ? animationSettings.MatchRotationDegrees : 55f;
        private float GetHintDurationSeconds() => animationSettings != null ? animationSettings.HintDurationSeconds : 0.7f;
        private float GetMismatchDelaySeconds() => animationSettings != null ? animationSettings.MismatchDelaySeconds : 0.2f;
        private float GetTrayMoveDurationSeconds() => animationSettings != null ? animationSettings.TrayMoveDurationSeconds : 0.22f;
        private float GetTrayViewportY() => animationSettings != null ? animationSettings.TrayViewportY : 0.84f;
        private float GetTrayViewportSlotSpacing() => animationSettings != null ? animationSettings.TrayViewportSlotSpacing : 0.1f;
        private float GetMatchViewportYOffset() => animationSettings != null ? animationSettings.MatchViewportYOffset : 0.05f;
        private float GetMatchDepthOffset() => animationSettings != null ? animationSettings.MatchDepthOffset : 1.25f;
        private float GetTrayDistanceFromCamera() => animationSettings != null ? animationSettings.TrayDistanceFromCamera : 8f;
        private float GetTrayDistancePadding() => animationSettings != null ? animationSettings.TrayDistancePadding : 0.75f;
        private float GetShakeDurationSeconds() => animationSettings != null ? animationSettings.ShakeDurationSeconds : 0.18f;
        private float GetShakeAmplitude() => animationSettings != null ? animationSettings.ShakeAmplitude : 0.12f;
    }
}
