using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public interface IFriendInviteController : IController<FriendInviteControllerLocal>
{
    public int GetRewardedId();
    public int GetCurrentInvited();
    public ERewardState GetRewardState(int rewardId);
    public void ClaimReward();
    public bool NotiUI();
}
public class FriendInviteCachedData : IControllerCachedData
{
    public int rewardedId;
    public int lastInvited;

    public void InitFirsTime()
    {
    }

    public void OnNewData()
    {
    }
}
public class FriendInviteControllerLocal :
#if LOCAL_BUILD
    BaseLocalController<FriendInviteCachedData>
#else
    CommonServerController<FriendInviteCachedData>
#endif
    , IFriendInviteController
{
    int currentInvited;
    int totalInvited;
    bool notiUI;
    public override string KeyData()
    {
        return "friend_invite";
    }

    public override string KeyEvent()
    {
        return Constant.EVENT_ON_FRIEND_UPDATE;
    }
    protected override void OnInitSuccess()
    {
        base.OnInitSuccess();
        totalInvited = AccountManager.Instance.TelegramUserInfo.ref_count;
        currentInvited = totalInvited - cachedData.lastInvited;
    }
    protected override void FirstTimeInit()
    {
        base.FirstTimeInit();
        UniRx.Observable.Interval(System.TimeSpan.FromMinutes(1)).Subscribe(_ => FetchRefCount()).AddTo(GameManager.Instance.gameObject);
    }
    void FetchRefCount()
    {
#if LOCAL_BUILD
        return;
#endif
        GameManager.Instance.StartCoroutine(HTTPManager.Instance.GetUserInfoTele(s =>
        {
            totalInvited = s.ref_count;
            if (currentInvited != totalInvited - cachedData.lastInvited)
            {
                currentInvited = totalInvited - cachedData.lastInvited;
                notiUI = true;
                OnValueChange();
            }
        }));
    }
    protected override void OnNextWeek()
    {
        base.OnNextWeek();
        cachedData.rewardedId = 0;
        cachedData.lastInvited = totalInvited;
        OnValueChange();
    }
    public void ClaimReward()
    {
        PackageResource pack = new PackageResource();
        foreach (var item in DataSystem.Instance.dataInviteFriend.dicRewards)
        {
            if (currentInvited >= item.Key && item.Key > cachedData.rewardedId)
            {
                cachedData.rewardedId = item.Key;
                pack.AddResource(GameResource.GetResource(item.Value));
            }
        }
        pack.ReceiveAndShow(EResourceFrom.InviteFriend);
        notiUI = false;
        OnValueChange();
    }

    public int GetCurrentInvited()
    {
        return currentInvited;
    }

    public int GetRewardedId()
    {
        return cachedData.rewardedId;
    }

    public bool NotiUI()
    {
        return notiUI;
    }

    public ERewardState GetRewardState(int rewardId)
    {
        if (GetRewardedId() > rewardId)
            return ERewardState.Claimed;
        if (currentInvited >= rewardId)
            return ERewardState.CanClaim;
        return ERewardState.Progress;
    }
}
