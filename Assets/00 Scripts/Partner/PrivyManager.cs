using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using UnityEngine;
#if UNITY_WEBGL
public class PrivyManager : Singleton<PrivyManager>
{
    // App Info


    public const string app_id = "cm665nz6402e36mzfszpg61o8";

    public const string client_id = "client-WY5g5RZRRxFf51QKMN6iwHiZZoSjAp2vxU1JpdZ7hu8h9";

    string accessToken = "";
    string refreshToken = "";
    string ca_id = "";
    string url_origin = "";

    public PrivyLoginInfo loginInfo;
    public PrivyAccountInfo accountInfo;
    public string privyUId;

    DateTime tokenExpireDate;

    void Start()
    {
#if UNITY_EDITOR
        return;
#endif
        PrivyBridge.PrivyInitModule(gameObject.name, "OnPrivyModuleMessage");
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
    }

    public void OnPrivyModuleMessage(string jsonData)
    {
        Debug.Log("On Receive Message");
        Debug.Log(jsonData);
    }

    public bool LoggedInPrivy()
    {
        return loginInfo != null && !string.IsNullOrEmpty(loginInfo.accessToken);
    }

    public bool LoggedInPrivyAccount()
    {
        return accountInfo != null && !string.IsNullOrEmpty(accountInfo.Id);
    }

    void OnTick()
    {
        if (LoggedInPrivy())
        {
            if (DateTime.UtcNow > tokenExpireDate)
            {
                RefreshToken();
            }
        }
    }
    public void RefreshToken()
    {
        tokenExpireDate.AddMinutes(5);
        string url = "https://auth.privy.io/api/v1/sessions";
        string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(new PrivyRefreshTokenRequest { refresh_token = refreshToken });
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("privy-app-id", app_id);
        headers.Add("privy-ca-id", ca_id);
        headers.Add("privy-client-id", client_id);
        headers.Add("origin", url_origin);
        headers.Add("Authorization", $"Bearer {accessToken}");
        HTTPManager.Instance.StartCoroutine(HTTPManager.Instance.SendHTTPRequest(url, EHTTPRequest.POST, jsonData, headers, s =>
        {
            Debug.Log(s);
            try
            {
                PrivyRoot root = Newtonsoft.Json.JsonConvert.DeserializeObject<PrivyRoot>(s);
                if (!string.IsNullOrEmpty(root.token))
                {
                    refreshToken = root.refresh_token;
                    HandleAccessToken(root.token);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error: " + e.Message);
                Debug.LogException(e);
            }
        }));
    }
    void HandleAccessToken(string token)
    {
        JwtPayload jwtPayload = Helper.ParseJwt(token);
        tokenExpireDate = Helper.ParseDateTime(jwtPayload.Expiration).AddMinutes(-5);
        privyUId = jwtPayload.userId;
        accessToken = token;
        TigerForge.EventManager.EmitEvent(Constant.ON_LOGIN_INFO_UPDATE);
    }
    public void Login()
    {
#if UNITY_EDITOR
        return;
#endif
        PrivyBridge.PrivyLogin();
    }
    public void Logout()
    {
#if UNITY_EDITOR
        return;
#endif
        PrivyBridge.PrivyLogout();
    }
    public void GetAccountInfo()
    {
#if UNITY_EDITOR
        return;
#endif
        PrivyBridge.PrivyAccountInfo();
    }
    public void OnClickExportWalletEvent()
    {
#if UNITY_EDITOR
        return;
#endif
        PrivyBridge.PrivyExportWallet();
    }
    public void OnClickCallContractActionLogEvent()
    {
#if UNITY_EDITOR
        return;
#endif
        PrivyBridge.PrivyCallContractActionLog();
    }
    public void OnListenAccountInfo(string data)
    {
        Debug.Log("OnListenAccountInfo: \n" + data);
        try
        {
            PrivyAccountInfo _accountInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<PrivyAccountInfo>(data);
            if (!string.IsNullOrEmpty(_accountInfo.Id))
            {
                accountInfo = _accountInfo;
                Debug.Log($"Account Info Received, {accountInfo.Id}");
            }
            TigerForge.EventManager.EmitEvent(Constant.ON_LOGIN_INFO_UPDATE);
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
            Debug.LogException(e);
        }
    }
    public void OnListenLogin(string data)
    {
        Debug.Log("OnListenLogin: \n" + data);
        try
        {
            PrivyLoginInfo _loginInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<PrivyLoginInfo>(data);
            if (!string.IsNullOrEmpty(_loginInfo.accessToken))
            {
                loginInfo = _loginInfo;
                Debug.Log("Account LoggedIn");
                refreshToken = loginInfo.refreshToken;
                ca_id = loginInfo.cId;
                url_origin = loginInfo.origin;
                HandleAccessToken(loginInfo.accessToken);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
            Debug.LogException(e);
        }
    }
    public void OnListenLogout(string data)
    {
        Debug.Log("OnListenLogout: \n" + data);
        try
        {
            MessageResponse messageResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageResponse>(data);
            if (messageResponse.status)
            {
                accessToken = "";
                refreshToken = "";
                ca_id = "";
                loginInfo = null;
                accountInfo = null;
                Debug.Log("Account LoggedOut");
                TigerForge.EventManager.EmitEvent(Constant.ON_LOGIN_INFO_UPDATE);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
            Debug.LogException(e);
        }
    }
    public void OnListenCheckLogin(string data)
    {
        Debug.Log("OnListenCheckLogin: \n" + data);
        try
        {
            PrivyLoginInfo _loginInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<PrivyLoginInfo>(data);
            if (!string.IsNullOrEmpty(_loginInfo.accessToken))
            {
                loginInfo = _loginInfo;
                Debug.Log("Account LoggedIn");
                refreshToken = loginInfo.refreshToken;
                ca_id = loginInfo.cId;
                url_origin = loginInfo.origin;
                HandleAccessToken(loginInfo.accessToken);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
            Debug.LogException(e);
        }
    }
}

public class PrivyLoginInfo
{
    public string accessToken;
    public string refreshToken;
    public string cId;
    public string origin;
}

#region Refresh Token
public class PrivyRefreshTokenRequest
{
    public string refresh_token;
}
public class PrivyUser
{
    public string id { get; set; }
    public long created_at { get; set; }
    public List<PrivyLinkedAccount> linked_accounts { get; set; }
    public List<object> mfa_methods { get; set; }
    public bool has_accepted_terms { get; set; }
    public bool is_guest { get; set; }
}

public class PrivyLinkedAccount
{
    public string type { get; set; }
    public string address { get; set; }
    public long? verified_at { get; set; }
    public long? first_verified_at { get; set; }
    public long? latest_verified_at { get; set; }
    public string id { get; set; }
    public int? wallet_index { get; set; }
    public string chain_id { get; set; }
    public string chain_type { get; set; }
    public bool? delegated { get; set; }
    public string wallet_client { get; set; }
    public string wallet_client_type { get; set; }
    public string connector_type { get; set; }
    public bool? imported { get; set; }
    public string recovery_method { get; set; }
    public string smart_wallet_type { get; set; }
}

public class PrivyRoot
{
    public PrivyUser user { get; set; }
    public string token { get; set; }
    public object privy_access_token { get; set; }
    public string refresh_token { get; set; }
    public string identity_token { get; set; }
    public string session_update_action { get; set; }
}
#endregion
#region AccountInfo
public class PrivyAccountInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("linkedAccounts")]
    public List<LinkedAccount> LinkedAccounts { get; set; }

    [JsonPropertyName("email")]
    public EmailInfo Email { get; set; }

    [JsonPropertyName("wallet")]
    public WalletInfo Wallet { get; set; }

    [JsonPropertyName("smartWallet")]
    public SmartWalletInfo SmartWallet { get; set; }

    [JsonPropertyName("delegatedWallets")]
    public List<object> DelegatedWallets { get; set; }

    [JsonPropertyName("mfaMethods")]
    public List<object> MfaMethods { get; set; }

    [JsonPropertyName("hasAcceptedTerms")]
    public bool HasAcceptedTerms { get; set; }

    [JsonPropertyName("isGuest")]
    public bool IsGuest { get; set; }
}

public class LinkedAccount
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("verifiedAt")]
    public DateTime VerifiedAt { get; set; }

    [JsonPropertyName("firstVerifiedAt")]
    public DateTime FirstVerifiedAt { get; set; }

    [JsonPropertyName("latestVerifiedAt")]
    public DateTime LatestVerifiedAt { get; set; }

    [JsonPropertyName("chainType")]
    public string ChainType { get; set; }

    [JsonPropertyName("walletClientType")]
    public string WalletClientType { get; set; }

    [JsonPropertyName("connectorType")]
    public string ConnectorType { get; set; }

    [JsonPropertyName("recoveryMethod")]
    public string RecoveryMethod { get; set; }

    [JsonPropertyName("imported")]
    public bool? Imported { get; set; }

    [JsonPropertyName("delegated")]
    public bool? Delegated { get; set; }

    [JsonPropertyName("walletIndex")]
    public int? WalletIndex { get; set; }

    [JsonPropertyName("smartWalletType")]
    public string SmartWalletType { get; set; }
}

public class EmailInfo
{
    [JsonPropertyName("address")]
    public string Address { get; set; }
}

public class WalletInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("chainType")]
    public string ChainType { get; set; }

    [JsonPropertyName("walletClientType")]
    public string WalletClientType { get; set; }

    [JsonPropertyName("connectorType")]
    public string ConnectorType { get; set; }

    [JsonPropertyName("recoveryMethod")]
    public string RecoveryMethod { get; set; }

    [JsonPropertyName("imported")]
    public bool Imported { get; set; }

    [JsonPropertyName("delegated")]
    public bool Delegated { get; set; }

    [JsonPropertyName("walletIndex")]
    public int WalletIndex { get; set; }
}

public class SmartWalletInfo
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("smartWalletType")]
    public string SmartWalletType { get; set; }
}

#endregion
#endif
