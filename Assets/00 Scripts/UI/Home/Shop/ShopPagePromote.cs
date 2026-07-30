using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopPagePromote : HomePanel
{
    public PromotionUIShopItem itemPrefab;
    public Transform itemParent;
    List<PromotionUIShopItem> lstItems = new List<PromotionUIShopItem>();

    public override void InitFirstTime()
    {
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_IAP_CHANGE, InitPack);
        InitPack();
    }
    void InitPack()
    {
        int need = DataSystem.Instance.dataIAP.packPromote.Count;
        int has = lstItems.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPrefab, itemParent));
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            if (i < need)
                lstItems[i].Init(DataSystem.Instance.dataIAP.packPromote[i]);
            else
                lstItems[i].gameObject.SetActive(false);
        }
    }
}
