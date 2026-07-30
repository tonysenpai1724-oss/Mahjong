using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MailBoxUIItem : MonoBehaviour
{
    public TextMeshProUGUI txtName, txtTime;
    public GameObject notiObj;
    public UiResourceItem itemPrefabs;
    public Transform itemParent;
    List<UiResourceItem> lstItems = new List<UiResourceItem>();

    public void InitData(MailItem mailItem)
    {
        gameObject.SetActive(true);
        txtName.text = mailItem.title;
        txtTime.text = Helper.ParseDateTime(mailItem.timeReceived).ToString();
        notiObj.SetActive(!mailItem.isRead);
        PackageResource reward = mailItem.GetRewards();
        int need = reward.lstResource.Count;
        int has = lstItems.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPrefabs, itemParent));
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            if (i < need)
                lstItems[i].InitResouce(reward.lstResource[i]);
            else
                lstItems[i].gameObject.SetActive(false);
        }
    }
}
