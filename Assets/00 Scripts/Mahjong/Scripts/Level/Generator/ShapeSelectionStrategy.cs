namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Centralizes shape-specific selection behavior used by the generator.
    /// </summary>
    internal static class ShapeSelectionStrategy
    {
        public static bool IsCompact(LevelShapeType shape)
        {
            return shape == LevelShapeType.Pyramid
                || shape == LevelShapeType.Dome
                || shape == LevelShapeType.Ramp;
        }

        public static bool RequiresCompleteShellSelection(LevelShapeType shape)
        {
            return shape == LevelShapeType.Cube
                || shape == LevelShapeType.Rectangle
                || shape == LevelShapeType.T
                || shape == LevelShapeType.H
                || shape == LevelShapeType.I
                || shape == LevelShapeType.L
                || shape == LevelShapeType.Cylinder;
        }
    }
}
