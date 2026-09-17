using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeSettingButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupSetting();
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
    }
}
