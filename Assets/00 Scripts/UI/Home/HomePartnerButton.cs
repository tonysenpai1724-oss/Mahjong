using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomePartnerButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupPartnerReferal();
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
    }
}
