using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UiGameplay : MonoBehaviour
{
    public TextMeshProUGUI txtLevel, txtTimer, txtTut, txtScore;
    public GameObject tutBox;

    private void Start()
    {
        UIManager.Instance.uIGameplay = this;
    }

    public void Initialize()
    {
        HideTextTut();
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        InitLevel();
        TigerForge.EventManager.StartListening(Constant.EVENT_LEVEL_INITED, InitLevel);
        OnTick();
    }
    void InitLevel()
    {
        if (GameManager.Instance.GameType == EGameType.Campaign)
        {
            txtLevel.text = $"Level {GameplayManager.Instance.CurrentLevel}";
            txtScore.gameObject.SetActive(false);
        }
        else
        {
            txtScore.gameObject.SetActive(true);
        }
    }
    void OnTick()
    {
        txtTimer.text = Helper.TimeToString(System.TimeSpan.FromSeconds(GameplayManager.Instance.LevelTime));
        txtScore.text = $"Score: {GameplayManager.Instance.Score}";
    }
    public void OnClickPauseGame()
    {
        if (GameplayManager.Instance.State == EGamePlayState.Running)
            UIManager.Instance.ShowPopupPauseGame();
    }

    public void ShowTextTut(string txt)
    {
        txtTut.text = txt;
        tutBox.SetActive(true);
    }
    public void HideTextTut()
    {
        tutBox.SetActive(false);
    }
}