using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupPauseGame : UIBase
{
    public void OnClickHome()
    {
        UIManager.Instance.ShowPopupConfirm(() =>
        {
            GameplayManager.Instance.EndGame(false);
        }, () => { }, "You will lose this level", "confirm quit", EButtonColor.Blue, "OK", EButtonColor.Green);
    }
}
