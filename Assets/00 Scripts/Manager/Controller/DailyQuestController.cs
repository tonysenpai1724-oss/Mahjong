using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDailyQuestController : IController<DailyQuestControllerLocal>
{
    public void UpdateQuestProgress(EAchievementType questType, long val = 1, bool replace = false);
    public List<QuestItem> GetQuestItems();
    public void ClaimQuestReward(EAchievementType questType);
    public List<QuestMileStone> GetDailyMileStone();
    public List<QuestMileStone> GetWeeklyMileStone();
    public void ClaimDailyMilestone();
    public void ClaimWeeklyMilestone();
    public int GetDailyPoint();
    public int GetWeeklyPoint();
    public void OnGoQuest(EAchievementType questType);
    public bool NotiQuest();
}
public class DailyQuestCachedData : IControllerCachedData
{
    public Dictionary<string, long> dicQuestProgress = new Dictionary<string, long>();
    public List<string> lstQuestClaimed = new List<string>();
    public int dailyPoint;
    public int weeklyPoint;
    public int dailyMilestoneClaimed;
    public int weeklyMilestoneClaimed;
    public void InitFirsTime()
    {
        List<EAchievementType> lstType = Helper.GetListEnum<EAchievementType>();
        foreach (var item in lstType)
        {
            if (!dicQuestProgress.ContainsKey(item.ToString()))
                dicQuestProgress.Add(item.ToString(), 0);
        }
    }
    public void OnNewData()
    {
    }
    public void OnNewDay()
    {
        dailyPoint = 0;
        dailyMilestoneClaimed = 0;
        lstQuestClaimed.Clear();
        dicQuestProgress.Clear();
        InitFirsTime();
    }
    public void OnNewWeek()
    {
        weeklyPoint = 0;
        weeklyMilestoneClaimed = 0;
    }
    public ERewardState GetQuestState(DailyQuestConfig config)
    {
        if (lstQuestClaimed.Contains(config.questType.ToString()))
        {
            return ERewardState.Claimed;
        }
        if (GetProgressQuest(config.questType) >= config.questCondition)
        {
            return ERewardState.CanClaim;
        }
        return ERewardState.Progress;
    }
    public ERewardState GetDailyMilestoneSate(int point)
    {
        if (dailyMilestoneClaimed >= point)
        {
            return ERewardState.Claimed;
        }
        if (dailyPoint >= point)
        {
            return ERewardState.CanClaim;
        }
        return ERewardState.Progress;
    }
    public ERewardState GetWeeklyMilestoneState(int point)
    {
        if (weeklyMilestoneClaimed >= point)
        {
            return ERewardState.Claimed;
        }
        if (weeklyPoint >= point)
        {
            return ERewardState.CanClaim;
        }
        return ERewardState.Progress;
    }
    public long GetProgressQuest(EAchievementType questType)
    {
        return dicQuestProgress[questType.ToString()];
    }
    public void UpdateQuestProgress(EAchievementType questType, long val = 1, bool replace = false)
    {
        if (!replace)
            dicQuestProgress[questType.ToString()] += val;
        else
            dicQuestProgress[questType.ToString()] = val;
    }
    public void SetClaimDailyQuest(EAchievementType questType)
    {
        lstQuestClaimed.Add(questType.ToString());
    }
    public void AddPoint(int point)
    {
        dailyPoint += point;
        weeklyPoint += point;
    }
    public void SetDailyMileStoneClaimed(int point)
    {
        dailyMilestoneClaimed = point;
    }
    public void SetWeeklyMileStoneClaimed(int point)
    {
        weeklyMilestoneClaimed = point;
    }
    public int GetDailyPoint()
    {
        return dailyPoint;
    }
    public int GetWeeklyPoint()
    {
        return weeklyPoint;
    }
}
public class DailyQuestControllerLocal :
#if LOCAL_BUILD
    BaseLocalController<DailyQuestCachedData>
#else
    CommonServerController<DailyQuestCachedData>
#endif
    , IDailyQuestController
{
    List<QuestItem> lstQuest;
    List<QuestMileStone> lstDailyMileStone;
    List<QuestMileStone> lstWeeklyMileStone;

    bool notiQuest;
    bool notiMileStoneDaily;
    bool notiMileStoneWeekly;

    public override string KeyData()
    {
        return "daily_quest";
    }

    public override string KeyEvent()
    {
        return Constant.EVENT_ON_DAILY_QUEST_CHANGE;
    }
    protected override void OnInitSuccess()
    {
        base.OnInitSuccess();
        if (DataSystem.Instance != null && DataSystem.Instance.dataDailyQuest != null)
        {
            DataSystem.Instance.dataDailyQuest.EnsureDefaultQuestConfigs();
        }

        EnsureDefaultQuestProgress();
        NewQuestList();
    }

    void EnsureDefaultQuestProgress()
    {
        if (cachedData == null)
        {
            return;
        }

        foreach (var questType in Helper.GetListEnum<EAchievementType>())
        {
            if (!cachedData.dicQuestProgress.ContainsKey(questType.ToString()))
            {
                cachedData.UpdateQuestProgress(questType, 0, true);
            }
        }

        if (!cachedData.dicQuestProgress.ContainsKey(EAchievementType.Login.ToString()) || cachedData.GetProgressQuest(EAchievementType.Login) <= 0)
        {
            cachedData.UpdateQuestProgress(EAchievementType.Login, 1, true);
        }
    }

    void NewQuestList()
    {
        if (DataSystem.Instance == null || DataSystem.Instance.dataDailyQuest == null)
        {
            return;
        }

        DataSystem.Instance.dataDailyQuest.EnsureDefaultQuestConfigs();

        lstQuest = new List<QuestItem>();
        foreach (var item in DataSystem.Instance.dataDailyQuest.lstQuestConfig)
        {
            if (item == null)
            {
                continue;
            }

            QuestItem quest = new QuestItem();
            quest.questConfig = item;
            quest.progress = cachedData.GetProgressQuest(item.questType);
            quest.rewardState = cachedData.GetQuestState(item);
            lstQuest.Add(quest);
        }
        CheckNotiQuest();

        lstDailyMileStone = new List<QuestMileStone>();
        int last = 0;
        var dailyMilestones = DataSystem.Instance.dataDailyQuest.dicDailyMilestone ?? new Dictionary<int, string>();
        foreach (var item in dailyMilestones)
        {
            QuestMileStone quest = new QuestMileStone();
            quest.isDaily = true;
            quest.pointRequire = item.Key;
            quest.lastPointRequire = last;
            last = item.Key;
            quest.currentPoint = GetDailyPoint();
            quest.rewardState = cachedData.GetDailyMilestoneSate(item.Key);
            PackageResource pack = new PackageResource();
            pack.AddResource(GameResource.GetResource(item.Value));
            quest.rewards = pack;
            lstDailyMileStone.Add(quest);
        }
        CheckNotiMileStoneDaily();

        lstWeeklyMileStone = new List<QuestMileStone>();
        last = 0;
        var weeklyMilestones = DataSystem.Instance.dataDailyQuest.dicWeeklyMilestone ?? new Dictionary<int, string>();
        foreach (var item in weeklyMilestones)
        {
            QuestMileStone quest = new QuestMileStone();
            quest.isDaily = false;
            quest.pointRequire = item.Key;
            quest.lastPointRequire = last;
            last = item.Key;
            quest.currentPoint = GetWeeklyPoint();
            quest.rewardState = cachedData.GetWeeklyMilestoneState(item.Key);
            PackageResource pack = new PackageResource();
            pack.AddResource(GameResource.GetResource(item.Value));
            quest.rewards = pack;
            lstWeeklyMileStone.Add(quest);
        }
        CheckNotiMileStoneWeekly();
        OnValueChange();
    }
    void CheckNotiQuest()
    {
        notiQuest = lstQuest.Exists(q => q.rewardState == ERewardState.CanClaim);
    }
    void CheckNotiMileStoneDaily()
    {
        notiMileStoneDaily = lstDailyMileStone.Exists(q => q.rewardState == ERewardState.CanClaim);
    }
    void CheckNotiMileStoneWeekly()
    {
        notiMileStoneWeekly = lstWeeklyMileStone.Exists(q => q.rewardState == ERewardState.CanClaim);
    }
    protected override void OnNextDay()
    {
        base.OnNextDay();
        cachedData.OnNewDay();
        NewQuestList();
    }
    protected override void OnNextWeek()
    {
        base.OnNextWeek();
        cachedData.OnNewWeek();
        NewQuestList();
    }

    public void ClaimDailyMilestone()
    {
        PackageResource pack = new PackageResource();
        foreach (var item in lstDailyMileStone)
        {
            if (item.rewardState == ERewardState.CanClaim)
            {
                item.rewardState = ERewardState.Claimed;
                cachedData.SetDailyMileStoneClaimed(item.pointRequire);
                pack.AddResource(item.rewards);
            }
        }
        pack.ReceiveAndShow(EResourceFrom.DailyQuest);
        CheckNotiMileStoneDaily();
        OnValueChange();
    }

    public void ClaimWeeklyMilestone()
    {
        PackageResource pack = new PackageResource();
        foreach (var item in lstWeeklyMileStone)
        {
            if (item.rewardState == ERewardState.CanClaim)
            {
                item.rewardState = ERewardState.Claimed;
                cachedData.SetWeeklyMileStoneClaimed(item.pointRequire);
                pack.AddResource(item.rewards);
            }
        }
        pack.ReceiveAndShow(EResourceFrom.DailyQuest);
        CheckNotiMileStoneWeekly();
        OnValueChange();
    }

    public void ClaimQuestReward(EAchievementType questType)
    {
        QuestItem quest = lstQuest.Find(q => q.questConfig.questType == questType);
        if (quest == null || quest.rewardState != ERewardState.CanClaim)
        {
            return;
        }

        quest.rewardState = ERewardState.Claimed;
        cachedData.SetClaimDailyQuest(questType);
        cachedData.AddPoint(quest.questConfig.pointReward);

        PackageResource rewardPackage = new PackageResource();
        rewardPackage.AddResource(new CommonResource(ECommonResource.Coin, quest.questConfig.pointReward));
        rewardPackage.ReceiveAndShow(EResourceFrom.DailyQuest);

        CheckNotiQuest();
        foreach (var item in lstDailyMileStone)
        {
            item.currentPoint = GetDailyPoint();
            item.rewardState = cachedData.GetDailyMilestoneSate(item.pointRequire);
        }
        CheckNotiMileStoneDaily();
        foreach (var item in lstWeeklyMileStone)
        {
            item.currentPoint = GetWeeklyPoint();
            item.rewardState = cachedData.GetWeeklyMilestoneState(item.pointRequire);
        }
        CheckNotiMileStoneWeekly();
        OnValueChange();
    }

    public List<QuestMileStone> GetDailyMileStone()
    {
        return lstDailyMileStone;
    }

    public int GetDailyPoint()
    {
        return cachedData.GetDailyPoint();
    }

    public List<QuestItem> GetQuestItems()
    {
        return lstQuest;
    }

    public List<QuestMileStone> GetWeeklyMileStone()
    {
        return lstWeeklyMileStone;
    }

    public int GetWeeklyPoint()
    {
        return cachedData.GetWeeklyPoint();
    }

    public bool NotiQuest()
    {
        return notiQuest || notiMileStoneDaily || notiMileStoneWeekly;
    }

    public void OnGoQuest(EAchievementType questType)
    {
        switch (questType)
        {
            case EAchievementType.Login:
                break;
            case EAchievementType.UseBooster:
                UIManager.Instance.ShowPopupBooster();
                break;
            case EAchievementType.LevelPlay:
            case EAchievementType.LevelWin:
            case EAchievementType.LevelCompleted:
            case EAchievementType.EndlessPlay:
            case EAchievementType.ReachEnlessStage:
            case EAchievementType.WinEndlessStage:
            case EAchievementType.SpendEnergy:
                UIManager.Instance.CloseAllPopup();
                break;
            case EAchievementType.SpendCoin:
                UIManager.Instance.CloseAllPopup();
                UIManager.Instance.uIHome.homeToggleButtons[0].OnClick();
                break;
            case EAchievementType.SpinLuckyWheel:
                UIManager.Instance.ShowPopupLuckyWheel();
                break;
            default:
                break;
        }
    }

    public void UpdateQuestProgress(EAchievementType questType, long val = 1, bool replace = false)
    {
        cachedData.UpdateQuestProgress(questType, val, replace);
        QuestItem quest = lstQuest.Find(q => q.questConfig.questType == questType);
        if (quest != null)
        {
            quest.progress = cachedData.GetProgressQuest(questType);
            quest.rewardState = cachedData.GetQuestState(quest.questConfig);
            CheckNotiQuest();
        }
        OnValueChange();
    }
}
[System.Serializable]
public class QuestItem
{
    public DailyQuestConfig questConfig;
    public long progress;
    public ERewardState rewardState;
}
public class QuestMileStone
{
    public bool isDaily;
    public int currentPoint;
    public int pointRequire;
    public int lastPointRequire;
    public PackageResource rewards;
    public ERewardState rewardState;
    public float GetProgressFill()
    {
        return Mathf.Clamp01((float)(currentPoint - lastPointRequire) / (float)(pointRequire - lastPointRequire));
    }
}