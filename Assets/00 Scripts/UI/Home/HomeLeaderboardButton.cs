using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeLeaderboardButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupLeaderboard();
    }

    protected override void CheckActive()
    {
        gameObject.SetActive(ILeaderboardController.Instance.ActiveLeaderboard());
    }

    protected override void CheckNoti()
    {
    }
}
