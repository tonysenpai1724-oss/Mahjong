using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class OCBridge
{
    [DllImport("__Internal")] public static extern void InitFirstTime();
    [DllImport("__Internal")] public static extern void GetProcessQuest(string gameObjectName, string callbackMethod);
    [DllImport("__Internal")] public static extern void GetClaimQuest(string gameObjectName, string callbackMethod);
    [DllImport("__Internal")] public static extern void SetGameData(string json);
    [DllImport("__Internal")] public static extern void GetGameData(string gameObjectName, string callbackMethod);

}
