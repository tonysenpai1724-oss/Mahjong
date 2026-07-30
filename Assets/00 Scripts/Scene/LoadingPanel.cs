using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using DG.Tweening;
using TMPro;
using UniRx;
public class LoadingPanel : Singleton<LoadingPanel>
{
    public Image splashImage;
    public CommonFill loadingFill;
    public Canvas loadingCanvas;
    public RectTransform loadingObj, moveTransitionRect;
    public Vector2 focusStartSize, moveTransitionStartSize;
    public AnimationCurve curveIn, curveOut;
    public TextMeshProUGUI txtLoading;
    Tween transitionTween;
    public bool Playing { get; private set; }
    string subfix = ".";
    string prefix = "Loading";
    public GameObject loadingWait;
    public LoginPanel loginPanel;
    private void Start()
    {
        Observable.Interval(System.TimeSpan.FromSeconds(1)).Subscribe(_ => VisualTextLoading());
    }
    public void VisualTextLoading()
    {
        subfix += ".";
        if (subfix.Length > 3)
            subfix = ".";
        txtLoading.text = prefix + subfix;
    }
    public void ShowTextLoading(string text)
    {
        prefix = text;
        VisualTextLoading();
    }
    public void ShowLoadingWait(bool wait)
    {
        loadingWait.SetActive(wait);
    }
    public void StartLoading()
    {

        splashImage.gameObject.SetActive(true);
        moveTransitionRect.gameObject.SetActive(false);
        loadingFill.DOKill();
        loadingFill.SetFill(0);
        loadingObj.gameObject.SetActive(true);
        gameObject.SetActive(true);
        loadingFill.DoFill(0.7f);
    }
    public IEnumerator EndLoading()
    {
        loadingFill.DOKill();
        bool fading = true;
        loadingFill.DoFill(1,() =>
        {
            fading = false;
        });
        yield return new WaitUntil(() => !fading);
        loadingObj.gameObject.SetActive(false);
        txtLoading.gameObject.SetActive(false);
    }
    public IEnumerator IEStartTransiton()
    {
        Playing = true;
        if (transitionTween != null)
            transitionTween.Kill(true);
        Vector2 focusPos = Vector2.zero;
        loadingObj.gameObject.SetActive(false);
        moveTransitionRect.gameObject.SetActive(false);
       bool  anim = true;
        moveTransitionRect.localEulerAngles = new Vector3(0, 0, Random.Range(0, 180));
        moveTransitionRect.sizeDelta = moveTransitionStartSize;
        moveTransitionRect.gameObject.SetActive(true);
        transitionTween = moveTransitionRect.DOSizeDelta(new Vector2(0, moveTransitionStartSize.y), 1f).SetEase(curveIn).SetUpdate(UpdateType.Normal, true).OnComplete(() =>
        {
            anim = false;
        });
        yield return new WaitUntil(() => !anim);
    }
    [Button()]
    void EndTransition()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(IEEndTransition());
    }
    public IEnumerator IEEndTransition()
    {
        splashImage.gameObject.SetActive(false);
        if (transitionTween != null)
            transitionTween.Kill(true);
        bool anim = false;
        if (moveTransitionRect.gameObject.activeInHierarchy)
        {
            anim = true;
            transitionTween = moveTransitionRect.DOSizeDelta(moveTransitionStartSize, 1f).SetEase(curveOut).SetUpdate(UpdateType.Normal, true).OnComplete(() =>
            {
                anim = false;
                moveTransitionRect.gameObject.SetActive(false);
            });
        }
        yield return new WaitUntil(() => !anim);
        Playing = false;
    }
    public void ShowWaitNetworkPanel()
    {

    }
    public void HideWaitNetworkPanel()
    {

    }
}
