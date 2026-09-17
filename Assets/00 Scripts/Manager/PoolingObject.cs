using UnityEngine;
using DG.Tweening;

public class PoolingObject : MonoBehaviour
{
    public string poolingKey;
    public System.Action _OnDespawn, _OnSpawn;
    protected bool activing;

    public virtual void OnSpawn()
    {
        gameObject.SetActive(true);
        activing = true;
        _OnSpawn?.Invoke();
    }

    public virtual void Despawn()
    {
        activing = false;
        gameObject.SetActive(false);
        ObjectPooler.Despawn(this);
        _OnDespawn?.Invoke();
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        transform.DOKill();
    }
}