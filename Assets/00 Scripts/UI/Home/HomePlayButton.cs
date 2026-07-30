using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomePlayButton : HomeFeatureButton
{
    public override void OnClick()
    {
        GameManager.Instance.PlayGame(EGameType.Campaign);
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
    }
}
