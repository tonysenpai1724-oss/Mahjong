
using  System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Level Definitions UI List", fileName = "LevelDefinitionsUIList")]
public class LevelDefinitionsUIList : SerializedScriptableObject
{
    public List<LevelDefinitionsUI> LevelDefinitions;
} 