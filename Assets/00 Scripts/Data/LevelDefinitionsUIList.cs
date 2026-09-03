
using  System.Collections.Generic;
using MahjongOut3D.LevelSystem;
using Sirenix.OdinInspector;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Level Definitions UI List", fileName = "LevelDefinitionsUIList")]
public class LevelDefinitionsUIList : SerializedScriptableObject
{
    public List<LevelDefinitionsUI> LevelDefinitions;
} 
[System.Serializable]
public class  LevelDefinitionsUI
{
    public Sprite HeaderImg,BodyImg;
    public LevelDifficulty Difficulty;
}
