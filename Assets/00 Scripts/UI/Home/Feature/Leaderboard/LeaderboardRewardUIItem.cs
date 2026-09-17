using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardRewardUIItem : MonoBehaviour
{
    public UiResourceItem itemPrefab;
    public Transform itemParent;
    List<UiResourceItem> lstItems = new List<UiResourceItem>();

    public TextMeshProUGUI txtRank;

    public void InitReward(LeaderboardRewardConfig config)
    {
        PackageResource package = config.GetRewards();
        txtRank.text = "#" + config.rank.ToString();
        int has = lstItems.Count;
        int need = package.lstResource.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPrefab, itemParent));
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            lstItems[i].gameObject.SetActive(i < package.lstResource.Count);
            if(i < package.lstResource.Count)
            {
                lstItems[i].InitResouce(package.lstResource[i]);
            }
        }
    }
}
