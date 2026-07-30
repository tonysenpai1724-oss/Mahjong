using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeMailBoxButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupMailBox();
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
        notiObj.SetActive(IMailboxController.Instance.NotiMail());
    }
}
