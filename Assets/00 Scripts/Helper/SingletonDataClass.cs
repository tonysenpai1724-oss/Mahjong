using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SingletonDataClass<T, D>
    where T : SingletonDataClass<T, D>, new()
    where D : SingletonCachedData, new()
{
    public static T Instance { get; } = new T();
    protected D cachedData;
    protected bool waitSaveData;
    protected abstract string KeyData();
    protected bool firstTimeInited;
    public string _KeyData => KeyData();
    public virtual void Init()
    {
        //if (!CPlayerPrefs.HasKey(KeyData()) || GameManager.Instance.IsnewPlayer)
        //{
        //    cachedData = new D();
        //    OnNewData();
        //}
        //else
        //{
        //    cachedData = Newtonsoft.Json.JsonConvert.DeserializeObject<D>(CPlayerPrefs.GetString(KeyData()));
        //}
        if (cachedData == null)
        {
            if (string.IsNullOrEmpty(GameManager.Instance.GetData(KeyData()))/* || GameManager.Instance.IsNewPlayer*/)
            {
                cachedData = new D();
                OnNewData();
            }
            else
            {
                cachedData = Newtonsoft.Json.JsonConvert.DeserializeObject<D>(GameManager.Instance.GetData(KeyData()));
            }
        }
        OnInitSuccess();
        SaveData();
    }
    public void SetCacheData(D cachedData)
    {
        this.cachedData = cachedData;
    }
    protected virtual void OnNewData()
    {

    }
    protected virtual void OnInitSuccess()
    {
        if (!firstTimeInited)
        {
            firstTimeInited = true;
            FirstTimeInit();
        }
    }
    protected virtual void FirstTimeInit()
    {
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_UPDATE, OnUpdate);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_NEW_DAY, OnNextDay);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
    }
    protected virtual void OnTick()
    {

    }
    protected virtual void OnNextDay()
    {

    }
    protected virtual void OnUpdate()
    {
        if (waitSaveData)
            SaveData();
    }
    protected virtual void SaveData()
    {
        if(SaveAsMetaData())
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(cachedData);
            GameManager.Instance.SaveData(KeyData(), json);
            //CPlayerPrefs.SetString(KeyData(), json);
        }
        waitSaveData = false;
    }
    protected virtual bool SaveAsMetaData()
    {
        return true;
    }
    public virtual void OnValueChange(bool log = true)
    {
        waitSaveData = true;
    }
}
public class SingletonCachedData
{

}