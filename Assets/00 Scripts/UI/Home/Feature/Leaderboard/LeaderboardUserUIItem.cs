using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUserUIItem : MonoBehaviour
{
    public Image avatar;
    public TextMeshProUGUI txtName, txtScore, txtRank;

    public void InitUser(LeaderboardUser user)
    {
        txtName.text = user.info.displayName;
        txtRank.text = "#" + user.currentRank.ToString();
        txtScore.text = "Score: " + user.currentScore.ToString();
    }
}
