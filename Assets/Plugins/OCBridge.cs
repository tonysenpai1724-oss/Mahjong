// using System.Collections;
// using System.Collections.Generic;
// using System.Runtime.InteropServices;
// using UnityEngine;

// public static class OCBridge
// {
//     [DllImport("__Internal")] public static extern void InitFirstTime();
//     [DllImport("__Internal")] public static extern void GetProcessQuest(string gameObjectName, string callbackMethod);
//     [DllImport("__Internal")] public static extern void GetClaimQuest(string gameObjectName, string callbackMethod);
//     [DllImport("__Internal")] public static extern void SetGameData(string json);
//     [DllImport("__Internal")] public static extern void GetGameData(string gameObjectName, string callbackMethod);

// }
using System.Runtime.InteropServices;
using UnityEngine;

public static class OCBridge
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] public static extern void InitFirstTime();
    [DllImport("__Internal")] public static extern void GetProcessQuest(string gameObjectName, string callbackMethod);
    [DllImport("__Internal")] public static extern void GetClaimQuest(string gameObjectName, string callbackMethod);
    [DllImport("__Internal")] public static extern void SetGameData(string json);
    [DllImport("__Internal")] public static extern void GetGameData(string gameObjectName, string callbackMethod);
#else
    public static void InitFirstTime()
    {
        Debug.Log("InitFirstTime not supported on this platform");
    }

    public static void GetProcessQuest(string gameObjectName, string callbackMethod)
    {
        Debug.Log("GetProcessQuest not supported on this platform");
    }

    public static void GetClaimQuest(string gameObjectName, string callbackMethod)
    {
        Debug.Log("GetClaimQuest not supported on this platform");
    }

    public static void SetGameData(string json)
    {
        Debug.Log("SetGameData not supported on this platform");
    }

    public static void GetGameData(string gameObjectName, string callbackMethod)
    {
        Debug.Log("GetGameData not supported on this platform");
    }
#endif
}