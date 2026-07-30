using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Data Leaderboard", menuName = "Data/Data Leaderboard")]
public class DataLeaderboard : SerializedScriptableObject
{
    public Dictionary<int,LeaderboardRewardConfig> dicRewards;
#if UNITY_EDITOR
    [Button()]
    void LoadData()
    {
        string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTGb3DXswc14ixMr5ebKLgR-5z8vftpIepg9w-EB2ZBsLMc8W9HA7QeQ_afX43T-peYbrlmKe2yv74a/pub?gid=1159103814&single=true&output=csv";
        System.Action<string> actionComplete = new System.Action<string>((string str) =>
        {
            dicRewards = new Dictionary<int, LeaderboardRewardConfig>();
            var data = CSVReader.ReadCSV(str);
            for (int i = 1; i < data.Count; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    LeaderboardRewardConfig config = new LeaderboardRewardConfig();
                    config.rank = Helper.ParseInt(_data[0]);
                    config.rewards = new List<string>();
                    for (int j = 1; j < _data.Length; j++)
                    {
                        config.rewards.Add(_data[j]);
                    }
                    dicRewards.Add(config.rank, config);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        });
        EditorCoroutine.start(Helper.IELoadData(url, actionComplete, name));
    }
#endif
}
public class LeaderboardRewardConfig
{
    public int rank;
    public List<string> rewards;
    public PackageResource GetRewards()
    {
        PackageResource packageResource = new PackageResource();
        for (int i = 0; i < rewards.Count; i++)
        {
            packageResource.AddResource(GameResource.GetResource(rewards[i]));
        }
        return packageResource;
    }
}
