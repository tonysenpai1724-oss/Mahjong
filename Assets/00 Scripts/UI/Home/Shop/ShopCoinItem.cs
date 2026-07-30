using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class ShopCoinItem : MonoBehaviour
{
    public EShopCoin shopCoin;
    public TextMeshProUGUI txtValue, txtPrice;
    CommonShopItem shopItem;
    public void InitItem()
    {
        shopItem = DataSystem.Instance.dataShop.dicShopCoin[shopCoin];
        txtValue.text = shopItem.GetRewards().lstResource[0].GetTextValue();
        txtPrice.text = shopItem.costValue.ToString();
    }
    public void OnClickBuy()
    {
        shopItem.BuyItem();
    }
}
