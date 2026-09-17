using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Sirenix.OdinInspector;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UI;
using DG.Tweening;
public class TestScripts : SerializedMonoBehaviour
{
    public CanvasGroup canvasGroup;
    public Image coinPrefabs;
    public float timePlayCoin = 1f;
    List<Image> lstCoin;
    private void Start()
    {
        lstCoin = new List<Image>();
        for (int i = 0; i < 100; i++)
        {
            Image coin = Instantiate(coinPrefabs, canvasGroup.transform);
            coin.gameObject.SetActive(false);
            lstCoin.Add(coin);
        }
        //canvasGroup.alpha = 0;
    }
    [Button()]
    public void Test()
    {
        StartCoroutine(IEPlayCoin());
    }
    IEnumerator IEPlayCoin()
    {
        //canvasGroup.alpha = 1;
        //canvasGroup.DOFade(0, timePlayCoin);
        foreach (var item in lstCoin)
        {
            item.transform.localScale = Vector3.one;
            item.gameObject.SetActive(true);
            item.color = new Color(1, 1, 1, 1);
            item.transform.localPosition = new Vector3(Random.Range(-2, 2), Random.Range(-2, 2), 0);
            float angle = Random.Range(0, 360);
            item.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360));
            item.transform.DOMove(Random.insideUnitCircle * 15, timePlayCoin / 2).SetEase(Ease.OutBack);
            item.transform.DOScale(Random.Range(2,2.5f), timePlayCoin / 2).SetEase(Ease.OutBack);
            item.DOFade(0, timePlayCoin / 2).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(timePlayCoin / lstCoin.Count);
        }
    }
}
public class TestClass
{
    public byte[] value;
}