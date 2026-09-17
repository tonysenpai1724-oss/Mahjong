public class HomeStreakButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupStreak();
    }

    protected override void CheckActive()
    {
   
    }

    protected override void CheckNoti()
    {
         // notiObj.SetActive(IDailyQuestController.Instance.NotiStreak());
    }
}