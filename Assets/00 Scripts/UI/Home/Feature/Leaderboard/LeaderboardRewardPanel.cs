using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeaderboardRewardPanel : MonoBehaviour
{
    public LeaderboardRewardUIItem itemPrefab;
    public Transform itemParent;
    List<LeaderboardRewardUIItem> lstItems = new List<LeaderboardRewardUIItem>();
    public LeaderboardRewardUIItem currentItem;
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
        int has = lstItems.Count;
        List<LeaderboardRewardConfig> lstConfig = DataSystem.Instance.dataLeaderboard.dicRewards.Values.ToList();
        int need = lstConfig.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPrefab, itemParent));
        }
        for (int i = 0; i < lstItems.Count; i++)
        {
            lstItems[i].gameObject.SetActive(i < need);
            if (i < need)
            {
                lstItems[i].InitReward(lstConfig[i]);
            }
        }
        currentItem.InitReward(ILeaderboardController.Instance.GetCurrentReward());
    }
    private void OnDisable()
    {
        TigerForge.EventManager.StopListening(Constant.ON_LEADERBOARD_UPDATE, InitContent);
    }
}
