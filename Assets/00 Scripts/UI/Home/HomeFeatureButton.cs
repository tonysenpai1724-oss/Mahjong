using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HomeFeatureButton : CommonButton
{
    protected override void Start()
    {
        base.Start();
        button.onClick.AddListener(OnClick);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        OnTick();
    }
    protected virtual void OnDisable()
    {
        TigerForge.EventManager.StopListening(Constant.EVENT_TIMER_TICK, OnTick);
    }
    protected virtual void OnTick()
    {
        CheckActive();
        CheckNoti();
    }
    protected abstract void CheckActive();
    protected abstract void CheckNoti();
    public abstract void OnClick();
}
