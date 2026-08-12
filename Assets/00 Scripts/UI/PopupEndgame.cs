using System.Collections;
using System.Collections.Generic;
using MahjongOut3D.Managers;
using TMPro;
using UnityEngine;

public class PopupEndgame : UIBase
{
    public TextMeshProUGUI txtShow;
    public CommonButton btnPlay;
    public UiResourceItem resourceItem;
    public Transform itemParent;
    public override void Show()
    {
        base.Show();
        ClearRewardItems();
        if (GameplayManager.Instance.PackReward != null)
        {
            foreach (var item in GameplayManager.Instance.PackReward.lstResource)
            {
                UiResourceItem uiItem = Instantiate(resourceItem, itemParent);
                uiItem.InitResouce(item, true);
            }
        }
        txtShow.text = GameplayManager.Instance.winGame ? "Level Win" : "Level Lose";
        bool maxLevel = IPlayerInfoController.Instance.CurrentLevel() >= IPlayerInfoController.Instance.MaxLevel();
        btnPlay.gameObject.SetActive(!maxLevel);
        btnPlay.txtVisual.text = GameplayManager.Instance.winGame ? "Next Level" : "Restart";
    }

    void ClearRewardItems()
    {
        if (itemParent == null)
            return;

        for (int i = itemParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemParent.GetChild(i).gameObject);
        }
    }

    public void OnClickPlay()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartCoroutine(IEOnClickPlay());
        }
    }

    IEnumerator IEOnClickPlay()
    {
        Debug.Log($"[Mahjong] PopupEndgame.OnClickPlay start. Win={GameplayManager.Instance.winGame}, GameType={GameManager.Instance.GameType}");
        Hide();
        yield return new WaitForSecondsRealtime(0.25f);
        yield return null;

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (GameManager.Instance.GameType == EGameType.Campaign && levelManager != null)
        {
            int currentIndex = levelManager.CurrentLevelIndex;
            int targetIndex = GameplayManager.Instance.winGame ? Mathf.Max(0, IPlayerInfoController.Instance.CurrentLevel() - 1) : currentIndex;
            Debug.Log($"[Mahjong] PopupEndgame trying level load. Current={currentIndex}, Target={targetIndex}");

            if (GameplayManager.Instance.winGame)
            {
                if (!levelManager.LoadLevel(targetIndex))
                {
                    Debug.LogWarning($"[Mahjong] Next level load failed for index {targetIndex}. Reloading current level {currentIndex}.");
                    levelManager.ReloadCurrentLevel();
                }
            }
            else
            {
                Debug.Log($"[Mahjong] Restarting current level {currentIndex}.");
                levelManager.ReloadCurrentLevel();
            }

            yield break;
        }

        Debug.LogWarning("[Mahjong] PopupEndgame fallback to GameManager.PlayGame because LevelManager was not found.");

        GameManager.Instance.PlayGame(GameManager.Instance.GameType);
    }
    public void OnClickHome()
    {
        GameManager.Instance.GoSceneHome();
    }
}
