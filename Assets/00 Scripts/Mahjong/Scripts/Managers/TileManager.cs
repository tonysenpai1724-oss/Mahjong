using System.Collections.Generic;
using MahjongOut3D.Core;
using MahjongOut3D.GameplayInput;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.TileSystem;
using MahjongOut3D.Utilities;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Owns tile registration, state event forwarding and tap raycasts for the runtime board.
    /// </summary>
    public sealed class TileManager : ManagerBehaviour
    {
        [SerializeField] private bool autoDiscoverSceneTiles = true;
        [SerializeField] private LayerMask tileLayerMask = ~0;
        [SerializeField, Min(1f)] private float maxRaycastDistance = 250f;
        [SerializeField] private TileExposureSettings exposureSettings;

        private readonly Dictionary<int, MahjongTile> tilesById = new Dictionary<int, MahjongTile>();
        private readonly Dictionary<int, bool> exposedStateByTileId = new Dictionary<int, bool>();
        private readonly RaycastHit[] raycastBuffer = new RaycastHit[32];

        private float xrayEndTime;
        private int activeXRayDepth;
        private bool wasXRayActiveLastFrame;

        /// <summary>
        /// Gets the number of active tracked tiles.
        /// </summary>
        public int ActiveTileCount => tilesById.Count;

        /// <summary>
        /// Gets the bootstrap order for the tile manager.
        /// </summary>
        public override int InitializationOrder => 20;

        /// <summary>
        /// Subscribes to input and discovers scene tiles during bootstrap.
        /// </summary>
        protected override void OnInitialize()
        {
            Context.EventBus.Subscribe<TileTapInputEvent>(HandleTileTapInput);
            Context.EventBus.Subscribe<ActiveVoxelGridChangedEvent>(HandleActiveGridChanged);
            Context.EventBus.Subscribe<VoxelGridCellChangedEvent>(HandleGridCellChanged);

            if (autoDiscoverSceneTiles)
            {
                DiscoverSceneTiles();
            }

            RefreshTileExposure();
        }

        /// <summary>
        /// Unsubscribes from input and clears the runtime tile cache.
        /// </summary>
        protected override void OnShutdown()
        {
            Context.EventBus.Unsubscribe<TileTapInputEvent>(HandleTileTapInput);
            Context.EventBus.Unsubscribe<ActiveVoxelGridChangedEvent>(HandleActiveGridChanged);
            Context.EventBus.Unsubscribe<VoxelGridCellChangedEvent>(HandleGridCellChanged);

            MahjongTile[] cachedTiles = new MahjongTile[tilesById.Count];
            tilesById.Values.CopyTo(cachedTiles, 0);

            for (int index = 0; index < cachedTiles.Length; index++)
            {
                UnregisterTile(cachedTiles[index]);
            }

            tilesById.Clear();
            exposedStateByTileId.Clear();
            xrayEndTime = 0f;
            activeXRayDepth = 0;
        }

        /// <summary>
        /// Registers a tile instance into the active runtime set.
        /// </summary>
        /// <param name="tile">Tile to register.</param>
        /// <returns>True when the tile was registered; otherwise false.</returns>
        public bool RegisterTile(MahjongTile tile)
        {
            if (tile == null)
            {
                return false;
            }

            if (tilesById.TryGetValue(tile.TileId, out MahjongTile existingTile) && existingTile != tile)
            {
                MahjongRuntimeLogger.LogWarning($"Duplicate tile id detected: {tile.TileId}. Registration skipped for {tile.name}.");
                return false;
            }

            if (tilesById.ContainsKey(tile.TileId))
            {
                return false;
            }

            tilesById.Add(tile.TileId, tile);
            tile.StateChanged += HandleTileStateChanged;
            tile.SelectionChanged += HandleTileSelectionChanged;
            exposedStateByTileId[tile.TileId] = false;
            return true;
        }

        /// <summary>
        /// Removes a tile instance from the active runtime set.
        /// </summary>
        /// <param name="tile">Tile to unregister.</param>
        public void UnregisterTile(MahjongTile tile)
        {
            if (tile == null)
            {
                return;
            }

            if (!tilesById.Remove(tile.TileId))
            {
                return;
            }

            tile.StateChanged -= HandleTileStateChanged;
            tile.SelectionChanged -= HandleTileSelectionChanged;
            exposedStateByTileId.Remove(tile.TileId);
        }

        /// <summary>
        /// Gets every currently registered tile.
        /// </summary>
        /// <returns>Enumeration of all registered tiles.</returns>
        public IEnumerable<MahjongTile> GetAllTiles()
        {
            return tilesById.Values;
        }

        /// <summary>
        /// Gets every tile that has not been permanently removed from the puzzle.
        /// </summary>
        /// <returns>Enumeration of every remaining tile.</returns>
        public IEnumerable<MahjongTile> GetRemainingTiles()
        {
            foreach (MahjongTile tile in tilesById.Values)
            {
                if (tile != null && !tile.IsRemoved)
                {
                    yield return tile;
                }
            }
        }

        /// <summary>
        /// Gets every tile currently considered exposed by the runtime grid rules.
        /// </summary>
        /// <returns>Enumeration of exposed tiles.</returns>
        public IEnumerable<MahjongTile> GetExposedTiles()
        {
            foreach (MahjongTile tile in tilesById.Values)
            {
                if (tile != null && IsTileExposed(tile))
                {
                    yield return tile;
                }
            }
        }

        /// <summary>
        /// Tries to resolve a tile by runtime identifier.
        /// </summary>
        /// <param name="tileId">Tile identifier to resolve.</param>
        /// <param name="tile">Resolved tile when found.</param>
        /// <returns>True when the tile exists; otherwise false.</returns>
        public bool TryGetTile(int tileId, out MahjongTile tile)
        {
            return tilesById.TryGetValue(tileId, out tile);
        }

        /// <summary>
        /// Tries to raycast a tile from the active gameplay camera.
        /// </summary>
        /// <param name="screenPosition">Screen position in pixels.</param>
        /// <param name="tile">Resolved tile when found.</param>
        /// <param name="hitInfo">Physics hit information.</param>
        /// <returns>True when a tile collider was hit; otherwise false.</returns>
        public bool TryRaycastTile(Vector2 screenPosition, out MahjongTile tile, out RaycastHit hitInfo)
        {
            tile = null;
            hitInfo = default;

            if (!Context.Services.TryGet(out CameraManager cameraManager) || cameraManager.ActiveCamera == null)
            {
                return false;
            }

            Camera activeCamera = cameraManager.ActiveCamera;
            Ray ray = activeCamera.ScreenPointToRay(screenPosition);
            int hitCount = Physics.RaycastNonAlloc(ray, raycastBuffer, maxRaycastDistance, tileLayerMask, QueryTriggerInteraction.Ignore);
            if (hitCount <= 0)
            {
                return false;
            }

            int closestHitIndex = GetClosestHitIndex(hitCount);
            if (closestHitIndex < 0)
            {
                return false;
            }

            hitInfo = raycastBuffer[closestHitIndex];
            tile = hitInfo.collider.GetComponentInParent<MahjongTile>();
            return tile != null;
        }

        /// <summary>
        /// Discovers and registers all Mahjong tile components already placed in the scene.
        /// </summary>
        public void DiscoverSceneTiles()
        {
            MahjongTile[] discoveredTiles = FindObjectsByType<MahjongTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < discoveredTiles.Length; index++)
            {
                RegisterTile(discoveredTiles[index]);
            }

            RefreshTileExposure();
        }

        /// <summary>
        /// Refreshes hidden and visible tile states based on the active grid and X-Ray rules.
        /// </summary>
        public void RefreshTileExposure()
        {
            if (!Context.Services.TryGet(out LevelManager levelManager) || levelManager.ActiveGrid == null)
            {
                return;
            }

            bool useSurfaceRules = levelManager.ActiveUsesSurfaceTilePlacement;
            bool xrayActive = IsXRayActive();
            foreach (MahjongTile tile in tilesById.Values)
            {
                if (tile == null || tile.IsRemoved || tile.IsMatched)
                {
                    continue;
                }

                bool shouldExpose = ShouldExposeTile(tile, levelManager.ActiveGrid, xrayActive, useSurfaceRules);
                bool shouldReveal = useSurfaceRules || shouldExpose;
                UpdateExposureState(tile, shouldExpose);

                if (tile.State == TileState.Selected && !shouldExpose)
                {
                    tile.Deselect();
                }

                if (tile.State == TileState.Hidden && shouldReveal)
                {
                    tile.SetVisible(true);
                }
                else if (tile.State == TileState.Visible && !shouldReveal)
                {
                    tile.SetVisible(false);
                }
            }
        }

        /// <summary>
        /// Determines whether a tile is exposed by the grid shell rules.
        /// </summary>
        /// <param name="tile">Tile to evaluate.</param>
        /// <returns>True when the tile is on the current exposed shell; otherwise false.</returns>
        public bool IsTileExposed(MahjongTile tile)
        {
            if (tile == null)
            {
                return false;
            }

            if (exposedStateByTileId.TryGetValue(tile.TileId, out bool isExposed))
            {
                return isExposed;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the tile currently satisfies the full selection rules.
        /// </summary>
        /// <param name="tile">Tile to evaluate.</param>
        /// <returns>True when the tile may be selected; otherwise false.</returns>
        public bool IsTileSelectable(MahjongTile tile)
        {
            if (tile == null || !tile.IsInteractable)
            {
                return false;
            }

            bool useSurfaceRules = Context.Services.TryGet(out LevelManager levelManager) && levelManager.ActiveUsesSurfaceTilePlacement;
            if (useSurfaceRules)
            {
                return IsTileExposed(tile);
            }

            if (exposureSettings != null && exposureSettings.RequireSurfaceExposure && !IsTileExposed(tile))
            {
                return false;
            }

            if (exposureSettings != null && exposureSettings.RequireDirectCameraVisibility)
            {
                return IsTileFullyVisibleFromCamera(tile);
            }

            return true;
        }

        /// <summary>
        /// Determines whether the tile should be considered valid for hint selection.
        /// </summary>
        /// <param name="tile">Tile to evaluate.</param>
        /// <returns>True when the tile is a valid hint candidate; otherwise false.</returns>
        public bool IsTileHintSelectable(MahjongTile tile)
        {
            if (tile == null || !tile.IsInteractable)
            {
                return false;
            }

            bool useSurfaceRules = Context.Services.TryGet(out LevelManager levelManager) && levelManager.ActiveUsesSurfaceTilePlacement;
            if (useSurfaceRules)
            {
                return IsTileExposed(tile);
            }

            return IsTileSelectable(tile);
        }

        /// <summary>
        /// Temporarily reveals one or more inner layers of the current voxel grid.
        /// </summary>
        /// <param name="durationSeconds">Effect duration in seconds.</param>
        /// <param name="surfaceDepth">Number of surface-depth layers to reveal.</param>
        public void EnableXRay(float durationSeconds, int surfaceDepth)
        {
            activeXRayDepth = Mathf.Max(1, surfaceDepth);
            xrayEndTime = GetCurrentTime() + Mathf.Max(0.1f, durationSeconds);
            wasXRayActiveLastFrame = true;
            RefreshTileExposure();
        }

        /// <summary>
        /// Disables the current X-Ray reveal effect immediately.
        /// </summary>
        public void DisableXRay()
        {
            xrayEndTime = 0f;
            activeXRayDepth = 0;
            wasXRayActiveLastFrame = false;
            RefreshTileExposure();
        }

        /// <summary>
        /// Monitors X-Ray expiry and refreshes tile visibility only when the effect ends.
        /// </summary>
        private void Update()
        {
            if (Context != null && Context.Services.TryGet(out LevelManager levelManager) && levelManager.ActiveUsesSurfaceTilePlacement)
            {
                RefreshTileExposure();
                return;
            }

            bool isXRayActive = IsXRayActive();
            if (wasXRayActiveLastFrame && !isXRayActive)
            {
                RefreshTileExposure();
            }

            wasXRayActiveLastFrame = isXRayActive;
        }
        

        /// <summary>
        /// Forwards tile tap input after resolving the hit tile through a camera raycast.
        /// </summary>
        /// <param name="eventData">Raw screen tap input payload.</param>
        private void HandleTileTapInput(TileTapInputEvent eventData)
        {
            if (!Context.Services.TryGet(out CameraManager cameraManager) || cameraManager.ActiveCamera == null)
            {
                return;
            }

            Camera activeCamera = cameraManager.ActiveCamera;
            Ray ray = activeCamera.ScreenPointToRay(eventData.ScreenPosition);

            if (!TryRaycastTile(eventData.ScreenPosition, out MahjongTile tile, out RaycastHit hitInfo))
            {
                return;
            }

            if (!tile.IsInteractable)
            {
                return;
            }

            if (!IsTileSelectable(tile))
            {
                return;
            }

            Context.EventBus.Publish(new TileTappedEvent(tile, eventData.ScreenPosition, ray, hitInfo));
        }

        /// <summary>
        /// Republishes tile state changes through the shared event bus.
        /// </summary>
        /// <param name="eventData">Tile state change payload.</param>
        private void HandleTileStateChanged(TileStateChangedEvent eventData)
        {
            Context.EventBus.Publish(eventData);
        }

        /// <summary>
        /// Republishes tile selection changes through the shared event bus.
        /// </summary>
        /// <param name="eventData">Tile selection change payload.</param>
        private void HandleTileSelectionChanged(TileSelectionChangedEvent eventData)
        {
            Context.EventBus.Publish(eventData);
        }

        /// <summary>
        /// Refreshes exposure whenever the active grid changes.
        /// </summary>
        /// <param name="eventData">Grid switch payload.</param>
        private void HandleActiveGridChanged(ActiveVoxelGridChangedEvent eventData)
        {
            RefreshTileExposure();
        }

        /// <summary>
        /// Refreshes exposure whenever grid occupancy changes.
        /// </summary>
        /// <param name="eventData">Grid cell change payload.</param>
        private void HandleGridCellChanged(VoxelGridCellChangedEvent eventData)
        {
            RefreshTileExposure();
        }

        /// <summary>
        /// Determines whether a tile should currently be revealed to the player.
        /// </summary>
        private bool ShouldExposeTile(MahjongTile tile, VoxelGridData grid, bool xrayActive, bool useSurfaceRules)
        {
            if (tile == null || grid == null || tile.IsRemoved || tile.IsMatched)
            {
                return false;
            }

            if (useSurfaceRules)
            {
                if (IsSurfaceTileLocallyExposed(tile))
                {
                    return true;
                }

                if (xrayActive)
                {
                    int currentShellIndex = GetCurrentSurfaceShellIndex();
                    int maxRevealShellIndex = currentShellIndex + Mathf.Max(1, activeXRayDepth);
                    return tile.SurfaceShellIndex <= maxRevealShellIndex;
                }

                return false;
            }

            if (IsTileSurfaceExposed(tile, grid))
            {
                return true;
            }

            if (xrayActive)
            {
                int surfaceDepth = GetBoundaryDepth(tile.GridCoordinate, grid.Size);
                return surfaceDepth <= Mathf.Max(1, activeXRayDepth);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a surface-placed tile has enough free outward face area to be selected.
        /// </summary>
        private bool IsSurfaceTileLocallyExposed(MahjongTile tile)
        {
            if (tile == null)
            {
                return false;
            }

            Collider tileCollider = tile.TileCollider;
            if (tileCollider == null)
            {
                return false;
            }

            Vector3 outwardNormal = tile.transform.up.normalized;
            Vector3[] samplePoints = tileCollider is BoxCollider boxCollider
                ? BuildSurfaceExposureSamplePoints(boxCollider, tile.transform, GetVisibilitySampleInset())
                : BuildSurfaceExposureSamplePoints(tileCollider.bounds, outwardNormal, GetVisibilitySampleInset());
            int openSampleCount = 0;
            float checkDistance = tileCollider is BoxCollider sizedBoxCollider
                ? GetSurfaceExposureCheckDistance(sizedBoxCollider, tile.transform)
                : GetSurfaceExposureCheckDistance(tileCollider.bounds, outwardNormal);

            for (int index = 0; index < samplePoints.Length; index++)
            {
                Vector3 rayOrigin = samplePoints[index] + (outwardNormal * GetVisibilityRayPadding());
                Ray ray = new Ray(rayOrigin, outwardNormal);
                int hitCount = Physics.RaycastNonAlloc(ray, raycastBuffer, checkDistance, tileLayerMask, QueryTriggerInteraction.Ignore);
                if (hitCount <= 0)
                {
                    openSampleCount++;
                    continue;
                }

                bool isBlocked = false;
                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    RaycastHit hit = raycastBuffer[hitIndex];
                    if (hit.collider == null)
                    {
                        continue;
                    }

                    MahjongTile hitTile = hit.collider.GetComponentInParent<MahjongTile>();
                    if (hitTile == null || hitTile == tile || hitTile.IsRemoved || hitTile.IsMatched)
                    {
                        continue;
                    }

                    isBlocked = true;
                    break;
                }

                if (!isBlocked)
                {
                    openSampleCount++;
                }
            }

            float openRatio = samplePoints.Length == 0 ? 0f : (float)openSampleCount / samplePoints.Length;
            return openRatio >= GetRequiredVisibleSampleRatio();
        }

        /// <summary>
        /// Determines whether the tile has at least one open cardinal direction on the active shell.
        /// </summary>
        private bool IsTileSurfaceExposed(MahjongTile tile, VoxelGridData grid)
        {
            if (tile == null || grid == null)
            {
                return false;
            }

            Vector3Int coordinate = tile.GridCoordinate;
            VoxelGridDirection[] directions = VoxelGridDirections.Cardinals;
            for (int index = 0; index < directions.Length; index++)
            {
                Vector3Int neighbor = grid.GetNeighborCoordinate(coordinate, directions[index]);
                if (!grid.Contains(neighbor) || !grid.IsOccupied(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validates that a tile is directly visible from the active camera using multi-sample raycasts.
        /// </summary>
        private bool IsTileFullyVisibleFromCamera(MahjongTile tile)
        {
            if (tile == null || !Context.Services.TryGet(out CameraManager cameraManager) || cameraManager.ActiveCamera == null)
            {
                return false;
            }

            Collider tileCollider = tile.TileCollider;
            if (tileCollider == null)
            {
                return false;
            }

            Camera activeCamera = cameraManager.ActiveCamera;
            Vector3 cameraPosition = activeCamera.transform.position;
            Vector3[] samplePoints = BuildVisibilitySamplePoints(tileCollider.bounds, GetVisibilitySampleInset());
            int visibleSampleCount = 0;

            for (int index = 0; index < samplePoints.Length; index++)
            {
                Vector3 targetPoint = samplePoints[index];
                Vector3 direction = targetPoint - cameraPosition;
                float distance = direction.magnitude + GetVisibilityRayPadding();
                if (distance <= Mathf.Epsilon)
                {
                    continue;
                }

                Ray ray = new Ray(cameraPosition, direction.normalized);
                int hitCount = Physics.RaycastNonAlloc(ray, raycastBuffer, distance, tileLayerMask, QueryTriggerInteraction.Ignore);
                if (hitCount <= 0)
                {
                    continue;
                }

                int closestHitIndex = GetClosestHitIndex(hitCount);
                if (closestHitIndex < 0)
                {
                    continue;
                }

                MahjongTile hitTile = raycastBuffer[closestHitIndex].collider.GetComponentInParent<MahjongTile>();
                if (hitTile == tile)
                {
                    visibleSampleCount++;
                }
            }

            float visibleRatio = samplePoints.Length == 0 ? 0f : (float)visibleSampleCount / samplePoints.Length;
            return visibleRatio >= GetRequiredVisibleSampleRatio();
        }

        /// <summary>
        /// Validates that the center of a tile is directly visible from the active camera.
        /// </summary>
        private bool IsTileCenterVisibleFromCamera(MahjongTile tile)
        {
            if (tile == null || !Context.Services.TryGet(out CameraManager cameraManager) || cameraManager.ActiveCamera == null)
            {
                return false;
            }

            Collider tileCollider = tile.TileCollider;
            if (tileCollider == null)
            {
                return false;
            }

            Camera activeCamera = cameraManager.ActiveCamera;
            Vector3 cameraPosition = activeCamera.transform.position;
            Vector3 targetPoint = tileCollider.bounds.center;
            Vector3 direction = targetPoint - cameraPosition;
            float distance = direction.magnitude + GetVisibilityRayPadding();
            if (distance <= Mathf.Epsilon)
            {
                return false;
            }

            Ray ray = new Ray(cameraPosition, direction.normalized);
            int hitCount = Physics.RaycastNonAlloc(ray, raycastBuffer, distance, tileLayerMask, QueryTriggerInteraction.Ignore);
            if (hitCount <= 0)
            {
                return false;
            }

            int closestHitIndex = GetClosestHitIndex(hitCount);
            if (closestHitIndex < 0)
            {
                return false;
            }

            MahjongTile hitTile = raycastBuffer[closestHitIndex].collider.GetComponentInParent<MahjongTile>();
            return hitTile == tile;
        }

        /// <summary>
        /// Updates cached exposure state and raises change events when needed.
        /// </summary>
        private void UpdateExposureState(MahjongTile tile, bool isExposed)
        {
            bool hadPreviousState = exposedStateByTileId.TryGetValue(tile.TileId, out bool previousState);
            exposedStateByTileId[tile.TileId] = isExposed;

            if (!hadPreviousState || previousState != isExposed)
            {
                Context.EventBus.Publish(new TileExposureChangedEvent(tile, isExposed));
            }
        }

        /// <summary>
        /// Builds center-and-corner visibility samples from collider bounds.
        /// </summary>
        private static Vector3[] BuildVisibilitySamplePoints(Bounds bounds, float inset)
        {
            Vector3 extents = bounds.extents - Vector3.one * inset;
            extents.x = Mathf.Max(0.001f, extents.x);
            extents.y = Mathf.Max(0.001f, extents.y);
            extents.z = Mathf.Max(0.001f, extents.z);

            Vector3 center = bounds.center;
            return new[]
            {
                center,
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z),
            };
        }

        /// <summary>
        /// Builds sample points on the outward-facing tile surface used for local exposure checks.
        /// </summary>
        private static Vector3[] BuildSurfaceExposureSamplePoints(Bounds bounds, Vector3 outwardNormal, float inset)
        {
            Vector3 extents = bounds.extents - Vector3.one * inset;
            extents.x = Mathf.Max(0.001f, extents.x);
            extents.y = Mathf.Max(0.001f, extents.y);
            extents.z = Mathf.Max(0.001f, extents.z);

            Vector3 faceCenter;
            Vector3 tangentA;
            Vector3 tangentB;

            if (Mathf.Abs(outwardNormal.x) > 0.5f)
            {
                faceCenter = bounds.center + new Vector3(Mathf.Sign(outwardNormal.x) * extents.x, 0f, 0f);
                tangentA = Vector3.up * extents.y;
                tangentB = Vector3.forward * extents.z;
            }
            else if (Mathf.Abs(outwardNormal.y) > 0.5f)
            {
                faceCenter = bounds.center + new Vector3(0f, Mathf.Sign(outwardNormal.y) * extents.y, 0f);
                tangentA = Vector3.right * extents.x;
                tangentB = Vector3.forward * extents.z;
            }
            else
            {
                faceCenter = bounds.center + new Vector3(0f, 0f, Mathf.Sign(outwardNormal.z) * extents.z);
                tangentA = Vector3.right * extents.x;
                tangentB = Vector3.up * extents.y;
            }

            return new[]
            {
                faceCenter,
                faceCenter - tangentA,
                faceCenter + tangentA,
                faceCenter - tangentB,
                faceCenter + tangentB,
                faceCenter - tangentA - tangentB,
                faceCenter - tangentA + tangentB,
                faceCenter + tangentA - tangentB,
                faceCenter + tangentA + tangentB,
            };
        }

        /// <summary>
        /// Builds sample points on the actual outward face of a rotated box collider.
        /// </summary>
        private static Vector3[] BuildSurfaceExposureSamplePoints(BoxCollider boxCollider, Transform transform, float inset)
        {
            Vector3 localCenter = boxCollider.center;
            Vector3 localHalfSize = boxCollider.size * 0.5f;
            float localInset = Mathf.Max(0.001f, inset);

            float x = Mathf.Max(0.001f, localHalfSize.x - localInset);
            float y = Mathf.Max(0.001f, localHalfSize.y - localInset);
            float z = Mathf.Max(0.001f, localHalfSize.z - localInset);

            Vector3 faceCenter = localCenter + new Vector3(0f, y, 0f);
            return new[]
            {
                transform.TransformPoint(faceCenter),
                transform.TransformPoint(faceCenter + new Vector3(-x, 0f, 0f)),
                transform.TransformPoint(faceCenter + new Vector3(x, 0f, 0f)),
                transform.TransformPoint(faceCenter + new Vector3(0f, 0f, -z)),
                transform.TransformPoint(faceCenter + new Vector3(0f, 0f, z)),
                transform.TransformPoint(faceCenter + new Vector3(-x, 0f, -z)),
                transform.TransformPoint(faceCenter + new Vector3(-x, 0f, z)),
                transform.TransformPoint(faceCenter + new Vector3(x, 0f, -z)),
                transform.TransformPoint(faceCenter + new Vector3(x, 0f, z)),
            };
        }

        /// <summary>
        /// Resolves how far outward to probe for a covering tile on the next shell.
        /// </summary>
        private float GetSurfaceExposureCheckDistance(Bounds bounds, Vector3 outwardNormal)
        {
            float axisSize;
            if (Mathf.Abs(outwardNormal.x) > 0.5f)
            {
                axisSize = bounds.size.x;
            }
            else if (Mathf.Abs(outwardNormal.y) > 0.5f)
            {
                axisSize = bounds.size.y;
            }
            else
            {
                axisSize = bounds.size.z;
            }

            return Mathf.Max(0.05f, axisSize + GetVisibilityRayPadding());
        }

        /// <summary>
        /// Resolves how far outward to probe for a covering tile on the next shell for box colliders.
        /// </summary>
        private float GetSurfaceExposureCheckDistance(BoxCollider boxCollider, Transform transform)
        {
            float scaledThickness = Mathf.Abs(boxCollider.size.y * transform.lossyScale.y);
            return Mathf.Max(0.05f, scaledThickness + GetVisibilityRayPadding());
        }

        /// <summary>
        /// Gets the closest valid hit inside the shared non-alloc raycast buffer.
        /// </summary>
        private int GetClosestHitIndex(int hitCount)
        {
            int closestIndex = -1;
            float closestDistance = float.MaxValue;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = raycastBuffer[index];
                if (hit.collider == null || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestIndex = index;
                closestDistance = hit.distance;
            }

            return closestIndex;
        }

        /// <summary>
        /// Calculates how deep a coordinate lies relative to the outer shell of the volume.
        /// </summary>
        private static int GetBoundaryDepth(Vector3Int coordinate, VoxelGridSize size)
        {
            int xDepth = Mathf.Min(coordinate.x, size.Width - 1 - coordinate.x);
            int yDepth = Mathf.Min(coordinate.y, size.Height - 1 - coordinate.y);
            int zDepth = Mathf.Min(coordinate.z, size.Depth - 1 - coordinate.z);
            return Mathf.Min(xDepth, Mathf.Min(yDepth, zDepth));
        }

        /// <summary>
        /// Resolves the outermost remaining shell index for surface-generated levels.
        /// </summary>
        private int GetCurrentSurfaceShellIndex()
        {
            int currentShellIndex = int.MaxValue;
            foreach (MahjongTile tile in tilesById.Values)
            {
                if (tile == null || tile.IsRemoved || tile.IsMatched)
                {
                    continue;
                }

                currentShellIndex = Mathf.Min(currentShellIndex, tile.SurfaceShellIndex);
            }

            return currentShellIndex == int.MaxValue ? 0 : currentShellIndex;
        }

        /// <summary>
        /// Gets a value indicating whether the temporary X-Ray reveal effect is active.
        /// </summary>
        private bool IsXRayActive()
        {
            if (activeXRayDepth <= 0 || xrayEndTime <= 0f)
            {
                return false;
            }

            if (GetCurrentTime() > xrayEndTime)
            {
                xrayEndTime = 0f;
                activeXRayDepth = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the active runtime time source used by the exposure system.
        /// </summary>
        private float GetCurrentTime()
        {
            if (exposureSettings != null && exposureSettings.UseUnscaledTime)
            {
                return Time.unscaledTime;
            }

            return Time.time;
        }

        /// <summary>
        /// Gets the required visible sample ratio.
        /// </summary>
        private float GetRequiredVisibleSampleRatio()
        {
            return exposureSettings != null ? exposureSettings.RequiredVisibleSampleRatio : 0.9f;
        }

        /// <summary>
        /// Gets the per-axis inset applied to visibility sample points.
        /// </summary>
        private float GetVisibilitySampleInset()
        {
            return exposureSettings != null ? exposureSettings.VisibilitySampleInset : 0.01f;
        }

        /// <summary>
        /// Gets the extra ray length padding used during visibility tests.
        /// </summary>
        private float GetVisibilityRayPadding()
        {
            return exposureSettings != null ? exposureSettings.VisibilityRayPadding : 0.05f;
        }
    }
}
