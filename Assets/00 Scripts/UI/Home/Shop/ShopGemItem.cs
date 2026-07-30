using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class ShopGemItem : MonoBehaviour
{
    public EIAPPackType packType;
    public TextMeshProUGUI txtValue, txtPrice;
    IAPConfig iAPConfig;
    public void InitItem()
    {
        iAPConfig = DataSystem.Instance.dataIAP.dicConfigs[packType];
        txtValue.text = iAPConfig.GetReward().lstResource[0].GetTextValue();
        txtPrice.text = iAPConfig.price.ToString();
    }
    public void OnClickBuy()
    {
        IIAPController.Instance.PurchasePack(iAPConfig.packType);
    }
}
