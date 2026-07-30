using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IMailboxController: IController<MailBoxController>
{
    public List<MailItem> GetMails();
    public bool NotiMail();
    public void ReadMail();
    public void AddMail(MailItem mail);
}
public class MailBoxCachedData : IControllerCachedData
{
    public List<MailItem> lstMails = new List<MailItem>();
    public void InitFirsTime()
    {
    }

    public void OnNewData()
    {
    }

    public void AddMail(MailItem mail)
    {
        lstMails.Add(mail);
        if (lstMails.Count > 30)
            lstMails.RemoveAt(0);
    }
    public void OnReadMail()
    {
        foreach (var item in lstMails)
        {
            item.isRead = true;
        }
    }
    public bool NotiMail()
    {
        foreach (var item in lstMails)
        {
            if (!item.isRead)
                return true;
        }
        return false;
    }
}
public class MailBoxController :
#if LOCAL_BUILD
    BaseLocalController<MailBoxCachedData>
#else
    CommonServerController<MailBoxCachedData>
#endif
    , IMailboxController
{
    public override string KeyData()
    {
        return "mail_box";
    }

    public override string KeyEvent()
    {
        return Constant.ON_MAIL_BOX_UPDATE;
    }

    bool notiMail;
    protected override void OnInitSuccess()
    {
        base.OnInitSuccess();
        notiMail = cachedData.NotiMail();
    }
    public void AddMail(MailItem mail)
    {
        cachedData.AddMail(mail);
        notiMail = true;
        OnValueChange();
    }

    public bool NotiMail()
    {
        return notiMail;
    }

    public void ReadMail()
    {
        notiMail = false;
        cachedData.OnReadMail();
        OnValueChange();
    }

    public List<MailItem> GetMails()
    {
        cachedData.lstMails.Sort((a, b) => b.timeReceived.CompareTo(a.timeReceived));
        return cachedData.lstMails;
    }
}
public class MailItem
{
    public string id;
    public long timeReceived;
    public string title;
    public string desc;
    public bool isRead;
    public List<string> lstRewards = new List<string>();
    public MailItem()
    {

    }
    public MailItem(string title, string desc, PackageResource rewards)
    {
        id = Guid.NewGuid().ToString().ToLower();
        timeReceived = DateTime.UtcNow.ToUnixTimestamp();
        this.title = title;
        this.desc = desc;
        isRead = false;
        foreach (var item in rewards.lstResource)
        {
            lstRewards.Add(item.GetRewardDataString());
        }
    }
    public PackageResource GetRewards()
    {
        PackageResource package = new PackageResource();
        foreach (var item in lstRewards)
        {
            package.AddResource(GameResource.GetResource(item));
        }
        return package;
    }
}
