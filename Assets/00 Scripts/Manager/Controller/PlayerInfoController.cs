using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerInfoConstant
{
    public int maxLevel = 50;
}

public interface IPlayerInfoController : IController<PlayerInfoController>
{
    public void WinLevel();
    public int CurrentLevel();
    public int MaxLevel();
}

public class PlayerInfoController :
#if LOCAL_BUILD
    BaseLocalController<PlayerInfoCachedData>
#else
    CommonServerController<PlayerInfoCachedData>
#endif
    , IPlayerInfoController
{
    public PlayerInfoConstant constant = new PlayerInfoConstant();

    public override string KeyData()
    {
        return "player_info";
    }
    public override string KeyEvent()
    {
        return Constant.EVENT_ON_PLAYER_INFO_CHANGE;
    }
    public void WinLevel()
    {
        if (CurrentLevel() < constant.maxLevel)
        {
            cachedData.level++;
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelCompleted, CurrentLevel() - 1, true);
            OnValueChange();
        }
    }
    public int CurrentLevel()
    {
        return cachedData.level;
    }
    public int MaxLevel()
    {
        return constant.maxLevel;
    }
}
public class PlayerInfoCachedData : IControllerCachedData
{
    public void InitFirsTime()
    {
    }
    public void OnNewData()
    {
    }
    public int level = 1;
}
