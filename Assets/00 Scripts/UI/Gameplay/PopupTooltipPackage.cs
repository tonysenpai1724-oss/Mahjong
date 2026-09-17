using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
namespace WE.UI
{
    public class PopupTooltipPackage : UIBase
    {
        public RectTransform bg, bgAnim, arrowTop, arrowBottom, topOffset, leftOffset, rightOffset, bgPin;
        public UiResourceItem itemPrefabs;
        public Transform itemParent;
        List<UiResourceItem> lstItems = new List<UiResourceItem>();
        Vector3 cachedPosition;
        public void Show(Vector3 position, PackageResource pack)
        {
            cachedPosition = position;
            int has = lstItems.Count;
            int need = pack.lstResource.Count;
            for (int i = 0; i < need - has; i++)
            {
                lstItems.Add(Instantiate(itemPrefabs, itemParent));
            }
            for (int i = 0; i < lstItems.Count; i++)
            {
                lstItems[i].gameObject.SetActive(i < pack.lstResource.Count);
                if (i < pack.lstResource.Count)
                {
                    lstItems[i].InitResouce(pack.lstResource[i]);
                    //lstItems[i].txtCount.gameObject.SetActive(false);
                }
            }
            Show();
            itemParent.gameObject.SetActive(false);
            itemParent.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            bgAnim.anchoredPosition = GetAnchorLocal(cachedPosition);
            bgAnim.sizeDelta = bg.sizeDelta;
            bg.anchoredPosition = Vector2.zero;

            bool top = bgAnim.anchoredPosition.y < GetAnchorLocal(topOffset.transform.position).y;
            float deltaY = bg.sizeDelta.y;
            bgAnim.anchoredPosition += new Vector2(0, deltaY * 0.6f * (top ? 1 : -1));
            arrowTop.gameObject.SetActive(!top);
            arrowBottom.gameObject.SetActive(top);
            Vector2 offsetLeft = GetAnchorLocal(leftOffset.transform.position);
            Vector2 offsetRight = GetAnchorLocal(rightOffset.transform.position);
            if (bgAnim.anchoredPosition.x > offsetRight.x)
                bg.anchoredPosition -= new Vector2(bgAnim.anchoredPosition.x - offsetRight.x, 0);
            if (bgAnim.anchoredPosition.x < offsetLeft.x)
                bg.anchoredPosition += new Vector2(offsetLeft.x - bgAnim.anchoredPosition.x, 0);
        }
        Vector2 GetAnchorLocal(Vector3 position)
        {
            bgPin.transform.position = position;
            return bgPin.anchoredPosition;
        }
    }
}
