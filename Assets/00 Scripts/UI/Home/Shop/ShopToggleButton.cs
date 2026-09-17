using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopToggleButton : ToggleButton
{
    public void OnClick()
    {
        (UIManager.Instance.uIHome.homePanels[0] as HomeShopPanel)
            .OnSelectButton(this);
    }
}
