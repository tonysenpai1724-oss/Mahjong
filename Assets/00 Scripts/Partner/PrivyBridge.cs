using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
#if UNITY_WEBGL
public static class PrivyBridge
{
    [DllImport("__Internal")]
    public static extern int IsMobile();

    [DllImport("__Internal")]
    public static extern void PrivyInitModule(string objectName, string methodName);

    [DllImport("__Internal")]
    public static extern void PrivyLogin();

    [DllImport("__Internal")]
    public static extern void PrivyLogout();

    [DllImport("__Internal")]
    public static extern void PrivyAccountInfo();

    [DllImport("__Internal")]
    public static extern void PrivyExportWallet();

    [DllImport("__Internal")]
    public static extern void PrivyCallContractActionLog();

    [DllImport("__Internal")]
    public static extern void ListenToResize(string objectName, string methodName);

    [DllImport("__Internal")]
    public static extern void CheckRotateScreen();
}
#endif
