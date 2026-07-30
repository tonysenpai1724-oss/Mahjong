using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopEnergyItem : MonoBehaviour
{
    public EShopEnergy shopEnergy;
    public TextMeshProUGUI txtValue, txtPrice;
    CommonShopItem shopItem;
    public void InitItem()
    {
        shopItem = DataSystem.Instance.dataShop.dicShopEnergy[shopEnergy];
        txtValue.text = shopItem.GetRewards().lstResource[0].GetTextValue();
        txtPrice.text = shopItem.costValue.ToString();
    }
    public void OnClickBuy()
    {
        shopItem.BuyItem();
    }
}
