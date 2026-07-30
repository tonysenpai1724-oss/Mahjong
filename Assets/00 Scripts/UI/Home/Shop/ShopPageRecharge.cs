using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopPageRecharge : HomePanel
{
    public List<ShopGemItem> lstGemItems;
    public List<ShopCoinItem> lstCoinItems;
    public List<ShopEnergyItem> lstEnergyItems;
    public override void InitFirstTime()
    {
        foreach (var item in lstGemItems)
        {
            item.InitItem();
        }
        foreach (var item in lstCoinItems)
        {
            item.InitItem();
        }
        foreach (var item in lstEnergyItems)
        {
            item.InitItem();
        }
    }
}
