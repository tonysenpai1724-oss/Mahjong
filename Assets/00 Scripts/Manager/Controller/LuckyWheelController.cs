using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILuckyWheelController : IController<LuckyWheelControllerLocal>
{
    public void SpinWheel(Action actionComplete);
    public bool CanSpinFree();
    public int GetSpinCost();
    public string GetNextFreeCountdown();
    public bool CanWaitNextFree();
}
public class LuckyWheelConstant
{
    public int freeDaily = 4;
    public int freeCooldown = 60; //minutes
    public int startGemCost = 30;
    public int gemCostStep = 5;
    public int GemCostIncrease = 5;
}
public class LuckyWheelCacheData : IControllerCachedData
{
    public long nextFreeSpin;
    public int todayFreeSpin;
    public int todayGemSpin;

    public void InitFirsTime()
    {
    }
    public void OnNewData()
    {
    }
}
public class LuckyWheelControllerLocal :
#if LOCAL_BUILD
    BaseLocalController<LuckyWheelCacheData>
#else
    CommonServerController<LuckyWheelCacheData>
#endif
    , ILuckyWheelController
{
    DateTime nextFree;
    LuckyWheelConstant constant = new LuckyWheelConstant();
    public override string KeyData()
    {
        return "lucky_wheel";
    }

    public override string KeyEvent()
    {
        return Constant.ON_LUCKY_SPIN;
    }

    protected override void OnInitSuccess()
    {
        base.OnInitSuccess();
        nextFree = Helper.ParseDateTime(cachedData.nextFreeSpin);
    }

    protected override void OnNextDay()
    {
        base.OnNextDay();
        cachedData.todayFreeSpin = 0;
        cachedData.todayGemSpin = 0;
        OnValueChange();
    }

    public bool CanSpinFree()
    {
        return DateTime.UtcNow > nextFree && cachedData.todayFreeSpin < constant.freeDaily;
    }

    public string GetNextFreeCountdown()
    {
        return "Next Free In: " + Helper.TimeToString(nextFree - DateTime.UtcNow);
    }

    public int GetSpinCost()
    {
        return constant.startGemCost + (cachedData.todayGemSpin / constant.gemCostStep) * constant.GemCostIncrease;
    }

    public void SpinWheel(Action actionComplete)
    {
        actionComplete += () =>
        {
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.SpinLuckyWheel);
        };
        if (CanSpinFree())
        {
            nextFree = DateTime.UtcNow.AddMinutes(constant.freeCooldown);
            cachedData.nextFreeSpin = nextFree.ToUnixTimestamp();
            cachedData.todayFreeSpin++;
            OnValueChange();
            actionComplete?.Invoke();
        }
        else
        {
            PackageResource packCost = new PackageResource();
            packCost.AddResource(new CommonResource(ECommonResource.Gem, -GetSpinCost()));
            actionComplete += () =>
            {
                cachedData.todayGemSpin++;
                OnValueChange();
            };
            packCost.ReceiveResource(EResourceFrom.LuckySpin, false, actionComplete);
        }
    }

    public bool CanWaitNextFree()
    {
        return cachedData.todayFreeSpin < constant.freeDaily;
    }
}
