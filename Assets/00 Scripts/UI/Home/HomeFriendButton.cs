using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeFriendButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupInviteFriend();
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
        notiObj.SetActive(IFriendInviteController.Instance.NotiUI());
    }
}
