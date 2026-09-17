using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.Data
{
    /// <summary>
    /// Groups a set of textures under one named category.
    /// </summary>
    [Serializable]
    public sealed class MahjongMaterialCategory
    {
        [SerializeField] private string categoryName;
        [SerializeField] private List<Texture2D> textures = new List<Texture2D>();

        /// <summary>
        /// Gets or sets the category label.
        /// </summary>
        public string CategoryName
        {
            get => categoryName;
            set => categoryName = value;
        }

        /// <summary>
        /// Gets the mutable texture list for this category.
        /// </summary>
        public List<Texture2D> Textures => textures;
    }
}
