using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using System.Linq;
using System;
public class PopupQuest : UIBase
{
    [FoldoutGroup("Milestone Weekly")]
    public List<UIQuestMilestone> lstWeeklyMileStone;
    [FoldoutGroup("Milestone Weekly")]
    public TextMeshProUGUI txtWeeklyPoint, txtWeeklyCountdown;

    [FoldoutGroup("Milestone Daily")]
    public List<UIQuestMilestone> lstDailyMileStone;
    [FoldoutGroup("Milestone Daily")]
    public TextMeshProUGUI txtDailyPoint, txtDailyCountdown;

    [FoldoutGroup("Quest")]
    public UIQuestItem itemPrefab;
    [FoldoutGroup("Quest")]
    public Transform itemParent;
    List<UIQuestItem> lstItems;

    public override void Show()
    {
        base.Show();
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_DAILY_QUEST_CHANGE, InitQuest);
        InitQuest();
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        OnTick();
    }

    public override void OnDisable()
    {
        TigerForge.EventManager.StopListening(Constant.EVENT_ON_DAILY_QUEST_CHANGE, InitQuest);
        TigerForge.EventManager.StopListening(Constant.EVENT_TIMER_TICK, OnTick);
        base.OnDisable();
    }
    void OnTick()
    {
        txtDailyCountdown.text = "Reset In: " + Helper.TimeToString(ITimerController.Instance.GetNextDay() - DateTime.UtcNow);
        txtWeeklyCountdown.text = "Reset In: " + Helper.TimeToString(ITimerController.Instance.GetNextWeek() - DateTime.UtcNow);
    }
    void InitQuest()
    {
        #region MileStone
        List<QuestMileStone> dailyMileStone = IDailyQuestController.Instance.GetDailyMileStone();
        for (int i = 0; i < lstDailyMileStone.Count; i++)
        {
            lstDailyMileStone[i].InitMileStone(dailyMileStone[i]);
        }
        txtDailyPoint.text = IDailyQuestController.Instance.GetDailyPoint().ToString();

        List<QuestMileStone> weeklyMileStone = IDailyQuestController.Instance.GetWeeklyMileStone();
        for (int i = 0; i < lstWeeklyMileStone.Count; i++)
        {
            lstWeeklyMileStone[i].InitMileStone(weeklyMileStone[i]);
        }
        txtWeeklyPoint.text = IDailyQuestController.Instance.GetWeeklyPoint().ToString();

        #endregion

        #region Quest
        List<QuestItem> lstQuest = IDailyQuestController.Instance.GetQuestItems()
            .OrderBy(q => Mathf.Abs(((float)((int)q.rewardState) - 0.9f))).ToList();
        if (lstItems == null)
        {
            lstItems = new List<UIQuestItem>();
            foreach (var item in lstQuest)
            {
                lstItems.Add(Instantiate(itemPrefab, itemParent));
            }
        }
        for (int i = 0; i < lstQuest.Count; i++)
        {
            lstItems[i].InitQuest(lstQuest[i]);
        }
        #endregion
    }

}
