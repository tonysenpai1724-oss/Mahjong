using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
public interface ITimerController : IController<TimerSystem>
{
    public bool IsNewDay();
    public bool IsNewWeek();
    public bool IsNewMonth();
    public bool IsNextFreeTicket();
    public void SetNextEnergyRegen(DateTime nextTime);
    public DateTime GetNextDay();
    public DateTime GetNextWeek();
    public DateTime GetNextMonth();
    public DateTime GetNextFreeEnergy();
}
public class TimerData : IControllerCachedData
{
    public long nextDay;
    public long nextWeek;
    public long nextMonth;
    public long nextFreeEnergy;
    public void InitFirsTime()
    {
    }
    public void OnNewData()
    {
    }
}
public class TimerSystem :
#if LOCAL_BUILD
    BaseLocalController<TimerData>
#else
    CommonServerController<TimerData>
#endif
    , ITimerController
{
    protected DateTime nextDay;
    protected DateTime nextWeek;
    protected DateTime nextMonth;
    protected DateTime nextFreeEnergy;

    public override string KeyData()
    {
        return "timer_system";
    }
    public override string KeyEvent()
    {
        return Constant.EVENT_TIMER_CHANGED;
    }

    protected override void OnInitSuccess()
    {
        base.OnInitSuccess();
        nextDay = Helper.ParseDateTime(cachedData.nextDay);
        nextWeek = Helper.ParseDateTime(cachedData.nextWeek);
        nextMonth = Helper.ParseDateTime(cachedData.nextMonth);
        nextFreeEnergy = Helper.ParseDateTime(cachedData.nextFreeEnergy);
    }

    protected override void OnTick()
    {
        base.OnTick();
        if (IsNewDay())
            TigerForge.EventManager.EmitEvent(Constant.EVENT_TIMER_NEW_DAY);
        if (IsNewWeek())
            TigerForge.EventManager.EmitEvent(Constant.EVENT_TIMER_NEW_WEEK);
        if (IsNewMonth())
            TigerForge.EventManager.EmitEvent(Constant.EVENT_TIMER_NEW_MONTH);
    }
    protected override void OnNextDay()
    {
        base.OnNextDay();
        nextDay = Helper.NextDay(DateTime.UtcNow);
        cachedData.nextDay = nextDay.ToUnixTimestamp();
        OnValueChange();
    }
    protected override void OnNextWeek()
    {
        base.OnNextWeek();
        nextWeek = Helper.NextWeek(DateTime.UtcNow);
        cachedData.nextWeek = nextWeek.ToUnixTimestamp();
        OnValueChange();
    }
    protected override void OnNextMonth()
    {
        base.OnNextMonth();
        nextMonth = Helper.NextMonth(DateTime.UtcNow);
        cachedData.nextMonth = nextMonth.ToUnixTimestamp();
        OnValueChange();
    }
    public bool IsNewDay()
    {
        return DateTime.UtcNow > nextDay;
    }

    public bool IsNewWeek()
    {
        return DateTime.UtcNow > nextWeek;
    }

    public bool IsNewMonth()
    {
        return DateTime.UtcNow > nextMonth;
    }

    public bool IsNextFreeTicket()
    {
        return DateTime.UtcNow > nextFreeEnergy;
    }

    public DateTime GetNextDay()
    {
        return nextDay;
    }

    public DateTime GetNextWeek()
    {
        return nextWeek;
    }

    public DateTime GetNextMonth()
    {
        return nextMonth;
    }

    public DateTime GetNextFreeEnergy()
    {
        return nextFreeEnergy;
    }

    public void SetNextEnergyRegen(DateTime nextTime)
    {
        nextFreeEnergy = nextTime;
        cachedData.nextFreeEnergy = nextTime.ToUnixTimestamp();
        OnValueChange();
    }
}
