using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
public class HomeToggleButton : ToggleButton
{
    public Button btn;
    public Image icon;
    public TextMeshProUGUI txtName;

    public void OnClick()
    {
        UIManager.Instance.uIHome.OnClickHomeButton(this);
    }
    public override void SetActive(bool isOn)
    {
        base.SetActive(isOn);
        txtName.DOKill();
        txtName.DOFade(isOn ? 1 : 0f, 0.2f);
        icon.rectTransform.DOKill();
        icon.rectTransform.DOAnchorPosY(isOn ? 70 : 10, 0.2f);
    }
}
