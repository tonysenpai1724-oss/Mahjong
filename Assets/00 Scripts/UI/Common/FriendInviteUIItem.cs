using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
public class FriendInviteUIItem : MonoBehaviour
{
    public GameObject fillBG, fillObj, btnClaim, checkObj, lockObj;
    public UiResourceItem uiResourceItem;
    public TextMeshProUGUI txtId;
    int rewardId;
    public void InitItem(int rewardId)
    {
        gameObject.SetActive(true);
        this.rewardId = rewardId;
        ERewardState rewardState = IFriendInviteController.Instance.GetRewardState(rewardId);
        fillBG.SetActive(rewardId > 1);
        fillObj.SetActive(rewardState != ERewardState.Progress);
        btnClaim.SetActive(rewardState == ERewardState.CanClaim);
        checkObj.SetActive(rewardState == ERewardState.Claimed);
        lockObj.SetActive(rewardState == ERewardState.Progress);
        txtId.text = rewardId.ToString();
        uiResourceItem.InitResouce(GameResource.GetResource(DataSystem.Instance.dataInviteFriend.dicRewards[rewardId]));
    }
    public void OnCickClaim()
    {
        IFriendInviteController.Instance.ClaimReward();
    }
}
