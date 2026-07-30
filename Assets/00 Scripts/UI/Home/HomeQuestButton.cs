using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeQuestButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupQuest();
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
        notiObj.SetActive(IDailyQuestController.Instance.NotiQuest());
    }
}
