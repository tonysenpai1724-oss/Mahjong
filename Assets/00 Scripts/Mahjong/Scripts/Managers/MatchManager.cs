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

        [SerializeField] private PowerUpSettings powerUpSettings;

        private readonly List<MahjongTile> selectedTiles = new List<MahjongTile>(SelectionTrayCapacity);
        private readonly Stack<MoveHistoryRecord> history = new Stack<MoveHistoryRecord>();
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
            selectedTiles.Clear();
            history.Clear();
            totalTiles = 0;
            IsResolvingMatch = false;
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
            if (history.Count == 0 || !TryUsePowerUp(PowerUpType.Undo, GetUndoCost()))
            {
                return false;
            }

            MoveHistoryRecord record = history.Pop();
            RestoreHistoryRecord(record);
            DeselectAll();
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
            LevelManager levelManager = GetLevelManager();
            if (tileManager == null || levelManager?.ActiveGrid == null)
            {
                return false;
            }

            List<MahjongTile> remainingTiles = new List<MahjongTile>(tileManager.GetRemainingTiles());
            if (remainingTiles.Count <= 1)
            {
                return false;
            }

            if (!TryUsePowerUp(PowerUpType.Shuffle, GetShuffleCost()))
            {
                return false;
            }

            history.Push(CreateSnapshotRecord("Shuffle", remainingTiles));
            DeselectAll();

            List<Vector3Int> coordinates = new List<Vector3Int>(remainingTiles.Count);
            for (int index = 0; index < remainingTiles.Count; index++)
            {
                coordinates.Add(remainingTiles[index].GridCoordinate);
            }

            ShuffleCoordinates(coordinates);
            levelManager.ActiveGrid.Clear();

            for (int index = 0; index < remainingTiles.Count; index++)
            {
                MahjongTile tile = remainingTiles[index];
                Vector3Int coordinate = coordinates[index];
                tile.SetGridCoordinate(coordinate);
                tile.SetLocalPose(levelManager.ActiveGrid.GetLocalPosition(coordinate), tile.transform.localEulerAngles);
                tile.Restore(true);
                levelManager.ActiveGrid.TryPlaceTile(tile.TileId, coordinate);
            }

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
            GameManager gameManager = GetGameManager();
            TileManager tileManager = GetTileManager();
            AnimationManager animationManager = GetAnimationManager();
            if (gameManager == null || tileManager == null || gameManager.CurrentState != GameFlowState.Gameplay || IsResolvingMatch || (animationManager != null && animationManager.IsAnimationLocked))
            {
                return;
            }

            MahjongTile tappedTile = eventData.Tile;
            if (tappedTile == null || !tileManager.IsTileSelectable(tappedTile))
            {
                return;
            }

            if (selectedTiles.Contains(tappedTile))
            {
                return;
            }

            MahjongTile matchingTrayTile = FindMatchingTrayTile(tappedTile.MatchId);
            if (selectedTiles.Count >= SelectionTrayCapacity && matchingTrayTile == null)
            {
                GetAudioManager()?.PlayLose();
                gameManager.LoseGameplay();
                return;
            }

            if (!tappedTile.TrySelect())
            {
                return;
            }

            StartCoroutine(MoveTileIntoSelectionTray(tappedTile, matchingTrayTile));
        }

        /// <summary>
        /// Resets runtime state and starts gameplay after a new level has been generated.
        /// </summary>
        private void HandleLevelGenerated(LevelGeneratedEvent eventData)
        {
            selectedTiles.Clear();
            history.Clear();
            totalTiles = eventData.SpawnedTileCount;
            GetGameManager()?.StartGameplay();
            PublishProgress();
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
                MoveHistoryRecord record = CreateSnapshotRecord("Match", new[] { firstTile, secondTile });
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

                firstTile.MarkRemoved();
                secondTile.MarkRemoved();
                RemoveTrayTileReference(firstTile);
                RemoveTrayTileReference(secondTile);
                history.Push(record);

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

                EvaluateBoardState();
            }
            finally
            {
                EndResolution();
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

                    if (tile.MatchId == trayTile.MatchId)
                    {
                        firstTile = trayTile;
                        secondTile = tile;
                        return true;
                    }
                }
            }

            List<MahjongTile> hintCandidates = GetOrderedHintCandidates(tileManager);
            Dictionary<int, MahjongTile> firstByMatchId = new Dictionary<int, MahjongTile>();
            for (int index = 0; index < hintCandidates.Count; index++)
            {
                MahjongTile tile = hintCandidates[index];
                if (tile == null || tile.IsRemoved || tile.IsBufferedSelection || !tileManager.IsTileHintSelectable(tile))
                {
                    continue;
                }

                if (firstByMatchId.TryGetValue(tile.MatchId, out MahjongTile existingTile) && existingTile != tile)
                {
                    firstTile = existingTile;
                    secondTile = tile;
                    return true;
                }

                firstByMatchId[tile.MatchId] = tile;
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
            GetTileManager()?.RefreshTileExposure();
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
                GetSaveManager()?.MarkLevelCompleted(GetLevelManager() != null ? GetLevelManager().CurrentLevelIndex : 0);
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

                    if (tile.MatchId == trayTile.MatchId)
                    {
                        firstTile = trayTile;
                        secondTile = tile;
                        return true;
                    }
                }
            }

            Dictionary<int, MahjongTile> firstByMatchId = new Dictionary<int, MahjongTile>();
            foreach (MahjongTile tile in tileManager.GetRemainingTiles())
            {
                if (tile == null || tile.IsRemoved || tile.IsBufferedSelection || !IsTileAvailableForBoardPair(tileManager, tile))
                {
                    continue;
                }

                if (firstByMatchId.TryGetValue(tile.MatchId, out MahjongTile existingTile) && existingTile != tile)
                {
                    firstTile = existingTile;
                    secondTile = tile;
                    return true;
                }

                firstByMatchId[tile.MatchId] = tile;
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
                });
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

                tile.SetGridCoordinate(snapshot.gridCoordinate);
                tile.SetBufferedSelection(false);
                tile.SetLocalPose(snapshot.localPosition, snapshot.localEulerAngles);
                levelManager.ActiveGrid.TryPlaceTile(snapshot.tileId, snapshot.gridCoordinate);
                tile.Restore(snapshot.state != TileState.Hidden);
            }
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
                StartCoroutine(ResolveMatchedPair(matchingTrayTile, tappedTile, true));
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
        /// Finds an already buffered tray tile with the supplied match id.
        /// </summary>
        private MahjongTile FindMatchingTrayTile(int matchId)
        {
            for (int index = 0; index < selectedTiles.Count; index++)
            {
                MahjongTile tile = selectedTiles[index];
                if (tile != null && !tile.IsRemoved && tile.MatchId == matchId)
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
        /// Randomizes a list of coordinates in-place.
        /// </summary>
        private static void ShuffleCoordinates(List<Vector3Int> coordinates)
        {
            for (int index = coordinates.Count - 1; index > 0; index--)
            {
                int swapIndex = Random.Range(0, index + 1);
                (coordinates[index], coordinates[swapIndex]) = (coordinates[swapIndex], coordinates[index]);
            }
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
        private GameManager GetGameManager() => Context.Services.Get<GameManager>();
        private AnimationManager GetAnimationManager() => Context.Services.Get<AnimationManager>();
        private AudioManager GetAudioManager() => Context.Services.Get<AudioManager>();
        private SaveManager GetSaveManager() => Context.Services.Get<SaveManager>();
    }
}
