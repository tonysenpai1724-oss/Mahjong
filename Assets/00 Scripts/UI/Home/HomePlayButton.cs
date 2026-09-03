using System.Collections;
using System.Collections.Generic;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.Managers;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System;
public class HomePlayButton : HomeFeatureButton
{
    public TMPro.TextMeshProUGUI txtLevel;
    public TMPro.TextMeshProUGUI txtDifficulty;
    public LevelCatalog levelCatalog;
    public Image difficultyHeaderImage;
    public Image difficultyBodyImage;
    public LevelDefinitionsUIList levelDefinitionsUI;
    
    protected override void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_BUTTON_STATE_CHANGE, OnButtonStateChange);
        if(notiObj != null)
            notiObj.SetActive(false);
        DisableAll += DisableButton;
        EnableAll += EnableButton;
          button.onClick.AddListener(OnClick);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        OnTick();
    }
    void OnEnable()
    {
        txtLevel.text ="Level " + IPlayerInfoController.Instance.CurrentLevel().ToString();
         LevelDefinition levelDef = levelCatalog.TryGetLevel(IPlayerInfoController.Instance.CurrentLevel() - 1, 
         out LevelDefinition def) ? def : null;
         txtDifficulty.text = levelDef != null ? levelDef.Difficulty.ToString() : "Unknown";
        if (levelDef != null)
        {
            foreach (var levelUI in levelDefinitionsUI.LevelDefinitions)
            {
                if (levelUI.Difficulty == levelDef.Difficulty)
                {

                    difficultyHeaderImage.sprite = levelUI.HeaderImg;
                    difficultyBodyImage.sprite = levelUI.BodyImg;
                    break;
                }
            }
        }
      

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
public class  LevelDefinitionsUI
{
    public Sprite HeaderImg,BodyImg;
    public LevelDifficulty Difficulty;
}

[System.Serializable]
[CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Level Definitions UI List", fileName = "LevelDefinitionsUIList")]
public class LevelDefinitionsUIList : SerializedScriptableObject
{
    public List<LevelDefinitionsUI> LevelDefinitions;
} 