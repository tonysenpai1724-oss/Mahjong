using System;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyConstant
{
    public int maxEnergy = 5;
    public int timeRecoverEnergy = 300;
}
public interface IPlayerResource : IController<PlayerResourceLocal>
{
    public static IPlayerResource Instance = new PlayerResourceLocal();
    public bool CheckResource(GameResource resource);
    public bool CheckListResource(List<GameResource> lstResources);
    public void AddResource(List<GameResource> lstResource, EResourceFrom resourceFrom,
            bool updateSever = false, System.Action actionSuccess = null, System.Action actionError = null);
    public long GetCommonResource(ECommonResource resourceType);
    public long GetExpireableResource(EExpireableResource resourceType);
    public int GetItemCount(string itemKey);
    public void SetItemCount(string itemKey, int count);
    public void AddItemCount(string itemKey, int amount);
    public bool TrySpendItem(string itemKey, int amount = 1);
    public bool GetContentResource(EContentActiveResource resourceType);
    public List<ItemResource> GetVisualResources();

    public long GetMaxEnergy();
}
public class PlayerResourceLocal :
#if LOCAL_BUILD
    BaseLocalController<PlayerResourceLocalData>
#else
    CommonServerController<PlayerResourceLocalData>
#endif
    , IPlayerResource
{
    public EnergyConstant energyConstant = new EnergyConstant();
    long lastEnergy;
    public override string KeyData()
    {
        return "player_resource";
    }

    public override string KeyEvent()
    {
        return Constant.ON_PLAYER_RESOURCE_UPDATE;
    }

    public bool CheckResource(GameResource resource)
    {
        return cachedData.CheckResource(resource);
    }

    public bool CheckListResource(List<GameResource> lstResources)
    {
        return cachedData.CheckListResource(lstResources);
    }
    public void AddResource(List<GameResource> lstResource, EResourceFrom resourceFrom, bool updateSever = false, Action actionSuccess = null, Action actionError = null)
    {
        if (!CheckListResource(lstResource))
        {
            UIManager.Instance.ShowDialog("Not Enough Resources");
            actionError?.Invoke();
            return;
        }
        cachedData.AddResource(lstResource);
        long currentEnergy = GetCommonResource(ECommonResource.Energy);
        if (lastEnergy >= GetMaxEnergy() && currentEnergy < GetMaxEnergy())
        {
            ITimerController.Instance.SetNextEnergyRegen(DateTime.UtcNow.AddSeconds(energyConstant.timeRecoverEnergy));
        }
        lastEnergy = currentEnergy;
        OnValueChange();
        actionSuccess?.Invoke();
    }

    public long GetCommonResource(ECommonResource resourceType)
    {
        return cachedData.GetCommonResource(resourceType);
    }

    public long GetExpireableResource(EExpireableResource resourceType)
    {
        return cachedData.GetExpireableResource(resourceType);
    }

    public int GetItemCount(string itemKey)
    {
        return cachedData.GetItemCount(itemKey);
    }

    public void SetItemCount(string itemKey, int count)
    {
        cachedData.SetItemCount(itemKey, count);
        OnValueChange();
    }

    public void AddItemCount(string itemKey, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        SetItemCount(itemKey, GetItemCount(itemKey) + amount);
    }

    public bool TrySpendItem(string itemKey, int amount = 1)
    {
        if (amount <= 0)
        {
            return true;
        }

        int currentCount = GetItemCount(itemKey);
        if (currentCount < amount)
        {
            return false;
        }

        SetItemCount(itemKey, currentCount - amount);
        return true;
    }


    public List<ItemResource> GetVisualResources()
    {
        return cachedData.GetListVisual();
    }

    public bool GetContentResource(EContentActiveResource resourceType)
    {
        return cachedData.GetContentActive(resourceType);
    }
    protected override void OnTick()
    {
        base.OnTick();
        while (CanRegenEnergy())
        {
            AddResource(new List<GameResource> { new CommonResource(ECommonResource.Energy, 1) }, EResourceFrom.TimeReward);
            ITimerController.Instance.SetNextEnergyRegen(ITimerController.Instance.GetNextFreeEnergy().AddSeconds(energyConstant.timeRecoverEnergy));
        }
    }
    bool CanRegenEnergy()
    {
        return GetCommonResource(ECommonResource.Energy) < energyConstant.maxEnergy
            && DateTime.UtcNow > ITimerController.Instance.GetNextFreeEnergy();
    }
    public long GetMaxEnergy()
    {
        return energyConstant.maxEnergy;
    }
}
public class PlayerResourceLocalData : IControllerCachedData
{
    public Dictionary<string, long> dicItemResource = new Dictionary<string, long>();

    Dictionary<ECommonResource, long> _commonResources;
    Dictionary<EExpireableResource, long> _expireableResources;
    Dictionary<EContentActiveResource, bool> _contentActiveResources;

    List<ItemResource> lstVisuals = new List<ItemResource>();

    public void InitFirsTime()
    {
        _commonResources = new Dictionary<ECommonResource, long>();
        List<ECommonResource> lstCommon = Helper.GetListEnum<ECommonResource>();
        foreach (var item in lstCommon)
        {
            string key = "cm" + item.ToString();
            if (!dicItemResource.ContainsKey(key))
                dicItemResource.Add(key, 0);
            _commonResources.Add(item, dicItemResource[key]);
            lstVisuals.Add(new CommonResource(item, dicItemResource[key]));
        }

        _expireableResources = new Dictionary<EExpireableResource, long>();
        List<EExpireableResource> lstExp = Helper.GetListEnum<EExpireableResource>();
        foreach (var item in lstExp)
        {
            string key = "time" + item.ToString();
            if (!dicItemResource.ContainsKey(key))
                dicItemResource.Add(key, 0);
            _expireableResources.Add(item, dicItemResource[key]);
            lstVisuals.Add(new ExpireableResource(item, dicItemResource[key]));
        }

        _contentActiveResources = new Dictionary<EContentActiveResource, bool>();
        List<EContentActiveResource> lstContent = Helper.GetListEnum<EContentActiveResource>();
        foreach (var item in lstContent)
        {
            string key = "content" + item.ToString();
            if (!dicItemResource.ContainsKey(key))
                dicItemResource.Add(key, 0);
            _contentActiveResources.Add(item, dicItemResource[key] != 0);
        }
    }
    public void OnNewData()
    {
    }
    public bool CheckResource(GameResource resource)
    {
        if (resource is CommonResource)
        {
            CommonResource cr = (CommonResource)resource;
            if (_commonResources[cr.resourceType] + cr.resourceValue < 0)
            {
                return false;
            }
        }
        else if (resource is ExpireableResource)
        {
            ExpireableResource er = (ExpireableResource)resource;
            if (_expireableResources[er.resourceType] + er.resourceValue < 0)
            {
                return false;
            }
        }
        return true;
    }
    public bool CheckListResource(List<GameResource> lstResource)
    {
        foreach (var resource in lstResource)
        {
            if (!CheckResource(resource))
                return false;
        }
        return true;
    }
    public void AddResource(GameResource resource)
    {
        if (resource is ItemResource)
        {
            if (resource is CommonResource)
            {
                CommonResource cr = (CommonResource)resource;
                _commonResources[cr.resourceType] += cr.resourceValue;
                dicItemResource["cm" + cr.resourceType] = _commonResources[cr.resourceType];
                DebugCustom.LogColor("AddResource", cr.resourceType.ToString(), _commonResources[cr.resourceType]);
                if(cr.resourceValue < 0)
                {
                    if(cr.resourceType == ECommonResource.Coin)
                        IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.SpendCoin, -cr.resourceValue);
                    else if(cr.resourceType == ECommonResource.Energy)
                        IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.SpendEnergy, -cr.resourceValue);
                }
            }
            else if (resource is ExpireableResource)
            {
                ExpireableResource er = (ExpireableResource)resource;
                _expireableResources[er.resourceType] += er.resourceValue;
                dicItemResource["time" + er.resourceType] = _expireableResources[er.resourceType];
            }
            ItemResource itemResource = (ItemResource)resource;
            foreach (var item in lstVisuals)
            {
                if (item.CompareType(resource))
                {
                    item.resourceValue += itemResource.resourceValue;
                    break;
                }
            }
        }
    }
    public void AddResource(List<GameResource> lstResource)
    {
        foreach (GameResource resource in lstResource)
        {
            AddResource(resource);
        }
    }
    public long GetCommonResource(ECommonResource resourceType)
    {
        return _commonResources[resourceType];
    }

    public long GetExpireableResource(EExpireableResource resourceType)
    {
        return _expireableResources[resourceType];
    }

    public int GetItemCount(string itemKey)
    {
        EnsureItemResourceDictionary();
        string key = NormalizeItemKey(itemKey);
        if (!dicItemResource.TryGetValue(key, out long count))
        {
            dicItemResource[key] = 0L;
            return 0;
        }

        if (count <= 0L)
        {
            return 0;
        }

        return count > int.MaxValue ? int.MaxValue : (int)count;
    }

    public void SetItemCount(string itemKey, int count)
    {
        EnsureItemResourceDictionary();
        dicItemResource[NormalizeItemKey(itemKey)] = Mathf.Max(0, count);
    }

    public bool GetContentActive(EContentActiveResource resourceType)
    {
        return _contentActiveResources[resourceType];
    }

    public List<ItemResource> GetListVisual()
    {
        return lstVisuals;
    }

    private void EnsureItemResourceDictionary()
    {
        if (dicItemResource == null)
        {
            dicItemResource = new Dictionary<string, long>();
        }
    }

    private static string NormalizeItemKey(string itemKey)
    {
        return string.IsNullOrWhiteSpace(itemKey) ? "unknown" : itemKey.Trim();
    }

}
