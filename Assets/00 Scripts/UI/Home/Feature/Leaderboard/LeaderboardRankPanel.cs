using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardRankPanel : MonoBehaviour
{
    public LeaderboardUserUIItem itemPrefab;
    public Transform itemParent;
    List<LeaderboardUserUIItem> lstItems = new List<LeaderboardUserUIItem>();
    public LeaderboardUserUIItem currentUser;
    public void SetActive(bool active)
    {
        if (active)
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
            return;
        }
        TigerForge.EventManager.StartListening(Constant.ON_LEADERBOARD_UPDATE, InitContent);
        InitContent();
    }
    void InitContent()
    {
        LeaderBoardInfo leaderBoardInfo = ILeaderboardController.Instance.GetLeaderboard();
        int has = lstItems.Count;
        int need = leaderBoardInfo.ranking.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPrefab, itemParent));
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            lstItems[i].gameObject.SetActive(i < need);
            if (i < need)
            {
                lstItems[i].InitUser(leaderBoardInfo.ranking[i]);
            }
        }
        DebugCustom.LogColor("Current Score: " + ILeaderboardController.Instance.GetSeasonScore(), Color.red);
        if (leaderBoardInfo.currentRanking.currentScore != ILeaderboardController.Instance.GetSeasonScore())
            leaderBoardInfo.currentRanking.currentScore = ILeaderboardController.Instance.GetSeasonScore();
        currentUser.InitUser(leaderBoardInfo.currentRanking);
    }
    private void OnDisable()
    {
        TigerForge.EventManager.StopListening(Constant.ON_LEADERBOARD_UPDATE, InitContent);
    }
}
