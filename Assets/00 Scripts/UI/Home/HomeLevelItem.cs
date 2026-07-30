using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HomeLevelItem : MonoBehaviour
{
    public Image bg;
    public Sprite sprOn, sprOff;
    public TextMeshProUGUI txtLevel;
    public GameObject glow, fillObj, fillImg;

    public void InitLevel(int level)
    {
        bool current = IPlayerInfoController.Instance.CurrentLevel() == level;
        bool start = level == 1;
        bool end = level == IPlayerInfoController.Instance.MaxLevel();
        bg.sprite = IPlayerInfoController.Instance.CurrentLevel() >= level ? sprOn : sprOff;
        glow.SetActive(current);
        fillObj.SetActive(!end);
        fillImg.SetActive(IPlayerInfoController.Instance.CurrentLevel() >= level);
        txtLevel.text = level.ToString();
    }
}
