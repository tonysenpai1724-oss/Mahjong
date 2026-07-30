using MahjongOut3D.Core;
using MahjongOut3D.LevelSystem;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Tracks level selection state and owns the active runtime voxel grid.
    /// </summary>
    public sealed class LevelManager : ManagerBehaviour
    {
        [SerializeField] private VoxelGridLayoutSettings defaultGridLayout;

        private VoxelGridData activeGrid;

        /// <summary>
        /// Gets the currently selected level index.
        /// </summary>
        public int CurrentLevelIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the currently active voxel grid.
        /// </summary>
        public VoxelGridData ActiveGrid => activeGrid;

        /// <summary>
        /// Gets the default layout used when creating new grids.
        /// </summary>
        public VoxelGridLayoutSettings DefaultGridLayout => defaultGridLayout;

        /// <summary>
        /// Gets the bootstrap order for the level manager.
        /// </summary>
        public override int InitializationOrder => 10;

        /// <summary>
        /// Applies the default level index from project settings.
        /// </summary>
        protected override void OnInitialize()
        {
            if (Context.ProjectSettings != null)
            {
                CurrentLevelIndex = Context.ProjectSettings.DefaultLevelIndex;
            }
        }

        /// <summary>
        /// Clears the active grid when the manager shuts down.
        /// </summary>
        protected override void OnShutdown()
        {
            SetActiveGrid(null);
        }

        /// <summary>
        /// Updates the currently selected level index.
        /// </summary>
        /// <param name="levelIndex">New selected level index.</param>
        public void SetCurrentLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
        }

        /// <summary>
        /// Creates a new voxel grid using the specified size and layout.
        /// </summary>
        /// <param name="gridSize">Dimensions of the grid to create.</param>
        /// <param name="layoutOverride">Optional layout override.</param>
        /// <returns>Newly created voxel grid instance.</returns>
        public VoxelGridData CreateGrid(VoxelGridSize gridSize, VoxelGridLayoutSettings layoutOverride = null)
        {
            return new VoxelGridData(gridSize, layoutOverride != null ? layoutOverride : defaultGridLayout);
        }

        /// <summary>
        /// Sets the active voxel grid and republishes grid events through the shared event bus.
        /// </summary>
        /// <param name="newGrid">Grid to mark as active.</param>
        public void SetActiveGrid(VoxelGridData newGrid)
        {
            if (ReferenceEquals(activeGrid, newGrid))
            {
                return;
            }

            VoxelGridData previousGrid = activeGrid;
            if (activeGrid != null)
            {
                activeGrid.CellChanged -= HandleGridCellChanged;
            }

            activeGrid = newGrid;
            if (activeGrid != null)
            {
                activeGrid.CellChanged += HandleGridCellChanged;
            }

            Context?.EventBus.Publish(new ActiveVoxelGridChangedEvent(previousGrid, activeGrid));
        }

        /// <summary>
        /// Clears the current active voxel grid reference.
        /// </summary>
        public void ClearActiveGrid()
        {
            SetActiveGrid(null);
        }

        /// <summary>
        /// Attempts to get the tile id at the specified active-grid coordinate.
        /// </summary>
        /// <param name="coordinate">Coordinate to inspect.</param>
        /// <param name="tileId">Resolved tile id.</param>
        /// <returns>True when the active grid contains a tile there; otherwise false.</returns>
        public bool TryGetTileIdAt(Vector3Int coordinate, out int tileId)
        {
            tileId = -1;
            return activeGrid != null && activeGrid.TryGetTileId(coordinate, out tileId);
        }

        /// <summary>
        /// Converts an active-grid coordinate into a local position.
        /// </summary>
        /// <param name="coordinate">Coordinate to convert.</param>
        /// <returns>Local-space position for the coordinate.</returns>
        public Vector3 GetLocalPosition(Vector3Int coordinate)
        {
            return activeGrid != null ? activeGrid.GetLocalPosition(coordinate) : coordinate;
        }

        /// <summary>
        /// Gets the local-space bounds of the active grid.
        /// </summary>
        /// <returns>Bounds of the active grid, or an empty bounds when none exists.</returns>
        public Bounds GetActiveGridLocalBounds()
        {
            return activeGrid != null ? activeGrid.GetLocalBounds() : new Bounds(Vector3.zero, Vector3.zero);
        }

        /// <summary>
        /// Republishes cell occupancy changes from the active grid.
        /// </summary>
        /// <param name="eventData">Grid cell change payload.</param>
        private void HandleGridCellChanged(VoxelGridCellChangedEvent eventData)
        {
            Context.EventBus.Publish(eventData);
        }
    }
}
