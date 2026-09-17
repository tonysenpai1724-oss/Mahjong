using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

public static class TelegramManager
{
#if UNITY_WEBGL
[DllImport("__Internal")]
    private static extern void Telegram_InitSDK();

    private static bool sdkInitialized = false;
    public static void InitializeSDK()
    {
#if  !UNITY_EDITOR
        Telegram_InitSDK();
        sdkInitialized = true;
#endif
    }
    // Đóng Web App
    [DllImport("__Internal")]
    private static extern void Telegram_Close();
    public static void Close()
    {
#if  !UNITY_EDITOR
        Telegram_Close();
#endif
    }

    // Mở hóa đơn (Invoice) với callback
    [DllImport("__Internal")]
    private static extern void Telegram_OpenInvoice(string url);
    public static void OpenInvoice(string url)
    {

#if UNITY_WEBGL
#if UNITY_EDITOR
        Debug.Log($"Opening invoice URL: {url}");
        Application.OpenURL(url);
        return;
#endif
        if (!sdkInitialized)
        {
            Debug.LogError("SDK is not initialized. Please initialize the SDK before calling OpenInvoice.");
            return;
        }
        Telegram_OpenInvoice(url);
#endif
    }

    // Mở liên kết Telegram
    [DllImport("__Internal")]
    private static extern void Telegram_OpenTelegramLink(string url);
    public static void OpenTelegramLink(string url)
    {
#if  !UNITY_EDITOR
        Telegram_OpenTelegramLink(url);
#else
        Application.OpenURL(url);
#endif
    }

    // Chia sẻ URL
    [DllImport("__Internal")]
    private static extern void Telegram_ShareURL(string url, string text);
    public static void ShareURL(string url, string text)
    {
#if  !UNITY_EDITOR
        Telegram_ShareURL(url, text);
#endif
    }
#endif
}
