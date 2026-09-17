using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Web;

public enum EHTTPRequest
{
    POST,
    GET,
    DELETE,
    PUT,
}

public class HTTPManager : Singleton<HTTPManager>
{
    public static string botUrl
    {
        get
        {
            switch (GameManager.Instance.BuildType)
            {
                case EBuildType.Publish:
                    break;
                case EBuildType.Dev:
                    break;
                case EBuildType.Local:
                    break;
                default:
                    break;
            }
            return "https://t.me/aaabbbcccdddd_test_bot";
        }
    }
    public static string host
    {
        get
        {
            switch (GameManager.Instance.BuildType)
            {
                case EBuildType.Publish:
                    break;
                case EBuildType.Dev:
                    break;
                case EBuildType.Local:
                    return "http://localhost:8080";
                default:
                    break;
            }

            return "https://st-auth-privy-game-lab-api.saworld.io";
        }
    }

    public static string hostTele
    {
        get
        {
            switch (GameManager.Instance.BuildType)
            {
                case EBuildType.Publish:
                    break;
                case EBuildType.Dev:
                    break;
                default:
                    break;
            }

            return "https://st-nut-bolt-auth-lab-api.saworld.io";
        }
    }

    public static string GetRefLink(string refCode)
    {
        return $"{botUrl}/Playgame?startApp={refCode}&startapp={refCode}";
    }
    public IEnumerator SendHTTPRequest(string url, EHTTPRequest requestType, string jsonData, Dictionary<string, string> customHeader = null,
    Action<string> actionComplete = null, Action<string> actionError = null)
    {
        DebugCustom.LogConsole(url);
        DebugCustom.LogConsole(requestType.ToString());
        DebugCustom.LogConsole(jsonData);
        using (UnityWebRequest request = new UnityWebRequest(url, requestType.ToString()))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if(customHeader != null)
            {
                foreach (var item in customHeader)
                {
                    request.SetRequestHeader(item.Key, item.Value);
                }
            }
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                DebugCustom.LogConsole(url);
                DebugCustom.LogErrorConsole("Result: " + request.result);
                DebugCustom.LogErrorConsole("Error: " + request.error);
                actionError?.Invoke(request.error);
            }
            else
            {
                DebugCustom.LogConsole("Response: " + request.downloadHandler.text);
                actionComplete?.Invoke(request.downloadHandler.text);
            }
        }
    }
    private IEnumerator SendRequest(string url, EHTTPRequest requestType, string jsonData,
        Action<string> actionComplete = null, Action<string> actionError = null, bool blockButton = false,
        bool needToken = true, bool needBearer = true)
    {
        if (blockButton)
        {
            CommonButton.DisableAll?.Invoke();
            LoadingPanel.Instance.ShowWaitNetworkPanel();
        }
        Dictionary<string, string> customHeader = new Dictionary<string, string>();
        if (needToken)
        {
            string authToken = needBearer ? "Bearer " : "";
            authToken += AccountManager.Instance.UserToken;
            customHeader.Add("Authorization", authToken);
        }
        yield return StartCoroutine(SendHTTPRequest(url, requestType, jsonData, customHeader, s =>
        {
            MessageResponse response = JsonConvert.DeserializeObject<MessageResponse>(s);

            if (response.status || response.success || response.code == 200)
                actionComplete?.Invoke(s);
            else
            {
                DebugCustom.LogErrorConsole($"Response Fail: {url}");
                DebugCustom.LogErrorJson(response);
                actionError?.Invoke(response.message);
            }
        }, actionError));
        if (blockButton)
        {
            CommonButton.EnableAll?.Invoke();
            LoadingPanel.Instance.HideWaitNetworkPanel();
        }
    }

    IEnumerator IELoadImage(string url, System.Action<Sprite> actionComplete)
    {
        DebugCustom.LogColor(url);
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                actionComplete?.Invoke(sprite);
            }
            else
            {
                Debug.LogError("Failed to load image: " + request.error);
            }
        }
    }

    public void LoadImage(string url, System.Action<Sprite> actionComplete)
    {
        StartCoroutine(IELoadImage(url, actionComplete));
    }

    //public IEnumerator IEGetMaintanceInfo(System.Action<MaintanceInfoResponse> actionComplete)
    //{
    //    string api = $"/v1/maintain/status";
    //    yield return (StartCoroutine(SendRequest($"{hostTele}{api}", EHTTPRequest.GET, "", s =>
    //    {
    //        MaintanceInfoResponse response = JsonConvert.DeserializeObject<MaintanceInfoResponse>(s);
    //        actionComplete?.Invoke(response);
    //    })));
    //}
    #region Login
    //#if UNITY_EDITOR
    public IEnumerator LoginDev(string userId, Action<LoginResponse> actionComplete = null)
    {
        string api = "/api/v1/telegram-app/login-demo-dev";
        LoginDemoRequest request = new LoginDemoRequest
        {
            telegramId = userId,
        };

        string jsonData = JsonConvert.SerializeObject(request);

        int count = 5;
        while (count > 0)
        {
            count--;
            MessageDataResponse<LoginResponse> login = null;
            bool delayRequest = true;

            StartCoroutine(SendRequest($"{hostTele}{api}", EHTTPRequest.POST, jsonData, s =>
            {
                login = JsonConvert.DeserializeObject<MessageDataResponse<LoginResponse>>(s);
                delayRequest = false;
            }, e => { delayRequest = false; }, false));

            yield return new WaitUntil(() => !delayRequest);
            if (login != null)
            {
                actionComplete?.Invoke(login.data);
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator IE_LoginTelegram(string initData, string refcode, Action<LoginTelegramResponse> actionComplete)
    {
        string api = "/api/v1/telegram-app/login";
        LoginTelegramRequest request = new LoginTelegramRequest
        {
            init_data = initData,
            refCode = refcode,
        };

        string jsonData = JsonConvert.SerializeObject(request);

        int count = 5;
        while (count > 0)
        {
            count--;
            LoginTelegramResponse login = null;
            bool delayRequest = true;

            StartCoroutine(SendRequest($"{hostTele}{api}",
                EHTTPRequest.POST, jsonData, s =>
                {
                    login = JsonConvert.DeserializeObject<LoginTelegramResponse>(s);
                    delayRequest = false;
                }, e => { delayRequest = false; }, false));

            yield return new WaitUntil(() => !delayRequest);
            if (login != null)
            {
                actionComplete?.Invoke(login);
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    //public IEnumerator LoginRequest(Action<string> actionComplete, Action<string> actionError)
    //{
    //    string api = $"/v1/auth/check_login";
    //    yield return (StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.GET, "", actionComplete,
    //        actionError, true, false)));
    //}

    #endregion

    #region User Info

    public IEnumerator GetUserInfoTele(Action<TelegramUserInfo> actionComplete, Action<string> actionError = null)
    {
        string api = $"/api/v1/telegram-app/user/info";

        int count = 5;
        while (count > 0)
        {
            count--;
            TelegramUserInfo userInfoBase = null;
            bool delayRequest = true;

            yield return StartCoroutine(SendRequest($"{hostTele}{api}", EHTTPRequest.GET, "", s =>
            {
                userInfoBase = JsonConvert.DeserializeObject<MessageDataResponse<TelegramUserInfo>>(s).data;
                delayRequest = false;
            }, error =>
            {
                actionError?.Invoke(error);
                delayRequest = false;
            }));

            yield return new WaitUntil(() => !delayRequest);
            if (userInfoBase != null)
            {
                actionComplete?.Invoke(userInfoBase);
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator GetUserInfo(Action<UserInfo> actionComplete, Action<string> actionError = null)
    {
        string api = $"/v1/user/info";

        int count = 5;
        while (count > 0)
        {
            count--;
            UserInfo userInfoBase = null;
            bool delayRequest = true;

            yield return StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.GET, "", s =>
            {
                userInfoBase = JsonConvert.DeserializeObject<MessageDataResponse<UserInfo>>(s).data;
                delayRequest = false;
            }, error =>
            {
                actionError?.Invoke(error);
                delayRequest = false;
            }));

            yield return new WaitUntil(() => !delayRequest);
            if (userInfoBase != null)
            {
                actionComplete?.Invoke(userInfoBase);
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public void UpdateUserInfo(UpdateInfoRequest request, Action<string> actionComplete = null, Action<string> actionError = null)
    {
        string api = $"/v1/user/info";
        string jsonData = JsonConvert.SerializeObject(request);
        StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.PUT, jsonData, actionComplete, actionError));
    }
    #endregion


    #region IAP
    public IEnumerator SendInvoiceLinks(CreateInvoiceLinkMessage message,
        Action<string> actionError = null)
    {
        string api = $"/api/v1/telegram-app/purchase/send-invoice";
        string jsonData = JsonConvert.SerializeObject(message);
        yield return StartCoroutine(SendRequest($"{hostTele}{api}", EHTTPRequest.POST, jsonData));
    }
    public void IEGenerateInvoiceLinks(CreateInvoiceLinkMessage message, Action<InvoiceLinksResponse> actionComplete,
        Action<string> actionError = null)
    {
        string api = $"/api/v1/telegram-app/purchase/create-invoice-links";
        string jsonData = JsonConvert.SerializeObject(message);
        StartCoroutine(SendRequest($"{hostTele}{api}", EHTTPRequest.POST, jsonData, s =>
        {
            InvoiceLinksResponse response = JsonConvert.DeserializeObject<InvoiceLinksResponse>(s);
            actionComplete?.Invoke(response);
        }, actionError, false, false, false));
    }

    public void GetIAPHistory(GetIAPHistoryRequest request, Action<IAPHistoryResponse> actionComplete, Action<string> actionError = null)
    {
        string api = $"/v1/iap/history";
        StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.POST, JsonConvert.SerializeObject(request), s =>
        {
            IAPHistoryResponse response = JsonConvert.DeserializeObject<IAPHistoryResponse>(s);
            actionComplete?.Invoke(response);
        }, actionError));
    }

    #endregion

    #region Leaderboard
    public void GetLeaderboardHistory(string key, Action<Dictionary<int, int>> actionComplete, Action<string> actionError = null)
    {
        string api = $"/v1/leaderboard/history";
        LeaderboardRequest request = new LeaderboardRequest
        {
            key = key
        };
        StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.POST, JsonConvert.SerializeObject(request), s =>
        {
            MessageDataResponse<Dictionary<int, int>> response = JsonConvert.DeserializeObject<MessageDataResponse<Dictionary<int, int>>>(s);
            actionComplete?.Invoke(response.data);
        }, actionError));
    }
    public void GetLeaderboard(string key, Action<LeaderBoardInfo> actionComplete, System.Action<string> actionError = null)
    {
        string api = $"/v1/leaderboard/list";
        GetLeaderboardListRequest request = new GetLeaderboardListRequest
        {
            key = key
        };
        StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.POST, JsonConvert.SerializeObject(request), s =>
        {
            MessageDataResponse<LeaderBoardInfo> response = JsonConvert.DeserializeObject<MessageDataResponse<LeaderBoardInfo>>(s);
            actionComplete?.Invoke(response.data);
        }, actionError));
    }
    public void UpdateLeaderboardScore(string key, int score, Action<string> actionComplete = null, Action<string> actionError = null)
    {
        string api = $"/v1/leaderboard/update";
        UpdateLeaderboardRequest request = new UpdateLeaderboardRequest
        {
            key = key,
            score = score
        };
        StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.POST, JsonConvert.SerializeObject(request), actionComplete, actionError));
    }
    #endregion

    #region Analystic

    public void LogEventAnalystic(string eventName, Dictionary<string, string> parameters)
    {
        //        if (GameManager.Instance.BuildType == EBuildType.Local)
        //            return;
        //#if UNITY_EDITOR
        //        return;
        //#endif
        //        //FirebaseWebGL.Scripts.FirebaseAnalytics.FirebaseAnalytics.LogEventParameter(eventName,
        //        //    JsonConvert.SerializeObject(parameters));
        //        return;
        //        string api = $"/v1/analytics";
        //        AnalysticMessage message = new AnalysticMessage(new AnalysticEvent(eventName, parameters));
        //        StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.POST, JsonConvert.SerializeObject(message)));
    }

    #endregion

    #region CommonData
    public void GetCommonData(string key, Action<string> actionComplete,
        Action<string> actionError)
    {
        string api = $"/v1/user/data";
        GetCommonDataRequest request = new GetCommonDataRequest
        {
            key = key
        };
        StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.POST, JsonConvert.SerializeObject(request), s =>
        {
            MessageDataResponse<CommonDataResponse> response = JsonConvert.DeserializeObject<MessageDataResponse<CommonDataResponse>>(s);
            actionComplete?.Invoke(response.data.data);
        }, actionError));
    }
    public IEnumerator SetCommonData(string key, string data)
    {
        string api = $"/v1/user/data/update";
        SetCommonDataRequest request = new SetCommonDataRequest
        {
            key = key,
            data = data
        };
        yield return StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.POST, JsonConvert.SerializeObject(request)));
    }
    public void GetCommonConfig(Action<Dictionary<string, string>> actionComplete,
        Action<string> actionError)
    {
        string api = $"/v1/client/configs";
        StartCoroutine(SendRequest($"{host}{api}", EHTTPRequest.GET, "", s =>
        {
            MessageDataResponse<Dictionary<string, string>> response = JsonConvert.DeserializeObject<MessageDataResponse<Dictionary<string, string>>>(s);
            actionComplete?.Invoke(response.data);
        }, actionError, false, false));
    }
    #endregion
}

#region Telegram URL Param

public static class TelegramUrlParser
{
    public static string ExtractSpecificQuery(string url)
    {
        string[] _pairQuerry = url.Split("tgWebAppData=");

        string queryString = _pairQuerry[1].Replace("%3D", "=").Replace("%26", "&").Replace("%25", "%");
        string[] pairs = queryString.Split('&');
        string result = string.Empty;

        foreach (string pair in pairs)
        {
            if (pair.StartsWith("query_id=") || pair.StartsWith("user=") || pair.StartsWith("auth_date=") ||
                pair.StartsWith("signature=") || pair.StartsWith("hash=")
                || pair.StartsWith("chat_instance=") || pair.StartsWith("chat_type=") ||
                pair.StartsWith("start_param="))
            {
                result += (result == string.Empty ? "" : "&") + pair;
            }
        }

        return result;
    }

    public static TelegrameUrlData GetUserDecodeFromUrl(string url)
    {
        bool hasStartParam = url.Contains("?tgWebAppStartParam=");
        string fragment1 = hasStartParam ? url.Split("?tgWebAppStartParam=")[1] : url;
        if (!fragment1.Contains("#"))
        {
            Debug.LogError("No fragment found in the URL.");
            return new TelegrameUrlData();
        }

        string[] fragments = fragment1.Split('#');
        string appStartParam = hasStartParam ? fragments[0] : "Direct_Login";
        string fragment = fragments[1];
        if (string.IsNullOrEmpty(fragment))
        {
            Debug.LogError("No fragment found in the URL.");
            return new TelegrameUrlData();
        }

        var queryParams1 = HttpUtility.ParseQueryString(fragment);
        string encodedData = queryParams1.Get("tgWebAppData");
        if (string.IsNullOrEmpty(encodedData))
        {
            DebugCustom.LogConsole("No tgWebAppData found in the URL.");
            return new TelegrameUrlData();
        }

        DebugCustom.LogColor(encodedData);
        string decodedData = HttpUtility.UrlDecode(encodedData);
        DebugCustom.LogColor(decodedData);
        var queryParams = HttpUtility.ParseQueryString(decodedData);

        // Lấy từng giá trị
        string queryId = queryParams["query_id"];
        string userJson = queryParams["user"];
        string authDateStr = queryParams["auth_date"];
        string signature = queryParams["signature"];
        string hash = queryParams["hash"];

        // Deserialize user JSON
        TelegrameUrlUser user = JsonConvert.DeserializeObject<TelegrameUrlUser>(userJson);

        user.AppStartParam = appStartParam;

        // Parse AuthDate
        long.TryParse(authDateStr, out long authDate);

        // Tạo đối tượng QueryParameters
        TelegrameUrlData queryParameters = new TelegrameUrlData
        {
            QueryId = queryId,
            User = user,
            AuthDate = authDate,
            Signature = signature,
            Hash = hash
        };
        DebugCustom.LogConsole(queryParameters);
        return queryParameters;
    }
}

public class TelegrameUrlUser
{
    [JsonProperty("id")] public long Id { get; set; }

    [JsonProperty("first_name")] public string FirstName { get; set; }

    [JsonProperty("last_name")] public string LastName { get; set; }

    [JsonProperty("username")] public string Username { get; set; }

    [JsonProperty("language_code")] public string LanguageCode { get; set; }

    [JsonProperty("allows_write_to_pm")] public bool AllowsWriteToPm { get; set; }

    [JsonProperty("photo_url")] public string PhotoUrl { get; set; }

    [JsonProperty("tgWebAppStartParam")] public string AppStartParam { get; set; }
}

public class TelegrameUrlData
{
    public string QueryId { get; set; }
    public TelegrameUrlUser User { get; set; }
    public long AuthDate { get; set; }
    public string Signature { get; set; }
    public string Hash { get; set; }
}

#endregion