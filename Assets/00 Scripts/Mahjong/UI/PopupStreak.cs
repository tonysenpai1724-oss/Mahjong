using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using Spine;

public class PopupStreak : UIBase
{
    [SerializeField] List<Image> listTick;
    [SerializeField] Image starImage;
    [SerializeField] Sprite starCompleteSprite;
    [SerializeField] Sprite starIncompleteSprite;
    [SerializeField] TextMeshProUGUI txtStreakOld, txtStreakNew;
    // [SerializeField] SkeletonAnimation skeletonAnimation;
    [SerializeField] float yTop, yMid, yBot;

    int streakCount = 0;
    List<DayOfWeek> listDayOfWeek;

    public override void Show()
    {
        base.Show();

        if (IPlayerInfoController.Instance == null)
            return;

        streakCount = IPlayerInfoController.Instance.GetCurrentStreak();
        listDayOfWeek = IPlayerInfoController.Instance.GetStreakDaysInWeek();
        if (listDayOfWeek == null)
            listDayOfWeek = new List<DayOfWeek>();

        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (txtStreakNew == null || txtStreakOld == null)
            return;

        if (listTick == null)
            listTick = new List<Image>();

        if (listDayOfWeek == null)
            listDayOfWeek = new List<DayOfWeek>();

        txtStreakNew.rectTransform.anchoredPosition = new(txtStreakNew.rectTransform.anchoredPosition.x, yTop);
        txtStreakOld.rectTransform.anchoredPosition = new(txtStreakOld.rectTransform.anchoredPosition.x, yMid);

        if (IPlayerInfoController.Instance == null)
            return;

        bool canPlayAnim = IPlayerInfoController.Instance.ShowStreakAnim();
        bool hasPlayedToday = IPlayerInfoController.Instance.HasPlayedToday();

        if (starImage != null)
        {
            starImage.sprite = (hasPlayedToday || streakCount > 0) ? starCompleteSprite : starIncompleteSprite;
        }

        // Skeleton animation is currently disabled.
        if (canPlayAnim)
        {
            if (hasPlayedToday)
                txtStreakOld.text = Mathf.Max(0, streakCount - 1).ToString();
            else
                txtStreakOld.text = streakCount.ToString();
        }
        else
        {
            txtStreakOld.text = streakCount.ToString();
        }

        if (streakCount > 7)
        {
            foreach (var item in listTick)
            {
                if (item != null)
                    item.fillAmount = 1f;
            }
            if (canPlayAnim && hasPlayedToday)
            {
                txtStreakNew.text = streakCount.ToString();
                txtStreakOld.rectTransform.DOAnchorPosY(yBot, 0.3f).SetUpdate(true);
                txtStreakNew.rectTransform.DOAnchorPosY(yMid, 0.5f).SetUpdate(true).SetEase(Ease.OutBounce);
                PlaySkeleton();
                IPlayerInfoController.Instance.ShowStreakAnimCompleted();
            }
            return;
        }

        for (int i = 0; i < listTick.Count; i++)
        {
            var item = listTick[i];
            if (item == null)
                continue;

            DayOfWeek day = (DayOfWeek)i;
            bool isDayInStreak = listDayOfWeek.Contains(day);
            bool isToday = day == DateTime.Now.Date.DayOfWeek;

            if (isDayInStreak)
            {
                if (isToday)
                {
                    item.fillAmount = hasPlayedToday ? 1f : (canPlayAnim ? 0f : 1f);
                }
                else
                {
                    item.fillAmount = 1f;
                }
            }
            else
            {
                item.fillAmount = 0f;
            }
        }

        if (animatorUI != null)
        {
            var clips = animatorUI.GetCurrentAnimatorClipInfo(0);
            if (clips != null && clips.Length > 0)
            {
                float delay = clips[0].clip.length;
                DOVirtual.DelayedCall(delay, () =>
                {
                    if (canPlayAnim && hasPlayedToday)
                    {
                        if (listTick.Count > (int)DateTime.Now.Date.DayOfWeek)
                        {
                            var currentDayTick = listTick[(int)DateTime.Now.Date.DayOfWeek];
                            if (currentDayTick != null)
                            {
                                currentDayTick.DOFillAmount(1f, 1f).SetUpdate(true).OnComplete(() =>
                                {
                                    txtStreakNew.text = streakCount.ToString();
                                    txtStreakOld.rectTransform.DOAnchorPosY(yBot, 0.3f).SetUpdate(true);
                                    txtStreakNew.rectTransform.DOAnchorPosY(yMid, 0.5f).SetUpdate(true).SetEase(Ease.OutBounce);
                                    PlaySkeleton();
                                    IPlayerInfoController.Instance.ShowStreakAnimCompleted();
                                });
                            }
                        }
                    }
                });
            }
        }
    }

    void PlaySkeleton()
    {
        // Skeleton animation is disabled for now.
    }

    void SkeletonCompleted(TrackEntry trackEntry)
    {
        // Skeleton animation is disabled for now.
    }

    void OnDestroy()
    {
        // Skeleton animation is disabled for now.
    }

    [Button]
    public void Cheat(int streak)
    {
        if (listDayOfWeek == null)
            listDayOfWeek = new List<DayOfWeek>();

        streakCount = streak;
        listDayOfWeek.Clear();
        for (int i = streakCount - 1; i >= 0; i--)
        {
            listDayOfWeek.Add(DateTime.Now.AddDays(-i).Date.DayOfWeek);
        }
        UpdateVisual();
    }
}