using System.Collections;
using TMPro;
using UnityEngine;

namespace WE.UI
{
    public class PopupTooltipResource : UIBase
    {
        public RectTransform bgAnim, bg, arrowTop, arrowBottom, topOffset, leftOffset, rightOffset, bgPin;
        public TextMeshProUGUI txtTitle, txtDesc;
        Vector3 cachedPosition;

        public void Show(Vector3 position, GameResource resource)
        {
            cachedPosition = position;
            txtTitle.text = resource.GetName();
            txtDesc.text = resource.GetDesc();


            bgAnim.anchoredPosition = GetAnchorLocal(cachedPosition);
            bg.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            bool top = bgAnim.anchoredPosition.y < GetAnchorLocal(topOffset.transform.position).y;
            float deltaY = bgAnim.sizeDelta.y;
            bgAnim.anchoredPosition += new Vector2(0, deltaY * 0.6f * (top ? 1 : -1));
            arrowTop.gameObject.SetActive(!top);
            arrowBottom.gameObject.SetActive(top);
            Vector2 offsetLeft = GetAnchorLocal(leftOffset.transform.position);
            Vector2 offsetRight = GetAnchorLocal(rightOffset.transform.position);
            if (bgAnim.anchoredPosition.x > offsetRight.x)
                bg.anchoredPosition -= new Vector2(bgAnim.anchoredPosition.x - offsetRight.x, 0);
            if (bgAnim.anchoredPosition.x < offsetLeft.x)
                bg.anchoredPosition += new Vector2(offsetLeft.x - bgAnim.anchoredPosition.x, 0);

            Show();
        }

        Vector2 GetAnchorLocal(Vector3 position)
        {
            bgPin.transform.position = position;
            return bgPin.anchoredPosition;
        }
    }
}