using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonToggleSetting : ToggleButton
{
    public EGameSetting gameSetting;
    protected override void Start()
    {
        base.Start();
        sprOn = DataSystem.Instance.dataSprites.dicSettingIcon[gameSetting][true];
        sprOff = DataSystem.Instance.dataSprites.dicSettingIcon[gameSetting][false];
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_GAME_SETTING_CHANGE, InitUI);
        InitUI();
    }
    void InitUI()
    {
        SetActive(IGameSettingController.Instance.GetSetting(gameSetting));
    }
    public void OnCick()
    {
        IGameSettingController.Instance.ToggleSetting(gameSetting);
    }
}
