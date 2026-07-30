using System.Collections;
using System.Collections.Generic;
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
    public void OnClickPlay()
    {
        GameManager.Instance.PlayGame(GameManager.Instance.GameType);
    }
    public void OnClickHome()
    {
        GameManager.Instance.GoSceneHome();
    }
}
