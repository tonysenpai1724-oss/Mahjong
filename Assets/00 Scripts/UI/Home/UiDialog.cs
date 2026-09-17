using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;
public class UiDialog : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI txtDialog;
    public float timeStay = 1f, timeFade = 2f;
    [Button()]
    public void ShowDialog(string message)
    {
        canvasGroup.DOKill();
        txtDialog.text = message;
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        StartCoroutine(IEShowDialog());
    }
    IEnumerator IEShowDialog()
    {
        yield return new WaitForSeconds(timeStay);

        canvasGroup.DOFade(0, timeFade).OnComplete(() =>
        {
            gameObject.SetActive(false);
        }).SetUpdate(true);

    }
    private void OnDisable()
    {
        StopAllCoroutines();
        canvasGroup.DOKill();
    }
}
