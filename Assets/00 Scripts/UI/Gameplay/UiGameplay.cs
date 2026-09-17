using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UiGameplay : MonoBehaviour
{
    public TextMeshProUGUI txtLevel, txtTimer, txtTut, txtScore, txtCombo;
    public GameObject tutBox;
    private Coroutine comboPopupRoutine;

    private void Start()
    {
        UIManager.Instance.uIGameplay = this;
    }

    public void Initialize()
    {
        HideTextTut();
        if (txtCombo != null)
        {
            txtCombo.gameObject.SetActive(false);
        }
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
        int comboDisplay = Mathf.Max(0, GameplayManager.Instance.CurrentCombo);
        txtScore.text = $"Score: {GameplayManager.Instance.Score} | Combo x{comboDisplay}";

        if (txtCombo != null && comboDisplay <= 1)
        {
            txtCombo.gameObject.SetActive(false);
        }
    }

    public void ShowComboText(int combo)
    {
        if (txtCombo == null)
        {
            return;
        }

        if (combo <= 1)
        {
            txtCombo.gameObject.SetActive(false);
            return;
        }

        txtCombo.text = $"COMBO x{combo}";
        txtCombo.gameObject.SetActive(true);

        if (comboPopupRoutine != null)
        {
            StopCoroutine(comboPopupRoutine);
        }

        comboPopupRoutine = StartCoroutine(ComboPopupRoutine());
    }

    private IEnumerator ComboPopupRoutine()
    {
        if (txtCombo == null)
        {
            yield break;
        }

        var startColor = txtCombo.color;
        startColor.a = 1f;
        txtCombo.color = startColor;
        txtCombo.transform.localScale = Vector3.one * 0.8f;
        txtCombo.rectTransform.anchoredPosition = new Vector2(0f, 0f);

        Sequence comboSequence = DOTween.Sequence();
        comboSequence.Append(txtCombo.rectTransform.DOAnchorPosY(90f, 0.35f).SetEase(Ease.OutCubic));
        comboSequence.Join(txtCombo.transform.DOScale(Vector3.one * 1.3f, 0.35f));
        comboSequence.Append(txtCombo.transform.DOScale(Vector3.one, 0.15f));
        comboSequence.Join(DOTween.To(() => txtCombo.color.a, alpha =>
        {
            var c = txtCombo.color;
            c.a = alpha;
            txtCombo.color = c;
        }, 0f, 0.45f).SetDelay(0.25f));
        comboSequence.OnComplete(() =>
        {
            txtCombo.gameObject.SetActive(false);
            var c = txtCombo.color;
            c.a = 1f;
            txtCombo.color = c;
            comboPopupRoutine = null;
        });

        yield return null;
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