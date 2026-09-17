using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data Lucky Wheel", menuName = "Data/Data Lucky Wheel")]
public class DataLuckyWheel : SerializedScriptableObject
{
    public Dictionary<string, float> dicRewards;
#if UNITY_EDITOR
    [Button()]
    void LoadData()
    {
        string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTGb3DXswc14ixMr5ebKLgR-5z8vftpIepg9w-EB2ZBsLMc8W9HA7QeQ_afX43T-peYbrlmKe2yv74a/pub?gid=1113740617&single=true&output=csv";
        System.Action<string> actionComplete = new System.Action<string>((string str) =>
        {
            dicRewards = new Dictionary<string, float>();
            var data = CSVReader.ReadCSV(str);
            for (int i = 1; i < data.Count; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    if (GameResource.GetResource(_data[0]) != null)
                    {
                        dicRewards.Add(_data[0], Helper.ParseFloat(_data[1]));
                    }
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        });
        EditorCoroutine.start(Helper.IELoadData(url, actionComplete, name));
    }
#endif
}
