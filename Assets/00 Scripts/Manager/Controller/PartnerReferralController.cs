using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IPartnerReferalController : IController<PartnerRefferalLocal>
{
    public void OnClickPartner(string url);
    public bool GetPartnerClaimed(string url);
}
public class PartnerReferalCachedData : IControllerCachedData
{
    public List<string> lstPartnerClaimed = new List<string>();
    public void InitFirsTime()
    {
    }
    public void OnNewData()
    {
    }
    public void AddPartnerClaimed(string url)
    {
        lstPartnerClaimed.Add(url);
    }
    public bool GetPartnerClaimed(string url)
    {
        return lstPartnerClaimed.Contains(url);
    }
}
public class PartnerRefferalLocal :
#if LOCAL_BUILD
    BaseLocalController<PartnerReferalCachedData>
#else
    CommonServerController<PartnerReferalCachedData>
#endif
    , IPartnerReferalController
{
    public override string KeyData()
    {
        return "partner_referal";
    }

    public override string KeyEvent()
    {
        return Constant.EVENT_ON_PARTNER_UPDATE;
    }

    public override IEnumerator IEFetchConfigs()
    {
        DataSystem.Instance.dataPartnerReferal.ParseConfig(GameManager.Instance.GetConfig("partner_referral"));
        yield return null;
    }

    public bool GetPartnerClaimed(string url)
    {
        return cachedData.GetPartnerClaimed(url);
    }

    public void OnClickPartner(string url)
    {
        if (!GetPartnerClaimed(url))
        {
            PartnerReferalConfig config = DataSystem.Instance.dataPartnerReferal.lstConfigs.Find(x => x.url == url);
            if (config != null)
            {
                config.GetRewards().ReceiveAndShow(EResourceFrom.PartnerReferal);
                cachedData.AddPartnerClaimed(url);
                OnValueChange();
            }
        }
    }
}
