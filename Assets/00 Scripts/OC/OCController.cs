using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OCController : Singleton<OCController>
{
    public void InitFirstTime()
    {
        OCBridge.InitFirstTime();
    }
    public void GetProcessQuest()
    {
        OCBridge.GetProcessQuest(name, nameof(OnGetProcessQuest));
    }
    public void GetClaimQuest()
    {
        OCBridge.GetClaimQuest(name, nameof(OnGetClaimQuest));
    }
    public void SetGameData(string json)
    {
        Debug.Log("SetGameData: " + json);
        OCBridge.SetGameData(json);
    }
    public void GetGameData()
    {
        OCBridge.GetGameData(name, nameof(OnGetGameData));
    }
    public void OnGetProcessQuest(string json)
    {
        Debug.Log("OnGetProcessQuest: " + json);
    }
    public void OnGetClaimQuest(string json)
    {
        Debug.Log("OnGetClaimQuest: " + json);
    }
    public void OnGetGameData(string json)
    {
        Debug.Log("OnGetGameData: " + json);
    }
}
