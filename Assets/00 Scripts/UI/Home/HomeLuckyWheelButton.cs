using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeLuckyWheelButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupLuckyWheel();
    }

    protected override void CheckActive()
    {

    }

    protected override void CheckNoti()
    {
        notiObj.SetActive(ILuckyWheelController.Instance.CanSpinFree());
    }
}
