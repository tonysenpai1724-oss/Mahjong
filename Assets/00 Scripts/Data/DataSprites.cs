using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data Sprites", menuName = "Data/Data Sprites")]
public class DataSprites : SerializedScriptableObject
{
    [FoldoutGroup("Resource")]
    public Dictionary<EUIResourceResolution, Dictionary<ECommonResource, Sprite>> dicCommomSprites;
    [FoldoutGroup("Resource")]
    public Dictionary<EUIResourceResolution, Dictionary<EVirtualResource, Sprite>> dicVirtualSprites;
    [FoldoutGroup("Resource")]
    public Dictionary<EUIResourceResolution, Dictionary<EExpireableResource, Sprite>> dicExpireableSprites;
    [FoldoutGroup("Resource")]
    public Dictionary<EUIResourceResolution, Dictionary<EContentActiveResource, Sprite>> dicContentActiveSprites;
    [FoldoutGroup("Resource/Rarity")]
    public Dictionary<EUIResourceResolution, Dictionary<ERarity, Sprite>> dicItemBg;
    [FoldoutGroup("Resource/Rarity")]
    public Dictionary<EUIResourceResolution, Dictionary<ERarity, Sprite>> dicItemBorder;
    [FoldoutGroup("Resource/Rarity")]
    public Dictionary<ERarity, Color> dicRarityColor;

    [FoldoutGroup("Button Sprite")]
    public Dictionary<EButtonColor, Sprite> dicButtonColor;
    [FoldoutGroup("Setting")]
    public Dictionary<EGameSetting, Dictionary<bool, Sprite>> dicSettingIcon;
    Dictionary<string, Sprite> dicDownloadedSprs;
    public void DownloadSprite(string url, System.Action<Sprite> actionComplete)
    {
        if (dicDownloadedSprs == null)
            dicDownloadedSprs = new Dictionary<string, Sprite>();
        dicDownloadedSprs.TryGetValue(url, out Sprite sprite);
        if (sprite != null)
            actionComplete?.Invoke(sprite);
        else
        {
            HTTPManager.Instance.LoadImage(url, spr =>
            {
                if (dicDownloadedSprs.ContainsKey(url))
                    dicDownloadedSprs[url] = spr;
                else
                    dicDownloadedSprs.Add(url, spr);
                actionComplete?.Invoke(spr);
            });
        }
    }
}
