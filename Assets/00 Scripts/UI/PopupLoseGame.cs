using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MahjongOut3D.Managers;
using Spine.Unity;

public class PopupLoseGame : UIBase
{
   public  Button btnRestart;
    public Button btnRevive;
   public  TextMeshProUGUI titleText;
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
    void PlayOpenThenIdle()
    {
        if (skeletonGraphic == null || skeletonGraphic.AnimationState == null)
            return;

        skeletonGraphic.AnimationState.SetAnimation(0, "Open", false);
        skeletonGraphic.AnimationState.AddAnimation(0, "Idle", true, 0f);
    }

    public override void OnDisable()
    {
        UnbindButtons();
        base.OnDisable();
    }

    void CacheReferences()
    {
        if (btnRestart == null)
            btnRestart = FindButton("Restart");
        if (btnRevive == null)
            btnRevive = FindButton("Revive");
        if (titleText == null)
            titleText = FindText("NO more slots!");
    }

    void BindButtons()
    {
        if (btnRestart != null)
        {
            btnRestart.onClick.RemoveListener(OnClickRestart);
            btnRestart.onClick.AddListener(OnClickRestart);
        }

        if (btnRevive != null)
        {
            btnRevive.onClick.RemoveListener(OnClickReviveAds);
            btnRevive.onClick.AddListener(OnClickReviveAds);
        }
    }

    void UnbindButtons()
    {
        if (btnRestart != null)
            btnRestart.onClick.RemoveListener(OnClickRestart);

        if (btnRevive != null)
            btnRevive.onClick.RemoveListener(OnClickReviveAds);
    }

    void RefreshText()
    {
        if (titleText != null)
            titleText.text = "Level Lose";
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

    void OnClickRestart()
    {
        GameplayManager.Instance.ClaimCurrentReward(1, EResourceFrom.ReviveIngame);
        if (GameManager.Instance != null)
            GameManager.Instance.StartCoroutine(IERestartLevel());
    }

    void OnClickReviveAds()
    {
#if UNITY_EDITOR
        GameplayManager.Instance.ReviveFromAds();
        Hide();
#else
        ShowRewardedAdsForRevive();
#endif
    }

    IEnumerator IERestartLevel()
    {
        Hide();
        yield return new WaitForSecondsRealtime(0.25f);
        yield return null;

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (GameManager.Instance.GameType == EGameType.Campaign && levelManager != null)
        {
            levelManager.ReloadCurrentLevel();
            yield break;
        }

        GameManager.Instance.PlayGame(GameManager.Instance.GameType);
    }

    void ShowRewardedAdsForRevive()
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
