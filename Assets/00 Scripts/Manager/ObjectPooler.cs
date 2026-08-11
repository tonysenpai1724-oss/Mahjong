using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Pooling
{
    public Queue<PoolingObject> pool;
    List<PoolingObject> cachedObj;
    PoolingObject refPrefabs;

    public Pooling(PoolingObject _refPrefabs)
    {
        refPrefabs = _refPrefabs;
        pool = new Queue<PoolingObject>();
        cachedObj = new List<PoolingObject>();
    }

    public PoolingObject GetObject()
    {
        PoolingObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            //DebugCustom.LogColor($"Pooling Object Get {obj.name}");
        }
        else
        {
            obj = Object.Instantiate(refPrefabs);
            //DebugCustom.LogColor($"Instantiate Object {obj.name}");
        }

        return obj;
    }

    public void SetObject(PoolingObject obj)
    {
        cachedObj.Add(obj);
        //DebugCustom.LogColor($"Pooling Object Set {obj.name}");
    }

    public void OnUpdate()
    {
        foreach (PoolingObject obj in cachedObj)
        {
            if (!pool.Contains(obj))
                pool.Enqueue(obj);
        }

        cachedObj.Clear();
    }

    public void ClearPool()
    {
        while (pool.Count > 0)
        {
            PoolingObject o = pool.Dequeue();
            Object.Destroy(o);
        }
    }
}

public class ObjectPooler : Singleton<ObjectPooler>
{
    /// <summary>
    /// delay for clear pool (secs)
    /// </summary>
    public float delayClear = 600;

    Dictionary<string, Pooling> dicPooling;
    Dictionary<string, float> dicCountDown;

    Dictionary<EAnimationEffectType, ParticleSystem> dicEffectInUse;

    public override void Awake()
    {
        base.Awake();
        dicPooling = new Dictionary<string, Pooling>();
        dicCountDown = new Dictionary<string, float>();
        dicEffectInUse = new Dictionary<EAnimationEffectType, ParticleSystem>();
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_UPDATE, OnUpdate);
    }

    void OnTick()
    {
        var keys = dicPooling.Keys.ToList();
        foreach (var key in keys)
        {
            dicCountDown[key] -= 1;
            if (dicCountDown[key] <= 0)
                ClearPool(key);
        }
    }

    private void OnUpdate()
    {
        foreach (var item in dicPooling)
        {
            item.Value.OnUpdate();
        }
    }

    public PoolingObject GetObject(PoolingObject _refPrefabs)
    {
        if (!dicPooling.ContainsKey(_refPrefabs.poolingKey))
        {
            dicPooling.Add(_refPrefabs.poolingKey, new Pooling(_refPrefabs));
            dicCountDown.Add(_refPrefabs.poolingKey, delayClear);
        }

        dicCountDown[_refPrefabs.poolingKey] = delayClear;
        return dicPooling[_refPrefabs.poolingKey].GetObject();
    }

    public void DespawnObj(PoolingObject obj)
    {
        if (dicPooling.ContainsKey(obj.poolingKey))
            dicPooling[obj.poolingKey].SetObject(obj);
        else
            Destroy(obj);
    }

    public void ClearAll()
    {
        foreach (var item in dicPooling)
        {
            item.Value.ClearPool();
        }

        dicPooling.Clear();
        dicCountDown.Clear();
    }

    public void ClearPool(string key)
    {
        if (dicPooling.ContainsKey(key))
        {
            dicPooling[key].ClearPool();
            dicPooling.Remove(key);
            dicCountDown.Remove(key);
        }
    }

    public void PlayGameFx(EAnimationEffectType fxType, Vector3 pos, float scale = 1, float rotation = 0, int emit = 1)
    {
        if (fxType == EAnimationEffectType.None)
            return;
        if (!dicEffectInUse.ContainsKey(fxType))
            dicEffectInUse.Add(fxType,
                Instantiate(DataSystem.Instance.dataAnimationEffect.dicEffect[fxType], this.transform));
        ParticleSystem fx = dicEffectInUse[fxType];
        fx.transform.position = pos;
        for (int i = 0; i < emit; i++)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.startSize = scale;
            emitParams.rotation = rotation;
            fx.Emit(emitParams, 1);
        }
    }

    public static T Spawn<T>(T refPrefabs, Transform parent = null) where T : PoolingObject
    {
        if (Instance == null || refPrefabs == null)
            return null;

        T obj = Instance.GetObject(refPrefabs) as T;
        if (obj == null)
            return null;

        obj.transform.SetParent(parent, true);
        obj.transform.localPosition = Vector3.zero;
        obj.OnSpawn();
        return obj;
    }

    public static T Spawn<T>(T refPrefabs, Vector3 pos, Transform parent = null) where T : PoolingObject
    {
        T obj = Spawn(refPrefabs, parent);
        obj.transform.position = pos;
        return obj;
    }

    public static void Despawn<T>(T obj) where T : PoolingObject
    {
        Instance?.DespawnObj(obj);
    }
}
