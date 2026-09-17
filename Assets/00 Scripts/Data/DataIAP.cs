using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
[CreateAssetMenu(fileName = "Data IAP", menuName = "Data/Data IAP")]
public class DataIAP : SerializedScriptableObject
{
    public Dictionary<EIAPPackType, IAPConfig> dicConfigs;
    public List<EIAPPackType> packPromote;
    public List<EIAPPackType> packGem;
    public Dictionary<string, EIAPPackType> dicId;
    public Dictionary<string, string> dicInvoice;
    public string configString;
    public string keyConfig = "iap_config";
#if UNITY_EDITOR
    [Button()]
    void LoadData()
    {
        string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTGb3DXswc14ixMr5ebKLgR-5z8vftpIepg9w-EB2ZBsLMc8W9HA7QeQ_afX43T-peYbrlmKe2yv74a/pub?gid=143197333&single=true&output=csv";
        System.Action<string> actionComplete = new System.Action<string>((string str) =>
        {
            dicId = new Dictionary<string, EIAPPackType>();
            dicConfigs = new Dictionary<EIAPPackType, IAPConfig>();
            dicInvoice = new Dictionary<string, string>();
            var data = CSVReader.ReadCSV(str);
            for (int i = 1; i < data.Count; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    if (Helper.TryToEnum(_data[0], out EIAPPackType packType))
                    {
                        IAPConfig config = new IAPConfig();
                        config.packType = packType;
                        config.packID = _data[1];
                        config.packName = _data[2];
                        config.maxBuy = Helper.ParseInt(_data[3]);
                        config.price = Helper.ParseInt(_data[4]);
                        config.invoiceUrl = _data[8];
                        config.lstReward = new List<string>();
                        for (int j = 9; j < _data.Length; j++)
                        {
                            if (!string.IsNullOrEmpty(_data[j]))
                                if (GameResource.GetResource(_data[j]) != null)
                                    config.lstReward.Add(_data[j]);
                        }
                        dicConfigs.Add(packType, config);
                        dicId.Add(config.packID, packType);
                        dicInvoice.Add(config.packID, config.invoiceUrl);
                    }
                }
            }
            ParseConfig(Helper.CompressToBase64GzipData(Newtonsoft.Json.JsonConvert.SerializeObject(dicInvoice)));

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
            dicInvoice = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(Helper.DecompressFromBase64GzipData(configString));
        }
        catch
        {
            Debug.LogError("Cannot Parse Data");
        }
    }
}
public class IAPConfig
{
    public EIAPPackType packType;
    public string packID;
    public string packName;
    public int maxBuy;
    public int price;
    public string invoiceUrl;
    public List<string> lstReward;
    public bool CanShow()
    {
        return maxBuy == 0 || maxBuy > IIAPController.Instance.GetPackBought(packType);
    }
    public PackageResource GetReward()
    {
        PackageResource pack = new PackageResource();
        foreach (var item in lstReward)
        {
            pack.AddResource(GameResource.GetResource(item));
        }
        return pack;
    }
    public InvoiceLinkMessage GetInvoiceMessage(bool fakePrice = false)
    {
        return new InvoiceLinkMessage
        {
            packageId = packID,
            title = packName,
            description = $"Purchase Nut&Bolt's {packName}",
            price = fakePrice ? 1 : price,
        };
    }
}
