using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupSetting : UIBase
{
    [Serializable]
    public class SettingToggleItem
    {
        public EGameSetting setting;
        public Button toggleButton;
        public Image fillImage;
        public RectTransform buttonRect;
        public Sprite spriteOn;
        public Sprite spriteOff;
        public RectTransform offPosition;
        public RectTransform onPosition;
        public Button offButton;
        public Button onButton;

        private Vector2 onButtonPosition;
        private Vector2 offButtonPosition;
        private float moveDistance = 22f;

     public void CachePositions()
{
    if (offPosition == null || onPosition == null)
        return;

    offButtonPosition = offPosition.anchoredPosition;
    onButtonPosition = onPosition.anchoredPosition;
}

        public void ApplyState(bool isOn, bool animate)
        {
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = 0;

                float targetFill = isOn ? 1f : 0f;
                if (animate)
                    fillImage.DOFillAmount(targetFill, 0.18f).SetEase(Ease.InOutQuad);
                else
                    fillImage.fillAmount = targetFill;
            }

            if (buttonRect != null)
            {
                Vector2 targetPos = isOn ? onButtonPosition : offButtonPosition;
                if (animate)
                    buttonRect.DOAnchorPos(targetPos, 0.18f).SetEase(Ease.InOutQuad);
                else
                    buttonRect.anchoredPosition = targetPos;
            }

            if (toggleButton != null)
            {
                Image buttonImage = toggleButton.image != null ? toggleButton.image : toggleButton.targetGraphic as Image;
                if (buttonImage != null)
                {
                    buttonImage.sprite = isOn ? spriteOn : spriteOff;
                    if (buttonImage.sprite == null)
                        buttonImage.color = isOn ? Color.white : new Color(1f, 1f, 1f, 0.8f);
                }
            }
        }
    }

    [SerializeField] private List<SettingToggleItem> settingItems = new List<SettingToggleItem>();

    private void Reset()
    {
        CacheAllPositions();
        BindButtons();
    }

    private void Awake()
    {
        CacheAllPositions();
        BindButtons();
        RefreshAll();
    }

    private void OnEnable()
    {
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_GAME_SETTING_CHANGE, RefreshAll);
        RefreshAll();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        TigerForge.EventManager.StopListening(Constant.EVENT_ON_GAME_SETTING_CHANGE, RefreshAll);
    }

    private void BindButtons()
    {
        foreach (var item in settingItems)
        {
            if (item == null || item.toggleButton == null)
                continue;

            item.toggleButton.onClick.RemoveAllListeners();
            item.toggleButton.onClick.AddListener(() => ToggleSetting(item));
              if (item.offButton != null)
        {
            item.offButton.onClick.RemoveAllListeners();
            item.offButton.onClick.AddListener(() => ToggleSetting(item));
        }

        // Vùng ON
        if (item.onButton != null)
        {
            item.onButton.onClick.RemoveAllListeners();
            item.onButton.onClick.AddListener(() => ToggleSetting(item));
        }
        }
    }

    private void CacheAllPositions()
    {
        foreach (var item in settingItems)
        {
            if (item != null)
                item.CachePositions();
        }
    }

    private void RefreshAll()
    {
        if (IGameSettingController.Instance == null)
            return;

        foreach (var item in settingItems)
        {
            if (item == null)
                continue;

            bool isOn = IGameSettingController.Instance.GetSetting(item.setting);
            item.ApplyState(isOn, false);
        }
    }

    private void ToggleSetting(SettingToggleItem item)
    {
        if (item == null || IGameSettingController.Instance == null)
            return;

        bool nextValue = !IGameSettingController.Instance.GetSetting(item.setting);
        IGameSettingController.Instance.ToggleSetting(item.setting);
        item.ApplyState(nextValue, true);
    }
}
