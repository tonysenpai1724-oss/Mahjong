using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ILeaderboardController : IController<LeaderboardController>
{
    public bool ActiveLeaderboard();
    public LeaderBoardInfo GetLeaderboard();
    public void UpdateScore(int score);
    public int GetTotalScore();
    public int GetSeasonScore();
    public string GetSeasonCountDown();
    public LeaderboardRewardConfig GetCurrentReward();
}
public class LeaderboardCachedData : IControllerCachedData
{
    public List<int> lstSeasonRewarded = new List<int>();
    public int seasonScore;
    public int totalScore;
    public int currentSeason;
    public void InitFirsTime()
    {
    }

    public void OnNewData()
    {
    }
    public bool GetSeasonRewarded(int season)
    {
        return lstSeasonRewarded.Contains(season);
    }
    public void SetSeasonRewarded(int season)
    {
        lstSeasonRewarded.Add(season);
    }
    public void UpdateScore(int score)
    {
        seasonScore += score;
        totalScore += score;
    }
    public int GetSeasonScore()
    {
        return seasonScore;
    }
    public int GetTotalScore()
    {
        return totalScore;
    }
    public void ResetSeasonScore()
    {
        seasonScore = 0;
    }
    public void SetCurrentSeason(int season)
    {
        currentSeason = season;
    }
}
public class LeaderboardController : CommonServerController<LeaderboardCachedData>, ILeaderboardController
{
    LeaderBoardInfo leaderboardInfo;
    DateTime seasonEnd;
    DateTime nextFetch;
    Dictionary<int, int> history;
    string keyLeaderboard = "challange_score";
    public override string KeyData()
    {
        return "leader_board";
    }

    public override string KeyEvent()
    {
        return Constant.ON_LEADERBOARD_UPDATE;
    }
    protected override void OnFirstTick()
    {
        base.OnFirstTick();
        FetchLeaderboard();
    }
    void CheckHistoryReward()
    {
        foreach (var item in history)
        {
            if (item.Key != leaderboardInfo.season.currentSeason && !cachedData.GetSeasonRewarded(item.Key) && item.Value > 0)
            {
                int rewardId = 0;
                foreach (var _item in DataSystem.Instance.dataLeaderboard.dicRewards)
                {
                    if(item.Value >= _item.Key)
                    {
                        rewardId = _item.Key;
                    }
                }
                if(rewardId >0)
                {
                    PackageResource pack = DataSystem.Instance.dataLeaderboard.dicRewards[rewardId].GetRewards();
                    pack.ReceiveAndShow(EResourceFrom.Leaderboard);
                    cachedData.SetSeasonRewarded(item.Key);
                    OnValueChange();
                    IMailboxController.Instance.AddMail(new MailItem($"Ranking {item.Value} Season {item.Key}", "", pack));
                }
            }
        }
    }
    protected override void OnTick()
    {
        base.OnTick();
        if (nextFetch < DateTime.UtcNow || seasonEnd < DateTime.UtcNow)
        {
            FetchLeaderboard();
        }
    }
    void FetchLeaderboard()
    {
        nextFetch = DateTime.UtcNow.AddMinutes(10);
        seasonEnd = DateTime.UtcNow.AddMinutes(10);
        HTTPManager.Instance.GetLeaderboard(keyLeaderboard, s =>
        {
            leaderboardInfo = s;
            leaderboardInfo.SortRanking();
            CheckSeason();
            HTTPManager.Instance.GetLeaderboardHistory(keyLeaderboard, s =>
            {
                history = s;
                CheckHistoryReward();
                OnValueChange();
            });
        });
    }
    void CheckSeason()
    {
        if (cachedData.currentSeason != leaderboardInfo.season.currentSeason)
        {
            OnNewSeason();
        }
        cachedData.SetCurrentSeason(leaderboardInfo.season.currentSeason);
        cachedData.seasonScore = leaderboardInfo.currentRanking.currentScore;
        seasonEnd = Helper.ParseDateTime(leaderboardInfo.season.endTime);
        OnValueChange();
    }
    void OnNewSeason()
    {
        cachedData.ResetSeasonScore();
        cachedData.SetCurrentSeason(leaderboardInfo.season.currentSeason);
        OnValueChange();
    }
    public LeaderBoardInfo GetLeaderboard()
    {
        return leaderboardInfo;
    }

    public int GetSeasonScore()
    {
        return cachedData.seasonScore;
    }

    public int GetTotalScore()
    {
        return cachedData.totalScore;
    }

    public void UpdateScore(int score)
    {
        cachedData.UpdateScore(score);
        OnValueChange();
        HTTPManager.Instance.UpdateLeaderboardScore(keyLeaderboard, cachedData.GetSeasonScore(), s =>
        {
            FetchLeaderboard();
        });
    }

    public string GetSeasonCountDown()
    {
        return Helper.TimeToString(seasonEnd - DateTime.UtcNow);
    }

    public LeaderboardRewardConfig GetCurrentReward()
    {
        int rewardId = 0;
        foreach (var rankReward in DataSystem.Instance.dataLeaderboard.dicRewards)
        {
            if (leaderboardInfo.currentRanking.currentRank >= rankReward.Key)
                rewardId = rankReward.Key;
        }
        LeaderboardRewardConfig targetConfig = null;
        if (rewardId > 0)
            targetConfig = DataSystem.Instance.dataLeaderboard.dicRewards[rewardId];
        else
            targetConfig = DataSystem.Instance.dataLeaderboard.dicRewards.Values.Last();
        return new LeaderboardRewardConfig()
        {
            rewards = targetConfig.rewards,
            rank = leaderboardInfo.currentRanking.currentRank
        };
    }

    public bool ActiveLeaderboard()
    {
        return leaderboardInfo != null && leaderboardInfo.season.IsActiveSeason();
    }
}
