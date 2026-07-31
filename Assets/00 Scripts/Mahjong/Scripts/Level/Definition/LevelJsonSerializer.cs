using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Converts level assets to and from JSON-compatible DTOs.
    /// </summary>
    public static class LevelJsonSerializer
    {
        /// <summary>
        /// Parses a JSON string into a serializable level DTO.
        /// </summary>
        /// <param name="json">JSON payload to parse.</param>
        /// <returns>Parsed level DTO, or null when the payload is invalid.</returns>
        public static LevelJsonData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<LevelJsonData>(json);
        }

        /// <summary>
        /// Converts a JSON DTO into runtime level tile definitions.
        /// </summary>
        /// <param name="jsonData">JSON DTO to convert.</param>
        /// <returns>Runtime level tile definitions.</returns>
        public static List<LevelTileDefinition> ToTileDefinitions(LevelJsonData jsonData)
        {
            List<LevelTileDefinition> tiles = new List<LevelTileDefinition>();
            if (jsonData == null || jsonData.tiles == null)
            {
                return tiles;
            }

            for (int index = 0; index < jsonData.tiles.Count; index++)
            {
                LevelJsonTileData tile = jsonData.tiles[index];
                if (tile == null)
                {
                    continue;
                }

                tiles.Add(new LevelTileDefinition
                {
                    MatchId = tile.matchId,
                    GridCoordinate = new Vector3Int(tile.x, tile.y, tile.z),
                    UseCustomLocalPosition = tile.useCustomLocalPosition,
                    LocalPosition = new Vector3(tile.posX, tile.posY, tile.posZ),
                    LocalEulerAngles = new Vector3(tile.rotX, tile.rotY, tile.rotZ),
                });
            }

            return tiles;
        }

        /// <summary>
        /// Converts a level definition asset into a JSON DTO.
        /// </summary>
        /// <param name="definition">Level definition asset to convert.</param>
        /// <returns>JSON DTO representing the level.</returns>
        public static LevelJsonData FromDefinition(LevelDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            LevelJsonData data = new LevelJsonData
            {
                levelName = definition.LevelName,
                width = definition.GridSize.Width,
                height = definition.GridSize.Height,
                depth = definition.GridSize.Depth,
                useSurfaceTilePlacement = definition.UseSurfaceTilePlacement,
                shape = definition.Shape,
                difficulty = definition.Difficulty,
            };

            if (definition.Tiles == null)
            {
                return data;
            }

            for (int index = 0; index < definition.Tiles.Count; index++)
            {
                LevelTileDefinition tile = definition.Tiles[index];
                if (tile == null)
                {
                    continue;
                }

                data.tiles.Add(new LevelJsonTileData
                {
                    matchId = tile.MatchId,
                    x = tile.GridCoordinate.x,
                    y = tile.GridCoordinate.y,
                    z = tile.GridCoordinate.z,
                    useCustomLocalPosition = tile.UseCustomLocalPosition,
                    posX = tile.LocalPosition.x,
                    posY = tile.LocalPosition.y,
                    posZ = tile.LocalPosition.z,
                    rotX = tile.LocalEulerAngles.x,
                    rotY = tile.LocalEulerAngles.y,
                    rotZ = tile.LocalEulerAngles.z,
                });
            }

            return data;
        }

        /// <summary>
        /// Serializes a level definition asset into a formatted JSON string.
        /// </summary>
        /// <param name="definition">Level definition asset to serialize.</param>
        /// <returns>Formatted JSON string.</returns>
        public static string ToJson(LevelDefinition definition)
        {
            LevelJsonData data = FromDefinition(definition);
            return data == null ? string.Empty : JsonUtility.ToJson(data, true);
        }
    }
}
