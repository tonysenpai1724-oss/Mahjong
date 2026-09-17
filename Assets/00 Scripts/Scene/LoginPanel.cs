using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class LoginPanel : MonoBehaviour
{
    public TextMeshProUGUI txtAccount;
    public Button btnLogin, btnLogout, btnPlay;
    public bool LoginSuccess { get; private set; }
    public void ShowLoginPanel()
    {
        LoginSuccess = false;
        gameObject.SetActive(true);
        TigerForge.EventManager.StartListening(Constant.ON_LOGIN_INFO_UPDATE, InitUI);
        InitUI();
    }
    public void HideLoginPanel()
    {
        gameObject.SetActive(false);
        TigerForge.EventManager.StopListening(Constant.ON_LOGIN_INFO_UPDATE, InitUI);
        LoginSuccess = true;
    }
    void InitUI()
    {
        txtAccount.text = "";
#if UNITY_WEBGL
        if (PrivyManager.Instance.LoggedInPrivy())
        {
            txtAccount.text = $"Logged In To Account:\n{PrivyManager.Instance.privyUId}";
            btnLogin.gameObject.SetActive(false);
            btnLogout.gameObject.SetActive(true);
            btnPlay.gameObject.SetActive(true);
        }
        else
        {
            txtAccount.text = "Please Login Or Create New Account";
            btnLogin.gameObject.SetActive(true);
            btnLogout.gameObject.SetActive(false);
            btnPlay.gameObject.SetActive(false);
        }
#endif
    }
    public void OnClickLogin()
    {

#if UNITY_WEBGL
        PrivyManager.Instance.Login();
#endif
    }
    public void OnClickLogout()
    {

#if UNITY_WEBGL
        PrivyManager.Instance.Logout();
#endif
    }
    public void OnClickPlay()
    {
        HideLoginPanel();
    }
}
