using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : CommonButton
{
    public bool deactivateButton = true;
    public Image bgBtn;
    public Sprite sprOn, sprOff;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (bgBtn == null)
        {
            bgBtn = GetComponent<Image>();
        }
    }
#endif
    public virtual void SetActive(bool isOn)
    {
        bgBtn.sprite = isOn ? sprOn : sprOff;
        if (deactivateButton)
            button.interactable = !isOn;
    }
}
