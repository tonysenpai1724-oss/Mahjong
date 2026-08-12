using MahjongOut3D.Core;
using MahjongOut3D.Data;
using MahjongOut3D.Gameplay;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.TileSystem;
using MahjongOut3D.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Owns tile selection, pair validation, board progression and gameplay power-ups.
    /// </summary>
    public sealed class MatchManager : ManagerBehaviour
    {
        private const float AnimationCompletionTimeoutSeconds = 5f;
        private const int SelectionTrayCapacity = 4;
        private const float MemoryRevealTimeoutSeconds = 2f;

        [Header("Memory Flip Difficulty")]
        [SerializeField, Range(0f, 1f)] private float hardMemoryFaceDownRatio = 0.35f;
        [SerializeField, Range(0f, 1f)] private float expertMemoryFaceDownRatio = 0.55f;

        [SerializeField] private PowerUpSettings powerUpSettings;

        private readonly List<MahjongTile> selectedTiles = new List<MahjongTile>(SelectionTrayCapacity);
        private readonly List<MahjongTile> memorySelectedTiles = new List<MahjongTile>(2);
        private readonly Stack<MoveHistoryRecord> history = new Stack<MoveHistoryRecord>();
        private readonly Queue<PendingMatchResolution> pendingMatchQueue = new Queue<PendingMatchResolution>();
        private readonly HashSet<MahjongTile> pendingMatchedTiles = new HashSet<MahjongTile>();
        private readonly Dictionary<MahjongTile, bool> memorySelectionOriginalFaceStates = new Dictionary<MahjongTile, bool>();
        private Coroutine memoryRevealTimeoutRoutine;
        private bool isMemoryRevealLocked;
        private int totalTiles;

        /// <summary>
        /// Gets a value indicating whether a match resolution is currently running.
        /// </summary>
        public bool IsResolvingMatch { get; private set; }

        /// <summary>
        /// Gets the currently selected tile count.
        /// </summary>
        public int SelectedTileCount => selectedTiles.Count;

        /// <summary>
        /// Gets the bootstrap order for the match manager.
        /// </summary>
        public override int InitializationOrder => 30;

        /// <summary>
        /// Subscribes to tile taps and level generation events.
        /// </summary>
        protected override void OnInitialize()
        {
            Context.EventBus.Subscribe<TileTappedEvent>(HandleTileTapped);
            Context.EventBus.Subscribe<LevelGeneratedEvent>(HandleLevelGenerated);
        }

        /// <summary>
        /// Clears transient runtime state during shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            Context.EventBus.Unsubscribe<TileTappedEvent>(HandleTileTapped);
            Context.EventBus.Unsubscribe<LevelGeneratedEvent>(HandleLevelGenerated);
            CancelMemoryRevealTimeout();
            selectedTiles.Clear();
            memorySelectedTiles.Clear();
            history.Clear();
            pendingMatchQueue.Clear();
            pendingMatchedTiles.Clear();
            memorySelectionOriginalFaceStates.Clear();
            totalTiles = 0;
            isMemoryRevealLocked = false;
            IsResolvingMatch = false;
        }

        private void LateUpdate()
        {
            if (selectedTiles.Count == 0)
            {
                return;
            }

            AnimationManager animationManager = GetAnimationManager();
            if (animationManager == null || animationManager.IsAnimationLocked)
            {
                return;
            }

            ReflowSelectionTray();
        }

        /// <summary>
        /// Starts a match resolution block if none is active.
        /// </summary>
        /// <returns>True when the lock was acquired; otherwise false.</returns>
        public bool TryBeginResolution()
        {
            if (IsResolvingMatch)
            {
                return false;
            }

            IsResolvingMatch = true;
            return true;
        }

        /// <summary>
        /// Ends the current match resolution block.
        /// </summary>
        public void EndResolution()
        {
            IsResolvingMatch = false;
        }

        /// <summary>
        /// Uses the hint power-up to highlight a valid exposed pair.
        /// </summary>
        /// <returns>True when a hint was used successfully; otherwise false.</returns>
        public bool UseHint()
        {
            if (!TryFindAnySelectablePair(out MahjongTile firstTile, out MahjongTile secondTile))
            {
                return false;
            }

            if (!TryUsePowerUp(PowerUpType.Hint, GetHintCost()))
            {
                return false;
            }

            GetAudioManager()?.PlayPowerUp(PowerUpType.Hint);
            GetAnimationManager()?.PlayHintSequence(firstTile, secondTile);
            Context.EventBus.Publish(new HintSuggestedEvent(firstTile, secondTile));
            Context.EventBus.Publish(new PowerUpUsedEvent(PowerUpType.Hint));
            return true;
        }

        /// <summary>
        /// Uses the undo power-up to restore the previous reversible board state.
        /// </summary>
        /// <returns>True when Undo succeeds; otherwise false.</returns>
        public bool UseUndo()
        {
            if (!HasSelectionHistoryRecord() || !TryUsePowerUp(PowerUpType.Undo, GetUndoCost()))
            {
                return false;
            }

            if (!TryPopLatestSelectionRecord(out MoveHistoryRecord record))
            {
                return false;
            }

            RestoreSelectionRecord(record);

            GetTileManager()?.RefreshTileExposure();
            PublishProgress();
            GetAudioManager()?.PlayPowerUp(PowerUpType.Undo);
            Context.EventBus.Publish(new PowerUpUsedEvent(PowerUpType.Undo));
            return true;
        }

        /// <summary>
        /// Uses the shuffle power-up to randomly redistribute remaining tiles.
        /// </summary>
        /// <returns>True when Shuffle succeeds; otherwise false.</returns>
        public bool UseShuffle()
        {
            TileManager tileManager = GetTileManager();
            if (tileManager == null)
            {
                return false;
            }

            List<MahjongTile> snapshotTiles = new List<MahjongTile>(tileManager.GetRemainingTiles());
            if (snapshotTiles.Count <= 1)
            {
                return false;
            }

            if (!TryUsePowerUp(PowerUpType.Shuffle, GetShuffleCost()))
            {
                return false;
            }

            DeselectAll();

            List<MahjongTile> remainingTiles = new List<MahjongTile>();
            foreach (MahjongTile tile in tileManager.GetRemainingTiles())
            {
                if (tile != null && !tile.IsBufferedSelection)
                {
                    remainingTiles.Add(tile);
                }
            }

            ShuffleTileMatches(remainingTiles);

            tileManager.RefreshTileExposure();
            GetAudioManager()?.PlayPowerUp(PowerUpType.Shuffle);
            Context.EventBus.Publish(new PowerUpUsedEvent(PowerUpType.Shuffle));
            EvaluateBoardState();
            return true;
        }

        /// <summary>
        /// Uses the bomb power-up to remove a valid exposed pair automatically.
        /// </summary>
        /// <returns>True when Bomb succeeds; otherwise false.</returns>
        public bool UseBomb()
        {
            if (!TryFindAnySelectablePair(out MahjongTile firstTile, out MahjongTile secondTile))
            {
                return false;
            }

            if (!TryUsePowerUp(PowerUpType.Bomb, GetBombCost()))
            {
                return false;
            }

            GetAudioManager()?.PlayPowerUp(PowerUpType.Bomb);
            StartCoroutine(ResolveMatchedPair(firstTile, secondTile, false));
            Context.EventBus.Publish(new PowerUpUsedEvent(PowerUpType.Bomb));
            return true;
        }

        /// <summary>
        /// Debug helper that automatically resolves any currently valid pair without consuming resources.
        /// </summary>
        /// <returns>True when a valid pair was found and scheduled for removal; otherwise false.</returns>
        public bool DebugAutoMatch()
        {
            if (IsResolvingMatch || !TryFindAnySelectablePair(out MahjongTile firstTile, out MahjongTile secondTile))
            {
                return false;
            }

            StartCoroutine(ResolveMatchedPair(firstTile, secondTile, false));
            return true;
        }

        /// <summary>
        /// Uses the X-Ray power-up to temporarily reveal one extra inner layer.
        /// </summary>
        /// <returns>True when X-Ray succeeds; otherwise false.</returns>
        public bool UseXRay()
        {
            TileManager tileManager = GetTileManager();
            if (tileManager == null)
            {
                return false;
            }

            if (!TryUsePowerUp(PowerUpType.XRay, GetXRayCost()))
            {
                return false;
            }

            tileManager.EnableXRay(GetXRayDurationSeconds(), GetXRayRevealDepth());
            GetAudioManager()?.PlayPowerUp(PowerUpType.XRay);
            Context.EventBus.Publish(new PowerUpUsedEvent(PowerUpType.XRay));
            return true;
        }

        /// <summary>
        /// Handles tile taps routed from the 3D raycast system.
        /// </summary>
        private void HandleTileTapped(TileTappedEvent eventData)
        {
            TileManager tileManager = GetTileManager();
            GameManager gameManager = GetGameManager();
            if (gameManager == null || tileManager == null || gameManager.CurrentFlowState != GameFlowState.Gameplay)
            {
                return;
            }

            MahjongTile tappedTile = eventData.Tile;
            if (tappedTile == null)
            {
                return;
            }

            if (IsTilePendingMatch(tappedTile))
            {
                return;
            }

            if (!tileManager.IsTileTapSelectable(tappedTile, eventData.HitInfo))
            {
                tappedTile.PlayBlockedTapFeedback();
                return;
            }

            if (selectedTiles.Contains(tappedTile) || memorySelectedTiles.Contains(tappedTile))
            {
                return;
            }

            MahjongTile matchingTrayTile = FindMatchingTrayTile(tappedTile);
            if (selectedTiles.Count >= SelectionTrayCapacity && matchingTrayTile == null)
            {
                GetAudioManager()?.PlayLose();
                gameManager.LoseGameplay();
                return;
            }

            if (!tappedTile.TrySelect())
            {
                tappedTile.PlayBlockedTapFeedback();
                return;
            }

            StartCoroutine(MoveTileIntoSelectionTray(tappedTile, matchingTrayTile));
        }

        /// <summary>
        /// Resets runtime state and starts gameplay after a new level has been generated.
        /// </summary>
        private void HandleLevelGenerated(LevelGeneratedEvent eventData)
        {
            CancelMemoryRevealTimeout();
            selectedTiles.Clear();
            memorySelectedTiles.Clear();
            history.Clear();
            pendingMatchQueue.Clear();
            pendingMatchedTiles.Clear();
            memorySelectionOriginalFaceStates.Clear();
            isMemoryRevealLocked = false;
            totalTiles = eventData.SpawnedTileCount;
            ApplyDifficultyTileFaceState();
            if (GameplayManager.Instance != null)
            {
                GameplayManager.Instance.SetState(EGamePlayState.Running);
            }
            GetGameManager()?.StartGameplay();
            PublishProgress();
        }

        private IEnumerator HandleMemoryModeTileTapped(MahjongTile tappedTile)
        {
            if (tappedTile == null)
            {
                yield break;
            }

            if (memorySelectedTiles.Count >= 2)
            {
                yield break;
            }

            if (!tappedTile.TrySelect())
            {
                tappedTile.PlayBlockedTapFeedback();
                yield break;
            }

            history.Push(CreateSelectionSnapshotRecord(tappedTile, true));

            isMemoryRevealLocked = true;
            CancelMemoryRevealTimeout();
            memorySelectionOriginalFaceStates[tappedTile] = tappedTile.IsFaceDown;
            memorySelectedTiles.Add(tappedTile);
            GetAudioManager()?.PlaySelect();

            bool completed = false;
            tappedTile.FlipFaceUp(() => completed = true);

            float elapsed = 0f;
            while (!completed && elapsed < AnimationCompletionTimeoutSeconds)
            {
                elapsed += GetResolutionDeltaTime();
                yield return null;
            }

            isMemoryRevealLocked = false;

            if (memorySelectedTiles.Count <= 1)
            {
                ScheduleMemoryRevealTimeout();
                yield break;
            }

            MahjongTile firstTile = memorySelectedTiles[0];
            MahjongTile secondTile = memorySelectedTiles[1];
            if (firstTile == null || secondTile == null)
            {
                HideMemorySelectedTilesInstant();
                yield break;
            }

            if (firstTile.HasSameVisualIdentity(secondTile))
            {
                QueueMatchedPairForResolution(firstTile, secondTile, true);
                yield break;
            }

            StartCoroutine(ResolveMemoryMismatch(firstTile, secondTile));
        }

        /// <summary>
        /// Resolves a successful pair match.
        /// </summary>
        private IEnumerator ResolveMatchedPair(MahjongTile firstTile, MahjongTile secondTile, bool rewardCoins)
        {
            if (!TryBeginResolution())
            {
                yield break;
            }

            try
            {
                firstTile.SetBufferedSelection(false);
                secondTile.SetBufferedSelection(false);
                firstTile.Deselect();
                secondTile.Deselect();
                GetAudioManager()?.PlayMatch();

                bool completed = false;
                AnimationManager animationManager = null;
                Context.Services.TryGet(out animationManager);
                if (animationManager != null)
                {
                    animationManager.PlayMatchSequence(firstTile, secondTile, () => completed = true);
                }
                else
                {
                    completed = true;
                }

                float elapsed = 0f;
                while (!completed && elapsed < AnimationCompletionTimeoutSeconds)
                {
                    elapsed += GetResolutionDeltaTime();
                    yield return null;
                }

                if (!completed)
                {
                    MahjongRuntimeLogger.LogWarning("Match animation timed out. Completing resolution to avoid locked input.");
                }

                LevelManager levelManager = GetLevelManager();
                if (levelManager?.ActiveGrid != null)
                {
                    levelManager.ActiveGrid.RemoveTile(firstTile.TileId);
                    levelManager.ActiveGrid.RemoveTile(secondTile.TileId);
                }

                pendingMatchedTiles.Remove(firstTile);
                pendingMatchedTiles.Remove(secondTile);
                firstTile.MarkRemoved();
                secondTile.MarkRemoved();
                RemoveTrayTileReference(firstTile);
                RemoveTrayTileReference(secondTile);

                if (rewardCoins)
                {
                    GetSaveManager()?.AddCoins(GetCoinsPerMatch());
                }

                ReflowSelectionTray();
                GetTileManager()?.RefreshTileExposure();
                Context.EventBus.Publish(new MatchSucceededEvent(firstTile, secondTile));
                PublishProgress();
                if (Context.Services.TryGet(out SaveManager saveManager) && saveManager.CurrentSave != null)
                {
                    Context.EventBus.Publish(new SaveDataLoadedEvent(saveManager.CurrentSave));
                }
            }
            finally
            {
                EndResolution();
                if (!TryStartNextQueuedMatch())
                {
                    EvaluateBoardState();
                }
            }
        }

        /// <summary>
        /// Resolves a mismatch by deselecting both tiles after a short delay.
        /// </summary>
        private IEnumerator ResolveMismatch(MahjongTile firstTile, MahjongTile secondTile)
        {
            if (!TryBeginResolution())
            {
                yield break;
            }

            try
            {
                GetAudioManager()?.PlayMismatch();
                bool completed = false;
                AnimationManager animationManager = null;
                Context.Services.TryGet(out animationManager);
                if (animationManager != null)
                {
                    animationManager.PlayMismatchDelay(() => completed = true);
                }
                else
                {
                    completed = true;
                }

                float elapsed = 0f;
                while (!completed && elapsed < AnimationCompletionTimeoutSeconds)
                {
                    elapsed += GetResolutionDeltaTime();
                    yield return null;
                }

                if (!completed)
                {
                    MahjongRuntimeLogger.LogWarning("Mismatch animation timed out. Completing resolution to avoid locked input.");
                }

                firstTile.Deselect();
                secondTile.Deselect();
            selectedTiles.Clear();
            Context.EventBus.Publish(new MatchFailedEvent(firstTile, secondTile));
        }
            finally
            {
                EndResolution();
            }
        }

        private IEnumerator ResolveMemoryMismatch(MahjongTile firstTile, MahjongTile secondTile)
        {
            if (!TryBeginResolution())
            {
                yield break;
            }

            CancelMemoryRevealTimeout();
            isMemoryRevealLocked = true;

            try
            {
                GetAudioManager()?.PlayMismatch();

                float elapsed = 0f;
                while (elapsed < MemoryRevealTimeoutSeconds)
                {
                    elapsed += GetResolutionDeltaTime();
                    yield return null;
                }

                bool firstCompleted = firstTile == null;
                bool secondCompleted = secondTile == null;

                if (firstTile != null)
                {
                    firstTile.Deselect();
                    if (ShouldRestoreMemoryTileFaceDown(firstTile))
                    {
                        firstTile.FlipFaceDown(() => firstCompleted = true);
                    }
                    else
                    {
                        firstCompleted = true;
                    }
                }

                if (secondTile != null)
                {
                    secondTile.Deselect();
                    if (ShouldRestoreMemoryTileFaceDown(secondTile))
                    {
                        secondTile.FlipFaceDown(() => secondCompleted = true);
                    }
                    else
                    {
                        secondCompleted = true;
                    }
                }

                elapsed = 0f;
                while ((!firstCompleted || !secondCompleted) && elapsed < AnimationCompletionTimeoutSeconds)
                {
                    elapsed += GetResolutionDeltaTime();
                    yield return null;
                }

                memorySelectedTiles.Clear();
                ClearMemorySelectionOriginalFaceState(firstTile);
                ClearMemorySelectionOriginalFaceState(secondTile);
                Context.EventBus.Publish(new MatchFailedEvent(firstTile, secondTile));
            }
            finally
            {
                isMemoryRevealLocked = false;
                EndResolution();
            }
        }

        /// <summary>
        /// Gets a stable delta time for resolution watchdog timers.
        /// </summary>
        private static float GetResolutionDeltaTime()
        {
            return Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        /// <summary>
        /// Attempts to find any valid selectable pair among currently exposed tiles.
        /// </summary>
        private bool TryFindAnySelectablePair(out MahjongTile firstTile, out MahjongTile secondTile)
        {
            firstTile = null;
            secondTile = null;

            TileManager tileManager = GetTileManager();
            if (tileManager == null)
            {
                return false;
            }

            for (int trayIndex = 0; trayIndex < selectedTiles.Count; trayIndex++)
            {
                MahjongTile trayTile = selectedTiles[trayIndex];
                if (trayTile == null || trayTile.IsRemoved)
                {
                    continue;
                }

                foreach (MahjongTile tile in tileManager.GetExposedTiles())
                {
                    if (tile == null || tile == trayTile || tile.IsRemoved || tile.IsBufferedSelection || !tileManager.IsTileHintSelectable(tile))
                    {
                        continue;
                    }

                    if (tile.HasSameVisualIdentity(trayTile))
                    {
                        firstTile = trayTile;
                        secondTile = tile;
                        return true;
                    }
                }
            }

            List<MahjongTile> hintCandidates = GetOrderedHintCandidates(tileManager);
            Dictionary<string, MahjongTile> firstByVisualKey = new Dictionary<string, MahjongTile>();
            for (int index = 0; index < hintCandidates.Count; index++)
            {
                MahjongTile tile = hintCandidates[index];
                if (tile == null || tile.IsRemoved || tile.IsBufferedSelection || !tileManager.IsTileHintSelectable(tile))
                {
                    continue;
                }

                string visualKey = tile.VisualMatchKey;
                if (firstByVisualKey.TryGetValue(visualKey, out MahjongTile existingTile) && existingTile != tile)
                {
                    firstTile = existingTile;
                    secondTile = tile;
                    return true;
                }

                firstByVisualKey[visualKey] = tile;
            }

            return false;
        }

        /// <summary>
        /// Gets hint candidates ordered from the outermost visible shell inward.
        /// </summary>
        private List<MahjongTile> GetOrderedHintCandidates(TileManager tileManager)
        {
            List<MahjongTile> hintCandidates = new List<MahjongTile>();
            if (tileManager == null)
            {
                return hintCandidates;
            }

            foreach (MahjongTile tile in tileManager.GetExposedTiles())
            {
                hintCandidates.Add(tile);
            }

            bool useSurfaceRules = Context.Services.TryGet(out LevelManager levelManager) && levelManager.ActiveUsesSurfaceTilePlacement;
            hintCandidates.Sort((left, right) => CompareHintCandidates(left, right, useSurfaceRules));
            return hintCandidates;
        }

        /// <summary>
        /// Orders hint candidates so auto-resolve prefers the outer shell first.
        /// </summary>
        private static int CompareHintCandidates(MahjongTile left, MahjongTile right, bool useSurfaceRules)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            if (useSurfaceRules)
            {
                int shellComparison = left.SurfaceShellIndex.CompareTo(right.SurfaceShellIndex);
                if (shellComparison != 0)
                {
                    return shellComparison;
                }
            }

            return left.TileId.CompareTo(right.TileId);
        }

        /// <summary>
        /// Deselects every currently selected tile.
        /// </summary>
        private void DeselectAll()
        {
            CancelMemoryRevealTimeout();

            LevelManager levelManager = GetLevelManager();
            for (int index = 0; index < selectedTiles.Count; index++)
            {
                MahjongTile tile = selectedTiles[index];
                if (tile != null)
                {
                    RestoreTileFromSelectionTray(tile, levelManager);
                }
            }

            selectedTiles.Clear();
            HideMemorySelectedTilesInstant();
            GetTileManager()?.RefreshTileExposure();
        }

        private void HideMemorySelectedTilesInstant()
        {
            for (int index = 0; index < memorySelectedTiles.Count; index++)
            {
                MahjongTile tile = memorySelectedTiles[index];
                if (tile == null)
                {
                    continue;
                }

                tile.StopFaceFlipAnimation(false);
                tile.Deselect();
                tile.SetFaceDown(ShouldRestoreMemoryTileFaceDown(tile), true);
                ClearMemorySelectionOriginalFaceState(tile);
            }

            memorySelectedTiles.Clear();
            isMemoryRevealLocked = false;
        }

        private void ScheduleMemoryRevealTimeout()
        {
            CancelMemoryRevealTimeout();
            if (!UsesMemoryFlipSelectionMode() || memorySelectedTiles.Count != 1)
            {
                return;
            }

            memoryRevealTimeoutRoutine = StartCoroutine(MemoryRevealTimeoutRoutine());
        }

        private IEnumerator MemoryRevealTimeoutRoutine()
        {
            float elapsed = 0f;
            while (elapsed < MemoryRevealTimeoutSeconds)
            {
                if (!UsesMemoryFlipSelectionMode() || memorySelectedTiles.Count != 1 || IsResolvingMatch || isMemoryRevealLocked)
                {
                    memoryRevealTimeoutRoutine = null;
                    yield break;
                }

                elapsed += GetResolutionDeltaTime();
                yield return null;
            }

            if (memorySelectedTiles.Count == 1)
            {
                MahjongTile tile = memorySelectedTiles[0];
                bool completed = tile == null;
                isMemoryRevealLocked = true;

                if (tile != null)
                {
                    tile.Deselect();
                    if (ShouldRestoreMemoryTileFaceDown(tile))
                    {
                        tile.FlipFaceDown(() => completed = true);
                    }
                    else
                    {
                        completed = true;
                    }
                }

                elapsed = 0f;
                while (!completed && elapsed < AnimationCompletionTimeoutSeconds)
                {
                    elapsed += GetResolutionDeltaTime();
                    yield return null;
                }

                memorySelectedTiles.Clear();
                ClearMemorySelectionOriginalFaceState(tile);
                isMemoryRevealLocked = false;
            }

            memoryRevealTimeoutRoutine = null;
        }

        private void CancelMemoryRevealTimeout()
        {
            if (memoryRevealTimeoutRoutine != null)
            {
                StopCoroutine(memoryRevealTimeoutRoutine);
                memoryRevealTimeoutRoutine = null;
            }
        }

        private void ApplyDifficultyTileFaceState()
        {
            TileManager tileManager = GetTileManager();
            if (tileManager == null)
            {
                return;
            }

            List<MahjongTile> tiles = new List<MahjongTile>();
            foreach (MahjongTile tile in tileManager.GetAllTiles())
            {
                if (tile == null || tile.IsRemoved || tile.IsMatched)
                {
                    continue;
                }

                tiles.Add(tile);
            }

            if (tiles.Count == 0)
            {
                return;
            }

            float faceDownRatio = GetMemoryFaceDownRatio();
            int faceDownCount = Mathf.Clamp(Mathf.RoundToInt(tiles.Count * faceDownRatio), 0, tiles.Count);

            for (int index = tiles.Count - 1; index > 0; index--)
            {
                int swapIndex = Random.Range(0, index + 1);
                (tiles[index], tiles[swapIndex]) = (tiles[swapIndex], tiles[index]);
            }

            for (int index = 0; index < tiles.Count; index++)
            {
                MahjongTile tile = tiles[index];
                tile.StopFaceFlipAnimation(false);
                tile.SetFaceDown(index < faceDownCount, true);
            }
        }

        private float GetMemoryFaceDownRatio()
        {
            LevelManager levelManager = GetLevelManager();
            if (levelManager?.ActiveLevelDefinition == null)
            {
                return 0f;
            }

            switch (levelManager.ActiveLevelDefinition.Difficulty)
            {
                case LevelDifficulty.Hard:
                    return hardMemoryFaceDownRatio;
                case LevelDifficulty.Expert:
                    return expertMemoryFaceDownRatio;
                default:
                    return 0f;
            }
        }

        private bool UsesMemoryFlipSelectionMode()
        {
            LevelManager levelManager = GetLevelManager();
            LevelDifficulty? difficulty = levelManager != null && levelManager.ActiveLevelDefinition != null
                ? levelManager.ActiveLevelDefinition.Difficulty
                : null;

            return difficulty == LevelDifficulty.Hard || difficulty == LevelDifficulty.Expert;
        }

        /// <summary>
        /// Publishes gameplay progress based on the number of remaining tiles.
        /// </summary>
        private void PublishProgress()
        {
            TileManager tileManager = GetTileManager();
            if (tileManager == null)
            {
                return;
            }

            int remainingTileCount = 0;
            foreach (MahjongTile tile in tileManager.GetRemainingTiles())
            {
                if (tile != null && !tile.IsRemoved)
                {
                    remainingTileCount++;
                }
            }

            Context.EventBus.Publish(new GameplayProgressChangedEvent(remainingTileCount, totalTiles));
        }

        /// <summary>
        /// Evaluates the current board for win or no-moves conditions.
        /// </summary>
        private void EvaluateBoardState()
        {
            TileManager tileManager = GetTileManager();
            GameManager gameManager = GetGameManager();
            if (tileManager == null || gameManager == null)
            {
                return;
            }

            int remainingTileCount = 0;
            foreach (MahjongTile tile in tileManager.GetRemainingTiles())
            {
                if (tile != null && !tile.IsRemoved)
                {
                    remainingTileCount++;
                }
            }

            if (remainingTileCount == 0)
            {
                GetSaveManager()?.AddCoins(GetCoinsPerLevelWin());
                IPlayerInfoController.Instance.WinLevel();
                GetAudioManager()?.PlayWin();
                gameManager.WinGameplay();
                return;
            }

            if (!TryFindAnyBoardPair(out _, out _))
            {
                Context.EventBus.Publish(new NoMovesRemainingEvent());
                GetAudioManager()?.PlayLose();
                gameManager.LoseGameplay();
            }
        }

        /// <summary>
        /// Attempts to find any remaining board pair without depending on the current camera visibility.
        /// </summary>
        private bool TryFindAnyBoardPair(out MahjongTile firstTile, out MahjongTile secondTile)
        {
            firstTile = null;
            secondTile = null;

            TileManager tileManager = GetTileManager();
            if (tileManager == null)
            {
                return false;
            }

            for (int trayIndex = 0; trayIndex < selectedTiles.Count; trayIndex++)
            {
                MahjongTile trayTile = selectedTiles[trayIndex];
                if (trayTile == null || trayTile.IsRemoved)
                {
                    continue;
                }

                foreach (MahjongTile tile in tileManager.GetRemainingTiles())
                {
                    if (tile == null || tile == trayTile || tile.IsRemoved || tile.IsBufferedSelection || !IsTileAvailableForBoardPair(tileManager, tile))
                    {
                        continue;
                    }

                    if (tile.HasSameVisualIdentity(trayTile))
                    {
                        firstTile = trayTile;
                        secondTile = tile;
                        return true;
                    }
                }
            }

            Dictionary<string, MahjongTile> firstByVisualKey = new Dictionary<string, MahjongTile>();
            foreach (MahjongTile tile in tileManager.GetRemainingTiles())
            {
                if (tile == null || tile.IsRemoved || tile.IsBufferedSelection || !IsTileAvailableForBoardPair(tileManager, tile))
                {
                    continue;
                }

                string visualKey = tile.VisualMatchKey;
                if (firstByVisualKey.TryGetValue(visualKey, out MahjongTile existingTile) && existingTile != tile)
                {
                    firstTile = existingTile;
                    secondTile = tile;
                    return true;
                }

                firstByVisualKey[visualKey] = tile;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a tile should count toward board move availability checks.
        /// </summary>
        private bool IsTileAvailableForBoardPair(TileManager tileManager, MahjongTile tile)
        {
            if (tileManager == null || tile == null || !tile.IsInteractable || tile.IsBufferedSelection)
            {
                return false;
            }

            bool useSurfaceRules = Context.Services.TryGet(out LevelManager levelManager) && levelManager.ActiveUsesSurfaceTilePlacement;
            if (useSurfaceRules)
            {
                return tileManager.IsTileExposed(tile);
            }

            return tileManager.IsTileExposed(tile);
        }

        /// <summary>
        /// Captures tile state snapshots for a reversible move.
        /// </summary>
        private static MoveHistoryRecord CreateSnapshotRecord(string actionName, IEnumerable<MahjongTile> tiles)
        {
            MoveHistoryRecord record = new MoveHistoryRecord { actionName = actionName };
            foreach (MahjongTile tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                record.snapshots.Add(new TileStateSnapshot
                {
                    tileId = tile.TileId,
                    matchId = tile.MatchId,
                    gridCoordinate = tile.GridCoordinate,
                    localPosition = tile.BoardLocalPosition,
                    localEulerAngles = tile.BoardLocalEulerAngles,
                    state = tile.State,
                    isBufferedSelection = tile.IsBufferedSelection,
                    isFaceDown = tile.IsFaceDown,
                    fillTexture = tile.FillTexture,
                });
            }

            return record;
        }

        /// <summary>
        /// Captures the pre-tray board state for a single tile selection.
        /// </summary>
        private MoveHistoryRecord CreateSelectionSnapshotRecord(MahjongTile tile, bool isMemoryRevealSelection = false)
        {
            MoveHistoryRecord record = CreateSnapshotRecord("Select", new[] { tile });
            for (int index = 0; index < record.snapshots.Count; index++)
            {
                record.snapshots[index].state = TileState.Visible;
                record.snapshots[index].isBufferedSelection = false;
            }

            return record;
        }

        /// <summary>
        /// Restores a previously captured history record.
        /// </summary>
        private void RestoreHistoryRecord(MoveHistoryRecord record)
        {
            if (record == null || record.snapshots == null || record.snapshots.Count == 0)
            {
                return;
            }

            LevelManager levelManager = GetLevelManager();
            TileManager tileManager = GetTileManager();
            if (levelManager?.ActiveGrid == null || tileManager == null)
            {
                return;
            }

            selectedTiles.Clear();

            for (int index = 0; index < record.snapshots.Count; index++)
            {
                TileStateSnapshot snapshot = record.snapshots[index];
                levelManager.ActiveGrid.RemoveTile(snapshot.tileId);
            }

            for (int index = 0; index < record.snapshots.Count; index++)
            {
                TileStateSnapshot snapshot = record.snapshots[index];
                if (!tileManager.TryGetTile(snapshot.tileId, out MahjongTile tile) || tile == null)
                {
                    continue;
                }

                tile.SetMatchId(snapshot.matchId);
                tile.SetupFillTexture(snapshot.fillTexture);
                tile.SetGridCoordinate(snapshot.gridCoordinate);
                tile.StopFaceFlipAnimation(false);
                bool restoreFaceDown = snapshot.isFaceDown;
                tile.SetFaceDown(restoreFaceDown, true);

                if (snapshot.isBufferedSelection)
                {
                    tile.Restore(true);
                    tile.SetBufferedSelection(true);
                    tile.DetachFromBoardParent();
                    if (snapshot.state == TileState.Selected)
                    {
                        tile.TrySelect();
                    }

                    if (tile.TileCollider != null)
                    {
                        tile.TileCollider.enabled = false;
                    }

                    selectedTiles.Add(tile);
                    continue;
                }

                tile.SetBufferedSelection(false);
                tile.RestoreBoardParent();
                tile.SetLocalPose(snapshot.localPosition, snapshot.localEulerAngles);
                tile.Restore(snapshot.state != TileState.Hidden);
                if (snapshot.state == TileState.Selected)
                {
                    tile.TrySelect();
                }

                if (snapshot.state != TileState.Hidden && snapshot.state != TileState.Removed && snapshot.state != TileState.Matched)
                {
                    levelManager.ActiveGrid.TryPlaceTile(snapshot.tileId, snapshot.gridCoordinate);
                }

                if (tile.TileCollider != null)
                {
                    tile.TileCollider.enabled = !tile.IsBufferedSelection && !tile.IsRemoved && !tile.IsMatched;
                }
            }

            ReflowSelectionTray();
        }

        /// <summary>
        /// Restores the most recently buffered tray tile back to its original board slot.
        /// </summary>
        private void RestoreSelectionRecord(MoveHistoryRecord record)
        {
            if (record == null || record.snapshots == null || record.snapshots.Count == 0)
            {
                return;
            }

            LevelManager levelManager = GetLevelManager();
            TileManager tileManager = GetTileManager();
            if (levelManager?.ActiveGrid == null || tileManager == null)
            {
                return;
            }

            for (int index = 0; index < record.snapshots.Count; index++)
            {
                TileStateSnapshot snapshot = record.snapshots[index];
                if (!tileManager.TryGetTile(snapshot.tileId, out MahjongTile tile) || tile == null)
                {
                    continue;
                }

                RemoveTrayTileReference(tile);
                levelManager.ActiveGrid.RemoveTile(snapshot.tileId);

                tile.SetMatchId(snapshot.matchId);
                tile.SetupFillTexture(snapshot.fillTexture);
                tile.SetGridCoordinate(snapshot.gridCoordinate);
                tile.SetBufferedSelection(false);
                tile.RestoreBoardParent();
                tile.SetLocalPose(snapshot.localPosition, snapshot.localEulerAngles);
                tile.Restore(snapshot.state != TileState.Hidden);
                tile.StopFaceFlipAnimation(false);
                bool restoreFaceDown = snapshot.isFaceDown;
                tile.SetFaceDown(restoreFaceDown, true);
                tile.Deselect();

                if (snapshot.state != TileState.Hidden && snapshot.state != TileState.Removed && snapshot.state != TileState.Matched)
                {
                    levelManager.ActiveGrid.TryPlaceTile(snapshot.tileId, snapshot.gridCoordinate);
                }

                if (tile.TileCollider != null)
                {
                    tile.TileCollider.enabled = !tile.IsRemoved && !tile.IsMatched;
                }
            }

            ReflowSelectionTray();
        }

        private void RestoreMemorySelectionRecord(MoveHistoryRecord record)
        {
            TileManager tileManager = GetTileManager();
            if (tileManager == null)
            {
                return;
            }

            CancelMemoryRevealTimeout();

            for (int index = 0; index < record.snapshots.Count; index++)
            {
                TileStateSnapshot snapshot = record.snapshots[index];
                if (!tileManager.TryGetTile(snapshot.tileId, out MahjongTile tile) || tile == null)
                {
                    continue;
                }

                RemoveTrayTileReference(tile);
                tile.StopFaceFlipAnimation(false);
                bool restoreFaceDown = snapshot.isFaceDown;
                tile.SetFaceDown(restoreFaceDown, true);
                tile.Restore(snapshot.state != TileState.Hidden);
                tile.Deselect();
            }

            isMemoryRevealLocked = false;
            memorySelectedTiles.Clear();
            GetTileManager()?.RefreshTileExposure();
        }

        /// <summary>
        /// Moves a tapped tile into the temporary 4-slot tray and resolves a match when possible.
        /// </summary>
        private IEnumerator MoveTileIntoSelectionTray(MahjongTile tappedTile, MahjongTile matchingTrayTile)
        {
            if (tappedTile == null)
            {
                yield break;
            }

            history.Push(CreateSelectionSnapshotRecord(tappedTile));

            if (tappedTile.IsFaceDown)
            {
                bool flipCompleted = false;
                tappedTile.FlipFaceUp(() => flipCompleted = true);

                float flipElapsed = 0f;
                while (!flipCompleted && flipElapsed < AnimationCompletionTimeoutSeconds)
                {
                    flipElapsed += GetResolutionDeltaTime();
                    yield return null;
                }
            }

            CommitTileToSelectionTray(tappedTile);

            bool usesNewTraySlot = matchingTrayTile == null || selectedTiles.Count < SelectionTrayCapacity;
            int targetSlotIndex = usesNewTraySlot ? Mathf.Clamp(selectedTiles.Count, 0, SelectionTrayCapacity - 1) : Mathf.Max(0, selectedTiles.IndexOf(matchingTrayTile));
            if (usesNewTraySlot)
            {
                selectedTiles.Add(tappedTile);
            }

            GetAudioManager()?.PlaySelect();

            bool completed = false;
            AnimationManager animationManager = GetAnimationManager();
            if (animationManager != null)
            {
                animationManager.PlayMoveToTray(tappedTile, targetSlotIndex, () => completed = true);
            }
            else
            {
                completed = true;
            }

            float elapsed = 0f;
            while (!completed && elapsed < AnimationCompletionTimeoutSeconds)
            {
                elapsed += GetResolutionDeltaTime();
                yield return null;
            }

            if (matchingTrayTile != null)
            {
                QueueMatchedPairForResolution(matchingTrayTile, tappedTile, true);
            }
        }

        private void QueueMatchedPairForResolution(MahjongTile firstTile, MahjongTile secondTile, bool rewardCoins)
        {
            if (firstTile == null || secondTile == null)
            {
                return;
            }

            ReservePairForMatchResolution(firstTile, secondTile);

            PendingMatchResolution pendingResolution = new PendingMatchResolution(firstTile, secondTile, rewardCoins);
            if (IsResolvingMatch)
            {
                pendingMatchQueue.Enqueue(pendingResolution);
                return;
            }

            StartCoroutine(ResolveMatchedPair(firstTile, secondTile, rewardCoins));
        }

        private void ReservePairForMatchResolution(MahjongTile firstTile, MahjongTile secondTile)
        {
            if (firstTile == null || secondTile == null)
            {
                return;
            }

            pendingMatchedTiles.Add(firstTile);
            pendingMatchedTiles.Add(secondTile);
            ClearMemorySelectionOriginalFaceState(firstTile);
            ClearMemorySelectionOriginalFaceState(secondTile);

            RemoveTrayTileReference(firstTile);
            RemoveTrayTileReference(secondTile);
            firstTile.Deselect();
            secondTile.Deselect();

            if (!firstTile.IsBufferedSelection && firstTile.TileCollider != null)
            {
                firstTile.TileCollider.enabled = false;
            }

            if (!secondTile.IsBufferedSelection && secondTile.TileCollider != null)
            {
                secondTile.TileCollider.enabled = false;
            }

            ReflowSelectionTray();
        }

        private bool TryStartNextQueuedMatch()
        {
            while (pendingMatchQueue.Count > 0)
            {
                PendingMatchResolution pendingResolution = pendingMatchQueue.Dequeue();
                if (pendingResolution.First == null || pendingResolution.Second == null)
                {
                    continue;
                }

                StartCoroutine(ResolveMatchedPair(pendingResolution.First, pendingResolution.Second, pendingResolution.RewardCoins));
                return true;
            }

            return false;
        }

        private bool IsTilePendingMatch(MahjongTile tile)
        {
            return tile != null && pendingMatchedTiles.Contains(tile);
        }

        private bool TryPopLatestSelectionRecord(out MoveHistoryRecord record)
        {
            record = null;
            if (history.Count == 0)
            {
                return false;
            }

            Stack<MoveHistoryRecord> skippedRecords = new Stack<MoveHistoryRecord>();
            while (history.Count > 0)
            {
                MoveHistoryRecord candidate = history.Pop();
                if (candidate != null && candidate.actionName == "Select")
                {
                    record = candidate;
                    break;
                }

                skippedRecords.Push(candidate);
            }

            while (skippedRecords.Count > 0)
            {
                history.Push(skippedRecords.Pop());
            }

            return record != null;
        }

        private bool HasSelectionHistoryRecord()
        {
            foreach (MoveHistoryRecord record in history)
            {
                if (record != null && record.actionName == "Select")
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldRestoreMemoryTileFaceDown(MahjongTile tile)
        {
            return tile != null
                && memorySelectionOriginalFaceStates.TryGetValue(tile, out bool wasFaceDown)
                && wasFaceDown;
        }

        private void ClearMemorySelectionOriginalFaceState(MahjongTile tile)
        {
            if (tile != null)
            {
                memorySelectionOriginalFaceStates.Remove(tile);
            }
        }

        /// <summary>
        /// Removes a tile from the board grid and marks it as buffered inside the temporary selection tray.
        /// </summary>
        private void CommitTileToSelectionTray(MahjongTile tile)
        {
            if (tile == null)
            {
                return;
            }

            tile.SetBufferedSelection(true);
            tile.DetachFromBoardParent();
            LevelManager levelManager = GetLevelManager();
            if (levelManager?.ActiveGrid != null)
            {
                levelManager.ActiveGrid.RemoveTile(tile.TileId);
            }

            if (tile.TileCollider != null)
            {
                tile.TileCollider.enabled = false;
            }

            GetTileManager()?.RefreshTileExposure();
        }

        /// <summary>
        /// Restores a buffered tray tile back onto the board grid.
        /// </summary>
        private void RestoreTileFromSelectionTray(MahjongTile tile, LevelManager levelManager)
        {
            if (tile == null)
            {
                return;
            }

            tile.SetBufferedSelection(false);
            tile.RestoreBoardParent();
            tile.RestoreBoardPose();
            if (levelManager?.ActiveGrid != null)
            {
                levelManager.ActiveGrid.TryPlaceTile(tile.TileId, tile.GridCoordinate);
            }

            tile.Deselect();
        }

        /// <summary>
        /// Finds an already buffered tray tile with the same visible image.
        /// </summary>
        private MahjongTile FindMatchingTrayTile(MahjongTile sourceTile)
        {
            if (sourceTile == null)
            {
                return null;
            }

            for (int index = 0; index < selectedTiles.Count; index++)
            {
                MahjongTile tile = selectedTiles[index];
                if (tile != null && !tile.IsRemoved && tile.HasSameVisualIdentity(sourceTile))
                {
                    return tile;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes a matched tile reference from the tray list when present.
        /// </summary>
        private void RemoveTrayTileReference(MahjongTile tile)
        {
            if (tile != null)
            {
                selectedTiles.Remove(tile);
            }
        }

        /// <summary>
        /// Repositions the remaining buffered tray tiles into compact slot order.
        /// </summary>
        private void ReflowSelectionTray()
        {
            AnimationManager animationManager = GetAnimationManager();
            if (animationManager == null)
            {
                return;
            }

            for (int index = 0; index < selectedTiles.Count; index++)
            {
                MahjongTile tile = selectedTiles[index];
                if (tile != null)
                {
                    animationManager.SnapToTray(tile, index);
                }
            }
        }

        /// <summary>
        /// Randomizes remaining pair identities while preserving the current block layout.
        /// </summary>
        private static void ShuffleTileMatches(List<MahjongTile> tiles)
        {
            if (tiles == null || tiles.Count <= 1)
            {
                return;
            }

            Dictionary<string, List<MahjongTile>> tilesByVisualKey = new Dictionary<string, List<MahjongTile>>();
            for (int index = 0; index < tiles.Count; index++)
            {
                MahjongTile tile = tiles[index];
                if (tile == null)
                {
                    continue;
                }

                string visualKey = tile.VisualMatchKey;
                if (!tilesByVisualKey.TryGetValue(visualKey, out List<MahjongTile> pairTiles))
                {
                    pairTiles = new List<MahjongTile>();
                    tilesByVisualKey.Add(visualKey, pairTiles);
                }

                pairTiles.Add(tile);
            }

            List<PairShuffleGroup> groups = new List<PairShuffleGroup>(tilesByVisualKey.Count);
            foreach (KeyValuePair<string, List<MahjongTile>> pair in tilesByVisualKey)
            {
                if (pair.Value == null || pair.Value.Count == 0)
                {
                    continue;
                }

                groups.Add(new PairShuffleGroup
                {
                    MatchId = pair.Value[0].MatchId,
                    VisualKey = pair.Key,
                    FillTexture = pair.Value[0].FillTexture,
                    Tiles = pair.Value,
                });
            }

            List<PairShuffleGroup> targetGroups = new List<PairShuffleGroup>(groups);
            List<PairShuffleGroup> sourceGroups = new List<PairShuffleGroup>(groups);

            for (int index = sourceGroups.Count - 1; index > 0; index--)
            {
                int swapIndex = Random.Range(0, index + 1);
                (sourceGroups[index], sourceGroups[swapIndex]) = (sourceGroups[swapIndex], sourceGroups[index]);
            }

            for (int index = 0; index < targetGroups.Count; index++)
            {
                PairShuffleGroup targetGroup = targetGroups[index];
                PairShuffleGroup sourceGroup = sourceGroups[index];
                if (targetGroup.Tiles == null)
                {
                    continue;
                }

                for (int tileIndex = 0; tileIndex < targetGroup.Tiles.Count; tileIndex++)
                {
                    MahjongTile tile = targetGroup.Tiles[tileIndex];
                    if (tile == null)
                    {
                        continue;
                    }

                    tile.SetMatchId(sourceGroup.MatchId);
                    tile.SetupFillTexture(sourceGroup.FillTexture);
                }
            }
        }

        private sealed class PairShuffleGroup
        {
            public int MatchId { get; set; }

            public string VisualKey { get; set; }

            public Texture2D FillTexture { get; set; }

            public List<MahjongTile> Tiles { get; set; }
        }

        private readonly struct PendingMatchResolution
        {
            public PendingMatchResolution(MahjongTile first, MahjongTile second, bool rewardCoins)
            {
                First = first;
                Second = second;
                RewardCoins = rewardCoins;
            }

            public MahjongTile First { get; }

            public MahjongTile Second { get; }

            public bool RewardCoins { get; }
        }

        /// <summary>
        /// Consumes power-up currency cost when possible.
        /// </summary>
        private bool TryUsePowerUp(PowerUpType powerUpType, int cost)
        {
            SaveManager saveManager = GetSaveManager();
            if (saveManager == null)
            {
                return false;
            }

            return saveManager.TrySpendCoins(cost);
        }

        private int GetCoinsPerMatch() => powerUpSettings != null ? powerUpSettings.CoinsPerMatch : 2;
        private int GetCoinsPerLevelWin() => powerUpSettings != null ? powerUpSettings.CoinsPerLevelWin : 30;
        private int GetHintCost() => powerUpSettings != null ? powerUpSettings.HintCost : 10;
        private int GetUndoCost() => powerUpSettings != null ? powerUpSettings.UndoCost : 15;
        private int GetShuffleCost() => powerUpSettings != null ? powerUpSettings.ShuffleCost : 20;
        private int GetBombCost() => powerUpSettings != null ? powerUpSettings.BombCost : 25;
        private int GetXRayCost() => powerUpSettings != null ? powerUpSettings.XRayCost : 18;
        private float GetXRayDurationSeconds() => 5f;
        private int GetXRayRevealDepth() => 1;

        private TileManager GetTileManager() => Context.Services.Get<TileManager>();
        private LevelManager GetLevelManager() => Context.Services.Get<LevelManager>();
        private GameManager GetGameManager() => GameManager.Instance;
        private AnimationManager GetAnimationManager() => Context.Services.Get<AnimationManager>();
        private AudioManager GetAudioManager() => Context.Services.Get<AudioManager>();
        private SaveManager GetSaveManager() => Context.Services.Get<SaveManager>();
    }
}
