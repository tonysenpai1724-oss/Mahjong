using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeChallangeButton : HomeFeatureButton
{
    public override void OnClick()
    {
        GameManager.Instance.PlayGame(EGameType.Endless);
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
    }
}
