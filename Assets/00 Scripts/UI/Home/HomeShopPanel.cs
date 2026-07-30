using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeShopPanel : HomePanel
{
    public List<ShopToggleButton> lstToggleButtons;
    public List<HomePanel> lstShopPages;
    ShopToggleButton currentSelected;
    public override void InitFirstTime()
    {
        foreach (var item in lstShopPages)
        {
            item.InitFirstTime();
        }
        lstToggleButtons[0].OnClick();
    }
    public override void SetActive(bool active)
    {
        base.SetActive(active);
        OnSelectButton(active ? currentSelected : null);
    }
    public void OnSelectButton(ShopToggleButton button)
    {
        if (button != null)
            currentSelected = button;
        for (int i = 0; i < lstToggleButtons.Count; i++)
        {
            lstToggleButtons[i].SetActive(lstToggleButtons[i] == button);
            lstShopPages[i].SetActive(lstToggleButtons[i] == button);
        }
    }
}
