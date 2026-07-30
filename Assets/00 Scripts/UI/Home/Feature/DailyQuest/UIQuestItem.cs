using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIQuestItem : MonoBehaviour
{
    public GameObject btnClaim, btnGo, checkObj;
    public CommonFill questFill;
    public TextMeshProUGUI txtDesc, txtProgress, txtPoint;

    public QuestItem questItem;

    public void InitQuest(QuestItem questItem)
    {
        this.questItem = questItem;
        txtDesc.text = questItem.questConfig.GetQuestDesc();
        txtPoint.text = questItem.questConfig.pointReward.ToString();
        int condition = questItem.questConfig.questCondition;
        int progress = (int)questItem.progress;

        questFill.SetFill((float)progress / (float)condition);
        txtProgress.text = $"{progress}/{condition}";
        ERewardState rewardState = questItem.rewardState;
        btnClaim.SetActive(rewardState == ERewardState.CanClaim);
        btnGo.SetActive(rewardState == ERewardState.Progress);
        checkObj.SetActive(rewardState == ERewardState.Claimed);
    }

    public void OnClickClaim()
    {
        IDailyQuestController.Instance.ClaimQuestReward(questItem.questConfig.questType);
    }

    public void OnClickGo()
    {
        IDailyQuestController.Instance.OnGoQuest(questItem.questConfig.questType);
    }
}
