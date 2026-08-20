using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MahjongOut3D.Managers;
using Spine.Unity;

public class PopupWinGame : UIBase
{
   public  Button btnPlay;
    public Button btnAds;
    public TextMeshProUGUI titleText;
    public SkeletonAnimation skeletonGraphic;

    public override void Show()
    {
       DebugCustom.LogColor("Show popup", gameObject.name);
        if (hackObj != null)
            hackObj.SetActive(GameManager.Instance.IsTester);
        if (blockPanel != null)
            blockPanel.SetActive(false);
        gameObject.SetActive(true);
        // if (GameplayManager.Instance != null)
        // {
        //     GameplayManager.Instance.SetState(EGamePlayState.Pause);
        // }
        if (UIManager.Instance != null)
        {
            if (!UIManager.Instance.lstOpenningUI.Contains(this))
                UIManager.Instance.lstOpenningUI.Add(this);
        }
        if (buttonClose != null)
        {
            buttonClose.onClick.AddListener(() =>
            {
                Hide();
            });
        }
        this.transform.SetAsLastSibling();
        CacheReferences();
        BindButtons();
        RefreshText();
PlayOpenThenIdle();
    }

    public override void OnDisable()
    {
        UnbindButtons();
        base.OnDisable();
    }

    void CacheReferences()
    {
        if (btnPlay == null)
            btnPlay = FindButton("Next") ?? FindButton("Button");
        if (btnAds == null)
            btnAds = FindButton("AdsBtn") ?? FindButton("Button (1)");
        if (titleText == null)
            titleText = FindText("Completed");
        if (skeletonGraphic == null)
            skeletonGraphic = GetComponentInChildren<SkeletonAnimation>(true);
    }

    void PlayOpenThenIdle()
    {
        if (skeletonGraphic == null || skeletonGraphic.AnimationState == null)
            return;

        skeletonGraphic.AnimationState.SetAnimation(0, "Open", false);
        skeletonGraphic.AnimationState.AddAnimation(0, "Idle", true, 0f);
    }

    void BindButtons()
    {
        if (btnPlay != null)
        {
            btnPlay.onClick.RemoveListener(OnClickNextLevel);
            btnPlay.onClick.AddListener(OnClickNextLevel);
        }

        if (btnAds != null)
        {
            btnAds.onClick.RemoveListener(OnClickAdsX2Reward);
            btnAds.onClick.AddListener(OnClickAdsX2Reward);
        }
    }

    void UnbindButtons()
    {
        if (btnPlay != null)
            btnPlay.onClick.RemoveListener(OnClickNextLevel);

        if (btnAds != null)
            btnAds.onClick.RemoveListener(OnClickAdsX2Reward);
    }

    void RefreshText()
    {
        if (titleText != null && GameplayManager.Instance != null)
            titleText.text = $"Level {GameplayManager.Instance.CurrentLevel} Completed";
    }

    Button FindButton(string objectName)
    {
        Transform target = FindDeepChild(transform, objectName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    TextMeshProUGUI FindText(string contains)
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.text.Contains(contains))
                return text;
        }

        return null;
    }

    void OnClickNextLevel()
    {
        GameplayManager.Instance.ClaimCurrentReward(1, EResourceFrom.ReviveIngame);
        if (GameManager.Instance != null)
            GameManager.Instance.StartCoroutine(IEGoNextLevel());
    }

    void OnClickAdsX2Reward()
    {
#if UNITY_EDITOR
        GameplayManager.Instance.ClaimCurrentReward(2, EResourceFrom.AdsReward);
        if (GameManager.Instance != null)
            GameManager.Instance.StartCoroutine(IEGoNextLevel());
#else
        ShowRewardedAdsForDoubleReward();
#endif
    }

    IEnumerator IEGoNextLevel()
    {
        Hide();
        yield return new WaitForSecondsRealtime(0.25f);
        yield return null;

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (GameManager.Instance.GameType == EGameType.Campaign && levelManager != null)
        {
            int targetIndex = Mathf.Max(0, IPlayerInfoController.Instance.CurrentLevel() - 1);
            if (!levelManager.LoadLevel(targetIndex))
                levelManager.ReloadCurrentLevel();
            yield break;
        }

        GameManager.Instance.PlayGame(GameManager.Instance.GameType);
    }

    void ShowRewardedAdsForDoubleReward()
    {
    }

    Transform FindDeepChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
