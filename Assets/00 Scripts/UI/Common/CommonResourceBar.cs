using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
public class CommonResourceBar : MonoBehaviour
{
    public Image iconResource;
    public TextMeshProUGUI txtValue;
    public float timeDelay, timeAnim;
    bool firstTime;
    long currentValue;
    Tween animTween;
    Coroutine delayCoroutine;
    public ECommonResource resourceType;
    void Start()
    {
        TigerForge.EventManager.StartListening(Constant.ON_PLAYER_RESOURCE_UPDATE, UpdateResource);
        iconResource.sprite = DataSystem.Instance.dataSprites.dicCommomSprites[EUIResourceResolution.x100][resourceType];
        UpdateResource();
    }
    private void OnDisable()
    {
        if (animTween != null)
            animTween.Kill();
        StopAllCoroutines();
    }
    private void OnEnable()
    {
        long nextValue = IPlayerResource.Instance.GetCommonResource(resourceType); 
        currentValue = nextValue;
        txtValue.text = currentValue.ToString();
    }
    void UpdateResource()
    {
        long nextValue = IPlayerResource.Instance.GetCommonResource(resourceType);
        if (!firstTime || !gameObject.activeInHierarchy)
        {
            currentValue = nextValue;
            txtValue.text = currentValue.ToString();
            firstTime = true;
        }
        else
        {
            if (delayCoroutine != null)
                StopCoroutine(delayCoroutine);
            delayCoroutine = StartCoroutine(IEDelayAnim(nextValue));
        }
    }
    IEnumerator IEDelayAnim(long nextValue)
    {
        if (nextValue > currentValue)
            yield return new WaitForSecondsRealtime(timeDelay);
        if (animTween != null)
            animTween.Kill();
        animTween = DOTween.To(() => currentValue, x => currentValue = x, nextValue, timeAnim).SetUpdate(UpdateType.Normal).SetUpdate(true).OnUpdate(() =>
        {
            txtValue.text = currentValue.ToString();
        }).OnComplete(() =>
        {
            currentValue = nextValue;
            txtValue.text = currentValue.ToString();
        });
    }
}
