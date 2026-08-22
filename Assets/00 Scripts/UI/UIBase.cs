using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBase : MonoBehaviour
{
    public bool allowBackKey;
    //public WindowID windowID;
    [SerializeField]
    protected Animator animatorUI;

    public Button buttonClose;
    public GameObject blockPanel;
    public GameObject hackObj;
    protected bool canAction => GameManager.Instance.GameState != EGameState.Loading;
    #region MonoBehaviour
    public virtual void OnDisable()
    {
        StopAllCoroutines();
        Dispose();
    }

    #endregion
    #region Method

    private void Dispose()
    {
        if (buttonClose != null)
            buttonClose.onClick.RemoveAllListeners();
    }
    public virtual void Show()
    {
        if (gameObject.activeSelf)
            return;

        DebugCustom.LogColor("Show popup", gameObject.name);
        if (hackObj != null)
            hackObj.SetActive(GameManager.Instance.IsTester);
        if (blockPanel != null)
            blockPanel.SetActive(false);
        gameObject.SetActive(true);
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.SetState(EGamePlayState.Pause);
        }
        if (UIManager.Instance != null)
        {
            if (!UIManager.Instance.lstOpenningUI.Contains(this))
                UIManager.Instance.lstOpenningUI.Add(this);
        }
        if (buttonClose != null)
        {
            buttonClose.onClick.AddListener(() =>
            {
                Hide();
            });
        }
        this.transform.SetAsLastSibling();

    }

    public virtual void Hide()
    {
        StartCoroutine(IEClose());
    }
    IEnumerator IEClose()
    {
        if (GameplayManager.Instance != null)
        {
            //bool showingAds = true;
            //GameManager.Instance.TryShowInterAds(() => { showingAds = false; }, name);
            //yield return new WaitUntil(() => !showingAds);
        }
        if (animatorUI != null)
            animatorUI.Play("Close");
        yield return new WaitForSecondsRealtime(0.2f);
        gameObject.SetActive(false);
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        AfterHideAction();
    }
    public virtual void AfterHideAction()
    {
    }
    #endregion
}
