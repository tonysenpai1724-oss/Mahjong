using System;
using MahjongOut3D.Data;
using UnityEngine;

namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Represents a single Mahjong tile instance inside the 3D puzzle block.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MahjongTile : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int tileId;
        [SerializeField] private int matchId;
        [SerializeField] private Vector3Int gridCoordinate;
        [SerializeField] private int surfaceShellIndex;
        [SerializeField] private Vector3 boardLocalPosition;
        [SerializeField] private Vector3 boardLocalEulerAngles;
        [SerializeField] private bool isBufferedSelection;
        [SerializeField] private Transform boardParent;

        [Header("Components")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshRenderer pieceRenderer;
        [SerializeField] private MeshRenderer fillRenderer;
        [SerializeField] private MeshRenderer matchIndicatorRenderer;
        [SerializeField] private Collider tileCollider;
        [SerializeField] private Animator animator;
        [SerializeField] private TileOutlinePresenter outlinePresenter;
        [SerializeField] private TileVisualController visualController;

        [Header("Runtime")]
        [SerializeField] private TileState state = TileState.Hidden;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock baseColorPropertyBlock;
        private bool hasDebugBaseColor;
        private Color debugBaseColor = Color.white;

        /// <summary>
        /// Occurs when the tile changes runtime state.
        /// </summary>
        public event Action<TileStateChangedEvent> StateChanged;

        /// <summary>
        /// Occurs when the tile enters or exits the selected state.
        /// </summary>
        public event Action<TileSelectionChangedEvent> SelectionChanged;

        /// <summary>
        /// Gets the unique tile identifier.
        /// </summary>
        public int TileId => tileId;

        /// <summary>
        /// Gets the shared match identifier used to pair this tile.
        /// </summary>
        public int MatchId => matchId;

        /// <summary>
        /// Gets the logical grid coordinate inside the voxel block.
        /// </summary>
        public Vector3Int GridCoordinate => gridCoordinate;

        /// <summary>
        /// Gets the nested shell depth for surface-generated levels, where zero is outermost.
        /// </summary>
        public int SurfaceShellIndex => Mathf.Max(0, surfaceShellIndex);

        /// <summary>
        /// Gets the authored board-local position used when restoring the tile from the selection tray.
        /// </summary>
        public Vector3 BoardLocalPosition => boardLocalPosition;

        /// <summary>
        /// Gets the authored board-local Euler rotation used when restoring the tile from the selection tray.
        /// </summary>
        public Vector3 BoardLocalEulerAngles => boardLocalEulerAngles;

        /// <summary>
        /// Gets a value indicating whether the tile is currently parked in the temporary selection tray.
        /// </summary>
        public bool IsBufferedSelection => isBufferedSelection;

        /// <summary>
        /// Gets the primary tile renderer.
        /// </summary>
        public MeshRenderer MeshRenderer => meshRenderer;

        /// <summary>
        /// Gets the optional renderer used to display a match-indicator material on the tile.
        /// </summary>
        public MeshRenderer MatchIndicatorRenderer => matchIndicatorRenderer;

        /// <summary>
        /// Gets the tile collider used for hit testing.
        /// </summary>
        public Collider TileCollider => tileCollider;

        /// <summary>
        /// Gets the tile animator reference.
        /// </summary>
        public Animator Animator => animator;

        /// <summary>
        /// Gets the tile outline presenter.
        /// </summary>
        public TileOutlinePresenter Outline => outlinePresenter;

        /// <summary>
        /// Gets the tile visual controller.
        /// </summary>
        public TileVisualController VisualController => visualController;

        /// <summary>
        /// Gets the current tile state.
        /// </summary>
        public TileState State => state;

        /// <summary>
        /// Gets a value indicating whether the tile can currently receive pointer hits.
        /// </summary>
        public bool IsInteractable => state == TileState.Visible || state == TileState.Selected;

        /// <summary>
        /// Gets a value indicating whether the tile has already been removed from the puzzle.
        /// </summary>
        public bool IsRemoved => state == TileState.Removed;

        /// <summary>
        /// Gets a value indicating whether the tile is currently in the matched transition state.
        /// </summary>
        public bool IsMatched => state == TileState.Matched;

        /// <summary>
        /// Applies serialized defaults once the component awakens.
        /// </summary>
        private void Awake()
        {
            CacheReferences();
            Setup();
            ApplyStateToPresentation(state, true);
        }

        /// <summary>
        /// Auto-caches tile references in the editor when values change.
        /// </summary>
        private void OnValidate()
        {
            CacheReferences();
            Setup();
        }

        /// <summary>
        /// Applies the configured material set to the tile's piece and fill renderers.
        /// </summary>
        public void Setup()
        {
           
        }
        public void SetupPieceMaterial(Material pieceMaterial)
        {
            if (pieceRenderer == null)
            {
                CacheReferences();
            }

            if (pieceRenderer != null)
            {
                pieceRenderer.sharedMaterial = pieceMaterial;
            }
        }

        public void SetupFillMaterial(Material fillMaterial)
        {
            if (fillRenderer == null)
            {
                CacheReferences();
            }

            if (fillRenderer != null)
            {
                fillRenderer.sharedMaterial = fillMaterial;
            }
        }
        /// <summary>
        /// Applies explicit piece and fill materials to the tile renderers.
        /// </summary>
        public void Setup(Material pieceMaterial, Material fillMaterial)
        {
            if (pieceRenderer == null || fillRenderer == null)
            {
                CacheReferences();
            }

            if (pieceRenderer != null)
            {
                pieceRenderer.sharedMaterial = pieceMaterial;
            }

            if (fillRenderer != null)
            {
                fillRenderer.sharedMaterial = fillMaterial;
            }
        }

        /// <summary>
        /// Applies runtime data generated by a level loader or voxel generator.
        /// </summary>
        /// <param name="runtimeData">Tile runtime data to apply.</param>
        public void ApplyRuntimeData(TileRuntimeData runtimeData)
        {
            if (runtimeData == null)
            {
                return;
            }

            tileId = runtimeData.TileId;
            matchId = runtimeData.MatchId;
            gridCoordinate = runtimeData.GridCoordinate;
            surfaceShellIndex = runtimeData.SurfaceShellIndex;
            boardLocalPosition = runtimeData.LocalPosition;
            boardLocalEulerAngles = runtimeData.LocalEulerAngles;
            isBufferedSelection = false;
            boardParent = transform.parent;
            transform.localPosition = runtimeData.LocalPosition;
            transform.localRotation = Quaternion.Euler(runtimeData.LocalEulerAngles);
            gameObject.name = $"MahjongTile_{tileId}_{matchId}";
        }

        /// <summary>
        /// Updates the local-space transform driven by the voxel grid.
        /// </summary>
        /// <param name="localPosition">Local-space position to apply.</param>
        /// <param name="localEulerAngles">Local-space rotation to apply.</param>
        public void SetLocalPose(Vector3 localPosition, Vector3 localEulerAngles)
        {
            boardLocalPosition = localPosition;
            boardLocalEulerAngles = localEulerAngles;
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEulerAngles);
        }

        /// <summary>
        /// Restores the current transform back to the cached board-local pose without changing board metadata.
        /// </summary>
        public void RestoreBoardPose()
        {
            transform.localPosition = boardLocalPosition;
            transform.localRotation = Quaternion.Euler(boardLocalEulerAngles);
        }

        /// <summary>
        /// Detaches the tile from the rotating board hierarchy while preserving world pose.
        /// </summary>
        public void DetachFromBoardParent()
        {
            if (boardParent == null)
            {
                boardParent = transform.parent;
            }

            transform.SetParent(null, true);
        }

        /// <summary>
        /// Reattaches the tile to its authored board parent while preserving world pose.
        /// </summary>
        public void RestoreBoardParent()
        {
            if (boardParent != null)
            {
                transform.SetParent(boardParent, true);
            }
        }

        /// <summary>
        /// Changes the tile visibility state.
        /// </summary>
        /// <param name="isVisible">True to show the tile; otherwise false.</param>
        public void SetVisible(bool isVisible)
        {
            SetState(isVisible ? TileState.Visible : TileState.Hidden);
        }

        /// <summary>
        /// Selects the tile when it is currently visible.
        /// </summary>
        /// <returns>True when the tile becomes selected; otherwise false.</returns>
        public bool TrySelect()
        {
            if (state == TileState.Selected)
            {
                return true;
            }

            if (state != TileState.Visible)
            {
                return false;
            }

            SetState(TileState.Selected);
            return true;
        }

        /// <summary>
        /// Returns a selected tile back to the visible state.
        /// </summary>
        public void Deselect()
        {
            if (state == TileState.Selected)
            {
                SetState(TileState.Visible);
            }
        }

        /// <summary>
        /// Marks the tile as matched and disables further interaction.
        /// </summary>
        public void MarkMatched()
        {
            SetState(TileState.Matched);
        }

        /// <summary>
        /// Marks the tile as removed and hides its presentation.
        /// </summary>
        public void MarkRemoved()
        {
            SetState(TileState.Removed);
        }

        /// <summary>
        /// Resets the tile to a hidden default state.
        /// </summary>
        public void ResetTile()
        {
            SetState(TileState.Hidden, true);
        }

        /// <summary>
        /// Restores a removed or matched tile back into the active puzzle.
        /// </summary>
        /// <param name="isVisible">True to restore the tile as visible; otherwise hidden.</param>
        public void Restore(bool isVisible)
        {
            isBufferedSelection = false;
            SetState(isVisible ? TileState.Visible : TileState.Hidden, true);
        }

        /// <summary>
        /// Marks whether the tile is currently parked inside the temporary selection tray.
        /// </summary>
        public void SetBufferedSelection(bool isBuffered)
        {
            isBufferedSelection = isBuffered;
            if (!isBuffered)
            {
                CacheReferences();
            }
        }

        /// <summary>
        /// Updates the logical grid coordinate used by the level systems.
        /// </summary>
        /// <param name="coordinate">New grid coordinate.</param>
        public void SetGridCoordinate(Vector3Int coordinate)
        {
            gridCoordinate = coordinate;
        }

        /// <summary>
        /// Resolves the offset between the tile root and the visual or collider center.
        /// </summary>
        /// <returns>Root-space offset that should be subtracted from spawn placement.</returns>
        public Vector3 GetPlacementOffset()
        {
            CacheReferences();

            if (TryGetPlacementLocalBounds(out Bounds placementBounds))
            {
                return placementBounds.center;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Resolves the physical placement size of the tile in root local space.
        /// </summary>
        /// <returns>Root-local bounds size when available; otherwise zero.</returns>
        public Vector3 GetPlacementSize()
        {
            CacheReferences();

            return TryGetPlacementLocalBounds(out Bounds placementBounds)
                ? placementBounds.size
                : Vector3.zero;
        }

        /// <summary>
        /// Applies a runtime base color override used to visually distinguish matched pairs.
        /// </summary>
        /// <param name="color">Base color for this tile instance.</param>
        public void SetDebugMatchColor(Color color)
        {
            if (visualController == null)
            {
                CacheReferences();
            }

            hasDebugBaseColor = true;
            debugBaseColor = color;
            visualController?.SetRuntimeBaseColor(color);
            visualController?.ApplyState(state, true);
            ApplyDirectBaseColorOverride();
        }

        /// <summary>
        /// Clears the runtime base color override and restores the tile's default material colors.
        /// </summary>
        public void ClearDebugMatchColor()
        {
            if (visualController == null)
            {
                CacheReferences();
            }

            hasDebugBaseColor = false;
            debugBaseColor = Color.white;
            visualController?.ClearRuntimeBaseColor();
            visualController?.ApplyState(state, true);

            if (meshRenderer != null)
            {
                meshRenderer.SetPropertyBlock(null);
            }
        }

        /// <summary>
        /// Assigns a material to the optional match-indicator renderer used for gameplay testing.
        /// </summary>
        /// <param name="material">Material to display on the indicator quad, or null to leave the current material unchanged.</param>
        public void SetMatchIndicatorMaterial(Material material)
        {
            if (matchIndicatorRenderer == null)
            {
                CacheReferences();
            }

            if (matchIndicatorRenderer != null && material != null)
            {
                matchIndicatorRenderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// Enables or disables the temporary hint highlight for this tile.
        /// </summary>
        /// <param name="isHighlighted">True to show hint feedback; otherwise false.</param>
        public void SetHintHighlighted(bool isHighlighted)
        {
            if (visualController == null)
            {
                CacheReferences();
            }

            if (visualController == null)
            {
                return;
            }

            visualController.SetHintHighlighted(isHighlighted);
            visualController.ApplyState(state, true);
        }

        /// <summary>
        /// Changes the runtime tile state and updates visuals and colliders.
        /// </summary>
        /// <param name="newState">New tile state.</param>
        /// <param name="force">True to force the transition even if the state matches.</param>
        public void SetState(TileState newState, bool force = false)
        {
            if (!force && state == newState)
            {
                return;
            }

            if (newState == TileState.Matched || newState == TileState.Removed || newState == TileState.Hidden)
            {
                SetHintHighlighted(false);
            }

            TileState previousState = state;
            state = newState;
            ApplyStateToPresentation(state, false);

            TileStateChangedEvent stateEvent = new TileStateChangedEvent(this, previousState, state);
            StateChanged?.Invoke(stateEvent);

            bool wasSelected = previousState == TileState.Selected;
            bool isSelected = state == TileState.Selected;
            if (wasSelected != isSelected)
            {
                SelectionChanged?.Invoke(new TileSelectionChangedEvent(this, isSelected));
            }
        }

        /// <summary>
        /// Caches missing component references from the tile hierarchy.
        /// </summary>
        private void CacheReferences()
        {
            if (visualController == null)
            {
                visualController = GetComponentInChildren<TileVisualController>(true);
            }

            if (meshRenderer == null)
            {
                meshRenderer = visualController != null ? visualController.GetPrimaryRenderer() : GetComponentInChildren<MeshRenderer>(true);
            }

            if (pieceRenderer == null || fillRenderer == null || matchIndicatorRenderer == null)
            {
                MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
                for (int index = 0; index < renderers.Length; index++)
                {
                    MeshRenderer renderer = renderers[index];
                    if (renderer == null)
                    {
                        continue;
                    }

                    string rendererName = renderer.gameObject.name;
                    if (pieceRenderer == null && rendererName.Equals("Mahjong Piece", StringComparison.OrdinalIgnoreCase))
                    {
                        pieceRenderer = renderer;
                    }

                    if (fillRenderer == null && rendererName.Equals("Object Fill", StringComparison.OrdinalIgnoreCase))
                    {
                        fillRenderer = renderer;
                    }

                    if (matchIndicatorRenderer == null && renderer != meshRenderer && rendererName.Equals("Quad", StringComparison.OrdinalIgnoreCase))
                    {
                        matchIndicatorRenderer = renderer;
                    }
                }
            }

            if (tileCollider == null)
            {
                tileCollider = GetComponentInChildren<Collider>(true);
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (outlinePresenter == null)
            {
                outlinePresenter = visualController != null ? visualController.GetOutlinePresenter() : GetComponentInChildren<TileOutlinePresenter>(true);
            }
        }

        /// <summary>
        /// Tries to resolve a stable bounds source for placement alignment.
        /// </summary>
        /// <param name="placementBounds">Resolved bounds when available.</param>
        /// <returns>True when a non-empty bounds source was found; otherwise false.</returns>
        private bool TryGetPlacementLocalBounds(out Bounds placementBounds)
        {
            placementBounds = default;

            if (meshRenderer != null)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    return TryTransformLocalBounds(meshFilter.transform, meshFilter.sharedMesh.bounds, out placementBounds);
                }
            }

            if (tileCollider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
            {
                return TryTransformLocalBounds(meshCollider.transform, meshCollider.sharedMesh.bounds, out placementBounds);
            }

            if (tileCollider is BoxCollider boxCollider)
            {
                return TryTransformLocalBounds(boxCollider.transform, new Bounds(boxCollider.center, boxCollider.size), out placementBounds);
            }

            return false;
        }

        /// <summary>
        /// Converts a component-local bounds into the tile root local space.
        /// </summary>
        private bool TryTransformLocalBounds(Transform sourceTransform, Bounds sourceBounds, out Bounds transformedBounds)
        {
            transformedBounds = default;
            if (sourceTransform == null || sourceBounds.size.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Matrix4x4 toRootMatrix = transform.worldToLocalMatrix * sourceTransform.localToWorldMatrix;
            Vector3 sourceCenter = sourceBounds.center;
            Vector3 sourceExtents = sourceBounds.extents;

            Vector3[] corners = new Vector3[8];
            int cornerIndex = 0;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = sourceCenter + Vector3.Scale(sourceExtents, new Vector3(x, y, z));
                        corners[cornerIndex++] = toRootMatrix.MultiplyPoint3x4(corner);
                    }
                }
            }

            transformedBounds = new Bounds(corners[0], Vector3.zero);
            for (int index = 1; index < corners.Length; index++)
            {
                transformedBounds.Encapsulate(corners[index]);
            }

            return transformedBounds.size.sqrMagnitude > Mathf.Epsilon;
        }

        /// <summary>
        /// Applies runtime state to colliders and visual presentation.
        /// </summary>
        /// <param name="currentState">Tile state to present.</param>
        /// <param name="instant">True to snap visual transitions immediately.</param>
        private void ApplyStateToPresentation(TileState currentState, bool instant)
        {
            if (tileCollider != null)
            {
                tileCollider.enabled = currentState == TileState.Visible || currentState == TileState.Selected;
            }

            if (matchIndicatorRenderer != null)
            {
                matchIndicatorRenderer.enabled = currentState != TileState.Hidden && currentState != TileState.Removed;
            }

            if (visualController != null)
            {
                visualController.ApplyState(currentState, instant);
            }
        }

        /// <summary>
        /// Reapplies the runtime base color directly onto the primary renderer so debug tinting stays visible.
        /// </summary>
        private void ApplyDirectBaseColorOverride()
        {
            if (!hasDebugBaseColor)
            {
                return;
            }

            if (meshRenderer == null)
            {
                CacheReferences();
            }

            if (meshRenderer == null)
            {
                return;
            }

            Material sharedMaterial = meshRenderer.sharedMaterial;
            if (sharedMaterial == null)
            {
                return;
            }

            if (baseColorPropertyBlock == null)
            {
                baseColorPropertyBlock = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(baseColorPropertyBlock);

            bool applied = false;
            if (sharedMaterial.HasProperty(BaseColorId))
            {
                baseColorPropertyBlock.SetColor(BaseColorId, debugBaseColor);
                applied = true;
            }

            if (sharedMaterial.HasProperty(ColorId))
            {
                baseColorPropertyBlock.SetColor(ColorId, debugBaseColor);
                applied = true;
            }

            if (applied)
            {
                meshRenderer.SetPropertyBlock(baseColorPropertyBlock);
            }
        }
    }
}
