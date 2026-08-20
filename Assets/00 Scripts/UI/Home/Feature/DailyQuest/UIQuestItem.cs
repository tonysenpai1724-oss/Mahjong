using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIQuestItem : MonoBehaviour
{
    public Image bg;
    public Sprite bgSpriteIncomplete;
    public Sprite bgSpriteComplete;
    public Image fillBar;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtProgress;
    public TextMeshProUGUI txtReward;
    public Button button;

    public QuestItem questItem;

    public void InitQuest(QuestItem questItem)
    {
        this.questItem = questItem;
        if (txtTitle != null)
        {
            txtTitle.text = questItem.questConfig.GetQuestDesc();
        }

        if (txtReward != null)
        {
            txtReward.text = $"{questItem.questConfig.pointReward}";
        }

        int condition = Mathf.Max(1, questItem.questConfig.questCondition);
        int progress = Mathf.Clamp((int)questItem.progress, 0, condition);
        float fillRatio = condition > 0 ? (float)progress / condition : 0f;

        if (fillBar != null)
        {
            fillBar.type = Image.Type.Filled;
            fillBar.fillMethod = Image.FillMethod.Horizontal;
            fillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillBar.fillAmount = Mathf.Clamp01(fillRatio);
        }

        if (txtProgress != null)
        {
            txtProgress.text = $"{progress}/{condition}";
        }

        bool isComplete = questItem.rewardState == ERewardState.CanClaim || questItem.rewardState == ERewardState.Claimed;

        if (bg != null)
        {
            bg.gameObject.SetActive(true);
            bg.sprite = isComplete ? bgSpriteComplete : bgSpriteIncomplete;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (questItem.rewardState == ERewardState.CanClaim)
            {
                button.onClick.AddListener(() => IDailyQuestController.Instance.ClaimQuestReward(questItem.questConfig.questType));
            }
            else
            {
                button.onClick.AddListener(() => IDailyQuestController.Instance.OnGoQuest(questItem.questConfig.questType));
            }
        }
    }
}
