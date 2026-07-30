using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IAchievementController : IController<AchievementController>
{
    public long GetAchievementProgress(EAchievementType achievementType);
    public void UpdateAchievementProgress(EAchievementType achievementType, long val = 1, bool replace = false);
}
public class AchievementController :
#if LOCAL_BUILD
    BaseLocalController<AchievementCachedData>
#else
    CommonServerController<AchievementCachedData>
#endif
    , IAchievementController
{
    public override string KeyData()
    {
        return "achievement";
    }

    public override string KeyEvent()
    {
        return Constant.EVENT_ON_ACHIEVEMENT_CHANGE;
    }
    protected override void OnNextDay()
    {
        base.OnNextDay();
        UpdateAchievementProgress(EAchievementType.Login);
    }
    public long GetAchievementProgress(EAchievementType achievementType)
    {
        return cachedData.GetAchievementProgress(achievementType);
    }

    public void UpdateAchievementProgress(EAchievementType achievementType, long val = 1, bool replace = false)
    {
        cachedData.dicAchivementProgress[achievementType.ToString()] += val;
        IDailyQuestController.Instance.UpdateQuestProgress(achievementType, val, replace);
        OnValueChange();
    }
}
public class AchievementCachedData : IControllerCachedData
{
    public Dictionary<string, long> dicAchivementProgress = new Dictionary<string, long>();

    public void InitFirsTime()
    {
        List<EAchievementType> lstType = Helper.GetListEnum<EAchievementType>();
        foreach (var item in lstType)
        {
            if (!dicAchivementProgress.ContainsKey(item.ToString()))
                dicAchivementProgress.Add(item.ToString(), 0);
        }
    }
    public void OnNewData()
    {
    }
    public long GetAchievementProgress(EAchievementType achievementType)
    {
        return dicAchivementProgress[achievementType.ToString()];
    }
    public void UpdateAchievementProgress(EAchievementType achievementType, long val = 1, bool replace = false)
    {
        if (!replace)
            dicAchivementProgress[achievementType.ToString()] += val;
        else
            dicAchivementProgress[achievementType.ToString()] = val;
    }
}