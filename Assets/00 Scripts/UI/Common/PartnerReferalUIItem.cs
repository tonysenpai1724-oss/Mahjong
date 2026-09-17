using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PartnerReferalUIItem : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI txtTitle;
    public UiResourceItem gemItem, activePointItem;
    public Button btn;
    public GameObject lockPanel;
    PartnerReferalConfig config;
    public void InitItem(PartnerReferalConfig config)
    {
        gameObject.SetActive(true);
        this.config = config;
        DataSystem.Instance.dataSprites.DownloadSprite(config.iconUrl, spr =>
        {
            icon.sprite = spr;
        });
        txtTitle.text = config.title;
        gemItem.InitResouce(new CommonResource(ECommonResource.Gem, config.gemReward));
        activePointItem.InitResouce(new CommonResource(ECommonResource.ActivePoint, config.activePointReward));
        btn.interactable = !IPartnerReferalController.Instance.GetPartnerClaimed(config.url);
        lockPanel.SetActive(!btn.interactable);
    }
    public void OnClick()
    {
#if UNITY_WEBGL
        TelegramManager.OpenTelegramLink(config.url);
#endif
        IPartnerReferalController.Instance.OnClickPartner(config.url);
    }
}
