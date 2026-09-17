using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeMainPanel : HomePanel
{
    public HomeLevelItem itemPrefab;
    public Transform itemParent;
    List<HomeLevelItem> lstItems = new List<HomeLevelItem>();

    public int scrollSpacingBottom, itemHeight;
    public RectTransform scrollTransform;

    public override void InitFirstTime()
    {
        //InitLevel();
    }
    // void InitLevel()
    // {
    //     int need = IPlayerInfoController.Instance.MaxLevel();
    //     int has = lstItems.Count;
    //     for (int i = 0; i < need - has; i++)
    //     {
    //         lstItems.Add(Instantiate(itemPrefab, itemParent));
    //     }
    //     for (int i = 0; i < lstItems.Count; i++)
    //     {
    //         gameObject.SetActive(i < need);
    //         if (i < need)
    //             lstItems[i].InitLevel(i + 1);
    //     }
    //     scrollTransform.gameObject.SetActive(false);
    //     Canvas.ForceUpdateCanvases();
    //     scrollTransform.gameObject.SetActive(true);
    //     SnapLevel(IPlayerInfoController.Instance.CurrentLevel());
    // }
    [Button()]
    void SnapLevel(int level)
    {
        float anchorY = -(scrollSpacingBottom + (level - 1) * itemHeight);
        scrollTransform.anchoredPosition = new Vector2(0, anchorY);
    }

}
