using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class UIFlyingObject : PoolingObject
{
    public SpriteRenderer icon;
    public AnimationCurve flyCurve;
    public float flyTime;
    public float flyTimeRandom;
    public void StartAnim(Sprite spr, float delay)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
        icon.sprite = spr;
        StartCoroutine(IEFlyingEffect(delay));
    }
    IEnumerator IEFlyingEffect(float delay)
    {
        yield return new WaitForSeconds(delay);
        List<Vector3> lstPath = new List<Vector3>();
        lstPath.Add(transform.localPosition);
        lstPath.Add(transform.localPosition / 2 + (Vector3)Random.insideUnitCircle * 3);
        lstPath.Add(Vector3.zero);
        transform.DOLocalPath(lstPath.ToArray(), flyTime * Random.Range(1 - flyTimeRandom, 1 + flyTimeRandom), PathType.CatmullRom).SetEase(flyCurve).OnComplete(() =>
        {
            Despawn();
            AudioManager.Instance.PlaySfx(ESfx.ReceiveCoinSfx);
        });
    }
}
