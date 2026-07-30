using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIQuestMilestone : MonoBehaviour
{
    public GameObject glow;
    public Image chestIcon;
    public Sprite sprOn, sprOff;
    public CommonFill questFill;
    public TextMeshProUGUI txtPoint;
    QuestMileStone mileStone;
    public void InitMileStone(QuestMileStone mileStone)
    {
        this.mileStone = mileStone;
        txtPoint.text = mileStone.pointRequire.ToString();
        chestIcon.sprite = mileStone.rewardState == ERewardState.Claimed ? sprOn : sprOff;
        glow.SetActive(mileStone.rewardState == ERewardState.CanClaim);
        questFill.SetFill(mileStone.GetProgressFill());
    }
    public void OnClick()
    {
        if (mileStone.rewardState == ERewardState.CanClaim)
        {
            if (mileStone.isDaily)
                IDailyQuestController.Instance.ClaimDailyMilestone();
            else
                IDailyQuestController.Instance.ClaimWeeklyMilestone();
        }
        else
        {
            PackageResource pack = new PackageResource();
            if (mileStone.isDaily)
                pack.AddResource(GameResource.GetResource(DataSystem.Instance.dataDailyQuest.dicDailyMilestone[mileStone.pointRequire]));
            else
                pack.AddResource(GameResource.GetResource(DataSystem.Instance.dataDailyQuest.dicWeeklyMilestone[mileStone.pointRequire]));
            UIManager.Instance.ShopPopupTooltipPackage(chestIcon.transform.position, pack);
        }
    }
}
