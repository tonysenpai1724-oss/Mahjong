using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;
using System.Linq;
using Sirenix.OdinInspector;
using Newtonsoft.Json;
public class AccountManager : Singleton<AccountManager>
{
    public string UserToken
    {
        get
        {
#if UNITY_WEBGL
            if (GameManager.Instance.Platform == EPlatform.Telegram)
                return tele_user_token;
            else
                return PrivyManager.Instance.loginInfo.accessToken;
#endif
            return tele_user_token;
        }
    }
    string tele_user_token;
    public string UserId { get; private set; }

    public string URL { get; private set; }
    public DateTime TokenExpireDate { get; private set; }
    public TelegramUserInfo TelegramUserInfo { get; private set; } = new TelegramUserInfo();
    public IEnumerator InitAccount()
    {
#if UNITY_WEBGL
        WebGLInput.captureAllKeyboardInput = false;
#endif
        LoadingPanel.Instance.ShowTextLoading("Get access Token");
#if LOCAL_BUILD
        Debug.Log("LocalBuild");
        yield break;
#endif
        var buildVersion = Resources.Load<BuildVersion>("BuildVersion");
        UserId = buildVersion.userId;

        switch (GameManager.Instance.Platform)
        {
            case EPlatform.Telegram:
#if UNITY_EDITOR
                yield return StartCoroutine(HTTPManager.Instance.LoginDev(buildVersion.userId, s =>
                {
                    tele_user_token = s.access_token;
                    //TokenExpireDate = Helper.ParseDateTime(s.expiresIn).AddMinutes(-5);
                }));
                yield return StartCoroutine(HTTPManager.Instance.GetUserInfoTele(s =>
                {
                    TelegramUserInfo = s;
                }));
                yield break;
#endif
                URL = Application.absoluteURL;
                yield return StartCoroutine(IELoginToServer());
                yield return StartCoroutine(HTTPManager.Instance.GetUserInfoTele(s =>
                {
                    TelegramUserInfo = s;
                }));
                break;
            case EPlatform.Privy:
#if UNITY_WEBGL && UNITY_EDITOR

                PrivyManager.Instance.OnListenLogin(UserId);
#endif
                LoadingPanel.Instance.loginPanel.ShowLoginPanel();
                yield return new WaitUntil(() => LoadingPanel.Instance.loginPanel.LoginSuccess);
                break;
            case EPlatform.Android:
                break;
            default:
                break;
        }
    }
    public IEnumerator IELoginToServer()
    {
        GameManager.Instance.GetUserDecodeFromUrl(URL);
        Debug.Log(URL);

        var initdata = TelegramUrlParser.ExtractSpecificQuery(URL);
        yield return StartCoroutine(HTTPManager.Instance.IE_LoginTelegram(initdata, GameManager.Instance.UserDecodeFromUrl.AppStartParam,
            s =>
            {
                tele_user_token = s.data.access_token;
                Debug.Log("UserToken: " + UserToken);
                JwtPayload payload = Helper.ParseJwt(UserToken);
                TokenExpireDate = Helper.ParseDateTime(payload.Expiration).AddMinutes(-5);
                Debug.Log("TokenExpireDate: " + TokenExpireDate);
                UserId = payload.TelegramId;
            }));
    }
}
