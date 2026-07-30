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
        return cachedData.GetSetting(setting);
    }

    public void ToggleSetting(EGameSetting setting)
    {
        cachedData.ToggleSetting(setting);
        OnValueChange();
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
        dicGameSetting[setting.ToString()] = !dicGameSetting[setting.ToString()];
    }
    public bool GetSetting(EGameSetting setting)
    {
        return dicGameSetting[setting.ToString()];
    }

    public void OnNewData()
    {
    }
}
