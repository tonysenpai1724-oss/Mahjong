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

            Vector3 worldCenter = GetWorldFocusPoint();
            Vector3 cameraRight = GetCameraRight();
            Vector3 cameraForward = GetCameraForward();
            if (cameraForward.sqrMagnitude <= Mathf.Epsilon)
            {
                cameraForward = Vector3.forward;
            }

            cameraForward.Normalize();

            Quaternion uprightCardRotation = Quaternion.LookRotation(Vector3.up, -cameraForward);

            float slideDistance = GetMatchSlideDistance();
            float stageOffset = Mathf.Max(0.45f, slideDistance * 0.45f);
            float liftHeight = Mathf.Max(0.75f, slideDistance * 3f);

            Vector3 impactWorld = worldCenter + (Vector3.up * liftHeight);
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
        /// Gets the current world focus point used for slide-out direction calculations.
        /// </summary>
        private Vector3 GetWorldFocusPoint()
        {
            if (Context.Services.TryGet(out CameraManager cameraManager) && cameraManager.OrbitCameraController != null)
            {
                return cameraManager.OrbitCameraController.CurrentFocusPoint;
            }

            return Vector3.zero;
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
        private float GetShakeDurationSeconds() => animationSettings != null ? animationSettings.ShakeDurationSeconds : 0.18f;
        private float GetShakeAmplitude() => animationSettings != null ? animationSettings.ShakeAmplitude : 0.12f;
    }
}
