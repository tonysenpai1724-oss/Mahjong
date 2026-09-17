using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class CommonFill : MonoBehaviour
{
    public RectTransform parent;
    public RectTransform fill;
    float fillValue = 0;
    Tween fillTween;
    public void SetFill(float value)
    {
        fillValue = Mathf.Clamp01(value);
        fill.offsetMax = new Vector2(-parent.sizeDelta.x * (1 - fillValue), fill.sizeDelta.y);
    }
    public void DoFill(float value, System.Action actionComplete = null)
    {
        value = Mathf.Clamp01(value);
        if (fillTween != null)
        {
            fillTween.Kill();
        }
        fillTween = DOTween.To(() => fillValue, x => fillValue = x, value, 0.5f)
            .OnUpdate(() => SetFill(fillValue))
            .SetUpdate(UpdateType.Normal, true)
            .OnComplete(() =>
            {
                fillValue = value;
                SetFill(fillValue);
                actionComplete?.Invoke();
            });
    }
}
