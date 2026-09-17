using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Stores runtime occupancy, coordinate math and layout utilities for a 3D voxel grid.
    /// </summary>
    public sealed class VoxelGridData
    {
        private const int EmptyTileId = -1;

        private readonly int[] tileIds;
        private readonly Dictionary<int, Vector3Int> coordinatesByTileId = new Dictionary<int, Vector3Int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelGridData"/> class.
        /// </summary>
        /// <param name="size">Grid dimensions.</param>
        /// <param name="layoutSettings">Layout settings used to place tiles in local space.</param>
        public VoxelGridData(VoxelGridSize size, VoxelGridLayoutSettings layoutSettings)
        {
            Size = size;
            LayoutSettings = layoutSettings;
            tileIds = new int[Size.Volume];
            Clear(false);
        }

        /// <summary>
        /// Occurs whenever a cell changes occupancy.
        /// </summary>
        public event Action<VoxelGridCellChangedEvent> CellChanged;

        /// <summary>
        /// Gets the dimensions of the current grid.
        /// </summary>
        public VoxelGridSize Size { get; }

        /// <summary>
        /// Gets the layout settings used by this grid.
        /// </summary>
        public VoxelGridLayoutSettings LayoutSettings { get; }

        /// <summary>
        /// Gets the number of occupied cells in the grid.
        /// </summary>
        public int OccupiedCount { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the grid contains no tiles.
        /// </summary>
        public bool IsEmpty => OccupiedCount == 0;

        /// <summary>
        /// Checks whether the specified coordinate is inside the grid bounds.
        /// </summary>
        /// <param name="coordinate">Coordinate to test.</param>
        /// <returns>True when the coordinate is inside the grid; otherwise false.</returns>
        public bool Contains(Vector3Int coordinate)
        {
            return Size.Contains(coordinate);
        }

        /// <summary>
        /// Checks whether the specified coordinate is currently occupied.
        /// </summary>
        /// <param name="coordinate">Coordinate to test.</param>
        /// <returns>True when a tile occupies the cell; otherwise false.</returns>
        public bool IsOccupied(Vector3Int coordinate)
        {
            return TryGetTileId(coordinate, out _);
        }

        /// <summary>
        /// Checks whether the coordinate lies on the outer shell of the grid volume.
        /// </summary>
        /// <param name="coordinate">Coordinate to test.</param>
        /// <returns>True when the coordinate lies on the volume boundary; otherwise false.</returns>
        public bool IsOnBoundary(Vector3Int coordinate)
        {
            if (!Contains(coordinate))
            {
                return false;
            }

            return coordinate.x == 0 || coordinate.x == Size.Width - 1
                || coordinate.y == 0 || coordinate.y == Size.Height - 1
                || coordinate.z == 0 || coordinate.z == Size.Depth - 1;
        }

        /// <summary>
        /// Attempts to place a tile id into the specified coordinate.
        /// </summary>
        /// <param name="tileId">Tile id to place.</param>
        /// <param name="coordinate">Target coordinate.</param>
        /// <returns>True when the tile was placed; otherwise false.</returns>
        public bool TryPlaceTile(int tileId, Vector3Int coordinate)
        {
            if (tileId < 0 || !Contains(coordinate) || IsOccupied(coordinate) || coordinatesByTileId.ContainsKey(tileId))
            {
                return false;
            }

            int index = ToFlatIndex(coordinate);
            tileIds[index] = tileId;
            coordinatesByTileId[tileId] = coordinate;
            OccupiedCount++;
            RaiseCellChanged(coordinate, EmptyTileId, tileId);
            return true;
        }

        /// <summary>
        /// Removes the tile at the specified coordinate.
        /// </summary>
        /// <param name="coordinate">Coordinate to clear.</param>
        /// <param name="removedTileId">Removed tile id when successful.</param>
        /// <returns>True when a tile was removed; otherwise false.</returns>
        public bool RemoveTileAt(Vector3Int coordinate, out int removedTileId)
        {
            removedTileId = EmptyTileId;
            if (!TryGetTileId(coordinate, out int tileId))
            {
                return false;
            }

            int index = ToFlatIndex(coordinate);
            tileIds[index] = EmptyTileId;
            coordinatesByTileId.Remove(tileId);
            OccupiedCount = Mathf.Max(0, OccupiedCount - 1);
            removedTileId = tileId;
            RaiseCellChanged(coordinate, tileId, EmptyTileId);
            return true;
        }

        /// <summary>
        /// Removes the specified tile id from the grid.
        /// </summary>
        /// <param name="tileId">Tile id to remove.</param>
        /// <returns>True when the tile was removed; otherwise false.</returns>
        public bool RemoveTile(int tileId)
        {
            if (!TryGetCoordinate(tileId, out Vector3Int coordinate))
            {
                return false;
            }

            return RemoveTileAt(coordinate, out _);
        }

        /// <summary>
        /// Moves the specified tile id to a new coordinate.
        /// </summary>
        /// <param name="tileId">Tile id to move.</param>
        /// <param name="targetCoordinate">Target coordinate to occupy.</param>
        /// <returns>True when the tile moved successfully; otherwise false.</returns>
        public bool TryMoveTile(int tileId, Vector3Int targetCoordinate)
        {
            if (!TryGetCoordinate(tileId, out Vector3Int currentCoordinate) || !Contains(targetCoordinate) || IsOccupied(targetCoordinate))
            {
                return false;
            }

            int currentIndex = ToFlatIndex(currentCoordinate);
            int targetIndex = ToFlatIndex(targetCoordinate);
            tileIds[currentIndex] = EmptyTileId;
            tileIds[targetIndex] = tileId;
            coordinatesByTileId[tileId] = targetCoordinate;
            RaiseCellChanged(currentCoordinate, tileId, EmptyTileId);
            RaiseCellChanged(targetCoordinate, EmptyTileId, tileId);
            return true;
        }

        /// <summary>
        /// Attempts to read the tile id at the specified coordinate.
        /// </summary>
        /// <param name="coordinate">Coordinate to inspect.</param>
        /// <param name="tileId">Resolved tile id when present.</param>
        /// <returns>True when the cell is occupied; otherwise false.</returns>
        public bool TryGetTileId(Vector3Int coordinate, out int tileId)
        {
            tileId = EmptyTileId;
            if (!Contains(coordinate))
            {
                return false;
            }

            int index = ToFlatIndex(coordinate);
            tileId = tileIds[index];
            return tileId != EmptyTileId;
        }

        /// <summary>
        /// Attempts to read the grid coordinate occupied by a tile id.
        /// </summary>
        /// <param name="tileId">Tile id to locate.</param>
        /// <param name="coordinate">Resolved coordinate when found.</param>
        /// <returns>True when the tile exists in the grid; otherwise false.</returns>
        public bool TryGetCoordinate(int tileId, out Vector3Int coordinate)
        {
            return coordinatesByTileId.TryGetValue(tileId, out coordinate);
        }

        /// <summary>
        /// Gets the neighbor coordinate in the specified direction.
        /// </summary>
        /// <param name="coordinate">Origin coordinate.</param>
        /// <param name="direction">Direction to step.</param>
        /// <returns>Neighbor coordinate.</returns>
        public Vector3Int GetNeighborCoordinate(Vector3Int coordinate, VoxelGridDirection direction)
        {
            return coordinate + VoxelGridDirections.GetOffset(direction);
        }

        /// <summary>
        /// Attempts to read the neighbor tile id in the specified direction.
        /// </summary>
        /// <param name="coordinate">Origin coordinate.</param>
        /// <param name="direction">Direction to inspect.</param>
        /// <param name="tileId">Resolved neighbor tile id.</param>
        /// <returns>True when the neighbor cell is occupied; otherwise false.</returns>
        public bool TryGetNeighborTileId(Vector3Int coordinate, VoxelGridDirection direction, out int tileId)
        {
            Vector3Int neighborCoordinate = GetNeighborCoordinate(coordinate, direction);
            return TryGetTileId(neighborCoordinate, out tileId);
        }

        /// <summary>
        /// Gets the local-space position for a coordinate based on the current layout.
        /// </summary>
        /// <param name="coordinate">Coordinate to convert.</param>
        /// <returns>Local-space position for the specified coordinate.</returns>
        public Vector3 GetLocalPosition(Vector3Int coordinate)
        {
            if (LayoutSettings == null)
            {
                return coordinate;
            }

            return LayoutSettings.GetLocalPosition(coordinate, Size);
        }

        /// <summary>
        /// Gets the local-space bounds occupied by the grid.
        /// </summary>
        /// <returns>Bounds describing the current grid volume.</returns>
        public Bounds GetLocalBounds()
        {
            if (LayoutSettings == null)
            {
                return new Bounds(Vector3.zero, Size.ToVector3Int());
            }

            return LayoutSettings.GetLocalBounds(Size);
        }

        /// <summary>
        /// Enumerates every valid coordinate in the grid volume.
        /// </summary>
        /// <returns>Sequence of all coordinates in the grid.</returns>
        public IEnumerable<Vector3Int> EnumerateAllCoordinates()
        {
            for (int z = 0; z < Size.Depth; z++)
            {
                for (int y = 0; y < Size.Height; y++)
                {
                    for (int x = 0; x < Size.Width; x++)
                    {
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates only the coordinates currently occupied by tiles.
        /// </summary>
        /// <returns>Sequence of occupied coordinates.</returns>
        public IEnumerable<Vector3Int> EnumerateOccupiedCoordinates()
        {
            foreach (KeyValuePair<int, Vector3Int> pair in coordinatesByTileId)
            {
                yield return pair.Value;
            }
        }

        /// <summary>
        /// Clears all grid occupancy state.
        /// </summary>
        public void Clear()
        {
            Clear(true);
        }

        /// <summary>
        /// Clears all grid occupancy state and optionally raises change events.
        /// </summary>
        /// <param name="raiseEvents">True to raise cell change events while clearing.</param>
        private void Clear(bool raiseEvents)
        {
            if (raiseEvents)
            {
                Vector3Int[] occupiedCoordinates = new Vector3Int[coordinatesByTileId.Count];
                coordinatesByTileId.Values.CopyTo(occupiedCoordinates, 0);

                for (int index = 0; index < occupiedCoordinates.Length; index++)
                {
                    Vector3Int coordinate = occupiedCoordinates[index];
                    if (TryGetTileId(coordinate, out int existingTileId))
                    {
                        int flatIndex = ToFlatIndex(coordinate);
                        tileIds[flatIndex] = EmptyTileId;
                        RaiseCellChanged(coordinate, existingTileId, EmptyTileId);
                    }
                }
            }
            else
            {
                for (int index = 0; index < tileIds.Length; index++)
                {
                    tileIds[index] = EmptyTileId;
                }
            }

            coordinatesByTileId.Clear();
            OccupiedCount = 0;
        }

        /// <summary>
        /// Converts a coordinate to its flat array index.
        /// </summary>
        /// <param name="coordinate">Coordinate to convert.</param>
        /// <returns>Flat array index.</returns>
        private int ToFlatIndex(Vector3Int coordinate)
        {
            return coordinate.x + (coordinate.y * Size.Width) + (coordinate.z * Size.Width * Size.Height);
        }

        /// <summary>
        /// Raises a cell-changed notification.
        /// </summary>
        /// <param name="coordinate">Changed coordinate.</param>
        /// <param name="previousTileId">Previous tile id.</param>
        /// <param name="currentTileId">Current tile id.</param>
        private void RaiseCellChanged(Vector3Int coordinate, int previousTileId, int currentTileId)
        {
            CellChanged?.Invoke(new VoxelGridCellChangedEvent(this, coordinate, previousTileId, currentTileId));
        }
    }
}
