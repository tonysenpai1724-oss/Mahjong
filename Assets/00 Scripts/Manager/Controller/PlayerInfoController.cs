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
    public void UpdateStreak();
    public int GetCurrentStreak();
    public List<DayOfWeek> GetStreakDaysInWeek();
    public bool HasPlayedToday();
    public bool ShowStreakAnim();
    public void ShowStreakAnimCompleted();
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
        if (CurrentLevel() >= constant.maxLevel)
            return;

        // Only count one win per day for streak purposes.
        if (HasPlayedToday())
        {
            cachedData.level++;
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelCompleted, CurrentLevel() - 1, true);
            OnValueChange();
            return;
        }

        cachedData.level++;
        UpdateStreak();
        IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelCompleted, CurrentLevel() - 1, true);
        OnValueChange();
    }

    public int CurrentLevel()
    {
        return cachedData.level;
    }

    public int MaxLevel()
    {
        return constant.maxLevel;
    }

    public void UpdateStreak()
    {
        DateTime today = DateTime.Now.Date;

        // One win per day keeps streak alive.
        if (cachedData.lastPlayDate == DateTime.MinValue)
        {
            cachedData.streakCount = 1;
            cachedData.lastPlayDate = today;
            cachedData.showStreakAnim = true;
            return;
        }

        DateTime lastDate = cachedData.lastPlayDate.Date;
        int daysDiff = (today - lastDate).Days;

        if (daysDiff == 0)
        {
            // Already counted a win today, do not increase streak again.
            return;
        }
        else if (daysDiff == 1)
        {
            cachedData.streakCount++;
            cachedData.lastPlayDate = today;
            cachedData.showStreakAnim = true;
        }
        else
        {
            // Missed at least one day, streak resets and starts fresh.
            cachedData.streakCount = 1;
            cachedData.lastPlayDate = today;
            cachedData.showStreakAnim = true;
        }
    }

    public int GetCurrentStreak()
    {
        if (cachedData.lastPlayDate != DateTime.MinValue)
        {
            DateTime today = DateTime.Now.Date;
            int daysDiff = (today - cachedData.lastPlayDate.Date).Days;

            if (daysDiff > 1)
            {
                return 0;
            }
            else if (daysDiff == 1)
            {
                return cachedData.streakCount;
            }
        }

        return cachedData.streakCount;
    }

    public List<DayOfWeek> GetStreakDaysInWeek()
    {
        List<DayOfWeek> streakDays = new List<DayOfWeek>();

        if (cachedData.streakCount == 0 || cachedData.lastPlayDate == DateTime.MinValue)
            return streakDays;

        if (cachedData.streakCount >= 7)
        {
            for (int i = 0; i < 7; i++)
            {
                streakDays.Add((DayOfWeek)i);
            }
            return streakDays;
        }

        DateTime today = DateTime.Now.Date;
        DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        DateTime endDate = cachedData.lastPlayDate.Date;
        DateTime startDate = endDate.AddDays(-(cachedData.streakCount - 1));

        for (int i = 0; i < 7; i++)
        {
            DateTime checkDate = startOfWeek.AddDays(i);
            if (checkDate >= startDate && checkDate <= endDate)
            {
                streakDays.Add(checkDate.DayOfWeek);
            }
        }

        return streakDays;
    }

    public bool HasPlayedToday()
    {
        if (cachedData.lastPlayDate == DateTime.MinValue)
            return false;

        return cachedData.lastPlayDate.Date == DateTime.Now.Date;
    }

    public bool ShowStreakAnim()
    {
        return cachedData.showStreakAnim;
    }

    public void ShowStreakAnimCompleted()
    {
        if (!cachedData.showStreakAnim)
            return;

        cachedData.showStreakAnim = false;
        OnValueChange();
    }

    protected override void OnNextDay()
    {
        cachedData.showStreakAnim = true;
        OnValueChange();
    }
}

public class PlayerInfoCachedData : IControllerCachedData
{
    public int streakCount = 0;
    public DateTime lastPlayDate = DateTime.MinValue;
    public bool showStreakAnim = true;

    public int level = 1;

    public void InitFirsTime()
    {
    }

    public void OnNewData()
    {
    }
}
