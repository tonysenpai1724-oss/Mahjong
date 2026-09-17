using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
[CreateAssetMenu(fileName = "Data Sample", menuName = "Data/Data Sample")]
public class DataSample : SerializedScriptableObject
{
    public List<float> hpMultiple, dmgMultiple, droprateMultiple;
#if UNITY_EDITOR
    [Button()]
    void LoadData() 
    {
        string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vR6WWE8YGVsW5gTYksN3fPeTWWjs6lGpQ06Pa1-4K5Y-lCowew945gARkLVOgTLpV1ufc6bvzx7YjaF/pub?gid=1730694833&single=true&output=csv";
        System.Action<string> actionComplete = new System.Action<string>((string str) =>
        {
            hpMultiple = new List<float>();
            dmgMultiple = new List<float>();
            droprateMultiple = new List<float>();

            var data = CSVReader.ReadCSV(str);
            for (int i = 2; i < data.Count; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    hpMultiple.Add(Helper.ParseFloat(_data[1]));
                    dmgMultiple.Add(Helper.ParseFloat(_data[2]));
                    droprateMultiple.Add(Helper.ParseFloat(_data[3]));
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        });
        EditorCoroutine.start(Helper.IELoadData(url, actionComplete, name));
    }
#endif

    #region Test
    [FoldoutGroup("Test")]
    public Dictionary<string, float> dicWeight;
    [FoldoutGroup("Test")]
    public Dictionary<string, int> dicValue;

    [FoldoutGroup("Test")]
    [Button()]
    void TestWeight(float weight)
    {
        List<string> lstSorted = dicWeight.Keys.ToList();
        lstSorted = lstSorted.OrderByDescending(x => dicWeight[x]).ToList();
        DebugCustom.LogColorJson(lstSorted);

        dicValue = new Dictionary<string, int>();
        while (weight > 0)
        {
            int i = 0;
            float subWeight = 0;
            while (i < lstSorted.Count && weight >= subWeight)
            {
                i++;
                subWeight += dicWeight[lstSorted[0]];
            }
            weight -= subWeight;
            Dictionary<string, float> dicRate = new Dictionary<string, float>();
            for (int j = 0; j < i; j++)
            {
                dicRate.Add(lstSorted[j], dicWeight[lstSorted[j]]);
            }
            string result = Helper.GetRandomByPercent(dicRate);
            if (!dicValue.ContainsKey(result))
                dicValue.Add(result, 0);
                dicValue[result]++;
        }
        DebugCustom.LogColorJson(dicValue);
    }
    #endregion
}
