using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Data Shop", menuName = "Data/Data Shop")]
public class DataShop : SerializedScriptableObject
{
    public Dictionary<EIAPPackType, IAPShopItem> dicShopIAP;
    public Dictionary<EShopEnergy, CommonShopItem> dicShopEnergy;
    public Dictionary<EShopCoin, CommonShopItem> dicShopCoin;

#if UNITY_EDITOR
    [Button()]
    void LoadData()
    {
        string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTGb3DXswc14ixMr5ebKLgR-5z8vftpIepg9w-EB2ZBsLMc8W9HA7QeQ_afX43T-peYbrlmKe2yv74a/pub?gid=143197333&single=true&output=csv";
        System.Action<string> actionComplete = new System.Action<string>((string str) =>
        {
            dicShopIAP = new Dictionary<EIAPPackType, IAPShopItem>();
            var data = CSVReader.ReadCSV(str);
            for (int i = 1; i < data.Count; i++)
            {
                var _data = data[i];
                if (!string.IsNullOrEmpty(_data[0]))
                {
                    if (Helper.TryToEnum(_data[0], out EIAPPackType packType))
                    {
                        IAPShopItem item = new IAPShopItem();
                        item.packType = packType;
                        item.itemName = _data[0];
                        item.packId = _data[1];
                        item.starCost = Helper.ParseInt(_data[3]);
                        for (int j = 4; j < _data.Length; j++)
                        {
                            item.rewardStrings.Add(_data[j]);
                        }
                        dicShopIAP.Add(packType, item);
                    }
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        });
        EditorCoroutine.start(Helper.IELoadData(url, actionComplete, name));
    }
#endif
}
public abstract class ShopItem
{
    public string itemName;
    public List<string> rewardStrings = new List<string>();
    PackageResource rewards;
    public abstract void BuyItem(System.Action actionComplete);
    public virtual PackageResource GetRewards()
    {
        if (rewards == null)
        {
            rewards = new PackageResource();
            foreach (var item in rewardStrings)
            {
                rewards.AddResource(GameResource.GetResource(item));
            }
        }
        return rewards;
    }
    public virtual void SetPackageResource(PackageResource package)
    {
        rewards = package;
    }
}
public class IAPShopItem : ShopItem
{
    public EIAPPackType packType;
    public string packId;
    public int starCost;

    public override void BuyItem(System.Action actionComplete)
    {
    }
}
public class CommonShopItem : ShopItem
{
    public ECommonResource costType;
    public int costValue;
    public virtual GameResource GetCost()
    {
        return new CommonResource(costType, -costValue);
    }
    public override void BuyItem(System.Action actionComplete = null)
    {
        PackageResource packCost = new PackageResource();
        packCost.AddResource(GetCost());
        packCost.ReceiveResource(EResourceFrom.DailyShop, true, () =>
        {
            GetRewards().ReceiveAndShow(EResourceFrom.DailyShop, true, actionComplete);
        });
    }
}
