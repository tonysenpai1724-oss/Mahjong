using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Reflection;

[Serializable]
public class RequestMessage
{
}

[Serializable]
public class MessageResponse
{
    public int code;
    public bool status;
    public bool success;
    public string message;
}

public class MessageDataResponse<T> : MessageResponse where T : class
{
    public T data;
}

public class LoginDemoRequest
{
    public string telegramId;
    //public string absAccount;
    //public string nonce;
    //public string signature;
}

public class LoginTelegramRequest
{
    public string init_data;
    public string refCode;
}

public class LoginTelegramResponse : MessageResponse
{
    public DataLoginTelegram data;

    public LoginTelegramResponse()
    {
    }
}
public class DataLoginTelegram
{
    public string access_token;

    public DataLoginTelegram()
    {
    }
}

public class LoginResponse
{
    public string access_token;
    //public string tokenType;
    //public long expiresIn;
}

public class MaintanceInfoResponse : MessageResponse
{
    public MaintanceInfo data;
}
public class MaintanceInfo
{
    public bool appMaintain;
    public long from;
    public long to;
    public string message;
}

#region UserInfo
public class TelegramUserInfo
{
    [JsonProperty("avatar")]
    public string avatarUrl;
    public long created_at;
    public string first_name;
    public string last_name;
    public string language_code;
    public int points;
    public string ref_code;
    public int ref_count;
    public string telegram_id;
    public string username;
}
[Serializable]
public class SaveUserInfoRequest : RequestMessage
{
    public UserInfo userInfo;
}
public class UserInfo
{
    public string id;
    public string displayName;
    public string avatar;
    public long createdAt;
    public string updatedAt;
    public List<GameResourceHTTP> wallet;
    public void UpdateInfo()
    {
        HTTPManager.Instance.UpdateUserInfo(new UpdateInfoRequest
        {
            avatar = avatar,
            displayName = displayName
        });
    }
}

public class GameResourceHTTP
{
    public string symbol;
    public long amount;
}

public class UpdateInfoRequest : RequestMessage
{
    public string avatar;
    public string displayName;
}
#endregion

#region IAP

public class CreateInvoiceLinkMessage : RequestMessage
{
    public List<InvoiceLinkMessage> createInvoiceLinks;
}

public class InvoiceLinkMessage
{
    public string currency = "XTR";
    public string telegramId = GameManager.Instance.UserId;
    public string packageId;
    public string title;
    public string description;
    public int price;
    public string photoUrl = "https://i.ibb.co/thk3xBV/icon-game.png";
    public int photoWidth = 512;
    public int photoHeight = 512;
}

public class InvoiceLinksResponse : MessageResponse
{
    public InvoiceLinksDic data;
}

public class InvoiceLinksDic
{
    public Dictionary<string, string> invoiceLinks = new Dictionary<string, string>();
}

public class GetIAPHistoryRequest : RequestMessage
{
    public int paging;
}

public class IAPHistoryResponse : MessageDataResponse<List<TelegramPurchaseData>>
{
    public IAPHistoryPageInfo page;
}
public class IAPHistoryPageInfo
{
    public int total;
    public int currentPage;
    public int pages;
}

public class TelegramPurchaseData
{
    public string id { get; set; }
    public string shopId { get; set; }
    public int price { get; set; }
    public long processAt { get; set; }
    public string telegramChargeId { get; set; }
    public string providerChargeId { get; set; }
    public int status { get; set; }

    public string GetPackName()
    {
        return DataSystem.Instance.dataIAP.dicConfigs[DataSystem.Instance.dataIAP.dicId[shopId]].packName;
    }
    public PackageResource GetRewards()
    {
        return DataSystem.Instance.dataIAP.dicConfigs[DataSystem.Instance.dataIAP.dicId[shopId]].GetReward();
    }
    public bool ValidPack()
    {
        return DataSystem.Instance.dataIAP.dicId.ContainsKey(shopId);
    }
}

#endregion
#region Leaderboard
public class UpdateLeaderboardRequest : RequestMessage
{
    public string key;
    public int score;
}
public class LeaderboardRequest : RequestMessage
{
    public string key;
}
public class GetLeaderboardListRequest: LeaderboardRequest
{
    int paging = 1;
}
public class UserDisplay
{
    public string displayName;
    public string avatar;
}
public class LeaderboardUser
{
    public UserDisplay info;
    public int currentRank;
    public int currentScore;
}
public class LeaderboardSeason
{
    public long startTime;
    public long endTime;
    public int currentSeason;
    public bool IsActiveSeason()
    {
        return DateTime.UtcNow.ToUnixTimestamp() > startTime && DateTime.UtcNow.ToUnixTimestamp() < endTime;
    }
}
public class LeaderBoardInfo
{
    public List<LeaderboardUser> ranking;
    public LeaderboardSeason season;
    public LeaderboardUser currentRanking;

    public void SortRanking()
    {
        ranking.Sort((a, b) => a.currentRank.CompareTo(b.currentRank));
    }
}
#endregion

#region Analystic

public class AnalysticMessage
{
    public List<AnalysticEvent> events;

    public AnalysticMessage(AnalysticEvent analysticEvent)
    {
        events = new List<AnalysticEvent>();
        events.Add(analysticEvent);
    }
}

public class AnalysticEvent
{
    [JsonProperty("name")] public string Name;
    [JsonProperty("params")] public Dictionary<string, string> Params = new Dictionary<string, string>();

    public AnalysticEvent(string name, Dictionary<string, string> _param)
    {
        Name = name;
        Params = _param;
    }
}

#endregion

#region Partner Quest
public class PartnerQuestResponse : MessageResponse
{
    public List<PartnerQuest> referrals;
}
public class PartnerQuest
{
    public string link;
    public string iconUrl;
    public string title;
    public bool status;
}
#endregion
#region Friend Referal
public class FriendReferalQuestConfig
{
    public int refCount;
}
public class FriendReferalInfoResponse : MessageResponse
{
    public int currentRef;
    public int claimedRef;
    public long nextTimeReset;
    public List<FriendReferalQuestConfig> questItems;
}
#endregion
#region CommonData
public class SetCommonDataRequest : RequestMessage
{
    public string key;
    public string data;
}
public class GetCommonDataRequest : RequestMessage
{
    public string key;
}
public class CommonDataResponse : MessageResponse
{
    public string key;
    public string data;
}
#endregion
public class JwtPayload
{

    [JsonProperty("exp")] public long Expiration { get; set; }

    [JsonProperty("iat")] public long IssuedAt { get; set; }

    [JsonProperty("telegram_id")] public string TelegramId { get; set; }

    [JsonProperty("sub")] public string userId { get; set; }
}