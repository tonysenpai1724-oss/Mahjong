using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data Partner", menuName = "Data/Data Partner")]

public class DataPartnerReferal : SerializedScriptableObject
{
    public List<PartnerReferalConfig> lstConfigs;
    public string configString;
    public string configKey = "partner_referral";
#if UNITY_EDITOR
    [Button()]
    void LoadData()
    {
        string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTGb3DXswc14ixMr5ebKLgR-5z8vftpIepg9w-EB2ZBsLMc8W9HA7QeQ_afX43T-peYbrlmKe2yv74a/pub?gid=1140871038&single=true&output=csv";
        System.Action<string> actionComplete = new System.Action<string>((string str) =>
        {
            lstConfigs = new List<PartnerReferalConfig>();
            var data = CSVReader.ReadCSV(str);
            for (int i = 1; i < data.Count; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    PartnerReferalConfig _config = new PartnerReferalConfig();
                    _config.url = _data[0];
                    _config.iconUrl = _data[1];
                    _config.title = _data[2];
                    _config.gemReward = Helper.ParseInt(_data[3]);
                    _config.activePointReward = Helper.ParseInt(_data[4]);
                    lstConfigs.Add(_config);
                }
            }
            ParseConfig(Helper.CompressToBase64GzipData(Newtonsoft.Json.JsonConvert.SerializeObject(lstConfigs)));

            UnityEditor.EditorUtility.SetDirty(this);
        });
        EditorCoroutine.start(Helper.IELoadData(url, actionComplete, name));
    }
#endif
    public void ParseConfig(string str)
    {
        configString = str;
        try
        {
            lstConfigs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PartnerReferalConfig>>(Helper.DecompressFromBase64GzipData(str));
        }
        catch
        {
            Debug.LogError("Cannot Parse Config");
        }
    }
}
public class PartnerReferalConfig
{
    public string url;
    public string iconUrl;
    public string title;
    public int gemReward;
    public int activePointReward;
    public PackageResource GetRewards()
    {
        PackageResource package = new PackageResource();
        package.AddResource(new CommonResource(ECommonResource.Gem, gemReward));
        package.AddResource(new CommonResource(ECommonResource.ActivePoint, activePointReward));
        return package;
    }
}
