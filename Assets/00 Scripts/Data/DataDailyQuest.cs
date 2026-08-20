using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data Daily Quest", menuName = "Data/Data Daily Quest")]
public class DataDailyQuest : SerializedScriptableObject
{
    public List<DailyQuestConfig> lstQuestConfig;
    [System.NonSerialized] public Dictionary<int, string> dicDailyMilestone = new Dictionary<int, string>();
    [System.NonSerialized] public Dictionary<int, string> dicWeeklyMilestone = new Dictionary<int, string>();

    private static readonly EAchievementType[] DefaultQuestOrder =
    {
        EAchievementType.Login,
        EAchievementType.LevelPlay,
        EAchievementType.Match3,
        EAchievementType.UseBooster,
    };

    private static DailyQuestConfig CreateDefaultQuestConfig(EAchievementType questType)
    {
        switch (questType)
        {
            case EAchievementType.Login:
                return new DailyQuestConfig { questType = EAchievementType.Login, questCondition = 1, pointReward = 30 };
            case EAchievementType.LevelPlay:
                return new DailyQuestConfig { questType = EAchievementType.LevelPlay, questCondition = 1, pointReward = 40 };
            case EAchievementType.Match3:
                return new DailyQuestConfig { questType = EAchievementType.Match3, questCondition = 3, pointReward = 60 };
            case EAchievementType.UseBooster:
                return new DailyQuestConfig { questType = EAchievementType.UseBooster, questCondition = 1, pointReward = 50, isBonusTask = false };
            default:
                return new DailyQuestConfig { questType = questType, questCondition = 1, pointReward = 0 };
        }
    }

    private static List<DailyQuestConfig> CreateDefaultQuestConfigs()
    {
        return DefaultQuestOrder.Select(CreateDefaultQuestConfig).ToList();
    }

    private void SanitizeQuestConfigs()
    {
        var allowed = new HashSet<EAchievementType>(DefaultQuestOrder);
        var validConfigs = (lstQuestConfig ?? new List<DailyQuestConfig>())
            .Where(q => q != null && allowed.Contains(q.questType))
            .GroupBy(q => q.questType)
            .Select(g => g.First())
            .ToList();

        var result = new List<DailyQuestConfig>();
        foreach (var questType in DefaultQuestOrder)
        {
            var existing = validConfigs.FirstOrDefault(q => q.questType == questType);
            var defaultConfig = CreateDefaultQuestConfig(questType);

            if (existing != null &&
                existing.questCondition == defaultConfig.questCondition &&
                existing.pointReward == defaultConfig.pointReward)
            {
                result.Add(existing);
            }
            else
            {
                result.Add(defaultConfig);
            }
        }

        lstQuestConfig = result;
    }

    public void EnsureDefaultQuestConfigs()
    {
        if (lstQuestConfig == null)
        {
            lstQuestConfig = new List<DailyQuestConfig>();
        }

        if (dicDailyMilestone == null)
        {
            dicDailyMilestone = new Dictionary<int, string>();
        }

        if (dicWeeklyMilestone == null)
        {
            dicWeeklyMilestone = new Dictionary<int, string>();
        }

        SanitizeQuestConfigs();

        if (lstQuestConfig.Count == 0)
        {
            lstQuestConfig = CreateDefaultQuestConfigs();
        }
    }
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
                        if (DefaultQuestOrder.Contains(questType))
                        {
                            DailyQuestConfig config = new DailyQuestConfig();
                            config.questType = questType;
                            config.questCondition = Helper.ParseInt(_data[1]);
                            config.pointReward = Helper.ParseInt(_data[2]);
                            lstQuestConfig.Add(config);
                        }
                    }
                }
            }

            SanitizeQuestConfigs();
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
[System.Serializable]
public class DailyQuestConfig
{
    public EAchievementType questType;
    public int questCondition;
    public int pointReward;
    public bool isBonusTask;
    public string GetQuestDesc()
    {
        switch (questType)
        {
            case EAchievementType.Login:
                return "Login";
            case EAchievementType.LevelPlay:
                return "Play 1 game";
            case EAchievementType.Match3:
                return "Match 3 tiles";
            case EAchievementType.UseBooster:
                return "Use 1 booster";
            default:
                return string.Format(Helper.GetI2Translation($"{questType}_quest_desc"), questCondition);
        }
    }
}