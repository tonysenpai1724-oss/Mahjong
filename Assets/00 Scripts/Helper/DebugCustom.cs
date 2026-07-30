using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;

public class DebugCustom
{
    public static bool IsLogConsole
    {
        get
        {
#if UNITY_EDITOR
            return true;
#endif
            switch (GameManager.Instance.BuildType)
            {
                case EBuildType.Publish:
                    return false;
                case EBuildType.Dev:
                case EBuildType.Local:
                    return true;
                default:
                    break;
            }

            return false;
        }
    }

    private static bool _isLogBug = true;

    private static bool IsLogBug
    {
        get
        {
            if (!_isLogBug)
                return false;
            return true;
        }
    }

    public static void LogConsole(params object[] content)
    {
        if (!IsLogConsole)
            return;
        string str = PrepareString(content);
        Debug.Log(str);
        //if (UIManager.Instance)
        //    UIManager.Instance.ShowDialog(str);
    }

    public static void LogErrorConsole(params object[] content)
    {
        if (!IsLogConsole)
            return;
        string str = PrepareString(content);
        Debug.LogError(str);
        //if (UIManager.Instance)
        //    UIManager.Instance.ShowDialog(str);
    }

    public static void LogWarning(params object[] content)
    {
#if UNITY_EDITOR
        if (!IsLogBug)
            return;
        string str = PrepareString(content);
        Debug.LogWarning(str);
#endif
    }

    public static void Log(params object[] content)
    {
#if UNITY_EDITOR
        if (!IsLogBug)
            return;
        string str = PrepareString(content);
        Debug.Log(str);
#endif
    }
#if UNITY_EDITOR
    public static string ReturnLog(params object[] content)
    {
        string str = PrepareString(content);
        Debug.Log(str);
        return str;
    }
#endif
    public static void LogError(params object[] content)
    {
#if UNITY_EDITOR
        if (!IsLogBug)
            return;
        string str = PrepareString(content);
        Debug.LogError(str);
#endif
    }

    public static void LogColor(params object[] content)
    {
#if UNITY_EDITOR || GAME_ROCKET
        if (!IsLogBug)
            return;
        string str = PrepareString(content);
        Debug.Log("<color=\"" + "#ffa500ff" + "\">" + str + "</color>");
#endif
    }

    public static void LogColorJson(params object[] content)
    {
#if UNITY_EDITOR
        if (!IsLogBug)
            return;
        string str = PrepareStringJson(content);
        Debug.Log("<color=\"" + "#ffa500ff" + "\">" + str + "</color>");
#endif
    }

    public static void LogJson(params object[] content)
    {
#if UNITY_EDITOR
        if (!IsLogBug)
            return;
        string str = PrepareStringJson(content);
        Debug.Log(str);
#endif
    }

    public static void LogErrorJson(params object[] content)
    {
#if UNITY_EDITOR
        if (!IsLogBug)
            return;
        string str = PrepareStringJson(content);
        Debug.LogError(str);
#endif
    }

    public static string PrepareString(object[] content)
    {
        string str = "";
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] is Exception)
            {
                Debug.LogException(content[i] as Exception);
                return (content[i] as Exception).Message;
            }
            if (i == content.Length - 1)
            {
                str += content[i].ToString();
            }
            else
            {
                str += content[i].ToString() + "__";
            }
        }
        return str;
    }
    public static string PrepareStringJson(object[] content)
    {
        string str = "";
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] is Exception)
            {
                Debug.LogException(content[i] as Exception);
                return (content[i] as Exception).Message;
            }
            if (i == content.Length - 1)
            {
                str += JsonConvert.SerializeObject(content[i]);
            }
            else
            {
                str += JsonConvert.SerializeObject(content[i]) + "__";
            }
        }
        return str;
    }
    static DateTime cachedTime;
    static long cachedGCBytes;

    public static void StartProfilerFunction()
    {
        cachedTime = DateTime.Now;
        cachedGCBytes = GC.GetTotalMemory(true);
    }

    public static void LogProfilerFunction(string funcName)
    {
        TimeSpan time = DateTime.Now - cachedTime;
        long totalBytes = GC.GetTotalMemory(true) - cachedGCBytes;
        LogError($"Function {funcName} Cost: {totalBytes} bytes GCAlloc, {time.TotalMilliseconds} Ticks Execute");
    }
}