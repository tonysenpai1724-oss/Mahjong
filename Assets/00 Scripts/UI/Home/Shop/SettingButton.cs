
public class SettingButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupSetting();
    }

    protected override void CheckActive()
    {
        gameObject.SetActive(true);
    }

    protected override void CheckNoti()
    {
    }
}