using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupDailyQuest : UIBase
{
    public UIQuestItem itemPrefab;
    public Transform itemParent;

    public Button bonusButton;
    public Image bonusFillBar;
    public TextMeshProUGUI bonusTitleText;
    public TextMeshProUGUI bonusProgressText;
    public Image lockBonusIcon;


    private readonly List<UIQuestItem> questItems = new List<UIQuestItem>();

    public override void Show()
    {
        base.Show();
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_DAILY_QUEST_CHANGE, InitQuest);
        InitQuest();
    }

    public override void OnDisable()
    {
        TigerForge.EventManager.StopListening(Constant.EVENT_ON_DAILY_QUEST_CHANGE, InitQuest);
        base.OnDisable();
    }

    private void InitQuest()
    {
        if (IDailyQuestController.Instance == null)
        {
            return;
        }

        List<QuestItem> quests = IDailyQuestController.Instance.GetQuestItems();
        if (quests == null)
        {
            return;
        }

        List<QuestItem> renderQuests = new List<QuestItem>();
        foreach (var quest in quests)
        {
            if (quest == null || quest.questConfig == null || quest.rewardState == ERewardState.Claimed)
            {
                continue;
            }
            renderQuests.Add(quest);
        }

        QuestItem bonusQuest = quests.Find(q => q != null && q.questConfig != null && q.questConfig.questType == EAchievementType.UseBooster);
        if (bonusQuest == null)
        {
            bonusQuest = new QuestItem
            {
                questConfig = new DailyQuestConfig
                {
                    questType = EAchievementType.UseBooster,
                    questCondition = 1,
                    pointReward = 50,
                    isBonusTask = false,
                },
                progress = 0,
                rewardState = ERewardState.Progress,
            };
        }

        bool hasDedicatedBonusUi = bonusButton != null || bonusFillBar != null || bonusTitleText != null || bonusProgressText != null || lockBonusIcon != null;

        if (hasDedicatedBonusUi)
        {
            UpdateBonusQuestView(bonusQuest, quests);
        }

        if (itemPrefab == null || itemParent == null)
        {
            return;
        }

        while (questItems.Count < renderQuests.Count)
        {
            UIQuestItem newItem = Instantiate(itemPrefab, itemParent);
            questItems.Add(newItem);
        }

        while (questItems.Count > renderQuests.Count)
        {
            UIQuestItem extraItem = questItems[questItems.Count - 1];
            questItems.RemoveAt(questItems.Count - 1);
            if (extraItem != null)
            {
                Destroy(extraItem.gameObject);
            }
        }

        for (int index = 0; index < renderQuests.Count; index++)
        {
            questItems[index].InitQuest(renderQuests[index]);
        }
    }

    private void UpdateBonusQuestView(QuestItem bonusQuest, List<QuestItem> quests)
    {
        if (bonusQuest == null || bonusQuest.questConfig == null)
        {
            if (bonusButton != null) bonusButton.gameObject.SetActive(false);
            if (bonusFillBar != null) bonusFillBar.gameObject.SetActive(false);
            if (bonusTitleText != null) bonusTitleText.gameObject.SetActive(false);
            if (bonusProgressText != null) bonusProgressText.gameObject.SetActive(false);
            if (lockBonusIcon != null) lockBonusIcon.gameObject.SetActive(false);
            return;
        }

        int totalTaskCount = quests != null ? quests.FindAll(q => q != null && q.questConfig != null).Count : 1;
        int completedTaskCount = quests != null ? quests.FindAll(q => q != null && q.questConfig != null && (q.rewardState == ERewardState.CanClaim || q.rewardState == ERewardState.Claimed)).Count : 0;
        int progress = Mathf.Clamp(completedTaskCount, 0, totalTaskCount);
        float fillRatio = totalTaskCount > 0 ? (float)progress / totalTaskCount : 0f;

        if (bonusTitleText != null)
        {
            bonusTitleText.text = bonusQuest.questConfig.GetQuestDesc();
            bonusTitleText.gameObject.SetActive(true);
        }

        if (lockBonusIcon != null)
        {
            lockBonusIcon.gameObject.SetActive(progress < totalTaskCount);
        }

        if (bonusFillBar != null)
        {
            bonusFillBar.type = Image.Type.Filled;
            bonusFillBar.fillMethod = Image.FillMethod.Horizontal;
            bonusFillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            bonusFillBar.fillAmount = Mathf.Clamp01(fillRatio);
            bonusFillBar.gameObject.SetActive(true);
            bonusFillBar.SetVerticesDirty();
        }

        if (bonusProgressText != null)
        {
            bonusProgressText.text = $"{progress}/{totalTaskCount}";
            bonusProgressText.gameObject.SetActive(true);
        }

        if (bonusButton != null)
        {
            bonusButton.gameObject.SetActive(true);
            bonusButton.onClick.RemoveAllListeners();

            if (progress >= totalTaskCount)
            {
                bonusButton.onClick.AddListener(() => IDailyQuestController.Instance.ClaimQuestReward(bonusQuest.questConfig.questType));
            }
            else
            {
                bonusButton.onClick.AddListener(() => UIManager.Instance.ShowPopupBooster());
            }
        }
    }
}
