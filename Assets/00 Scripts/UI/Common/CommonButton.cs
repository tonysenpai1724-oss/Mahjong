using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
public class CommonButton : MonoBehaviour
{
    public static Action DisableAll, EnableAll;
    public Button button;
    public TextMeshProUGUI txtVisual;
    public GameObject notiObj;
    public EButtonType buttonType;
#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (txtVisual == null)
            txtVisual = GetComponentInChildren<TextMeshProUGUI>();
    }
#endif
    protected virtual void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_BUTTON_STATE_CHANGE, OnButtonStateChange);
        if (button != null)
            button.onClick.AddListener(PlaySoundButton);
        if(notiObj != null)
            notiObj.SetActive(false);
        DisableAll += DisableButton;
        EnableAll += EnableButton;
    }
    private void OnDestroy()
    {
        DisableAll -= DisableButton;
        EnableAll -= EnableButton;
    }
   protected virtual void DisableButton()
    {
        button.interactable = false;
    }
    protected virtual void EnableButton()
    {
        button.interactable = true;
    }
    public void PlaySoundButton()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(ESfx.ButtonSfx);
    }
    protected void OnButtonStateChange()
    {
        if (button != null)
            button.enabled = GameManager.Instance.DicButtonState[buttonType] && GameManager.Instance.DicButtonState[EButtonType.Common];
    }
    public void SetupButton(EButtonColor color, string txt, bool canInteract, System.Action actionClick)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(PlaySoundButton);
            button.onClick.AddListener(() => actionClick?.Invoke());
            button.interactable = canInteract;
            if (txtVisual != null)
                txtVisual.text = txt;
            button.image.sprite = DataSystem.Instance.dataSprites.dicButtonColor[color];
        }
    }
}
