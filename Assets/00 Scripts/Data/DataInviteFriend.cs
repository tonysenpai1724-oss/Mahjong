using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Data Invite Friend", menuName = "Data/Data Invite Friend")]
public class DataInviteFriend : SerializedScriptableObject
{
    public Dictionary<int, string> dicRewards;
#if UNITY_EDITOR
    [Button()]
    void LoadData()
    {
        string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTGb3DXswc14ixMr5ebKLgR-5z8vftpIepg9w-EB2ZBsLMc8W9HA7QeQ_afX43T-peYbrlmKe2yv74a/pub?gid=2033014880&single=true&output=csv";
        System.Action<string> actionComplete = new System.Action<string>((string str) =>
        {
            dicRewards = new Dictionary<int, string>();
            var data = CSVReader.ReadCSV(str);
            for (int i = 1; i < data.Count; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    dicRewards.Add(int.Parse(_data[0]), _data[1]);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        });
        EditorCoroutine.start(Helper.IELoadData(url, actionComplete, name));
    }
#endif
}
