using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BottomNavButton : MonoBehaviour
{
    private TextMeshProUGUI txtName;
    private RectTransform iconRoot;
    private Button button;
    private float iconStartY;

    public float selectedIconOffsetY = 35f;
    public float duration = 0.2f;

    private BottomNavBar bar;

    public RectTransform RectTransform => transform as RectTransform;

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoWire();
    }
#endif

    private void Awake()
    {
        AutoWire();

        if (iconRoot != null)
            iconStartY = iconRoot.anchoredPosition.y;
    }

    private void AutoWire()
    {
        if (iconRoot == null)
        {
            Transform iconTransform = transform.Find("Icon") ?? transform.Find("icon") ?? transform.Find("Image");
            if (iconTransform != null)
                iconRoot = iconTransform as RectTransform;
        }

        if (iconRoot == null)
            iconRoot = transform as RectTransform;

        if (txtName == null)
        {
            Transform textTransform = transform.Find("Text (TMP)") ?? transform.Find("Text") ?? transform.Find("Name");
            if (textTransform != null)
                txtName = textTransform.GetComponent<TextMeshProUGUI>();
        }

        if (button == null)
            button = GetComponent<Button>();
    }

    public void Bind(BottomNavBar owner)
    {
        bar = owner;

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        bar?.Select(this);
    }

    public void SetSelected(bool isOn)
    {
        if (button != null)
            button.interactable = !isOn;

        RectTransform targetIcon = iconRoot;
        if (targetIcon != null)
        {
            targetIcon.DOKill();
            targetIcon.DOAnchorPosY(iconStartY + (isOn ? selectedIconOffsetY : 0f), duration).SetEase(Ease.OutBack);
        }

        if (txtName != null)
        {
            txtName.DOKill();
            txtName.DOFade(isOn ? 1f : 0f, duration);
        }
    }
}
