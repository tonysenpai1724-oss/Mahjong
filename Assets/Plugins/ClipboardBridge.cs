using System.Runtime.InteropServices;
using UnityEngine;

public class ClipboardBridge : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CopyToClipboard(string text);
#endif

    public static void CopyText(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CopyToClipboard(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif

        Debug.Log("Text copied: " + text);
    }
}
