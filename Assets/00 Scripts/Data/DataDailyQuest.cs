using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data Daily Quest", menuName = "Data/Data Daily Quest")]
public class DataDailyQuest : SerializedScriptableObject
{
    public List<DailyQuestConfig> lstQuestConfig;
    public Dictionary<int, string> dicDailyMilestone;
    public Dictionary<int, string> dicWeeklyMilestone;
#if UNITY_EDITOR
    [Button()]
    void LoadData()
    {
        string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTGb3DXswc14ixMr5ebKLgR-5z8vftpIepg9w-EB2ZBsLMc8W9HA7QeQ_afX43T-peYbrlmKe2yv74a/pub?gid=1056972199&single=true&output=csv";
        System.Action<string> actionComplete = new System.Action<string>((string str) =>
        {
            lstQuestConfig = new List<DailyQuestConfig>();
            dicDailyMilestone = new Dictionary<int, string>();
            dicWeeklyMilestone = new Dictionary<int, string>();

            var data = CSVReader.ReadCSV(str);
            for (int i = 1; i <= 5; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    if (Helper.TryToEnum(_data[0], out EAchievementType questType))
                    {
                        DailyQuestConfig config = new DailyQuestConfig();
                        config.questType = questType;
                        config.questCondition = Helper.ParseInt(_data[1]);
                        config.pointReward = Helper.ParseInt(_data[2]);
                        lstQuestConfig.Add(config);
                    }
                }
            }
            for (int i = 9; i <= 13; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    dicDailyMilestone.Add(Helper.ParseInt(_data[0]), _data[1]);
                }
            }

            for (int i = 16; i <= 20; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    dicWeeklyMilestone.Add(Helper.ParseInt(_data[0]), _data[1]);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        });
        EditorCoroutine.start(Helper.IELoadData(url, actionComplete, name));
    }
#endif
}
public class DailyQuestConfig
{
    public EAchievementType questType;
    public int questCondition;
    public int pointReward;
    public string GetQuestDesc()
    {
        return string.Format(Helper.GetI2Translation($"{questType}_quest_desc"), questCondition);
    }
}