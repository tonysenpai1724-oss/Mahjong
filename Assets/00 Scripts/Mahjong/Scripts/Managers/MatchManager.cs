using MahjongOut3D.Core;
using MahjongOut3D.Data;
using MahjongOut3D.Gameplay;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.TileSystem;
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
        [SerializeField] private PowerUpSettings powerUpSettings;

        private readonly List<MahjongTile> selectedTiles = new List<MahjongTile>(2);
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

            DeselectAll();
            GetAudioManager()?.PlayPowerUp(PowerUpType.Bomb);
            StartCoroutine(ResolveMatchedPair(firstTile, secondTile, false));
            Context.EventBus.Publish(new PowerUpUsedEvent(PowerUpType.Bomb));
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
            if (gameManager == null || tileManager == null || gameManager.CurrentState != GameFlowState.Gameplay || IsResolvingMatch)
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

            if (!tappedTile.TrySelect())
            {
                return;
            }

            selectedTiles.Add(tappedTile);
            GetAudioManager()?.PlaySelect();

            if (selectedTiles.Count < 2)
            {
                return;
            }

            MahjongTile firstTile = selectedTiles[0];
            MahjongTile secondTile = selectedTiles[1];
            if (firstTile.MatchId == secondTile.MatchId)
            {
                StartCoroutine(ResolveMatchedPair(firstTile, secondTile, true));
            }
            else
            {
                StartCoroutine(ResolveMismatch(firstTile, secondTile));
            }
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

            MoveHistoryRecord record = CreateSnapshotRecord("Match", new[] { firstTile, secondTile });
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

            while (!completed)
            {
                yield return null;
            }

            LevelManager levelManager = GetLevelManager();
            if (levelManager?.ActiveGrid != null)
            {
                levelManager.ActiveGrid.RemoveTile(firstTile.TileId);
                levelManager.ActiveGrid.RemoveTile(secondTile.TileId);
            }

            firstTile.MarkRemoved();
            secondTile.MarkRemoved();
            history.Push(record);

            if (rewardCoins)
            {
                GetSaveManager()?.AddCoins(GetCoinsPerMatch());
            }

            selectedTiles.Clear();
            GetTileManager()?.RefreshTileExposure();
            Context.EventBus.Publish(new MatchSucceededEvent(firstTile, secondTile));
            PublishProgress();
            if (Context.Services.TryGet(out SaveManager saveManager) && saveManager.CurrentSave != null)
            {
                Context.EventBus.Publish(new SaveDataLoadedEvent(saveManager.CurrentSave));
            }
            EvaluateBoardState();
            EndResolution();
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

            while (!completed)
            {
                yield return null;
            }

            firstTile.Deselect();
            secondTile.Deselect();
            selectedTiles.Clear();
            Context.EventBus.Publish(new MatchFailedEvent(firstTile, secondTile));
            EndResolution();
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

            Dictionary<int, MahjongTile> firstByMatchId = new Dictionary<int, MahjongTile>();
            foreach (MahjongTile tile in tileManager.GetExposedTiles())
            {
                if (tile == null || tile.IsRemoved || !tileManager.IsTileHintSelectable(tile))
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
        /// Deselects every currently selected tile.
        /// </summary>
        private void DeselectAll()
        {
            for (int index = 0; index < selectedTiles.Count; index++)
            {
                if (selectedTiles[index] != null)
                {
                    selectedTiles[index].Deselect();
                }
            }

            selectedTiles.Clear();
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

            if (!TryFindAnySelectablePair(out _, out _))
            {
                Context.EventBus.Publish(new NoMovesRemainingEvent());
                GetAudioManager()?.PlayLose();
                gameManager.LoseGameplay();
            }
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
                    localPosition = tile.transform.localPosition,
                    localEulerAngles = tile.transform.localEulerAngles,
                    state = tile.State,
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
                tile.SetLocalPose(snapshot.localPosition, snapshot.localEulerAngles);
                levelManager.ActiveGrid.TryPlaceTile(snapshot.tileId, snapshot.gridCoordinate);
                tile.Restore(snapshot.state != TileState.Hidden);
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
