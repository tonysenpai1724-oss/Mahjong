using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PopupPartnerReferal : UIBase
{
    public PartnerReferalUIItem itemPrefab;
    public Transform itemParent;
    List<PartnerReferalUIItem> lstItems;
    public override void Show()
    {
        base.Show();
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_PARTNER_UPDATE, InitUI);
        InitUI();
    }
    public override void OnDisable()
    {
        base.OnDisable();
        TigerForge.EventManager.StopListening(Constant.EVENT_ON_PARTNER_UPDATE, InitUI);
    }
    void InitUI()
    {
        if (lstItems == null)
        {
            lstItems = new List<PartnerReferalUIItem>();
            foreach (var item in DataSystem.Instance.dataPartnerReferal.lstConfigs)
            {
                lstItems.Add(Instantiate(itemPrefab, itemParent));
            }
        }
        List<PartnerReferalConfig> lstConfigs = DataSystem.Instance.dataPartnerReferal.lstConfigs
            .OrderBy(t => IPartnerReferalController.Instance.GetPartnerClaimed(t.url)).ToList();
        for (int i = 0; i < lstItems.Count; i++)
        {
            if (i < lstConfigs.Count)
            {
                lstItems[i].InitItem(lstConfigs[i]);
            }
            else
            {
                lstItems[i].gameObject.SetActive(false);
            }
        }
    }
}
