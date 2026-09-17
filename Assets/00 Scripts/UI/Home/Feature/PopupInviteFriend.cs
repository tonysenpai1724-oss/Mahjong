using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
public class PopupInviteFriend : UIBase
{
    public TextMeshProUGUI txtInvited, txtCountDown;
    public FriendInviteUIItem itemPrefab;
    public Transform itemParent;
    List<FriendInviteUIItem> lstItems;
    List<int> lstIds;
    public override void Show()
    {
        base.Show();
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_FRIEND_UPDATE, InitUI);
        InitUI();
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        OnTick();
    }
    public override void OnDisable()
    {
        base.OnDisable();
        TigerForge.EventManager.StopListening(Constant.EVENT_ON_FRIEND_UPDATE, InitUI);
        TigerForge.EventManager.StopListening(Constant.EVENT_TIMER_TICK, OnTick);
    }
    void OnTick()
    {
        txtCountDown.text = $"Reset In: {Helper.TimeToString(ITimerController.Instance.GetNextWeek() - DateTime.UtcNow)}";
    }
    void InitUI()
    {
        txtInvited.text = $"Friend Invited: {IFriendInviteController.Instance.GetCurrentInvited()}";
        if (lstItems == null)
        {
            lstItems = new List<FriendInviteUIItem>();
            lstIds = new List<int>();
            foreach (var item in DataSystem.Instance.dataInviteFriend.dicRewards)
            {
                lstItems.Add(Instantiate(itemPrefab, itemParent));
                lstIds.Add(item.Key);
            }
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            lstItems[i].InitItem(lstIds[lstItems.Count - i - 1]);
        }
    }
    public void OnClickCoppy()
    {
        ClipboardBridge.CopyText(RefLink());
        UIManager.Instance.ShowDialog("referral link copied");
    }
    public void OnClickInvite()
    {
#if UNITY_WEBGL
        TelegramManager.ShareURL(RefLink(), "🎮 Let's play together and collect lots of rewards!!! 👇\n👉 ");
#endif
    }
    string RefLink() => HTTPManager.GetRefLink(AccountManager.Instance.TelegramUserInfo.ref_code);
}
