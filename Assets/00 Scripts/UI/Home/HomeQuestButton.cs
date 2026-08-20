using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeQuestButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupDailyQuest();
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
        notiObj.SetActive(IDailyQuestController.Instance.NotiQuest());
    }
}
