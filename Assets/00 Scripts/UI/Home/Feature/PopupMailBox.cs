using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupMailBox : UIBase
{
    public MailBoxUIItem itemPrefab;
    public Transform itemParent;
    List<MailBoxUIItem> lstItems = new List<MailBoxUIItem>();

    public override void Show()
    {
        base.Show();
        List<MailItem> lstMail = IMailboxController.Instance.GetMails();
        int need = lstMail.Count;
        int has = lstItems.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPrefab, itemParent));
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            if (i < need)
                lstItems[i].InitData(lstMail[i]);
            else
                lstItems[i].gameObject.SetActive(false);
        }
        IMailboxController.Instance.ReadMail();
    }
}
