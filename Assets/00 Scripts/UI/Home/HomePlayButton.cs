using System.Collections;
using System.Collections.Generic;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.Managers;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
public class HomePlayButton : HomeFeatureButton
{
    public TMPro.TextMeshProUGUI txtLevel;
  //  public TMPro.TextMeshProUGUI txtDifficulty;
    public LevelCatalog levelCatalog;
    public Image difficultyImage;
    public LevelDefinitionsUI levelDefinitionsUI;
    void OnEnable()
    {
        txtLevel.text ="Level " + IPlayerInfoController.Instance.CurrentLevel().ToString();
         LevelDefinition levelDef = levelCatalog.TryGetLevel(IPlayerInfoController.Instance.CurrentLevel() - 1, 
         out LevelDefinition def) ? def : null;
         Sprite difficultySprite = null;
        if (levelDef != null)
        {
            foreach (var kvp in levelDefinitionsUI.levelDefinitions)
            {
                if (kvp.Value == levelDef.Difficulty)
                {
                    difficultySprite = kvp.Key;
                    break;
                }
            }
        }
        difficultyImage.sprite = difficultySprite;
      

        // txtDifficulty.text = levelDef != null ? levelDef.Difficulty.ToString() : "Unknown";
    }
    public override void OnClick()
    {
        GameManager.Instance.PlayGame(EGameType.Campaign);
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
    }
}

[System.Serializable]
[CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Level Definitions UI", fileName = "LevelDefinitionsUI")]
public class  LevelDefinitionsUI:SerializedScriptableObject
{
    public Dictionary<Sprite, LevelDifficulty> levelDefinitions = new Dictionary<Sprite, LevelDifficulty>();
}