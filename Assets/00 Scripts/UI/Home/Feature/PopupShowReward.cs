using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupShowReward : UIBase
{
    public UiResourceItem itemPefabs;
    public Transform itemParent;
    List<UiResourceItem> lstItems;
    PackageResource package;
    public void Show(PackageResource resource)
    {
        AudioManager.Instance.PlaySfx(ESfx.RewardedSfx);
        package = new PackageResource();
        foreach (var item in resource.lstResource)
        {
            if (item.VisualReward())
            {
                if (item is ItemResource)
                {
                    if ((item as ItemResource).resourceValue > 0)
                        package.AddResource(item);
                }
                else
                    package.AddResource(item);
            }
        }
        if (lstItems == null)
            lstItems = new List<UiResourceItem>();
        int need = resource.lstResource.Count;

        int has = lstItems.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPefabs, itemParent));
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            lstItems[i].gameObject.SetActive(false);
        }
        base.Show();
        StartCoroutine(IEShowReward());
    }
    IEnumerator IEShowReward()
    {
        for (int i = 0; i < package.lstResource.Count; i++)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            lstItems[i].InitResouce(package.lstResource[i], true);
        }
    }
}
