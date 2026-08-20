using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BottomNavButton : HomeToggleButton
{
    [Header("Shared Active Visual")]
    public RectTransform activeVisual;
    public CanvasGroup activeVisualGroup;

    [Header("Button Visual")]
    public RectTransform iconRoot;
    public CanvasGroup txtNameGroup;

    [Header("Position")]
    public float iconOnY = 70f;
    public float iconOffY = 10f;
    public float activeVisualOnY = 38f;

    [Header("Animation")]
    public float duration = 0.2f;
    public Ease moveEase = Ease.OutBack;
    public Ease fadeEase = Ease.OutQuad;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (iconRoot == null && icon != null)
            iconRoot = icon.rectTransform;

        if (txtNameGroup == null && txtName != null)
            txtNameGroup = txtName.GetComponent<CanvasGroup>();
    }
#endif

    public override void SetActive(bool isOn)
    {
        if (deactivateButton && button != null)
            button.interactable = !isOn;

        RectTransform targetIcon = iconRoot != null ? iconRoot : icon != null ? icon.rectTransform : null;
        if (targetIcon != null)
        {
            targetIcon.DOKill();
            targetIcon.DOAnchorPosY(isOn ? iconOnY : iconOffY, duration).SetEase(moveEase);
        }

        if (txtNameGroup != null)
        {
            txtNameGroup.DOKill();
            txtNameGroup.DOFade(isOn ? 1f : 0f, duration).SetEase(fadeEase);
        }
        else if (txtName != null)
        {
            txtName.DOKill();
            txtName.DOFade(isOn ? 1f : 0f, duration).SetEase(fadeEase);
        }

        if (isOn)
            MoveActiveVisualToThisButton();
    }

    private void MoveActiveVisualToThisButton()
    {
        if (activeVisual == null)
            return;

        Vector2 targetPosition = activeVisual.anchoredPosition;
        targetPosition.x = GetTargetXInActiveVisualParent();
        targetPosition.y = activeVisualOnY;

        activeVisual.gameObject.SetActive(true);
        activeVisual.DOKill();
        activeVisual.DOAnchorPos(targetPosition, duration).SetEase(moveEase);

        if (activeVisualGroup != null)
        {
            activeVisualGroup.DOKill();
            activeVisualGroup.DOFade(1f, duration).SetEase(fadeEase);
        }
    }

    private float GetTargetXInActiveVisualParent()
    {
        RectTransform buttonRect = transform as RectTransform;
        RectTransform activeParent = activeVisual.parent as RectTransform;

        if (buttonRect == null || activeParent == null)
            return activeVisual.anchoredPosition.x;

        Vector3 worldCenter = buttonRect.TransformPoint(buttonRect.rect.center);
        Vector3 localCenter = activeParent.InverseTransformPoint(worldCenter);
        return localCenter.x;
    }
}
