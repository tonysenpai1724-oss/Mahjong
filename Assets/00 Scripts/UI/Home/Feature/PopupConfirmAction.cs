using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PopupConfirmAction : UIBase
{
    public CommonButton btnOk, btnNo;
    public TextMeshProUGUI txtTitle, txtDesc;

    public void ShowConfirmAction(System.Action actionOk, System.Action actionNo = null, string txtDesc = "Confirm Action?", string txtTile = "Warning",
        EButtonColor okColor = EButtonColor.Yellow, string txtBtnOk = "OK", EButtonColor noColor = EButtonColor.Blue, string txtBtnNo = "No")
    {
        this.txtTitle.text = txtTile;
        this.txtDesc.text = txtDesc;
        btnNo.gameObject.SetActive(actionNo != null);
        btnNo.SetupButton(noColor, txtBtnNo, true, () =>
        {
            actionNo?.Invoke();
            Hide();
        });
        btnOk.SetupButton(okColor, txtBtnOk, true, () => {
            actionOk?.Invoke();
            Hide();
        });
        Show();
    }
    public void ShowWarning(System.Action actionClose,string txtDesc, string txtTile = "Warning", EButtonColor okColor = EButtonColor.Yellow, string txtBtnOk = "OK")
    {
        this.txtTitle.text = txtTile;
        this.txtDesc.text = txtDesc;
        btnNo.gameObject.SetActive(false);
        btnOk.SetupButton(okColor, txtBtnOk, true, () =>
        {
            Hide();
            actionClose?.Invoke();
        });
        Show();
    }

}
