using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public enum ELeaderboardContent
{
    RankPanel,
    RewardPanel,
}
public class PopupLeaderboard : UIBase
{
    public ELeaderboardContent currentContent;
    public LeaderboardRankPanel rankPanel;
    public LeaderboardRewardPanel rewardPanel;

    public ToggleButton btnRank, btnReward;

    public TextMeshProUGUI txtSeason, txtCountDown;
    public override void Show()
    {
        base.Show();
        SelectContent(ELeaderboardContent.RankPanel);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        OnTick();
    }
    public override void OnDisable()
    {
        base.OnDisable();
        TigerForge.EventManager.StopListening(Constant.EVENT_TIMER_TICK, OnTick);
    }
    void OnTick()
    {
        txtSeason.text = "Season " + ILeaderboardController.Instance.GetLeaderboard().season.currentSeason.ToString();
        txtCountDown.text = "Season End In: " + ILeaderboardController.Instance.GetSeasonCountDown();
    }
    void SelectContent(ELeaderboardContent content)
    {
        rankPanel.SetActive(content == ELeaderboardContent.RankPanel);
        rewardPanel.SetActive(content == ELeaderboardContent.RewardPanel);
        btnRank.SetActive(content == ELeaderboardContent.RankPanel);
        btnReward.SetActive(content == ELeaderboardContent.RewardPanel);
    }
    public void OnClickRank()
    {
        SelectContent(ELeaderboardContent.RankPanel);
    }
    public void OnClickReward()
    {
        SelectContent(ELeaderboardContent.RewardPanel);
    }
}
