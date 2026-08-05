using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.Data
{
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Data/Mahjong Material", fileName = "MahjongMaterial")]
    public sealed class MahjongMaterialSO : ScriptableObject
    {
        [SerializeField] public List<Material> pieceMaterial;
        [SerializeField] public List<Material> fillMaterial;
    }
}
