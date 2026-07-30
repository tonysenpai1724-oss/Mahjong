using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PromotionUIShopItem : MonoBehaviour
{
    public UiResourceItem itemPrefab;
    public Transform itemParent;
    List<UiResourceItem> lstItems = new List<UiResourceItem>();
    public TextMeshProUGUI txtName, txtCost;
    IAPConfig iapConfig;
    public void Init(EIAPPackType packType)
    {
        gameObject.SetActive(true);
        iapConfig = DataSystem.Instance.dataIAP.dicConfigs[packType];
        if (!iapConfig.CanShow())
        {
            gameObject.SetActive(false);
            return;
        }
        txtName.text = iapConfig.packName;
        txtCost.text = iapConfig.price.ToString();
        PackageResource reward = iapConfig.GetReward();
        int has = lstItems.Count;
        int need = reward.lstResource.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPrefab, itemParent));
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            if (i < need)
                lstItems[i].InitResouce(reward.lstResource[i]);
            else
                lstItems[i].gameObject.SetActive(false);
        }
    }
    public void OnClickBuy()
    {
        IIAPController.Instance.PurchasePack(iapConfig.packType);
    }
}
