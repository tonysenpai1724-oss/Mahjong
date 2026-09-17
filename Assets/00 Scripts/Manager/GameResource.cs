using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public abstract class GameResource
{
    public static GameResource GetResource(string input)
    {
        string[] resourceData = input.Split("|");
        if (resourceData[0] == "CommonResource")
        {
            if (resourceData[1].TryToEnum(out ECommonResource resourceType))
            {
                long resouceValue = Helper.ParseInt(resourceData[2]);
                return new CommonResource(resourceType, resouceValue);
            }
        }

        if (resourceData[0] == "VirtualResource")
        {
            if (resourceData[1].TryToEnum(out EVirtualResource resourceType))
            {
                long resourceValue = Helper.ParseInt(resourceData[2]);
                return new VirtualResource(resourceType, resourceValue);
            }
        }

        if (resourceData[0] == "ExpireableResource")
        {
            if (resourceData[1].TryToEnum(out EExpireableResource resourceType))
            {
                long resourceValue = Helper.ParseInt(resourceData[2]);
                return new ExpireableResource(resourceType, resourceValue);
            }
        }

        if (resourceData[0] == "ContentActiveResource")
        {
            if (resourceData[1].TryToEnum(out EContentActiveResource resourceType))
                return new ContentActiveResource(resourceType);
        }

        Debug.LogError($"Null Parse Resource, {input}");
        return null;
    }

    public abstract bool VisualReward();
    public abstract bool VisualInventory();
    public abstract Sprite GetIcon(EUIResourceResolution resolution = EUIResourceResolution.x200);

    public virtual Sprite GetBG(EUIResourceResolution resolution = EUIResourceResolution.x200)
    {
        return DataSystem.Instance.dataSprites.dicItemBg[resolution][GetResourceRarity()];
    }

    public virtual Sprite GetBorder(EUIResourceResolution resolution = EUIResourceResolution.x200)
    {
        return DataSystem.Instance.dataSprites.dicItemBorder[resolution][GetResourceRarity()];
    }

    public abstract string GetName();
    public abstract string GetDesc();
    public abstract string GetTextValue();
    public abstract bool CompareType(GameResource source);
    public abstract ERarity GetResourceRarity();
    public abstract string GetRewardDataString();
}

public class PackageResource
{
    public List<GameResource> lstResource = new List<GameResource>();

    public PackageResource()
    {
    }

    public PackageResource(List<GameResource> resources)
    {
        lstResource = resources;
    }

    public void AddResource(PackageResource pack)
    {
        AddResource(pack.lstResource);
    }

    public void AddResource(List<GameResource> lstResource)
    {
        foreach (var item in lstResource)
        {
            AddResource(item);
        }
    }

    public void AddResource(GameResource newResource)
    {
        foreach (var item in lstResource)
        {
            if (item is ItemResource && newResource is ItemResource && item.CompareType(newResource))
            {
                ItemResource _item = (ItemResource)item;
                ItemResource _newResource = (ItemResource)newResource;

                _item.resourceValue += _newResource.resourceValue;
                return;
            }
        }

        lstResource.Add(newResource);
    }

    public long GetCommonResource(ECommonResource resourceType)
    {
        foreach (var item in lstResource)
        {
            if (item is CommonResource)
            {
                CommonResource res = item as CommonResource;
                if (res.resourceType == resourceType)
                    return res.resourceValue;
            }
        }

        return 0;
    }

    public void ReceiveResource(EResourceFrom resourceFrom, bool updateSever = false, Action actionComplete = null, Action actionError = null)
    {
        List<GameResource> lstVisual = new List<GameResource>();
        foreach (var item in lstResource)
        {
            if (item is VirtualResource)
            {
                VirtualResource vir = (VirtualResource)item;
                lstVisual.AddRange(vir.GetRealityResource());
            }
            else
                lstVisual.Add(item);
        }
        IPlayerResource.Instance.AddResource(lstVisual, resourceFrom, updateSever, actionComplete, actionError);
    }

    public void ReceiveAndShow(EResourceFrom resourceFrom, bool updateServer = false, Action actionComplete = null)
    {
        actionComplete += () => UIManager.Instance.PendingAction(()=> UIManager.Instance.ShowPopupReward(this));
        ReceiveResource(resourceFrom, updateServer, actionComplete);
    }

    public bool CanShowResource()
    {
        if (lstResource.Count > 0)
        {
            foreach (var item in lstResource)
            {
                if (item.VisualReward())
                {
                    return true;
                }
            }
        }

        return false;
    }
    public PackageResource GetCost()
    {
        PackageResource packCost = new PackageResource();
        foreach (var resource in lstResource)
        {
            if (resource is ItemResource)
            {
                ItemResource item = (ItemResource)resource;
                if (item.resourceValue < 0)
                    packCost.AddResource(item);
            }
        }
        return packCost;
    }
    public PackageResource GetReward()
    {
        PackageResource packReward = new PackageResource();
        foreach (var resource in lstResource)
        {
            if (resource is ItemResource)
            {
                ItemResource item = (ItemResource)resource;
                if (item.resourceValue > 0)
                    packReward.AddResource(item);
            }
            else
            {
                packReward.AddResource(resource);
            }
        }
        return packReward;
    }
}

public abstract class ItemResource : GameResource
{
    public long resourceValue;
    public abstract bool CanUseResource();
    public abstract void SetupButton(CommonButton button);
    public override bool VisualInventory()
    {
        return VisualReward() && resourceValue > 0;
    }
    public override string GetTextValue()
    {
        return Helper.GetTextResourceValue(resourceValue);
    }
}

public class CommonResource : ItemResource
{
    public ECommonResource resourceType;

    public override Sprite GetIcon(EUIResourceResolution resolution = EUIResourceResolution.x200)
    {
        return DataSystem.Instance.dataSprites.dicCommomSprites[resolution][resourceType];
    }

    public override string GetName()
    {
        return Helper.GetI2Translation($"{resourceType}_name");
    }

    public override string GetDesc()
    {
        return Helper.GetI2Translation($"{resourceType}_desc");
    }

    public override bool CompareType(GameResource source)
    {
        if (source is CommonResource)
        {
            CommonResource common = source as CommonResource;
            return common.resourceType == resourceType;
        }

        return false;
    }

    public override ERarity GetResourceRarity()
    {
        switch (resourceType)
        {
            case ECommonResource.Coin:
                return ERarity.Common;
            case ECommonResource.Gem:
                return ERarity.Epic;
            case ECommonResource.Energy:
                return ERarity.Rare;
            case ECommonResource.ActivePoint:
                return ERarity.Mythical;
            case ECommonResource.Exp:
                return ERarity.Great;
        }

        return ERarity.Common;
    }

    public override bool VisualReward()
    {
        return resourceValue > 0;
    }

    public CommonResource()
    {

    }

    public CommonResource(ECommonResource resourceType, long value)
    {
        this.resourceType = resourceType;
        resourceValue = value;
    }
    public override bool CanUseResource()
    {
        return true;
    }

    public override void SetupButton(CommonButton button)
    {
        //if (resourceType == ECommonResource.ChestPiece)
        //{
        //    button.SetupButton(EButtonColor.Yellow, "USE", true, () => { DebugCustom.LogColor("Chest piece use!!"); });
        //}
        //else
        //{
        //    button.SetupButton(EButtonColor.Yellow, "USE", true, () => { UIManager.Instance.ShowPopupGun(); });
        //}
    }
    public override string GetRewardDataString()
    {
        return $"CommonResource|{resourceType}|{resourceValue}";
    }
}

public class VirtualResource : ItemResource
{
    public EVirtualResource resourceType;

    public VirtualResource(EVirtualResource resourceType, long resourceValue)
    {
        this.resourceType = resourceType;
        this.resourceValue = resourceValue;
    }

    public override bool CanUseResource()
    {
        return false;
    }

    public override bool CompareType(GameResource source)
    {
        if (source is VirtualResource)
        {
            VirtualResource vir = (VirtualResource)source;
            return vir.resourceType == resourceType;
        }
        return false;
    }

    public override string GetDesc()
    {
        return Helper.GetI2Translation($"{resourceType}_desc");
    }

    public override Sprite GetIcon(EUIResourceResolution resolution = EUIResourceResolution.x200)
    {
        return DataSystem.Instance.dataSprites.dicVirtualSprites[resolution][resourceType];
    }

    public override string GetName()
    {
        return Helper.GetI2Translation($"{resourceType}_name");
    }

    public override ERarity GetResourceRarity()
    {
        switch (resourceType)
        {
            default:
                break;
        }
        return ERarity.Common;
    }
    public override void SetupButton(CommonButton button)
    {

    }
    public override bool VisualInventory()
    {
        return false;
    }
    public override bool VisualReward()
    {
        return true;
    }
    public List<GameResource> GetRealityResource()
    {
        List<GameResource> lstSource = new List<GameResource>();
        switch (resourceType)
        {
            default:
                break;
        }
        return lstSource;
    }
    public override string GetRewardDataString()
    {
        return $"VirtualResource|{resourceType}|{resourceValue}";
    }
}
public class ExpireableResource : ItemResource
{
    public EExpireableResource resourceType;
    public ExpireableResource(EExpireableResource resourceType, long resourceValue)
    {
        this.resourceType = resourceType;
        this.resourceValue = resourceValue;
    }
    public override bool CanUseResource()
    {
        return false;
    }

    public override bool CompareType(GameResource source)
    {
        if (source is ExpireableResource)
        {
            ExpireableResource exp = (ExpireableResource)source;
            return exp.resourceType == resourceType;
        }
        return false;
    }

    public override string GetDesc()
    {
        return Helper.GetI2Translation($"{resourceType}_desc");
    }

    public override Sprite GetIcon(EUIResourceResolution resolution = EUIResourceResolution.x200)
    {
        return DataSystem.Instance.dataSprites.dicExpireableSprites[resolution][resourceType];
    }

    public override string GetName()
    {
        return Helper.GetI2Translation($"{resourceType}_name");
    }

    public override ERarity GetResourceRarity()
    {
        return ERarity.Epic;
    }

    public override void SetupButton(CommonButton button)
    {

    }
    public override bool VisualReward()
    {
        return true;
    }
    public override string GetTextValue()
    {
        return "";
    }
    public override string GetRewardDataString()
    {
        return $"ExpireableResource|{resourceType}|{resourceValue}";
    }
}
public class ContentActiveResource : GameResource
{
    public EContentActiveResource resourceType;

    public ContentActiveResource(EContentActiveResource resourceType)
    {
        this.resourceType = resourceType;
    }

    public override bool CompareType(GameResource source)
    {
        return false;
    }

    public override string GetDesc()
    {
        return Helper.GetI2Translation($"{resourceType}_desc");
    }

    public override Sprite GetIcon(EUIResourceResolution resolution = EUIResourceResolution.x200)
    {
        return DataSystem.Instance.dataSprites.dicContentActiveSprites[resolution][resourceType];
    }

    public override string GetName()
    {
        return Helper.GetI2Translation($"{resourceType}_name");
    }

    public override ERarity GetResourceRarity()
    {
        return ERarity.Mythical;
    }

    public override string GetTextValue()
    {
        return "";
    }

    public override bool VisualInventory()
    {
        return false;
    }

    public override bool VisualReward()
    {
        return true;
    }
    public override string GetRewardDataString()
    {
        return $"ContentActiveResource|{resourceType}";
    }
}