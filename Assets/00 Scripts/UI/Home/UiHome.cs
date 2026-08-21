using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class UiHome : MonoBehaviour
{
    public Transform coinBar, heartBar;
    public List<HomeToggleButton> homeToggleButtons;
    public List<HomePanel> homePanels;

    private void Start()
    {
        UIManager.Instance.uIHome = this;
    }
    public void InitHome()
    {
        homeToggleButtons[1].OnClick();
        foreach (var item in homePanels)
        {
            item.InitFirstTime();
        }
    }
    public void OnClickHomeButton(HomeToggleButton button)
    {
        for (int i = 0; i < homeToggleButtons.Count; i++)
        {
            homeToggleButtons[i].SetActive(homeToggleButtons[i] == button);
            homePanels[i].SetActive(homeToggleButtons[i] == button);
        }
    }

}
