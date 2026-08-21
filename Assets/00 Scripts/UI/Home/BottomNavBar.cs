using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BottomNavBar : MonoBehaviour
{
    public RectTransform bgIcon;
    public List<BottomNavButton> tabs = new List<BottomNavButton>();
    public List<HomePanel> panels = new List<HomePanel>();

    public float hiddenIconY = -110f;
    public float activeIconY = 9f;
    public float moveDuration = 0.2f;
    public Ease moveEase = Ease.OutBack;

    private BottomNavButton currentTab;

    private void Awake()
    {
        if (bgIcon != null)
            bgIcon.SetAsFirstSibling();

        if (tabs.Count == 0)
            tabs.AddRange(GetComponentsInChildren<BottomNavButton>(true));

        for (int i = 0; i < tabs.Count; i++)
            tabs[i].Bind(this);
    }

    public void Select(BottomNavButton tab)
    {
        if (tab == null || currentTab == tab)
            return;

        currentTab = tab;

        if (bgIcon != null)
        {
            bgIcon.SetAsFirstSibling();

            Vector2 targetPosition = bgIcon.anchoredPosition;
            targetPosition.x = GetTargetX(tab);
            targetPosition.y = hiddenIconY;

            bgIcon.gameObject.SetActive(true);
            bgIcon.DOKill();
            bgIcon.anchoredPosition = targetPosition;
            bgIcon.DOAnchorPosY(activeIconY, moveDuration).SetEase(moveEase);
        }

        for (int i = 0; i < tabs.Count; i++)
            tabs[i].SetSelected(tabs[i] == tab);

        for (int i = 0; i < panels.Count; i++)
            panels[i].SetActive(i < tabs.Count && tabs[i] == tab);
    }

    public void Select(int index)
    {
        if (index < 0 || index >= tabs.Count)
            return;

        Select(tabs[index]);
    }

    private float GetTargetX(BottomNavButton tab)
    {
        if (bgIcon == null || tab == null)
            return 0f;

        RectTransform tabRect = tab.RectTransform;
        RectTransform iconParent = bgIcon.parent as RectTransform;
        if (tabRect == null || iconParent == null)
            return bgIcon.anchoredPosition.x;

        Vector3 worldCenter = tabRect.TransformPoint(tabRect.rect.center);
        Vector3 localCenter = iconParent.InverseTransformPoint(worldCenter);
        return localCenter.x;
    }
}
