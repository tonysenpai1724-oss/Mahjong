using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameSettingController : IController<GameSettingController>
{
    public bool GetSetting(EGameSetting setting);
    public void ToggleSetting(EGameSetting setting);
}
public class GameSettingController : 
#if LOCAL_BUILD
    BaseLocalController<GameSettingCachedData>
#else
    CommonServerController<GameSettingCachedData>
#endif
    , IGameSettingController
{
    private GameSettingCachedData defaultData;

    public override string KeyData()
    {
        return "game_setting";
    }

    public override string KeyEvent()
    {
        return Constant.EVENT_ON_GAME_SETTING_CHANGE;
    }

    public bool GetSetting(EGameSetting setting)
    {
        return GetReadableData().GetSetting(setting);
    }

    public void ToggleSetting(EGameSetting setting)
    {
        if (cachedData == null)
        {
            Debug.LogWarning($"Game setting data is not initialized yet. Ignore toggle: {setting}");
            return;
        }

        cachedData.ToggleSetting(setting);
        OnValueChange();
    }

    private GameSettingCachedData GetReadableData()
    {
        if (cachedData != null)
        {
            return cachedData;
        }

        if (defaultData == null)
        {
            defaultData = new GameSettingCachedData();
            defaultData.InitFirsTime();
        }

        return defaultData;
    }
}
public class GameSettingCachedData : IControllerCachedData
{
    public Dictionary<string, bool> dicGameSetting = new Dictionary<string, bool>();
    public void InitFirsTime()
    {
        List<EGameSetting> lstType = Helper.GetListEnum<EGameSetting>();
        foreach (var item in lstType)
        {
            if (!dicGameSetting.ContainsKey(item.ToString()))
                dicGameSetting.Add(item.ToString(), true);
        }
    }
    public void ToggleSetting(EGameSetting setting)
    {
        string key = setting.ToString();
        if (!dicGameSetting.ContainsKey(key))
        {
            dicGameSetting.Add(key, true);
        }

        dicGameSetting[key] = !dicGameSetting[key];
    }
    public bool GetSetting(EGameSetting setting)
    {
        string key = setting.ToString();
        if (!dicGameSetting.ContainsKey(key))
        {
            dicGameSetting.Add(key, true);
        }

        return dicGameSetting[key];
    }

    public void OnNewData()
    {
    }
}
